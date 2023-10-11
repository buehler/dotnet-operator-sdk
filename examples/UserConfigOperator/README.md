# KubeOps Demo Operator

This is a demo operator for the KubeOps framework. It is a simple operator that
creates a `Config Map` object for each `V1UserConfig` object.

To run the operator, build the project such that the required installer objects
are generated with the dotnet tool. Then, build the docker image and install
the operator on your cluster.

If you use Docker for Desktop with the Kubernetes extension, you
can just build the image and then use "imagePullPolicy: Never".

A running example on Docker for Desktop looks like this:

1. Build the project locally
2. Build the docker image with `docker build -t kubeops-demo-operator:latest .`
3. Add the imagePullPolicy patch to the created `kustomization.yaml` in the config dir
   ```yaml
   patches:
     - target:
         kind: Deployment
         labelSelector: operator-deployment=kubernetes-operator
       patch: |-
         - op: add
           path: /spec/template/spec/containers/0/imagePullPolicy
           value: Never
   ```
4. Set the image correctly in the `kustomization.yaml`
   ```yaml
   images:
     - name: operator
       newName: kubeops-demo-operator
       newTag: latest
   ```
5. Install the operator locally with `kustomize build config | kubectl apply -f -`

This should install the operator in your cluster. Now when you
apply the test entity (with `kubectl apply -f test_entity.yaml`)
a config map should be created.
