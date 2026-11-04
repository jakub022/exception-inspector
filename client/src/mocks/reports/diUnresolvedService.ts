import type { AnalysisReport, StackFrame } from "../../api/types";

const frameworkFrames: StackFrame[] = [
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.CreateArgumentCallSites",
    parameters:
      "ServiceIdentifier serviceIdentifier, Type implementationType, CallSiteChain callSiteChain, ParameterInfo[] parameters, Boolean throwIfCallSiteNotFound",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.CreateConstructorCallSite",
    parameters:
      "ResultCache lifetime, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.CreateExact",
    parameters:
      "ServiceDescriptor descriptor, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.TryCreateExact",
    parameters:
      "ServiceDescriptor descriptor, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.TryCreateExact",
    parameters:
      "ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.CreateCallSite",
    parameters:
      "ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteFactory.GetCallSite",
    parameters:
      "ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceProvider.CreateServiceAccessor",
    parameters: "ServiceIdentifier serviceIdentifier",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>.GetOrAdd",
    parameters: "TKey key, Func<TKey, TValue> valueFactory",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService",
    parameters:
      "ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService",
    parameters: "Type serviceType",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService",
    parameters: "IServiceProvider provider",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
  {
    method:
      "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<T>",
    parameters: "IServiceProvider provider",
    file: null,
    line: null,
    kind: "framework",
    isCompilerGenerated: false,
    asyncBoundaryBefore: false,
  },
];

const userFrames: StackFrame[] = [
  {
    method: "Program.<>c.<<Main>$>b__0_4",
    parameters: "",
    file: "Program.cs",
    line: 36,
    kind: "user",
    isCompilerGenerated: true,
    asyncBoundaryBefore: false,
  },
  {
    method: "Program.<>c__DisplayClass0_0.<<Main>$>g__Capture|1",
    parameters: "String name, Action action",
    file: "Program.cs",
    line: 19,
    kind: "user",
    isCompilerGenerated: true,
    asyncBoundaryBefore: false,
  },
];

const frames = [...frameworkFrames, ...userFrames];

export const diUnresolvedServiceReport = {
  analysisId: "0192d7f0-9c4d-7b6f-9f2d-2f8f2f8a6e31",
  engineVersion: "0.1.0",
  verdict: {
    title: "Missing dependency registration",
    summary:
      "IOrderRepository could not be resolved while creating OrderService.",
    exceptionType: "System.InvalidOperationException",
    exceptionMessage:
      "Unable to resolve service for type 'IOrderRepository' while attempting to activate 'OrderService'.",
  },
  fix: {
    explanation:
      "Register an implementation of IOrderRepository in the dependency-injection container before resolving OrderService.",
    code: "services.AddScoped<IOrderRepository, OrderRepository>();",
    docsUrl:
      "https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection",
  },
  location: {
    method: "Program.<>c.<<Main>$>b__0_4",
    file: "Program.cs",
    line: 36,
  },
  evidence: [
    "Unable to resolve service for type 'IOrderRepository'.",
    "The failure occurred while attempting to activate 'OrderService'.",
  ],
  extended: {
    exceptionChain: [
      {
        type: "System.InvalidOperationException",
        message:
          "Unable to resolve service for type 'IOrderRepository' while attempting to activate 'OrderService'.",
        isWrapper: false,
        frames,
        children: [],
        truncated: false,
      },
    ],
    frames,
    findings: [
      {
        ruleId: "dependency-injection-unresolved-service",
        title: "A required service was not registered",
        explanation:
          "The dependency-injection container was asked to create OrderService, but no registration was found for IOrderRepository.",
        evidence: [
          "Unable to resolve service for type 'IOrderRepository'.",
          "Attempting to activate 'OrderService'.",
        ],
      },
    ],
    warnings: [],
    stats: {
      totalFrames: frames.length,
      userFrames: userFrames.length,
      frameworkFrames: frameworkFrames.length,
      thirdPartyFrames: 0,
      unparsedLines: 0,
    },
    redactions: ["A user file path was redacted from the stack trace."],
  },
} satisfies AnalysisReport;
