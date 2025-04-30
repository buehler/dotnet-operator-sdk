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
*   **`EnableLeaderElection`**: (Default: `true`) Controls whether leader election is enabled. When enabled, only one instance (the leader) of the operator will actively reconcile resources, ensuring high availability without conflicting actions. See the [Leader Election](./leader-election.md) section for more details. *(Placeholder: This section needs to be created)*

## Webhook Settings

If your operator uses [Webhooks](./webhooks.md), specific settings relate to the webhook server:

*   **Port Configuration:** By default, KubeOps hosts webhooks on port `8080` for HTTP and `8443` for HTTPS. This can be configured via standard ASP.NET Core methods (e.g., `ASPNETCORE_URLS` environment variable, `UseUrls()` in `Program.cs`).
*   **TLS Configuration:** Secure communication (HTTPS) is mandatory for webhooks. Certificate management is crucial. While KubeOps can work with certificates provided via mounted volumes or other means, integration with tools like `cert-manager` is common for automated certificate provisioning in-cluster.

## Advanced Settings

*(Placeholder: Add details on advanced settings like Kubernetes client configuration, watcher timeouts, metrics endpoints, etc.)*
