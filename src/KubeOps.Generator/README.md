# KubeOps Generator

This is a C# source generator for KubeOps and operators.
It is used to generate convenience functions to help registering
resources within an operator.

## Usage

The generator is automatically used when the `KubeOps.Generator` package is referenced.

```bash
dotnet add package KubeOps.Generator
```

which results in the following `csproj` reference:

```xml
<ItemGroup>
    <PackageReference Include="KubeOps.Generator" Version="..." />
</ItemGroup>
```

## Generated Sources

The generator will automatically generate functions for the `IOperatorBuilder`.

### Entity Metadata / Entity Definitions

The generator creates a file in the root namespace called `EntityDefinitions.g.cs`.
This file contains all entities that are annotated with the `KubernetesEntityAttribute`.
The static class contains the `EntityMetadata` for the entities as well
as a function to register all entities within the `IOperatorBuilder`.

#### Example

```csharp
using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Entities;

public static class EntityDefinitions
{
    public static readonly EntityMetadata V1TestEntity = new("TestEntity", "v1", "testing.dev", null);
    public static IOperatorBuilder RegisterEntities(this IOperatorBuilder builder)
    {
        builder.AddEntity<global::Operator.Entities.V1TestEntity>(V1TestEntity);
        return builder;
    }
}
```

### Controller Registrations

The generator creates a file in the root namespace called `ControllerRegistrations.g.cs`.
This file contains a function to register all found controllers
(i.e. classes that implement the `IEntityController<T>` interface).

#### Example

```csharp
using KubeOps.Abstractions.Builder;

public static class ControllerRegistrations
{
    public static IOperatorBuilder RegisterControllers(this IOperatorBuilder builder)
    {
        builder.AddController<global::Operator.Controller.V1TestEntityController, global::Operator.Entities.V1TestEntity>();
        return builder;
    }
}
```

### Finalizer Registrations

The generator creates a file in the root namespace called `FinalizerRegistrations.g.cs`.
This file contains all finalizers with generated finalizer-identifiers.
Further, a function to register all finalizers is generated.

#### Example

```csharp
using KubeOps.Abstractions.Builder;

public static class FinalizerRegistrations
{
    public const string FinalizerOneIdentifier = "testing.dev/finalizeronefinalizer";
    public const string FinalizerTwoIdentifier = "testing.dev/finalizertwofinalizer";
    public static IOperatorBuilder RegisterFinalizers(this IOperatorBuilder builder)
    {
        builder.AddFinalizer<global::Operator.Finalizer.FinalizerOne, global::Operator.Entities.V1TestEntity>(FinalizerOneIdentifier);
        builder.AddFinalizer<global::Operator.Finalizer.FinalizerTwo, global::Operator.Entities.V1TestEntity>(FinalizerTwoIdentifier);
        return builder;
    }
}
```

### General Operator Extensions

The generator creates a file in the root namespace called `OperatorExtensions.g.cs`.
It contains convenience functions to register all generated sources.

#### Example

```csharp
using KubeOps.Abstractions.Builder;

public static class OperatorBuilderExtensions
{
    public static IOperatorBuilder RegisterComponents(this IOperatorBuilder builder)
    {
        builder.RegisterEntities();
        builder.RegisterControllers();
        builder.RegisterFinalizers();
        return builder;
    }
}
```
