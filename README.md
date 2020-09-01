# dotnet operator sdk

This package (sadly DotnetOperatorSdk is already taken on nuget, so its "KubeOps")
is a kubernetes operator sdk written in dotnet. It is heavily inspired by "kubebuilder"
that provides the same and more functions for kubernetes operators in go.

The goal was to learn about resource watching in .net and provide a neat way of writing a
custom operator yourself.

## Terminology

- `Entity`: A model - an entity - that is used in kubernetes. An entity defines the CRD.
- `Resource`: An instance of an entity.
- `Controller` or `ResourceController`: An instance of a resource manager
  that is responsible for the reconciliation of an entity.
- `Finalizer`: A special resource manager that is attached to the entity
  via identifier. The finalizers are called when an entity is deleted
  on kubernetes.
- `CRD`: CustomResourceDefinition of kubernetes.

## Features

As of now, the operator sdk supports - roughly - the following features:

- Controller with all operations of an entity
  - Created
  - Updated
  - NotModified
  - StatusModified
  - Deleted
- Finalizers for entities
- Prometheus metrics for queues / caches / watchers
- Healthchecks, split up to "readiness" and "liveness" (or both)
- Commands for the operator (for exact documentation run: `dotnet run -- --help`)
  - `Run`: Start the operator and run the asp.net application
  - `Install`: Install the found CRD's into the actual configured
    cluster in your kubeconfig
  - `Uninstall`: Remove the CRDs from your cluster
  - `Generate CRD`: Generate the yaml for your CRDs
  - `Generate Docker`: Generate a dockerfile for your operator
  - `Generate Installer`: Generate a kustomization yaml for your operator
  - `Generate Operator`: Generate the yaml for your operator (rbac / role / etc)
  - `Generate RBAC`: Generate rbac roles for your CRDs

Other features and ideas are listed in the repositories "issues".

## How To Use

Using this sdk is pretty simple:

- Create a new asp.net core application
- Install the package
- Replace the `Run` function in `Program.ch`
- Add the operator to `Startup.cs`
- Write entities / controllers / finalizers
- Go.

### Install the package

```bash
dotnet add package KubeOps
```

That's it.

### Replace the Run function

In your `Program.cs` file, replace `Build().Run()` with `Build().RunOperator(args)`:

```csharp
public static class Program
{
    public static Task<int> Main(string[] args) => CreateHostBuilder(args).Build().RunOperator(args);

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
```

This adds the default commands (like run and the code generators) to your app.

_NOTE_: Technically you don't need to replace the function,
but if you don't, the other commands like yaml generation
are not available to your application.

### Add to Startup.cs

```csharp
public class Startup
{
    /* snip... */
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddKubernetesOperator(s => s.Name = "test-operator") // config / settings here
            .AddFinalizer<TestEntityFinalizer>()
            .AddController<TestController>(); // Add controllers / finalizers / ... here

        // your own dependencies
        services.AddTransient<IManager, TestManager.TestManager>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // fire up the mappings for the operator
        // this is technically not needed, but if you don't call this
        // function, the healthchecks and mappings are not
        // mapped to endpoints (therefore not callable)
        app.UseKubernetesOperator();
    }
}
```

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
// in a resource controller
await AttachFinalizer<TestEntityFinalizer>(resource);
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
