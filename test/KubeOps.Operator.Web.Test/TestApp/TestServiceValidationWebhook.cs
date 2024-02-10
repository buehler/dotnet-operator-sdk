using k8s.Models;

using KubeOps.Operator.Web.Webhooks.Admission.Validation;

namespace KubeOps.Operator.Web.Test.TestApp;

[ValidationWebhook(typeof(V1Service))]
public class TestServiceValidationWebhook : ValidationWebhook<V1Service>;
