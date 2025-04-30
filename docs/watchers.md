# Watching Related Resources

This section explains how to configure your KubeOps operator controller to react to changes in Kubernetes resources other than the primary custom resource it manages.

This is useful when the state or configuration of your custom resource depends on, or influences, other resources like Secrets, ConfigMaps, Deployments, or even other custom resources.

*TODO: Add content explaining the concept, the `OperatorBuilder` configuration methods (e.g., `Watches<TEntity, TRelated>`), how to map related resource events back to owner resources (using labels, owner references, or custom logic), and provide clear examples.*
