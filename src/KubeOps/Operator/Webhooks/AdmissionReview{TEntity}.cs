using k8s;

namespace KubeOps.Operator.Webhooks
{
    internal sealed class AdmissionReview<TEntity> : IKubernetesObject
    {
        public AdmissionReview()
        {
        }

        public AdmissionReview(AdmissionResponse response) => Response = response;

        public string ApiVersion { get; set; } = "admission.k8s.io/v1";

        public string Kind { get; set; } = "AdmissionReview";

        public AdmissionRequest<TEntity>? Request { get; set; }

        public AdmissionResponse? Response { get; set; }
    }
}
