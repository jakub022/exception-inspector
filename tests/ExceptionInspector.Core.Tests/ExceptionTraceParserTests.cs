using ExceptionInspector.Core.Analysis;

namespace ExceptionInspector.Core.Tests;

public sealed class ExceptionTraceParserTests
{
  #region Fixture and structure tests

  public static IEnumerable<object[]> FixtureNames =>
    Directory
      .GetFiles(Path.Combine(AppContext.BaseDirectory, "Fixtures"), "*.txt")
      .Select(path => new object[] { Path.GetFileName(path) });

  [Fact]
  public void ParseDiFixtureExtractsHeaderAndFrames()
  {
    var input = File.ReadAllText(GetFixturePath("di-unresolved-service.txt"));

    var result = ExceptionTraceParser.Parse(input);

    Assert.Equal("System.InvalidOperationException", result.ExceptionType);
    Assert.Contains("Unable to resolve service", result.ExceptionMessage);
    Assert.Equal(15, result.Frames.Count);
    Assert.Equal(2, result.Stats.UserFrames);
    Assert.Equal(13, result.Stats.FrameworkFrames);
    Assert.Equal(0, result.Stats.ThirdPartyFrames);
    Assert.Equal(0, result.Stats.UnparsedLines);
  }

  [Fact]
  public void ParseDiFixtureExtractsFirstUserLocation()
  {
    var input = File.ReadAllText(GetFixturePath("di-unresolved-service.txt"));

    var result = ExceptionTraceParser.Parse(input);
    var firstUserFrame = result.Frames.First(frame => frame.Kind == "user");

    Assert.Equal("Program.<Main>$ lambda b__0_4", firstUserFrame.Method);
    Assert.EndsWith("/Program.cs", firstUserFrame.File);
    Assert.Equal(36, firstUserFrame.Line);
    Assert.True(firstUserFrame.IsCompilerGenerated);
    Assert.Equal(result.Frames, result.ExceptionChain[0].Frames);
  }

  [Fact]
  public void ParseHeaderWithoutMessageAddsWarning()
  {
    var result = ExceptionTraceParser.Parse("System.Exception");

    Assert.Equal("System.Exception", result.ExceptionType);
    Assert.Empty(result.ExceptionMessage);
    Assert.Contains(
      result.Warnings,
      warning => warning.Contains("separate message", StringComparison.Ordinal));
  }

  [Fact]
  public void ParseAggregateFixtureAttachesAsyncBoundaryToFollowingFrame()
  {
    var input = File.ReadAllText(GetFixturePath("aggregate-two-tasks.txt"));

    var result = ExceptionTraceParser.Parse(input);
    var boundaryFrames = result.Frames
      .Where(frame => frame.AsyncBoundaryBefore)
      .ToArray();

    Assert.Equal(2, boundaryFrames.Length);
    Assert.All(
      boundaryFrames,
      frame => Assert.Equal(
        "System.Threading.ExecutionContext.RunFromThreadPoolDispatchLoop",
        frame.Method));
  }

  [Fact]
  public void ParseInnerExceptionFixtureBuildsExceptionChild()
  {
    var input = File.ReadAllText(GetFixturePath("async-wrapped-inner.txt"));

    var result = ExceptionTraceParser.Parse(input);
    var root = Assert.Single(result.ExceptionChain);
    var child = Assert.Single(root.Children);

    Assert.Equal("System.ApplicationException", root.Type);
    Assert.Equal("System.InvalidOperationException", child.Type);
    Assert.True(root.IsWrapper);
    Assert.False(child.IsWrapper);
    Assert.Collection(root.Frames, _ => { }, _ => { });
    Assert.Collection(child.Frames, _ => { }, _ => { }, _ => { });
    Assert.True(result.Frames.Count == 5);
    Assert.Equal(
      "CheckoutHandler.LoadCartAsync local function Throw",
      result.Frames[0].Method);
  }

