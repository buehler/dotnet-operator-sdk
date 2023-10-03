namespace KubeOps.Operator.Finalizer;

internal record FinalizerRegistration(string Identifier, Type FinalizerType, Type EntityType);
