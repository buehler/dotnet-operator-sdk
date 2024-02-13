using k8s.Models;

using KubeOps.Operator.Web.Webhooks.Admission.Mutation;

namespace KubeOps.Operator.Web.Test.TestApp;

[MutationWebhook(typeof(V1Service))]
public class TestServiceMutationWebhook : MutationWebhook<V1Service>;
