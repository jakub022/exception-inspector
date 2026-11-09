using ExceptionInspector.Core.Models;

namespace ExceptionInspector.Core.Analysis;

public sealed class ExceptionTraceParser
{
  private const int MaxInputLength = 200_000;
  private const int MaxFrameCount = 10_000;
  private const int MaxNestingDepth = 100;

  public static AnalysisResult Parse(string input)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(input);

    var result = new AnalysisResult();
    var inputWasTruncated = input.Length > MaxInputLength;
    if (inputWasTruncated)
    {
      input = input[..MaxInputLength];
      result.Warnings.Add(
        $"Input exceeded {MaxInputLength} characters and was truncated.");
    }

    var lines = input.Split(['\r', '\n']);
    var firstLine = lines
        .Select(line => line.Trim())
        .FirstOrDefault(line => line.Length > 0);

    if (firstLine is null)
    {
      result.Warnings.Add("The input did not contain any readable lines.");
      return result;
    }
    ParseExceptionHeader(firstLine, result);

    result.ExceptionChain.Add(new ExceptionNode
    {
      Type = result.ExceptionType,
      Message = result.ExceptionMessage,
      IsWrapper = false,
    });

    var rootNode = result.ExceptionChain[0];
    var nodeStack = new Stack<ExceptionNode>();
    var currentNode = rootNode;
    var headerLineFound = false;
    var asyncBoundaryBeforeNextFrame = false;

    foreach (var rawLine in lines)
    {
      var line = rawLine.Trim();

      if (line.Length == 0)
      {
        continue;
      }

      if (!headerLineFound)
      {
        headerLineFound = line == firstLine;
        continue;
      }

      if (line == "at" ||
          line.StartsWith("at ", StringComparison.Ordinal))
      {
        if (TryParseStackFrame(
          line,
          asyncBoundaryBeforeNextFrame,
          out var frame,
          out var malformedLocation))
        {
          if (result.Frames.Count >= MaxFrameCount)
          {
            rootNode.Truncated = true;
            result.Warnings.Add(
              $"Parsing stopped after {MaxFrameCount} stack frames.");
            break;
          }

          result.Frames.Add(frame);
          currentNode.Frames.Add(frame);
          asyncBoundaryBeforeNextFrame = false;

          if (malformedLocation)
          {
            result.Warnings.Add(
              $"Could not parse stack frame line number: {line}");
            result.Stats.UnparsedLines++;
          }
        }
        else
        {
          result.Warnings.Add($"Could not parse stack frame: {line}");
          result.Stats.UnparsedLines++;
        }

        continue;
      }

      if (line.StartsWith("---> ", StringComparison.Ordinal) &&
          TryParseAggregateExceptionHeader(line, out var aggregateChild))
      {
        var aggregateParent = nodeStack.TryPeek(out var parent)
          ? parent
          : rootNode;
        aggregateParent.Children.Add(aggregateChild);
        aggregateParent.IsWrapper = true;
        currentNode = aggregateChild;
        continue;
      }

      if (line.StartsWith("---> ", StringComparison.Ordinal) &&
          TryParseExceptionHeader(line[5..], out var childNode))
      {
        if (nodeStack.Count >= MaxNestingDepth)
        {
          rootNode.Truncated = true;
          result.Warnings.Add(
            $"Parsing stopped after {MaxNestingDepth} nested exceptions.");
          break;
        }

        childNode.IsWrapper = false;
        currentNode.Children.Add(childNode);
        nodeStack.Push(currentNode);
        currentNode.IsWrapper = true;
        currentNode = childNode;
        continue;
      }

      if (line == "--- End of inner exception stack trace ---" &&
          nodeStack.TryPop(out var parentNode))
      {
        currentNode = parentNode;
        continue;
      }

      if (line == "--- End of stack trace from previous location ---")
      {
        asyncBoundaryBeforeNextFrame = true;
        continue;
      }

      result.Stats.UnparsedLines++;
    }

    result.Stats.TotalFrames = result.Frames.Count;
    result.Stats.UserFrames = result.Frames.Count(frame => frame.Kind == "user");
    result.Stats.FrameworkFrames = result.Frames.Count(frame => frame.Kind == "framework");
    result.Stats.ThirdPartyFrames = result.Frames.Count(frame => frame.Kind == "thirdParty");

    if (inputWasTruncated)
    {
      rootNode.Truncated = true;
    }

