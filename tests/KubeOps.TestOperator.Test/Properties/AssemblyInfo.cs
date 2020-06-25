using Xunit;

[assembly: CollectionBehavior(
    CollectionBehavior.CollectionPerAssembly,
    MaxParallelThreads = 1,
    DisableTestParallelization = true)]