  [Fact]
  public void ParseAggregateFixtureBuildsSiblingExceptionChildren()
  {
    var input = File.ReadAllText(GetFixturePath("aggregate-two-tasks.txt"));

    var result = ExceptionTraceParser.Parse(input);
    var root = Assert.Single(result.ExceptionChain);

    Assert.Equal("System.AggregateException", root.Type);
    Assert.True(root.IsWrapper);
    Assert.Equal(2, root.Children.Count);
    Assert.Equal(
      "System.InvalidOperationException",
      root.Children[0].Type);
    Assert.Equal(
      "System.Collections.Generic.KeyNotFoundException",
      root.Children[1].Type);
  }

  [Theory]
  [MemberData(nameof(FixtureNames))]
  public void ParseEveryFixtureProducesExceptionAndFrames(string fileName)
  {
    var input = File.ReadAllText(GetFixturePath(fileName));

    var result = ExceptionTraceParser.Parse(input);

    Assert.NotEmpty(result.ExceptionType);
    Assert.NotEmpty(result.Frames);
  }

  #endregion

  #region Robustness tests

  [Fact]
  public void ParseFrameWithoutLocationClassifiesItAsThirdParty()
  {
    var result = ExceptionTraceParser.Parse(
      "System.Exception: Failure\n   at Vendor.Library.Handler.Run()");

    var frame = Assert.Single(result.Frames);

    Assert.Equal("Vendor.Library.Handler.Run", frame.Method);
    Assert.Equal(string.Empty, frame.Parameters);
    Assert.Null(frame.File);
    Assert.Null(frame.Line);
    Assert.Equal("thirdParty", frame.Kind);
  }

  [Fact]
  public void ParseMalformedFrameAddsWarningAndUnparsedLine()
  {
    var result = ExceptionTraceParser.Parse(
      "System.Exception: Failure\n   at ");

    Assert.Empty(result.Frames);
    Assert.Equal(1, result.Stats.UnparsedLines);
    Assert.Contains(result.Warnings, warning => warning.Contains(
      "Could not parse stack frame",
      StringComparison.Ordinal));
  }

  [Fact]
  public void ParseMalformedFrameLineNumberPreservesFrameAndAddsWarning()
  {
    var result = ExceptionTraceParser.Parse(
      "System.Exception: Failure\n   at Orders.Load() in src/Orders.cs:line unknown");

    var frame = Assert.Single(result.Frames);
    Assert.Equal("src/Orders.cs:line unknown", frame.File);
    Assert.Null(frame.Line);
    Assert.Equal(1, result.Stats.UnparsedLines);
    Assert.Contains(result.Warnings, warning => warning.Contains(
      "Could not parse stack frame line number",
      StringComparison.Ordinal));
  }

  [Fact]
  public void ParseOversizedInputTruncatesAndAddsWarning()
  {
    var input = $"System.Exception: Failure\n{new string('x', 200_001)}";

    var result = ExceptionTraceParser.Parse(input);

    Assert.True(result.ExceptionChain[0].Truncated);
    Assert.Contains(result.Warnings, warning => warning.Contains(
      "exceeded 200000 characters",
      StringComparison.Ordinal));
  }

  [Fact]
  public void ParseTooManyFramesTruncatesAndAddsWarning()
  {
    var frames = string.Join(
      '\n',
      Enumerable.Range(0, 10_001)
        .Select(_ => "   at A()"));
    var result = ExceptionTraceParser.Parse(
      $"System.Exception: Failure\n{frames}");

    Assert.Equal(10_000, result.Frames.Count);
    Assert.True(result.ExceptionChain[0].Truncated);
    Assert.Contains(result.Warnings, warning => warning.Contains(
      "stopped after 10000 stack frames",
      StringComparison.Ordinal));
  }

  #endregion

  private static string GetFixturePath(string fileName)
  {
    return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
  }
}
