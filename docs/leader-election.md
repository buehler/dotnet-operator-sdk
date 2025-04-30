# Leader Election

When running multiple replicas of your operator for high availability, only one instance should be actively reconciling resources at any given time to prevent conflicting actions and ensure consistent state management. Kubernetes provides a **Leader Election** mechanism to achieve this.

## Concept

Leader election typically involves using a shared resource, like a [Lease](https://kubernetes.io/docs/reference/kubernetes-api/cluster-resources/lease-v1/) object in the Kubernetes API, as a lock. Operator instances attempt to acquire or renew the lease. The instance holding the lease is designated the "leader" and performs active reconciliation. Other instances remain idle ("followers") but are ready to take over if the leader fails.

## KubeOps Implementation

KubeOps integrates leader election seamlessly:

1.  **Automatic Handling:** When enabled, KubeOps automatically manages the leader election process using the Kubernetes Lease API.
2.  **Lease Management:** It creates and manages a `Lease` object within the cluster (usually in the namespace the operator runs in, or `kube-system` / `kube-public` if cluster-scoped).
3.  **Controller Activation:** Only the leader instance's controllers will actively receive events and run reconciliation loops (`ReconcileAsync`). Follower instances remain dormant in terms of reconciliation.
4.  **Health Checks:** KubeOps includes health checks that reflect the leader status, allowing Kubernetes probes to potentially restart non-leader pods if desired (though typically, followers just wait).

## Configuration

Leader Election is configured via [Operator Configuration](./operator-configuration.md) settings, typically in `appsettings.json` under the `KubeOps` section:

```json
{
  "KubeOps": {
    "Name": "my-operator.my-pod-xyz", // Important: Should be unique per instance/pod
    "EnableLeaderElection": true,     // Default: true
    "LeaderElectionId": "",          // Optional: Custom name for the Lease object
    // LeaseDuration, RenewDeadline, RetryPeriod might be configurable in future versions or via underlying client settings.
  }
}
```

*   **`EnableLeaderElection`**: (Boolean, Default: `true`) Set to `true` to enable leader election, `false` to disable (useful for local development/debugging or single-instance deployments).
*   **`Name` (`OperatorSettings.Name`)**: Crucial for leader election. This **must be unique** for each replica/pod instance of your operator. A common pattern is to set this dynamically using the pod's name via the Downward API.
*   **`LeaderElectionId`**: (String, Optional) Allows specifying a custom name for the `Lease` object used for locking. If empty (default), KubeOps generates a name based on the assembly name or a default identifier. Providing an explicit ID can be useful for identifying the lease object easily.

By default, leader election is enabled, ensuring safe operation when deploying multiple replicas of your KubeOps operator.
