# Source Generation

KubeOps leverages C# Source Generators to reduce boilerplate code and streamline the setup process for your operator. The primary package responsible for this is `KubeOps.Generator`.

## What are Source Generators?

[Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) are a C# compiler feature that lets developers inspect user code during compilation and generate new C# source files on-the-fly. These generated files are added to the user's compilation like any other code.

## How KubeOps Uses Them

Currently, the main use case for source generators within KubeOps (`KubeOps.Generator`) is to **automatically register operator components** (Controllers, Finalizers, Webhooks) with the .NET Dependency Injection (DI) container.

### Automatic DI Registration

Instead of manually registering each of your controller, finalizer, and webhook implementations in your `Program.cs` file, the source generator scans your project for classes implementing the relevant KubeOps interfaces:

*   `IResourceController<TEntity>`
*   `IResourceFinalizer<TEntity>`
*   `IValidationWebhook<TEntity>`
*   `IMutationWebhook<TEntity>`
*   `IConversionWebhook<TEntity>`

Based on these findings, it generates extension methods (typically on `IOperatorBuilder`) that handle the registration process automatically.

**Before (Manual Registration - No longer required):**

```csharp
// In Program.cs (Conceptual - Old Way)
builder.Services
    .AddKubernetesOperator()
    .AddController<MyController, V1MyEntity>()
    .AddFinalizer<MyFinalizer, V1MyEntity>()
    .AddValidationWebhook<MyValidator, V1MyEntity>();
```

**After (With Source Generator):**

```csharp
// In Program.cs (Current Way)
builder.Services.AddKubernetesOperator();

// The KubeOps.Generator package automatically finds and registers
// controllers, finalizers, and webhooks found in the assembly.
```

### Benefits

*   **Reduced Boilerplate:** Eliminates the need for repetitive DI registration code in `Program.cs`.
*   **Improved Maintainability:** Automatically picks up new components as you add them, reducing the chance of forgetting to register something.
*   **Compile-Time Safety:** Errors in component definitions might be caught earlier during the build process.

## Usage

To use the source generators, simply **reference the `KubeOps.Operator` package** (or `KubeOps.Operator.Web` if using webhooks) in your operator's project file (`.csproj`). The `KubeOps.Generator` package is included as a dependency and the generators will run automatically during the build.

```xml
<!-- Example .csproj reference -->
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework> <!-- or net9.0 -->
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Referencing KubeOps.Operator automatically brings in KubeOps.Generator -->
    <PackageReference Include="KubeOps.Operator" Version="[Version]" />
    <!-- Or KubeOps.Operator.Web if you need webhooks -->
    <!-- <PackageReference Include="KubeOps.Operator.Web" Version="[Version]" /> -->
  </ItemGroup>

</Project>
```

You generally don't need to interact directly with the generated code, but you can inspect it via Visual Studio's Solution Explorer under `Dependencies > Analyzers > KubeOps.Generator` if needed.
