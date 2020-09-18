# Settings

To configure the operator, use the @"KubeOps.Operator.OperatorSettings"
that are configurable during the generic host extension method
@"KubeOps.Operator.ServiceCollectionExtensions.AddKubernetesOperator(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{KubeOps.Operator.OperatorSettings})"
or @"KubeOps.Operator.ServiceCollectionExtensions.AddKubernetesOperator(Microsoft.Extensions.DependencyInjection.IServiceCollection,KubeOps.Operator.OperatorSettings)".

You can configure things like the name of the operator,
if it should use namespacing, and other elements like the
urls of metrics and lease durations for the leader election.

Please look at the documention over at: @"KubeOps.Operator.OperatorSettings"
to know what the fields mean.
