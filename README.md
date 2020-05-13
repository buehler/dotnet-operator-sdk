# dotnet operator sdk

This package (sadly DotnetOperatorSdk is already taken on nuget, so its "KubeOps")
is a kubernetes operator sdk written in dotnet. It is heavily inspired by "kubebuilder"
that provides the same and more functions for kubernetes operators in go.

The goal was to learn about resource watching in .net and provide a neat way of writing a
custom operator yourself.

## Terminology

- `Entity`: A model - an entity - that is used in kubernetes. An entity defines the CRD.
- `Resource`: An instance of an entity.

## How To Use

Using this sdk is pretty simple:

- Install the package
- Map the main function
- Write entities / controllers / finalizers
- Go.

### Install the package

```bash
dotnet add package KubeOps
```

That's it.

### Map the main function

In your `Program.cs` file, map the main function to a kubernetes operator:

```csharp
public static class Program
{
    public static Task<int> Main(string[] args) =>
        new KubernetesOperator()
            .ConfigureServices(
                services =>
                {
                    // add resource controllers here
                    // add finalizers here
                })
            .Run(args);
}
```

This adds the default commands (like run and the code generators) to your app.

### Write Entities

A custom entity could be:

```csharp
class FooSpec
{
    public string? Test { get; set; }
}

[KubernetesEntity(Group = "test", ApiVersion = "v1")]
public class Foo : CustomKubernetesEntity<FooSpec>
{
}
```

Now a CRD for your "Foo" class is generated on build.

#### Validation

You can use the various validator attributes to customize your crd:

(all attributes are on properties with the exception of the Description)

- `Description`: Describe the property or class
- `ExternalDocs`: Add a link to an external documentation
- `Items`: Customize MinItems / MaxItems and if the items should be unique
- `Lenght`: Customize the length of something
- `MultipleOf`: A number should be a multiple of
- `Pattern`: A valid ECMA script regex (e.g. `/\d*/`)
- `RangeMaximum`: The maximum of a value (with option to exclude the max itself)
- `RangeMinimum`: The minimum of a value (with option to exclude the min itself)
- `Required`: The field is listed in the required fields

### Write Controllers

```csharp
[EntityRbac(typeof(ClusterDatabaseHost), Verbs = RbacVerb.All)]
public class FooCtrl: ResourceControllerBase<Foo>
{
    protected override async Task<TimeSpan?> Created(ClusterDatabaseHost resource){}
    // overwrite other methods here.
}
```

The entity rbac attribute does provide the information needed about
your needed roles / rules.

If you return `null` in an event function, the event is not requeued.
If you return a timespan, then the event is requeued after this delay.

If the function throws an error, the event is requeued with an exponential backoff.

The backoff function is defined as follows:

```csharp
private const double MaxRetrySeconds = 64;
private TimeSpan ExponentialBackoff(int retryCount) => TimeSpan
            .FromSeconds(Math.Min(Math.Pow(2, retryCount), MaxRetrySeconds))
            .Add(TimeSpan.FromMilliseconds(_rnd.Next(0, 1000)));
```

### Write Finalizers

A finalizer can be as simple as:

```csharp
public class FooFinalizer : ResourceFinalizerBase<Foo>
{
    public override async Task Finalize(Foo resource)
    {
        // do something with the resource.
    }
}
```

And can be added to a resource with:

```csharp
await resource.RegisterFinalizer<FooFinalizer, Foo>();
```

After the finalizer ran successfully on a resource, it is unregistered
on the resource.

## Commands

There are default command line commands which you can see when using
`dotnet run -- --help` in your project. As you can see, you can run
multiple commands. Some of them do install / uninstall your crds in
your currently selected kubernetes cluster or can generate code.

## Code Generation

When installing this package, you also reference the default Targets and Props
that come with the build engine. While building the following elements are generated:

- Dockerfile (if not already present)
- CRDs for your custom entities
- RBAC roles and role bindings for your requested resources
- Deployment files for your operator
- Installation file for your operator (kustomize)

The dockerfile will not be overwritten in case you have custom elements in there.
The installation files won't be overwritten as well if you have custom elements in there.

To regenerate those two elements, just delete them and rebuild your code.

## Prop Settings

You can overwrite the default behaviour of the building parts with the following
variables that you can add in a `<PropertyGroup>` in your `csproj` file:

| Property               | Description                                                                | Default Value                                                           |
| ---------------------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| KubeOpsDockerfilePath  | The path of the dockerfile                                                 | $(SolutionDir)Dockerfile<br>or<br>$(MSBuildProjectDirectory)\Dockerfile |
| KubeOpsDockerTag       | Which dotnet sdk / run tag should be used                                  | latest                                                                  |
| KubeOpsConfigRoot      | The base directory for generated elements                                  | $(SolutionDir)config<br>or<br>$(MSBuildProjectDirectory)\config         |
| KubeOpsCrdDir          | The directory for the generated crds                                       | \$(KubeOpsConfigRoot)\crds                                              |
| KubeOpsCrdFormat       | Output format for crds                                                     | Yaml                                                                    |
| KubeOpsCrdUseOldCrds   | Use V1Beta version of crd instead of V1<br>(for kubernetes version < 1.16) | false                                                                   |
| KubeOpsRbacDir         | Where to put the roles                                                     | \$(KubeOpsConfigRoot)\rbac                                              |
| KubeOpsRbacFormat      | Output format for rbac                                                     | Yaml                                                                    |
| KubeOpsOperatorDir     | Where to put operator related elements<br>(e.g. Deployment)                | \$(KubeOpsConfigRoot)\operator                                          |
| KubeOpsOperatorFormat  | Output format for the operator                                             | Yaml                                                                    |
| KubeOpsInstallerDir    | Where to put the installation files<br>(e.g. Namespace / Kustomization)    | \$(KubeOpsConfigRoot)\install                                           |
| KubeOpsInstallerFormat | Output format for the installation files                                   | Yaml                                                                    |
| KubeOpsSkipDockerfile  | Skip dockerfile during build                                               | ""                                                                      |
| KubeOpsSkipCrds        | Skip crd generation during build                                           | ""                                                                      |
| KubeOpsSkipRbac        | Skip rbac generation during build                                          | ""                                                                      |
| KubeOpsSkipOperator    | Skip operator generation during build                                      | ""                                                                      |
| KubeOpsSkipInstaller   | Skip installer generation during build                                     | ""                                                                      |
