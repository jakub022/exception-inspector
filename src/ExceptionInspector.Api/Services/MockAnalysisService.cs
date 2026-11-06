using ExceptionInspector.Api.Models;

namespace ExceptionInspector.Api.Services;

public sealed class MockAnalysisService : IAnalysisService
{
    public Task<AnalysisReport> AnalyzeAsync(
        AnalyzeRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var frameworkFrames = new[]
        {
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.CreateArgumentCallSites",
                "ServiceIdentifier serviceIdentifier, Type implementationType, CallSiteChain callSiteChain, ParameterInfo[] parameters, Boolean throwIfCallSiteNotFound",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.CreateConstructorCallSite",
                "ResultCache lifetime, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.CreateExact",
                "ServiceDescriptor descriptor, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.TryCreateExact",
                "ServiceDescriptor descriptor, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.TryCreateExact",
                "ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.CreateCallSite",
                "ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.GetCallSite",
                "ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceProvider.CreateServiceAccessor",
                "ServiceIdentifier serviceIdentifier",
                null, null, "framework", false, false),
            new StackFrame(
                "System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>.GetOrAdd",
                "TKey key, Func<TKey, TValue> valueFactory",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService",
                "ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService",
                "Type serviceType",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService",
                "IServiceProvider provider",
                null, null, "framework", false, false),
            new StackFrame(
                "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<T>",
                "IServiceProvider provider",
                null, null, "framework", false, false)
        };

        var userFrames = new[]
        {
            new StackFrame(
                "Program.<>c.<<Main>$>b__0_4",
                string.Empty,
                "Program.cs",
                36,
                "user",
                true,
                false),
            new StackFrame(
                "Program.<>c__DisplayClass0_0.<<Main>$>g__Capture|1",
                "String name, Action action",
                "Program.cs",
                19,
                "user",
                true,
                false)
        };

        var frames = frameworkFrames.Concat(userFrames).ToArray();
        var exception = new ExceptionNode(
            "System.InvalidOperationException",
            "Unable to resolve service for type 'IOrderRepository' while attempting to activate 'OrderService'.",
            false,
            frames,
            Array.Empty<ExceptionNode>(),
            false);
        var evidence = new[]
        {
            "Unable to resolve service for type 'IOrderRepository'.",
            "The failure occurred while attempting to activate 'OrderService'."
        };
        var findingEvidence = new[]
        {
            "Unable to resolve service for type 'IOrderRepository'.",
            "Attempting to activate 'OrderService'."
        };
        var findings = new[]
        {
            new Finding(
                "dependency-injection-unresolved-service",
                "A required service was not registered",
                "The dependency-injection container was asked to create OrderService, but no registration was found for IOrderRepository.",
                findingEvidence)
        };
        var exceptionChain = new[] { exception };
        var redactions = new[] { "A user file path was redacted from the stack trace." };

        return Task.FromResult(new AnalysisReport(
            Guid.NewGuid().ToString(),
            "0.1.0",
            new Verdict(
                "Missing dependency registration",
                "IOrderRepository could not be resolved while creating OrderService.",
                "System.InvalidOperationException",
                "Unable to resolve service for type 'IOrderRepository' while attempting to activate 'OrderService'."),
            new Fix(
                "Register an implementation of IOrderRepository in the dependency-injection container before resolving OrderService.",
                "services.AddScoped<IOrderRepository, OrderRepository>();",
                "https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection"),
            new BlameLocation(
                "Program.<>c.<<Main>$>b__0_4",
                "Program.cs",
                36),
            evidence,
            new ExtendedInfo(
                exceptionChain,
                frames,
                findings,
                Array.Empty<string>(),
                new AnalysisStats(frames.Length, userFrames.Length, frameworkFrames.Length, 0, 0),
                redactions)));
    }
}
