using ExceptionInspector.Core.Analysis;

namespace ExceptionInspector.Core.Tests;

public sealed class ExceptionAnalyzerTests
{
  #region Redaction and location tests

  [Fact]
  public void AnalyzeRedactsSourcePathsAndPreservesFileName()
  {
    var input = File.ReadAllText(GetFixturePath("di-unresolved-service.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var userFrame = result.Frames.First(frame => frame.Kind == "user");

    Assert.Equal("<user-path>/Program.cs", userFrame.File);
    Assert.DoesNotContain(
      result.Frames,
      frame => frame.File?.Contains("/Users/", StringComparison.Ordinal) == true);
    Assert.Single(result.Redactions);
  }

  [Fact]
  public void AnalyzeSelectsFirstUserFrameAsBlameLocation()
  {
    var input = File.ReadAllText(GetFixturePath("di-unresolved-service.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);

    Assert.NotNull(result.Location);
    Assert.Equal(
      "Program.<Main>$ lambda b__0_4",
      result.Location.Method);
    Assert.Equal("<user-path>/Program.cs", result.Location.File);
    Assert.Equal(36, result.Location.Line);
  }

  [Theory]
  [InlineData(
    @"System.Exception: Failure
   at Orders.Load() in C:\Users\alice\src\Orders\Program.cs:line 42")]
  [InlineData(
    "System.Exception: Failure\n   at Orders.Load() in src/Orders/Program.cs:line 42")]
  public void AnalyzeRedactsWindowsAndRelativePaths(string input)
  {
    var result = new ExceptionAnalyzer().Analyze(input);

    var frame = Assert.Single(result.Frames);
    Assert.Equal("Orders.Load", frame.Method);
    Assert.Equal("<user-path>/Program.cs", frame.File);
    Assert.Equal(42, frame.Line);
    Assert.Equal("<user-path>/Program.cs", result.Location?.File);
  }

  [Theory]
  [InlineData("Authorization: Bearer abc.def-123")]
  [InlineData("access_token=abc.def-123")]
  public void AnalyzeRedactsLabeledAuthenticationTokens(string tokenText)
  {
    var result = new ExceptionAnalyzer().Analyze(
      $"System.Exception: {tokenText}\n   at Orders.Load()");

    Assert.Contains("<token>", result.ExceptionMessage);
    Assert.DoesNotContain("abc.def-123", result.ExceptionMessage);
    Assert.Contains(
      result.Redactions,
      redaction => redaction.Contains(
        "authentication token",
        StringComparison.Ordinal));
  }

  [Fact]
  public void AnalyzeRedactsEmailAddresses()
  {
    var result = new ExceptionAnalyzer().Analyze(
      "System.Exception: Contact alice@example.com\n   at Orders.Load()");

    Assert.Contains("<email>", result.ExceptionMessage);
    Assert.DoesNotContain("alice@example.com", result.ExceptionMessage);
    Assert.Contains(
      result.Redactions,
      redaction => redaction.Contains(
        "email address",
        StringComparison.Ordinal));
  }

  [Theory]
  [InlineData("System.Exception: Request from 192.168.1.25")]
  [InlineData("System.Exception: Request from 10.0.0.1:5015")]
  public void AnalyzeRedactsIpv4Addresses(string input)
  {
    var result = new ExceptionAnalyzer().Analyze(
      $"{input}\n   at Orders.Load()");

    Assert.Contains("<ip>", result.ExceptionMessage);
    Assert.Contains(
      result.Redactions,
      redaction => redaction.Contains(
        "IPv4 address",
        StringComparison.Ordinal));
  }

  [Fact]
  public void AnalyzeRedactsConnectionStringSecrets()
  {
    var result = new ExceptionAnalyzer().Analyze(
      "System.Exception: Server=db;Password=sup3r-secret;User Id=app\n   at Orders.Load()");

    Assert.Contains("Password=<secret>", result.ExceptionMessage);
    Assert.DoesNotContain("sup3r-secret", result.ExceptionMessage);
    Assert.Contains(
      result.Redactions,
      redaction => redaction.Contains(
        "secret value",
        StringComparison.Ordinal));
  }

  #endregion

  #region Rule tests

  [Theory]
  [InlineData(
    "di-unresolved-service.txt",
    "dependency-injection-unresolved-service")]
  [InlineData("nre-in-linq-lambda.txt", "null-reference")]
  [InlineData("invalid-cast.txt", "invalid-cast")]
  [InlineData("dictionary-key-missing.txt", "dictionary-key-not-found")]
  [InlineData("linq-first-empty.txt", "linq-empty-sequence")]
  [InlineData("json-type-mismatch.txt", "json-type-mismatch")]
  [InlineData("format-culture-decimal.txt", "format-parsing")]
  [InlineData("ef-disposed-context.txt", "ef-context-disposed")]
  [InlineData("async-wrapped-inner.txt", "wrapped-exception")]
  [InlineData("aggregate-two-tasks.txt", "aggregate-exception")]
  public void AnalyzeFixturesProduceExpectedRule(
    string fileName,
    string ruleId)
  {
    var input = File.ReadAllText(GetFixturePath(fileName));

    var result = new ExceptionAnalyzer().Analyze(input);

    Assert.Contains(result.Findings, finding => finding.RuleId == ruleId);
  }

  [Fact]
  public void AnalyzeUnmatchedFixtureProducesNoFindings()
  {
    var input = File.ReadAllText(GetFixturePath("argument-null.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);

    Assert.Empty(result.Findings);
    Assert.Null(result.Fix);
  }

  [Fact]
  public void AnalyzeDiFixtureProducesFindingAndFix()
  {
    var input = File.ReadAllText(GetFixturePath("di-unresolved-service.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("dependency-injection-unresolved-service", finding.RuleId);
    Assert.Contains("IOrderRepository", finding.Explanation);
    Assert.Contains("OrderService", finding.Explanation);
    Assert.NotNull(result.Fix);
    Assert.Contains("IOrderRepository", result.Fix.Explanation);
    Assert.NotNull(result.Fix.DocsUrl);
  }

  [Fact]
  public void AnalyzeNullReferenceFixtureProducesFindingAndFix()
  {
    var input = File.ReadAllText(GetFixturePath("nre-in-linq-lambda.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("null-reference", finding.RuleId);
    Assert.Equal("A null object was accessed", finding.Title);
    Assert.Contains(
      "Object reference not set to an instance of an object.",
      finding.Evidence);
    Assert.Contains("b__0_19", finding.Evidence[1]);
    Assert.NotNull(result.Fix);
    Assert.Contains("null case", result.Fix.Explanation);
    Assert.Equal(
      "https://learn.microsoft.com/dotnet/api/system.nullreferenceexception",
      result.Fix.DocsUrl);
  }

  [Fact]
  public void AnalyzeInvalidCastFixtureProducesFindingAndFix()
  {
    var input = File.ReadAllText(GetFixturePath("invalid-cast.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("invalid-cast", finding.RuleId);
    Assert.Equal(
      "An object was cast to an incompatible type",
      finding.Title);
    Assert.Contains("System.String", finding.Evidence[0]);
    Assert.Contains("System.Int32", finding.Evidence[0]);
    Assert.NotNull(result.Fix);
    Assert.Contains("runtime type", result.Fix.Explanation);
    Assert.Equal(
      "https://learn.microsoft.com/dotnet/api/system.invalidcastexception",
      result.Fix.DocsUrl);
  }

  [Fact]
  public void AnalyzeMissingDictionaryKeyFixtureProducesFindingAndFix()
  {
    var input = File.ReadAllText(
      GetFixturePath("dictionary-key-missing.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("dictionary-key-not-found", finding.RuleId);
    Assert.Equal("A dictionary key was missing", finding.Title);
    Assert.Contains("SKU-42", finding.Evidence[0]);
    Assert.NotNull(result.Fix);
    Assert.Contains("TryGetValue", result.Fix.Explanation);
    Assert.Equal(
      "https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2.trygetvalue",
      result.Fix.DocsUrl);
  }

  [Fact]
  public void AnalyzeLinqEmptySequenceFixtureProducesFindingAndFix()
  {
    var input = File.ReadAllText(GetFixturePath("linq-first-empty.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("linq-empty-sequence", finding.RuleId);
    Assert.Equal(
      "A LINQ operation received an empty sequence",
      finding.Title);
    Assert.Contains("Sequence contains no elements", finding.Evidence);
    Assert.NotNull(result.Fix);
    Assert.Contains("FirstOrDefault", result.Fix.Explanation);
    Assert.Equal(
      "https://learn.microsoft.com/dotnet/api/system.linq.enumerable.firstordefault",
      result.Fix.DocsUrl);
  }

  [Fact]
  public void AnalyzeJsonTypeMismatchFixtureProducesFindingAndFix()
  {
    var input = File.ReadAllText(
      GetFixturePath("json-type-mismatch.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(
      result.Findings,
      item => item.RuleId == "json-type-mismatch");

    Assert.Equal("json-type-mismatch", finding.RuleId);
    Assert.Equal("A JSON value has the wrong type", finding.Title);
    Assert.Contains("System.Int32", finding.Evidence[0]);
    Assert.Contains("$.Id", finding.Evidence[0]);
    Assert.NotNull(result.Fix);
    Assert.Contains("reported path", result.Fix.Explanation);
    Assert.Equal(
      "https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/converters-how-to",
      result.Fix.DocsUrl);
  }

  [Fact]
  public void AnalyzeFormatParsingFixtureProducesFindingAndFix()
  {
    var input = File.ReadAllText(
      GetFixturePath("format-culture-decimal.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("format-parsing", finding.RuleId);
    Assert.Equal(
      "A value could not be parsed in the expected format",
      finding.Title);
    Assert.Contains("12,5", finding.Evidence[0]);
    Assert.NotNull(result.Fix);
    Assert.Contains("explicit culture", result.Fix.Explanation);
    Assert.Equal(
      "https://learn.microsoft.com/dotnet/standard/base-types/parse-strings",
      result.Fix.DocsUrl);
  }

  [Fact]
  public void AnalyzeDisposedEntityFrameworkContextFixtureProducesFindingAndFix()
  {
    var input = File.ReadAllText(
      GetFixturePath("ef-disposed-context.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("ef-context-disposed", finding.RuleId);
    Assert.Equal(
      "An Entity Framework context was used after disposal",
      finding.Title);
    Assert.Contains("disposed context instance", finding.Evidence[0]);
    Assert.NotNull(result.Fix);
    Assert.Contains("dependency injection", result.Fix.Explanation);
    Assert.Equal(
      "https://learn.microsoft.com/ef/core/dbcontext-configuration/",
      result.Fix.DocsUrl);
  }

  [Fact]
  public void AnalyzeWrappedExceptionFixtureProducesWrapperFinding()
  {
    var input = File.ReadAllText(
      GetFixturePath("async-wrapped-inner.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("wrapped-exception", finding.RuleId);
    Assert.Equal(
      "An exception was wrapped by another exception",
      finding.Title);
    Assert.Contains(
      "System.ApplicationException",
      finding.Evidence[0]);
    Assert.Null(result.Fix);
  }

  [Fact]
  public void AnalyzeAggregateExceptionFixtureProducesAggregateFinding()
  {
    var input = File.ReadAllText(
      GetFixturePath("aggregate-two-tasks.txt"));

    var result = new ExceptionAnalyzer().Analyze(input);
    var finding = Assert.Single(result.Findings);

    Assert.Equal("aggregate-exception", finding.RuleId);
    Assert.Equal(
      "Multiple exceptions were reported together",
      finding.Title);
    Assert.Contains("2 nested exception node(s)", finding.Evidence[0]);
    Assert.Null(result.Fix);
  }

  #endregion

  private static string GetFixturePath(string fileName)
  {
    return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
  }
}
