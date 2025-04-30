# Deploying Your KubeOps Operator

Once you have developed and tested your operator locally ([Getting Started](./getting-started.md)), the next step is to deploy it to your Kubernetes cluster.

Deploying a KubeOps operator involves packaging it as a container image and creating the necessary Kubernetes resources to run it.

## Prerequisites

*   A running [Kubernetes cluster](https://kubernetes.io/docs/setup/).
*   [`kubectl`](https://kubernetes.io/docs/tasks/tools/install-kubectl/) configured to interact with your cluster.
*   [Docker](https://www.docker.com/get-started) (or another container build tool like Podman) installed locally.
*   Access to a [container registry](https://docs.docker.com/docker-hub/repos/) where you can push your operator image (e.g., Docker Hub, Azure Container Registry (ACR), Amazon Elastic Container Registry (ECR), Google Container Registry (GCR), GitHub Container Registry (ghcr.io)).

## Steps

1.  **Build the Container Image:**
    *   KubeOps project templates (`dotnet new operator ...`) typically include a [`Dockerfile`](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/) in the main operator project (e.g., `MyFirstOperator.Operator/Dockerfile`).
    *   This `Dockerfile` typically uses [multi-stage builds](https://docs.docker.com/build/building/multi-stage/) to compile your .NET operator code and create a lean runtime image.
    *   Navigate to the directory containing the `Dockerfile`.
    *   Build the image using `docker build`. Remember to tag it appropriately with your container registry path, image name, and a version tag:

        ```bash
        # Example for Docker Hub
        docker build -t your-dockerhub-username/my-first-operator:v0.1.0 .

        # Example for Azure Container Registry (ACR)
        # docker build -t myregistry.azurecr.io/my-first-operator:v0.1.0 .
        ```

2.  **Push the Image to a Registry:**
    *   Log in to your container registry using `docker login` (or the specific login command for your registry, e.g., `az acr login`).
    *   Push the tagged image:

        ```bash
        # Example for Docker Hub
        docker push your-dockerhub-username/my-first-operator:v0.1.0

        # Example for ACR
        # docker push myregistry.azurecr.io/my-first-operator:v0.1.0
        ```

3.  **Generate Kubernetes Manifests:**
    *   Use the [KubeOps CLI tool](./cli.md) to generate the necessary Kubernetes YAML manifests based on your code (Entities, Controllers, Finalizers, Webhooks).
    *   It's recommended to generate these into a dedicated output directory (e.g., `./deploy`).
    *   Run the following commands from your solution root or a directory where the CLI tool can find your projects:

        ```bash
        # Ensure the output directory exists
        mkdir deploy

        # Generate CRDs (from your Entities project)
        dotnet kubeops generate crds --project ./MyFirstOperator.Entities/MyFirstOperator.Entities.csproj --output-path ./deploy

        # Generate Operator resources (RBAC, Deployment) based on the operator project
        # **IMPORTANT:** Update the --image parameter to match the image you pushed!
        dotnet kubeops generate operator --project ./MyFirstOperator.Operator/MyFirstOperator.Operator.csproj --image your-dockerhub-username/my-first-operator:v0.1.0 --output-path ./deploy

        # Generate Webhook configurations (if using webhooks)
        # This often requires the compiled assembly path
        # Adjust the path to your operator's built DLL
        # dotnet kubeops generate webhooks --assembly ./MyFirstOperator.Operator/bin/Debug/net8.0/MyFirstOperator.Operator.dll --output-path ./deploy
        ```
    *   **Review Generated Manifests:** Inspect the YAML files in the `./deploy` directory. Key files include:
        *   [CRD definitions](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definitions/) (`*.crd.yaml`) - Defines your custom resource types.
        *   `namespace.yaml` (often generated) - Defines the namespace where the operator will run (e.g., `my-first-operator-system`). Applying this multiple times may result in an "already exists" message, which is typically safe to ignore.
        *   [`ServiceAccount`](https://kubernetes.io/docs/tasks/configure-pod-container/configure-service-account/) (`service_account.yaml`) - Identity for the operator pod.
        *   [RBAC](https://kubernetes.io/docs/reference/access-authn-authz/rbac/) resources (`role.yaml`, `cluster_role.yaml`, `role_binding.yaml`, `cluster_role_binding.yaml`) - Grant permissions to the ServiceAccount.
        *   [`Deployment`](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/) (`deployment.yaml`) - Defines how your operator pod(s) run.
        *   [`Service`](https://kubernetes.io/docs/concepts/services-networking/service/) (`service.yaml` - if webhooks are used) - Exposes the operator's webhook endpoints internally so the Kubernetes API server can reach them.
        *   [Webhook configurations](https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/#webhook-configuration) (`*.validating.yaml`, `*.mutating.yaml`, CRD updates for conversion) - Configures admission/conversion webhooks if used.

4.  **Apply Manifests to Cluster:**
    *   Use [`kubectl apply`](https://kubernetes.io/docs/reference/generated/kubectl/kubectl-commands#apply) to create or update the resources in your cluster using the generated manifests.
    *   Apply the entire directory:

        ```bash
        kubectl apply -f ./deploy
        ```
    *   Alternatively, apply specific files if needed.

5.  **Verify Deployment:**
    *   Check if the operator pod is running:
        ```bash
        # Replace 'my-first-operator-system' with the actual namespace if different
        kubectl get pods -n my-first-operator-system 
        ```
    *   Check the operator logs:
        ```bash
        # Get the pod name from the previous command
        kubectl logs <operator-pod-name> -n my-first-operator-system -f
        ```
    *   Try creating an instance of your custom resource to see if the operator reconciles it.

## Important Considerations

*   **Image Pull Secrets:** If you pushed your image to a private container registry, your Kubernetes cluster needs credentials to pull it. You'll need to create an [`ImagePullSecret`](https://kubernetes.io/docs/tasks/configure-pod-container/pull-image-private-registry/) and reference it in the `ServiceAccount` used by your operator's `Deployment`. The `dotnet kubeops generate operator` command has flags (`--image-pull-secret`) to help reference existing secrets, but you still need to create the secret itself first using `kubectl create secret docker-registry ...`.
*   **RBAC:** Ensure the generated [RBAC](https://kubernetes.io/docs/reference/access-authn-authz/rbac/) roles (`ClusterRole` or `Role`) grant the necessary permissions for your operator to interact with the resources it manages (as defined by `[EntityRbac]` attributes - see [RBAC Generation](./rbac-generation.md) - or manual requirements).
*   **Namespaces:** By default, the templates and CLI might generate resources within a specific [Namespace](https://kubernetes.io/docs/concepts/overview/working-with-objects/namespaces/) (e.g., `your-operator-system`). Ensure this is appropriate for your cluster setup.
*   **Webhooks & TLS:** If using webhooks, ensure the TLS certificates and service routing are correctly configured as detailed in the [Webhooks documentation](./webhooks.md).
*   **Updates:** To update your operator, typically you'll:
    1.  Build and push a new image version.
    2.  Re-run `dotnet kubeops generate operator --image ...` with the *new* image tag.
    3.  Run `kubectl apply -f ./deploy` again. Kubernetes will perform a [rolling update](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/#rolling-update-deployment) of the `Deployment`.

See concrete deployment manifests and configurations in the GitHub repository, for example:
[`examples/Operator/deploy/`](https://github.com/ewassef/dotnet-operator-sdk/tree/main/examples/Operator/deploy)
