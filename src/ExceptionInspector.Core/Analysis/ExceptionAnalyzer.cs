using ExceptionInspector.Core.Models;
using System.Text.RegularExpressions;

namespace ExceptionInspector.Core.Analysis;

public sealed partial class ExceptionAnalyzer : IExceptionAnalyzer
{
  public AnalysisResult Analyze(string input)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(input);

    var result = ExceptionTraceParser.Parse(input);
    RedactSourcePaths(result);
    RedactAuthTokens(result);
    RedactEmailAddresses(result);
    RedactIpAddresses(result);
    RedactSecrets(result);
    SelectBlameLocation(result);
    ApplyDependencyInjectionRule(result);
    ApplyNullReferenceRule(result);
    ApplyInvalidCastRule(result);
    ApplyMissingDictionaryKeyRule(result);
    ApplyLinqEmptySequenceRule(result);
    ApplyJsonTypeMismatchRule(result);
    ApplyFormatParsingRule(result);
    ApplyDisposedEntityFrameworkContextRule(result);
    ApplyWrapperAndAggregateRules(result);

    return result;
  }

  #region Private analysis methods

  private static void RedactSecrets(AnalysisResult result)
  {
    var redactionCount = 0;
    result.ExceptionMessage = RedactSecrets(
      result.ExceptionMessage,
      ref redactionCount);

    foreach (var node in EnumerateNodes(result.ExceptionChain))
    {
      node.Message = RedactSecrets(
        node.Message,
        ref redactionCount);
    }

    for (var index = 0; index < result.Warnings.Count; index++)
    {
      result.Warnings[index] = RedactSecrets(
        result.Warnings[index],
        ref redactionCount);
    }

    if (redactionCount > 0)
    {
      result.Redactions.Add(
        $"{redactionCount} secret value(s) were redacted.");
    }
  }

  private static string RedactSecrets(
    string value,
    ref int redactionCount)
  {
    var matches = SecretValueRegex().Matches(value);
    redactionCount += matches.Count;
    return SecretValueRegex().Replace(value, "$1<secret>");
  }

  [GeneratedRegex(
    @"(?i)(\b(?:password|pwd|accountkey|client[_-]?secret|secret|connectionstring)\s*[=:]\s*)[^;\s,]+",
    RegexOptions.CultureInvariant,
    matchTimeoutMilliseconds: 100)]
  private static partial Regex SecretValueRegex();

  private static void RedactIpAddresses(AnalysisResult result)
  {
    var redactionCount = 0;
    result.ExceptionMessage = RedactIpAddresses(
      result.ExceptionMessage,
      ref redactionCount);

    foreach (var node in EnumerateNodes(result.ExceptionChain))
    {
      node.Message = RedactIpAddresses(
        node.Message,
        ref redactionCount);
    }

    for (var index = 0; index < result.Warnings.Count; index++)
    {
      result.Warnings[index] = RedactIpAddresses(
        result.Warnings[index],
        ref redactionCount);
    }

    if (redactionCount > 0)
    {
      result.Redactions.Add(
        $"{redactionCount} IPv4 address(es) were redacted.");
    }
  }

  private static string RedactIpAddresses(
    string value,
    ref int redactionCount)
  {
    var matches = IPv4AddressRegex().Matches(value);
    redactionCount += matches.Count;
    return IPv4AddressRegex().Replace(value, "<ip>");
  }

  [GeneratedRegex(
    @"(?<![\d.])(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(?![\d.])",
    RegexOptions.CultureInvariant,
    matchTimeoutMilliseconds: 100)]
  private static partial Regex IPv4AddressRegex();

  private static void RedactEmailAddresses(AnalysisResult result)
  {
    var redactionCount = 0;
    result.ExceptionMessage = RedactEmailAddresses(
      result.ExceptionMessage,
      ref redactionCount);

    foreach (var node in EnumerateNodes(result.ExceptionChain))
    {
      node.Message = RedactEmailAddresses(
        node.Message,
        ref redactionCount);
    }

    for (var index = 0; index < result.Warnings.Count; index++)
    {
      result.Warnings[index] = RedactEmailAddresses(
        result.Warnings[index],
        ref redactionCount);
    }

    if (redactionCount > 0)
    {
      result.Redactions.Add(
        $"{redactionCount} email address(es) were redacted.");
    }
  }

  private static string RedactEmailAddresses(
    string value,
    ref int redactionCount)
  {
    var matches = EmailAddressRegex().Matches(value);
    redactionCount += matches.Count;
    return EmailAddressRegex().Replace(value, "<email>");
  }

  [GeneratedRegex(
    @"[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+",
    RegexOptions.CultureInvariant,
    matchTimeoutMilliseconds: 100)]
  private static partial Regex EmailAddressRegex();

  private static void RedactAuthTokens(AnalysisResult result)
  {
    var redactionCount = 0;
    result.ExceptionMessage = RedactAuthTokens(
      result.ExceptionMessage,
      ref redactionCount);

    foreach (var node in EnumerateNodes(result.ExceptionChain))
    {
      node.Message = RedactAuthTokens(node.Message, ref redactionCount);
    }

    for (var index = 0; index < result.Warnings.Count; index++)
    {
      result.Warnings[index] = RedactAuthTokens(
        result.Warnings[index],
        ref redactionCount);
    }

    if (redactionCount > 0)
    {
      result.Redactions.Add(
        $"{redactionCount} authentication token value(s) were redacted.");
    }
  }

  private static string RedactAuthTokens(
    string value,
    ref int redactionCount)
  {
    var matches = AuthTokenRegex().Matches(value);
    redactionCount += matches.Count;
    return AuthTokenRegex().Replace(value, "$1<token>");
  }

  [GeneratedRegex(
    @"(?i)(Authorization\s*:\s*Bearer\s+|Bearer\s+|(?:api[_-]?key|access[_-]?token)\s*[=:]\s*)[A-Za-z0-9._~+/=-]+",
    RegexOptions.CultureInvariant,
    matchTimeoutMilliseconds: 100)]
  private static partial Regex AuthTokenRegex();

  private static void SelectBlameLocation(AnalysisResult result)
  {
    var firstUserFrame = result.Frames.FirstOrDefault(
      frame => frame.Kind == "user");

    if (firstUserFrame is null)
    {
      return;
    }

    result.Location = new BlameLocation
    {
      Method = firstUserFrame.Method,
      File = firstUserFrame.File,
      Line = firstUserFrame.Line,
    };
  }

  private static void ApplyWrapperAndAggregateRules(AnalysisResult result)
  {
    var nodes = EnumerateNodes(result.ExceptionChain).ToArray();

    if (nodes.Any(node => string.Equals(
      node.Type,
      "System.AggregateException",
      StringComparison.Ordinal)))
    {
      var finding = new Finding
      {
        RuleId = "aggregate-exception",
        Title = "Multiple exceptions were reported together",
        Explanation =
          "An AggregateException contains multiple exception branches; inspect each branch because more than one operation may have failed.",
      };
      finding.Evidence.Add(
        $"AggregateException contains {nodes.Count(node => node.Type != "System.AggregateException")} nested exception node(s).");
      result.Findings.Add(finding);
    }

    var wrappedNodes = nodes
      .Where(node =>
        node.IsWrapper &&
        !string.Equals(
          node.Type,
          "System.AggregateException",
          StringComparison.Ordinal))
      .ToArray();

    if (wrappedNodes.Length == 0)
    {
      return;
    }

    var wrapperFinding = new Finding
    {
      RuleId = "wrapped-exception",
      Title = "An exception was wrapped by another exception",
      Explanation =
        "The outer exception adds context, while a nested exception usually contains the more specific failure.",
    };
    wrapperFinding.Evidence.Add(
      $"Wrapped exception type(s): {string.Join(", ", wrappedNodes.Select(node => node.Type))}.");
    result.Findings.Add(wrapperFinding);
  }

  private static IEnumerable<ExceptionNode> EnumerateNodes(
    IEnumerable<ExceptionNode> nodes)
  {
    foreach (var node in nodes)
    {
      yield return node;

      foreach (var child in EnumerateNodes(node.Children))
      {
        yield return child;
      }
    }
  }

  private static void ApplyDisposedEntityFrameworkContextRule(
    AnalysisResult result)
  {
    if (!string.Equals(
      result.ExceptionType,
      "System.ObjectDisposedException",
      StringComparison.Ordinal) ||
      !result.ExceptionMessage.Contains(
        "disposed context instance",
        StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    var finding = new Finding
    {
      RuleId = "ef-context-disposed",
      Title = "An Entity Framework context was used after disposal",
      Explanation =
        "An Entity Framework Core context was disposed before code finished using it.",
    };
    finding.Evidence.Add(result.ExceptionMessage);
    result.Findings.Add(finding);
    result.Fix = new SuggestedFix
    {
      Explanation =
        "Let dependency injection manage the context lifetime, and avoid returning or using the context after its scope or an enclosing using block has ended.",
      DocsUrl =
        "https://learn.microsoft.com/ef/core/dbcontext-configuration/",
    };
  }

  private static void ApplyFormatParsingRule(AnalysisResult result)
  {
    if (!string.Equals(
      result.ExceptionType,
      "System.FormatException",
      StringComparison.Ordinal))
    {
      return;
    }

    var finding = new Finding
    {
      RuleId = "format-parsing",
      Title = "A value could not be parsed in the expected format",
      Explanation =
        "The input text does not match the format or culture expected by the parsing operation.",
    };
    finding.Evidence.Add(result.ExceptionMessage);
    result.Findings.Add(finding);
    result.Fix = new SuggestedFix
    {
      Explanation =
        "Validate the input format and use an explicit culture when parsing culture-sensitive values; use a TryParse method when invalid input is expected.",
      DocsUrl =
        "https://learn.microsoft.com/dotnet/standard/base-types/parse-strings",
    };
  }

  private static void ApplyJsonTypeMismatchRule(AnalysisResult result)
  {
    const string messagePrefix = "The JSON value could not be converted to ";

    if (!string.Equals(
      result.ExceptionType,
      "System.Text.Json.JsonException",
      StringComparison.Ordinal) ||
      !result.ExceptionMessage.StartsWith(
        messagePrefix,
        StringComparison.Ordinal))
    {
      return;
    }

    var finding = new Finding
    {
      RuleId = "json-type-mismatch",
      Title = "A JSON value has the wrong type",
      Explanation =
        "The JSON token type does not match the .NET property type expected during deserialization.",
    };
    finding.Evidence.Add(result.ExceptionMessage);
    result.Findings.Add(finding);
    result.Fix = new SuggestedFix
    {
      Explanation =
        "Check the JSON value at the reported path and make its type match the target property, or update the model or converter if the JSON format is intentional.",
      DocsUrl =
        "https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/converters-how-to",
    };
  }

  private static void ApplyLinqEmptySequenceRule(AnalysisResult result)
  {
    if (!string.Equals(
      result.ExceptionType,
      "System.InvalidOperationException",
      StringComparison.Ordinal) ||
      !string.Equals(
        result.ExceptionMessage,
        "Sequence contains no elements",
        StringComparison.Ordinal))
    {
      return;
    }

    var finding = new Finding
    {
      RuleId = "linq-empty-sequence",
      Title = "A LINQ operation received an empty sequence",
      Explanation =
        "A LINQ operation such as First or Single expected at least one element, but the input sequence was empty.",
    };
    finding.Evidence.Add(result.ExceptionMessage);
    result.Findings.Add(finding);
    result.Fix = new SuggestedFix
    {
      Explanation =
        "If an empty sequence is valid, use FirstOrDefault or SingleOrDefault and handle the default value, or check the sequence before requiring an element.",
      DocsUrl =
        "https://learn.microsoft.com/dotnet/api/system.linq.enumerable.firstordefault",
    };
  }

  private static void ApplyMissingDictionaryKeyRule(AnalysisResult result)
  {
    if (!string.Equals(
      result.ExceptionType,
      "System.Collections.Generic.KeyNotFoundException",
      StringComparison.Ordinal))
    {
      return;
    }

    var finding = new Finding
    {
      RuleId = "dictionary-key-not-found",
      Title = "A dictionary key was missing",
      Explanation =
        "The code attempted to read a dictionary entry using a key that is not present.",
    };
    finding.Evidence.Add(result.ExceptionMessage);
    result.Findings.Add(finding);
    result.Fix = new SuggestedFix
    {
      Explanation =
        "Verify that the key exists before reading the entry, or use TryGetValue when a missing key is an expected possibility.",
      DocsUrl =
        "https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2.trygetvalue",
    };
  }

  private static void ApplyInvalidCastRule(AnalysisResult result)
  {
    if (!string.Equals(
      result.ExceptionType,
      "System.InvalidCastException",
      StringComparison.Ordinal))
    {
      return;
    }

    var finding = new Finding
    {
      RuleId = "invalid-cast",
      Title = "An object was cast to an incompatible type",
      Explanation =
        "The code attempted to treat an object as a type that it does not represent.",
    };
    finding.Evidence.Add(result.ExceptionMessage);
    result.Findings.Add(finding);
    result.Fix = new SuggestedFix
    {
      Explanation =
        "Check the runtime type before casting, use a compatible target type, or convert the value explicitly when a value conversion is intended.",
      DocsUrl =
        "https://learn.microsoft.com/dotnet/api/system.invalidcastexception",
    };
  }

  private static void ApplyNullReferenceRule(AnalysisResult result)
  {
    if (!string.Equals(
      result.ExceptionType,
      "System.NullReferenceException",
      StringComparison.Ordinal))
    {
      return;
    }

    var finding = new Finding
    {
      RuleId = "null-reference",
      Title = "A null object was accessed",
      Explanation =
        "Code attempted to access a member on an object reference whose value was null.",
    };
    finding.Evidence.Add(result.ExceptionMessage);

    var userFrame = result.Frames.FirstOrDefault(frame => frame.Kind == "user");
    if (userFrame is not null)
    {
      finding.Evidence.Add(
        $"Inspect '{userFrame.Method}' for the null reference.");
    }

    result.Findings.Add(finding);
    result.Fix = new SuggestedFix
    {
      Explanation =
        "Check the objects used at the reported frame before accessing their members, and initialize the missing value or handle the null case.",
      DocsUrl =
        "https://learn.microsoft.com/dotnet/api/system.nullreferenceexception",
    };
  }

  private static void ApplyDependencyInjectionRule(AnalysisResult result)
  {
    const string serviceMarker = "Unable to resolve service for type '";
    const string activationMarker = "' while attempting to activate '";

    if (!result.ExceptionMessage.StartsWith(
      serviceMarker,
      StringComparison.Ordinal) ||
      !result.ExceptionMessage.Contains(
        activationMarker,
        StringComparison.Ordinal))
    {
      return;
    }

    var serviceStart = serviceMarker.Length;
    var serviceEnd = result.ExceptionMessage.IndexOf(
      '\'',
      serviceStart);
    var activationMarkerIndex = result.ExceptionMessage.IndexOf(
      activationMarker,
      serviceEnd < 0 ? 0 : serviceEnd,
      StringComparison.Ordinal) + activationMarker.Length;

    if (serviceEnd < 0 || activationMarkerIndex < activationMarker.Length)
    {
      return;
    }

    var activationEnd = result.ExceptionMessage.IndexOf(
      '\'',
      activationMarkerIndex);

    if (activationEnd < 0)
    {
      return;
    }

    var serviceType = result.ExceptionMessage[serviceStart..serviceEnd];
    var activationType = result.ExceptionMessage[activationMarkerIndex..activationEnd];

    var finding = new Finding
    {
      RuleId = "dependency-injection-unresolved-service",
      Title = "Missing dependency registration",
      Explanation =
        $"The dependency-injection container could not create {activationType} because {serviceType} was not registered.",
    };
    finding.Evidence.Add(
      $"Unable to resolve service for type '{serviceType}'.");
    finding.Evidence.Add(
      $"Attempting to activate '{activationType}'.");
    result.Findings.Add(finding);

    result.Fix = new SuggestedFix
    {
      Explanation =
        $"Register an implementation of {serviceType} in the dependency-injection container before resolving {activationType}.",
      DocsUrl =
        "https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection",
    };
  }

  private static void RedactSourcePaths(AnalysisResult result)
  {
    foreach (var frame in result.Frames)
    {
      if (frame.File is null)
      {
        continue;
      }

      var separatorIndex = frame.File.LastIndexOfAny(['/', '\\']);

      if (separatorIndex < 0)
      {
        continue;
      }

      frame.File = $"<user-path>/{frame.File[(separatorIndex + 1)..]}";
    }

    if (result.Frames.Any(frame => frame.File?.StartsWith(
      "<user-path>/",
      StringComparison.Ordinal) == true))
    {
      result.Redactions.Add(
        "A source file path was redacted from the stack trace.");
    }
  }

  #endregion
}