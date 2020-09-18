# Operator utils

There are two basic utilities that should be mentioned:

- Healthchecks
- Metrics

## Healthchecks

This is a basic feature of asp.net. The operator sdk makes use of
it and splits them up into `Liveness` and `Readiness` checks.

With the appropriate methods, you can add an `IHealthCheck` interface
to either `/ready`, `/health` or both.

The urls can be configured via @"KubeOps.Operator.OperatorSettings".

- @"KubeOps.Operator.Builder.IOperatorBuilder.AddHealthCheck``1(System.String)":
  adds a healthcheck to ready and liveness
- @"KubeOps.Operator.Builder.IOperatorBuilder.AddLivenessCheck``1(System.String)":
  adds a healthcheck to the liveness route only
- @"KubeOps.Operator.Builder.IOperatorBuilder.AddReadinessCheck``1(System.String)":
  adds a healthcheck to the readiness route only

## Metrics

By default, the operator lists some interessting metrics on the
`/metrics` route. The url can be configured via @"KubeOps.Operator.OperatorSettings".

There are many counters on how many elements have been reconciled, if the
controllers and queues are up and how many elements are in timed requeue state.

Please have a look at the metrics if you run your operator locally or online
to see which metrics are available.

Of course you can also have a look at the used metrics classes to see the
implementation: [Metrics Implementations](https://github.com/buehler/dotnet-operator-sdk/tree/master/src/KubeOps/Operator/DevOps).
