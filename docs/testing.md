# Testing Your Operator

Testing is crucial for ensuring your Kubernetes operator behaves correctly and reliably. Given the interaction with the Kubernetes API and potentially external systems, a multi-layered testing strategy is recommended.

## Unit Testing

Unit tests focus on isolating and testing individual components of your operator, such as controllers, finalizers, or webhooks, without requiring a running Kubernetes cluster.

*   **Scope:** Test individual C# classes (Controllers, Finalizers, Webhooks, custom services) in isolation.
*   **Dependencies:** Mock external dependencies, especially `IKubernetesClient`, `ILogger`, and any other injected services. Popular .NET mocking libraries include [Moq](https://github.com/moq/moq) and [NSubstitute](https://nsubstitute.github.io/).
*   **Process:**
    1.  Instantiate the class under test, providing mocked dependencies.
    2.  Prepare input data (e.g., a `TEntity` instance for `ReconcileAsync`).
    3.  Configure mock behavior (e.g., `mockClient.Setup(...).ReturnsAsync(...)` for Moq).
    4.  Execute the method under test (e.g., `await controller.ReconcileAsync(entity)`).
    5.  Assert the results (e.g., return values, status updates on the input entity).
    6.  Verify interactions with mocks (e.g., `mockClient.Verify(c => c.CreateAsync(...), Times.Once)`).
*   **Goal:** Verify the internal logic of each component works as expected given specific inputs and dependency responses.

**Example (Conceptual Controller Unit Test using Moq & xUnit):**

```csharp
using Moq;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller.Results;
using Microsoft.Extensions.Logging;
using MyOperator.Controllers; // Assuming your controller lives here
using MyOperator.Entities;    // Assuming your entity lives here
using Xunit;

public class MyControllerTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldCreateConfigMap_WhenMissing()
    {
        // Arrange
        var mockLogger = Mock.Of<ILogger<MyController>>(); // Concise Moq setup
        var mockClient = new Mock<IKubernetesClient>();

        var controller = new MyController(mockLogger.Object, mockClient.Object);
        var entity = new V1MyResource 
        { 
            Metadata = new V1ObjectMeta { Name = "test-resource", NamespaceProperty = "test-ns" },
            Spec = new V1MyResource.MySpec { Message = "Hello" }
        };

        // Example: Define the expected ConfigMap
        var expectedConfigMap = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = $"{entity.Name}-config", NamespaceProperty = entity.Namespace() },
            Data = new Dictionary<string, string> { { "message", entity.Spec.Message } }
            // OwnerReferences would typically be set here too
        };

        // Mock KubernetesClient responses:
        // - Simulate GetAsync returns null (not found)
        mockClient.Setup(c => c.GetAsync<k8s.Models.V1ConfigMap>(It.IsAny<string>(), It.IsAny<string?>()))
                  .ReturnsAsync((k8s.Models.V1ConfigMap?)null); // Simulate ConfigMap doesn't exist

        // - Setup CreateAsync to succeed (no return value needed)
        mockClient.Setup(c => c.CreateAsync(It.IsAny<V1ConfigMap>()))
                  .Returns(Task.CompletedTask);

        // Act
        var result = await controller.ReconcileAsync(entity);

        // Assert
        Assert.Null(result); // Expect successful reconcile (null result means no requeue requested)

        // Verify that CreateAsync was called once with a ConfigMap matching expectations
        // (Using It.Is<> for complex object matching or manual property checks)
        mockClient.Verify(
            c => c.CreateAsync(It.Is<V1ConfigMap>(cm => 
                cm.Metadata.Name == expectedConfigMap.Metadata.Name && 
                cm.Data["message"] == expectedConfigMap.Data["message"])), 
            Times.Once);
    }

    // Add more tests for update scenarios, error handling, status updates, etc.
}
```

## Integration Testing

Integration tests verify the interaction between your operator and a real (or simulated) Kubernetes API server. This is essential for catching issues related to API calls, resource watching, and CRD handling.

**Key Goal:** Test that your operator correctly interacts with the Kubernetes API for its core functions (CRD management, resource creation/update based on CR spec, webhook validation/mutation) without necessarily running the full reconciliation loop in the test process.

### Using `envtest` (Recommended)

`envtest`, part of the Kubernetes [controller-runtime](https://github.com/kubernetes-sigs/controller-runtime) project, starts a temporary `kube-apiserver` and `etcd` instance locally, providing a real API server endpoint for your tests without needing Docker or a full cluster.

**Pros:**

*   Fast startup.
*   Lightweight.
*   Real API server for testing CRD application, API interactions, admission webhooks (requires separate webhook server setup/tunneling for testing).

**Cons:**

*   No Kubelets or built-in controllers (e.g., Deployment controller). Cannot test interactions involving Pod scheduling or built-in controller behavior directly.

**Setup:**

1.  **Install `envtest`:** Download the `setup-envtest` utility script/binary or the `envtest` binaries directly. Add the location of `kube-apiserver` and `etcd` to your system's PATH or configure your test runner to find them. See the [controller-runtime envtest setup](https://book.kubebuilder.io/reference/envtest.html) documentation.
2.  **Test Fixture:** Use a test fixture (like xUnit's `IAsyncLifetime`) to manage the `envtest` process lifecycle (start before tests, stop after).

**Example (Conceptual Integration Test using `envtest`, xUnit & `k8s-client`):**

```csharp
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient; // Or use the official k8s.Kubernetes directly
using System.Diagnostics;
using Xunit;
using YamlDotNet.Serialization; // For loading manifests

// --- Test Fixture to manage envtest lifecycle ---
public class EnvtestFixture : IAsyncLifetime
{
    private Process? _envtestProcess;
    public string KubeconfigPath { get; private set; } = string.Empty;
    public Kubernetes? Client { get; private set; }

    public async Task InitializeAsync()
    {
        // 1. Start envtest (adjust path and arguments as needed)
        // Assumes setup-envtest was used or envtest binaries are in PATH
        // The '-p' flag prints the paths, including the kubeconfig file path.
        // Using KUBEBUILDER_ASSETS is often more robust.
        var assetsPath = Environment.GetEnvironmentVariable("KUBEBUILDER_ASSETS");
        if (string.IsNullOrEmpty(assetsPath))
        {
            // Fallback or throw - envtest needs to know where kube-apiserver/etcd are.
            // Example: You might run 'setup-envtest use <version> -p path > envtest_paths.txt'
            // and read the path from there.
            throw new InvalidOperationException("KUBEBUILDER_ASSETS environment variable not set. Cannot locate envtest binaries.");
        }

        KubeconfigPath = Path.Combine(Path.GetTempPath(), $"kubeconfig-envtest-{Guid.NewGuid()}");
        var startInfo = new ProcessStartInfo
        {
            FileName = "envtest", // Assumes envtest is in PATH
            Arguments = $"local -k {KubeconfigPath}", // Start local env, output kubeconfig
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Environment = { { "KUBEBUILDER_ASSETS", assetsPath } } // Crucial!
        };

        _envtestProcess = Process.Start(startInfo);
        // TODO: Add robust process waiting & error handling
        // Wait for envtest to be ready (e.g., check for output, wait fixed time - crude)
        await Task.Delay(TimeSpan.FromSeconds(15)); // Adjust timing based on envtest startup

        // 2. Configure Kubernetes Client
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(KubeconfigPath);
        Client = new Kubernetes(config);

        // 3. Apply CRDs (Essential before creating CR instances)
        // Assumes your CRD YAML is in your test project (e.g., copied from main project)
        await ApplyManifestAsync("path/to/your/crd.v1.yaml");
    }

    public async Task DisposeAsync()
    {
        _envtestProcess?.Kill(entireProcessTree: true); // Ensure etcd/apiserver are stopped
        _envtestProcess?.Dispose();
        if (File.Exists(KubeconfigPath)) File.Delete(KubeconfigPath);
        await Task.CompletedTask;
    }

    private async Task ApplyManifestAsync(string manifestPath)
    {
        var yamlContent = await File.ReadAllTextAsync(manifestPath);
        var objects = KubernetesYaml.LoadAllFromString<IKubernetesObject<V1ObjectMeta>>(yamlContent);
        foreach (var obj in objects)
        {
            try
            {
                // Simplified apply - real implementation needs GET/PATCH/CREATE logic
                // For CRDs, CREATE is usually sufficient in tests
                if (obj is V1CustomResourceDefinition crd)
                {
                     await Client.ApiextensionsV1.CreateCustomResourceDefinitionAsync(crd);
                     // TODO: Add wait logic for CRD to be established
                     await Task.Delay(TimeSpan.FromSeconds(2)); 
                }
                // Add cases for other resource types if needed
            }
            catch (Microsoft.Rest.HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            { /* Already exists - ignore for simple test setup */ }
        }
    }
}

// --- Example Test Class using the Fixture ---
public class MyResourceIntegrationTests : IClassFixture<EnvtestFixture>
{
    private readonly EnvtestFixture _fixture;
    private readonly Kubernetes _client;

    public MyResourceIntegrationTests(EnvtestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client!;
    }

    [Fact]
    public async Task Can_Create_And_Get_CustomResource()
    {
        // Arrange
        var resource = new V1MyResource // Your generated CRD class
        {
            ApiVersion = V1MyResource.KubeApiVersion,
            Kind = V1MyResource.KubeKind,
            Metadata = new V1ObjectMeta { Name = "test-cr", NamespaceProperty = "default" },
            Spec = new V1MyResource.MySpec { Message = "Integration Test" }
        };

        // Act
        V1MyResource createdResource = null;
        try
        {
             // Use generic client helper or specific API group client
             createdResource = await _client.CreateNamespacedCustomObjectAsync<V1MyResource>(
                 resource, 
                 V1MyResource.KubeGroup, 
                 V1MyResource.KubeApiVersion, 
                 "default", // Namespace
                 V1MyResource.KubePluralName);

             var retrievedResource = await _client.GetNamespacedCustomObjectAsync<V1MyResource>(
                 V1MyResource.KubeGroup, 
                 V1MyResource.KubeApiVersion, 
                 "default", 
                 V1MyResource.KubePluralName, 
                 "test-cr");

            // Assert
            Assert.NotNull(createdResource);
            Assert.NotNull(retrievedResource);
            Assert.Equal("test-cr", retrievedResource.Metadata.Name);
            Assert.Equal("Integration Test", retrievedResource.Spec.Message);
        }
        finally
        { 
            // Cleanup: Delete the resource after the test
            if (createdResource != null)
            {
                 await _client.DeleteNamespacedCustomObjectAsync(
                    V1MyResource.KubeGroup, 
                    V1MyResource.KubeApiVersion, 
                    "default", 
                    V1MyResource.KubePluralName, 
                    "test-cr");
            }
        }
    }
}

### Using Kind / Minikube

Using [Kind (Kubernetes IN Docker)](https://kind.sigs.k8s.io/) or [Minikube](https://minikube.sigs.k8s.io/docs/) provides a more complete Kubernetes environment, including Kubelets and built-in controllers. This is useful if your operator's logic depends on the behavior of standard controllers (e.g., checking Deployment status, Pod readiness).

*   *Setup:* Requires Docker and the `kind` or `minikube` CLI installed.
*   *Pros:* Provides a more complete Kubernetes environment, including Kubelets and built-in controllers. Allows testing interactions between your operator and standard Kubernetes resources (Pods, Deployments, etc.).
*   *Cons:* Slower startup than `envtest`, requires more system resources (especially Docker Desktop on Windows/macOS).

The test structure is similar to `envtest` (apply CRDs, create CRs, assert), but the client configuration changes:

```csharp
// Typically, Kind/Minikube update your default kubeconfig (~/.kube/config)
// Or you can point to a specific one via KUBECONFIG environment variable
var config = KubernetesClientConfiguration.BuildDefaultConfig(); 

// Or, if 'kind' provides a specific kubeconfig file:
// var kubeconfigPath = Environment.GetEnvironmentVariable("KIND_KUBECONFIG_PATH"); // Example env var
// var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeconfigPath);

var client = new Kubernetes(config);

// ... rest of the test logic (apply CRD, create/get CR) ...
```

Starting/stopping the Kind/Minikube cluster would typically happen outside the test process, perhaps in CI scripts (`kind create cluster`, `kind delete cluster`).

### Using a Shared/Remote Test Cluster

Using a dedicated test cluster (e.g., a namespace in a shared development cluster, a cloud-based test cluster) provides the most realistic environment.

*   *Pros:* Most realistic environment, tests real network policies, storage, etc.
*   *Cons:* Can be slow to provision/access, resource-intensive, potentially expensive, requires careful environment management and cleanup, potential for conflicts if shared.

Client configuration usually involves pointing to the appropriate kubeconfig file for that cluster.

**Important Integration Testing Considerations:**

*   **CRD Application:** Always apply your CRDs to the test API server *before* attempting to create instances of your custom resource.
*   **Cleanup:** Meticulously delete all resources created during a test (CR instances, namespaces if used) in a `finally` block or test teardown method to ensure test isolation.
*   **Waiting/Polling:** When testing asynchronous operations (like waiting for a resource to be created or a status to update), implement robust waiting logic (e.g., polling with timeouts) instead of fixed `Task.Delay` calls.
*   **Testing Operator Logic:** The examples above primarily test interaction with the API server. To test the operator's *reconciliation logic* in an integration environment, you often need to run the operator host itself, configured to point to the test cluster. This can be done within the test process using `Microsoft.Extensions.Hosting` or by running the compiled operator as a separate process against the test cluster (closer to E2E testing).

## End-to-End (E2E) Testing

E2E tests validate the complete workflow of your operator in a production-like environment, potentially including interactions with external systems it manages.

*   These tests are often run as part of a CI/CD pipeline against a dedicated test cluster (similar to integration testing with a test cluster, but usually involves running the *actual compiled operator image*).
*   Focus on validating complete user scenarios from start to finish.
*   Verify the operator's overall behavior, including interactions between controllers, finalizers, webhooks, and the Kubernetes API.
*   Typically involves deploying the packaged operator container image to the test cluster.

**Key Considerations for All Testing:**

*   **Test Frameworks:** Use standard .NET test frameworks like [xUnit](https://xunit.net/), [NUnit](https://nunit.org/), or MSTest.
*   **Test Isolation:** Ensure tests do not interfere with each other, especially integration/E2E tests. Proper cleanup is vital.
*   **CI/CD Integration:** Automate your tests in your CI/CD pipeline for continuous feedback.