    return result;
  }

  #region Frame parsing

  private static bool TryParseStackFrame(
    string line,
    bool asyncBoundaryBefore,
    out StackFrame frame,
    out bool malformedLocation)
  {

    frame = new StackFrame();
    malformedLocation = false;

    if (line.Length <= 3)
    {
      return false;
    }

    var content = line[3..].Trim();
    var locationSeparator = content.LastIndexOf(" in ", StringComparison.Ordinal);
    var methodText = locationSeparator >= 0
      ? content[..locationSeparator].Trim()
      : content;
    var locationText = locationSeparator >= 0
      ? content[(locationSeparator + 4)..].Trim()
      : null;

    if (methodText.Length == 0)
    {
      return false;
    }

    var openParenthesis = methodText.IndexOf('(');
    var closeParenthesis = methodText.LastIndexOf(')');
    var method = methodText;
    string? parameters = null;

    if (openParenthesis >= 0 &&
        closeParenthesis > openParenthesis &&
        closeParenthesis == methodText.Length - 1)
    {
      method = methodText[..openParenthesis];
      parameters = methodText[(openParenthesis + 1)..closeParenthesis];
    }

    var (file, lineNumber, hasMalformedLineNumber) =
      ParseLocation(locationText);
    malformedLocation = hasMalformedLineNumber;
    var isCompilerGenerated = method.Contains('<');
    frame = new StackFrame
    {
      Method = CreateDisplayMethod(method),
      Parameters = parameters,
      File = file,
      Line = lineNumber,
      Kind = ClassifyFrame(method, file),
      IsCompilerGenerated = isCompilerGenerated,
      AsyncBoundaryBefore = asyncBoundaryBefore,
    };

    return true;
  }

  private static string CreateDisplayMethod(string method)
  {
    const string lambdaMarker = ".<>c.";
    var lambdaMarkerIndex = method.IndexOf(
      lambdaMarker,
      StringComparison.Ordinal);
    var lambdaNameStart = lambdaMarkerIndex < 0
      ? -1
      : method.IndexOf(
        "<<",
        lambdaMarkerIndex + lambdaMarker.Length,
        StringComparison.Ordinal);
    var lambdaNameEnd = lambdaNameStart < 0
      ? -1
      : method.IndexOf(
        ">b__",
        lambdaNameStart,
        StringComparison.Ordinal);

    if (lambdaMarkerIndex >= 0 &&
        lambdaNameStart >= 0 &&
        lambdaNameEnd > lambdaNameStart)
    {
      return string.Concat(
        method[..lambdaMarkerIndex],
        ".",
        method[(lambdaNameStart + 1)..lambdaNameEnd],
        " lambda ",
        method[(lambdaNameEnd + 1)..]);
    }

    const string localFunctionMarker = ">g__";
    var localFunctionMarkerIndex = method.IndexOf(
      localFunctionMarker,
      StringComparison.Ordinal);
    var localFunctionOwnerStart = localFunctionMarkerIndex < 0
      ? -1
      : method.LastIndexOf(
        ".<",
        localFunctionMarkerIndex,
        StringComparison.Ordinal);
    var localFunctionOwnerEnd = localFunctionOwnerStart < 0
      ? -1
      : method.IndexOf(
        '>',
        localFunctionOwnerStart + 2);
    var localFunctionNameEnd = localFunctionMarkerIndex < 0
      ? -1
      : method.IndexOf(
        '|',
        localFunctionMarkerIndex);

    if (localFunctionOwnerStart >= 0 &&
        localFunctionOwnerEnd > localFunctionOwnerStart &&
        localFunctionNameEnd > localFunctionMarkerIndex)
    {
      return string.Concat(
        method[..localFunctionOwnerStart],
        ".",
        method[(localFunctionOwnerStart + 2)..localFunctionOwnerEnd],
        " local function ",
        method[(localFunctionMarkerIndex + localFunctionMarker.Length)..localFunctionNameEnd]);
    }

    return method;
  }

  private static (string? File, int? Line, bool MalformedLineNumber) ParseLocation(
    string? location)
  {
    if (string.IsNullOrWhiteSpace(location))
    {
      return (null, null, false);
    }

    const string lineMarker = ":line ";
    var lineMarkerIndex = location.LastIndexOf(lineMarker, StringComparison.Ordinal);

    if (lineMarkerIndex < 0 ||
        !int.TryParse(location[(lineMarkerIndex + lineMarker.Length)..], out var line))
    {
      return (location, null, lineMarkerIndex >= 0);
    }

    return (location[..lineMarkerIndex], line, false);
  }

  private static string ClassifyFrame(string method, string? file)
  {
    if (method.StartsWith("System.", StringComparison.Ordinal) ||
        method.StartsWith("Microsoft.", StringComparison.Ordinal))
    {
      return "framework";
    }

    return file is null ? "thirdParty" : "user";
  }

  #endregion

  #region Exception parsing

  private static void ParseExceptionHeader(
      string line,
      AnalysisResult result)
  {
    var separatorIndex = line.IndexOf(':');

    if (separatorIndex < 0)
    {
      result.ExceptionType = line.Trim();
      result.Warnings.Add(
        "The exception line did not contain a separate message.");
      return;
    }

    result.ExceptionType = line[..separatorIndex].Trim();
    result.ExceptionMessage = line[(separatorIndex + 1)..].Trim();
  }

  private static bool TryParseExceptionHeader(
      string line,
      out ExceptionNode exceptionNode)
  {
    var separatorIndex = line.IndexOf(':');

    if (separatorIndex < 0)
    {
      exceptionNode = new ExceptionNode
      {
        Type = line.Trim(),
      };
      return exceptionNode.Type.Length > 0;
    }

    exceptionNode = new ExceptionNode
    {
      Type = line[..separatorIndex].Trim(),
      Message = line[(separatorIndex + 1)..].Trim(),
    };

    return exceptionNode.Type.Length > 0;
  }

  private static bool TryParseAggregateExceptionHeader(
      string line,
      out ExceptionNode exceptionNode)
  {
    const string marker = "---> (Inner Exception #";
    exceptionNode = new ExceptionNode();

    if (!line.StartsWith(marker, StringComparison.Ordinal))
    {
      return false;
    }

    var headerStart = line.IndexOf(") ", marker.Length, StringComparison.Ordinal);

    return headerStart >= 0 &&
      TryParseExceptionHeader(line[(headerStart + 2)..], out exceptionNode);
  }

  #endregion
}