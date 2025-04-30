# Testing Your Operator

Testing is crucial for ensuring your Kubernetes operator behaves correctly and reliably. Given the interaction with the Kubernetes API and potentially external systems, a multi-layered testing strategy is recommended.

## Unit Testing

Unit tests focus on isolating and testing individual components of your operator, such as controllers, finalizers, or webhooks, without requiring a running Kubernetes cluster.

*   **Controllers/Finalizers/Webhooks:** Instantiate your class directly, mocking any dependencies like `ILogger` or `IKubernetesClient` (using libraries like Moq or NSubstitute). You can then call methods like `ReconcileAsync` or `FinalizeAsync` with crafted entity objects and assert the expected outcome (e.g., return value, calls to mocked dependencies).
*   **Helper Services:** Test any custom services or utility classes used by your operator components in isolation.

**Example (Conceptual Controller Unit Test using Moq):**

```csharp
using Moq;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller.Results;
using Microsoft.Extensions.Logging;
using MyOperator.Controllers;
using MyOperator.Entities;
using Xunit;

public class MyControllerTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldCreateConfigMap_WhenMissing()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MyController>>();
        var mockClient = new Mock<IKubernetesClient>();

        var controller = new MyController(mockLogger.Object, mockClient.Object);
        var entity = new V1MyResource 
        { 
            Metadata = new k8s.Models.V1ObjectMeta { Name = "test-resource", NamespaceProperty = "test-ns" },
            Spec = new V1MyResource.MySpec { /* ... */ }
        };

        // Mock KubernetesClient responses
        mockClient.Setup(c => c.GetAsync<k8s.Models.V1ConfigMap>(It.IsAny<string>(), It.IsAny<string?>()))
                  .ReturnsAsync((k8s.Models.V1ConfigMap?)null); // Simulate ConfigMap doesn't exist

        // Act
        var result = await controller.ReconcileAsync(entity);

        // Assert
        Assert.Null(result); // Expect successful reconcile (no requeue)
        mockClient.Verify(c => c.CreateObject(It.IsAny<k8s.Models.V1ConfigMap>()), Times.Once); // Verify ConfigMap was created
    }
}
```

## Integration Testing

Integration tests verify the interaction between your operator and a real (or simulated) Kubernetes API server. This is essential for catching issues related to API calls, resource watching, and CRD handling.

**Common Approaches:**

1.  **`envtest` (Recommended):** Part of the `controller-runtime` project (Go-based), `envtest` spins up a local Kubernetes API server and etcd instance without needing a full cluster. While KubeOps is .NET, you can often use `envtest` binaries alongside your .NET tests to provide a realistic API endpoint for your operator/tests to interact with. The .NET Kubernetes client can be configured to point to the `envtest` API server.
    *   *Setup:* Requires downloading `envtest` binaries.
    *   *Pros:* Fast startup, lightweight, realistic API behavior.
    *   *Cons:* Doesn't test interaction with actual Kubelets or other controllers.

2.  **Kind (Kubernetes IN Docker) / Minikube:** Run tests against a local, single-node Kubernetes cluster running in Docker (Kind) or a VM (Minikube). This provides a more complete Kubernetes environment.
    *   *Setup:* Requires Docker and Kind/Minikube installation.
    *   *Pros:* Tests against a real cluster environment.
    *   *Cons:* Slower startup than `envtest`, requires more resources.

3.  **Test Cluster:** Use a dedicated test cluster (e.g., a namespace in a shared development cluster).
    *   *Pros:* Most realistic environment.
    *   *Cons:* Can be slow, resource-intensive, potential for conflicts if shared.

Integration tests typically involve:
*   Starting the test environment (e.g., `envtest` or Kind).
*   Deploying your operator's CRDs.
*   (Optionally) Running your operator against the test cluster.
*   Using a Kubernetes client within your test code to:
    *   Create/update/delete instances of your custom resource.
    *   Assert that the operator reacts correctly (e.g., creates dependent resources, updates status).
    *   Clean up resources after the test.

## End-to-End (E2E) Testing

E2E tests validate the complete workflow of your operator in a production-like environment, potentially including interactions with external systems it manages.

*   These tests are often run as part of a CI/CD pipeline against a dedicated test cluster.
*   They focus on user scenarios and verifying the overall behavior and integration.

*(Placeholder: Add more details on specific testing libraries, frameworks, and patterns useful for KubeOps, potentially including examples using test fixtures and specific test environment setup.)*
