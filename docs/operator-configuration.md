# Operator Configuration

KubeOps utilizes the standard .NET configuration system, allowing you to configure operator settings primarily through `appsettings.json` files, environment variables, or command-line arguments.

This page details the common configuration options available for tuning your operator's behavior.

## Basic Settings (`OperatorSettings`)

Many core settings are bound to the `OperatorSettings` class found in `KubeOps.Operator.Settings`. You can configure these in your `appsettings.json` under a corresponding section, typically `"KubeOps"`:

```json
{
  "KubeOps": {
    "Name": "MyOperator.Instance.Identifier",
    "Namespace": "",
    "EnableLeaderElection": true
  }
}
```

*   **`Name`**: A unique name for this specific operator instance. This is particularly important when running multiple instances of the same operator, especially for leader election. It's recommended to make this unique per pod, often using the pod name.
*   **`Namespace`**: (Optional) If set, restricts the operator to watch resources only within this specific namespace. If empty or omitted (default), the operator watches cluster-wide (subject to RBAC permissions).
*   **`EnableLeaderElection`**: (Default: `true`) Controls whether leader election is enabled. When enabled, only one instance (the leader) of the operator will actively reconcile resources, ensuring high availability without conflicting actions. See the [Leader Election](./leader-election.md) section for more details.

## Webhook Settings

If your operator uses [Webhooks](./webhooks.md), specific settings relate to the webhook server:

*   **Port Configuration:** By default, KubeOps hosts webhooks on port `8080` for HTTP and `8443` for HTTPS. This can be configured via standard ASP.NET Core methods (e.g., `ASPNETCORE_URLS` environment variable, `UseUrls()` in `Program.cs`).
*   **TLS Configuration:** Secure communication (HTTPS) is mandatory for webhooks. Certificate management is crucial. While KubeOps can work with certificates provided via mounted volumes or other means, integration with tools like `cert-manager` is common for automated certificate provisioning in-cluster.

## Advanced Settings

*(Placeholder: Add details on advanced settings like Kubernetes client configuration, watcher timeouts, metrics endpoints, etc.)*

## Accessing Configuration

You can access configuration values within your controllers, finalizers, webhooks, or other services using standard .NET dependency injection patterns:

1.  **Injecting `IConfiguration`**: For direct access to the entire configuration.

    ```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class MyController // Or Finalizer, Webhook, etc.
{
    private readonly ILogger<MyController> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _operatorName;

    public MyController(ILogger<MyController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Example: Read a specific value
        _operatorName = _configuration.GetValue<string>("KubeOps:Name") ?? "DefaultOperatorName";
    }

    // ... Controller methods ...
}
```

2.  **Using the Options Pattern**: Bind configuration sections to strongly-typed classes. This is often preferred for better type safety and separation of concerns.

    **a. Define an Options Class:**
    ```csharp
public class MyCustomSettings
{
    public string? ApiKey { get; set; }
    public int DefaultTimeoutSeconds { get; set; } = 30;
}
```

    **b. Configure Binding in `Program.cs`:**
    ```csharp
// Assuming 'builder' is WebApplication.CreateBuilder(args) or similar
builder.Services.Configure<MyCustomSettings>(builder.Configuration.GetSection("MyOperatorCustomSection"));
```

    **c. Inject `IOptions<T>`:**
    ```csharp
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

public class AnotherController
{
    private readonly ILogger<AnotherController> _logger;
    private readonly MyCustomSettings _settings;

    // Inject IOptions<MyCustomSettings>
    public AnotherController(ILogger<AnotherController> logger, IOptions<MyCustomSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value; // Access the actual settings object via .Value

        _logger.LogInformation($"Using API Key (partial): {_settings.ApiKey?.Substring(0, 4)}...");
        _logger.LogInformation($"Default Timeout: {_settings.DefaultTimeoutSeconds}");
    }

    // ... Controller methods ...
}
```

    **d. Corresponding `appsettings.json` Section:**
    ```json
{
  "KubeOps": { /* ... KubeOps settings ... */ },
  "MyOperatorCustomSection": {
    "ApiKey": "your-secret-api-key-here",
    "DefaultTimeoutSeconds": 60
  }
  /* ... other settings ... */
}
```

By leveraging these standard .NET patterns, you can manage your operator's configuration effectively.
