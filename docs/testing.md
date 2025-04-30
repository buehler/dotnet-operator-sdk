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
        mockClient.Verify(c => c.CreateAsync(It.IsAny<k8s.Models.V1ConfigMap>()), Times.Once); // Verify ConfigMap was created
    }
}
```

## Integration Testing

Integration tests verify the interaction between your operator and a real (or simulated) Kubernetes API server. This is essential for catching issues related to API calls, resource watching, and CRD handling.

**Common Approaches:**

1.  **`envtest` (Recommended):** Part of the Kubernetes [controller-runtime](https://github.com/kubernetes-sigs/controller-runtime) project (Go-based), `envtest` sets up and runs a local instance of the Kubernetes API server (`kube-apiserver`) and `etcd` for testing purposes. It doesn't require Docker or a full Kubelet.
    *   *Setup:* Requires downloading `envtest` binaries suitable for your OS. You'll typically manage these via a script or setup step in your test project. Your tests will need to start the `envtest` process and obtain the connection details (kubeconfig) to configure the .NET `KubernetesClientConfiguration`.
    *   *Pros:* Fast startup compared to full clusters, lightweight, provides a real API server for testing CRD application, validation/mutation webhooks, and basic API interactions.
    *   *Cons:* Does not include Kubelets or other controllers (e.g., Deployment controller), so it cannot test interactions involving Pod scheduling or built-in controller behavior. Primarily tests API-level interactions.

2.  **[Kind (Kubernetes IN Docker)](https://kind.sigs.k8s.io/) / [Minikube](https://minikube.sigs.k8s.io/docs/):** Run tests against a local, single-node or multi-node Kubernetes cluster running in Docker (`kind`) or a lightweight VM (`minikube`).
    *   *Setup:* Requires Docker and the `kind` or `minikube` CLI installed.
    *   *Pros:* Provides a more complete Kubernetes environment, including Kubelets and built-in controllers. Allows testing interactions between your operator and standard Kubernetes resources (Pods, Deployments, etc.).
    *   *Cons:* Slower startup than `envtest`, requires more system resources (especially Docker Desktop on Windows/macOS).

3.  **Test Cluster:** Use a dedicated test cluster (e.g., a namespace in a shared development cluster, a cloud-based test cluster).
    *   *Pros:* Most realistic environment, tests real network policies, storage, etc.
    *   *Cons:* Can be slow to provision/access, resource-intensive, potentially expensive, requires careful environment management and cleanup, potential for conflicts if shared.

Integration tests typically involve:
*   Starting the test environment (e.g., `envtest` process, `kind create cluster`).
*   Configuring the .NET Kubernetes client (`KubernetesClientConfiguration.BuildConfigFromConfigFile` or equivalent) to point to the test environment.
*   Deploying your operator's CRDs to the test environment using the client.
*   (Optionally) Running your operator executable against the test cluster (less common for pure integration tests, more for E2E).
*   Using a Kubernetes client within your test code (e.g., using `Microsoft.Extensions.Hosting` for DI or direct client instantiation) to:
    *   Create/update/delete instances of your custom resource.
    *   Wait for expected conditions (using polling or watches).
    *   Assert that the operator (if running) or the API server (e.g., for webhook validation) behaves correctly (e.g., dependent resources are created, status is updated, validation rejects invalid specs).
*   Cleaning up all created resources meticulously after each test to ensure isolation.

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
