using System.Collections.Generic;

namespace KubeOps.Operator.Webhooks
{
    /// <summary>
    /// Interface that is primarily used for dependency injection.
    /// To implement a validation webhook, use <see cref="IValidationWebhook{TEntity}"/>.
    /// This interface provides basic operation information.
    /// </summary>
    public interface IValidationWebhook
    {
        /// <summary>
        /// The operations that the webhook wants to be notified about.
        /// All subscribed events are forwarded to the validation webhook.
        /// </summary>
        ValidatedOperations Operations { get; }

        internal string WebhookName => $"{GetType().Namespace ?? "root"}.{GetType().Name}";

        internal IList<string> SupportedOperations
        {
            get
            {
                if (Operations.HasFlag(ValidatedOperations.All))
                {
                    return new List<string> { "*" };
                }

                var result = new List<string>();

                if (Operations.HasFlag(ValidatedOperations.Create))
                {
                    result.Add("CREATE");
                }

                if (Operations.HasFlag(ValidatedOperations.Update))
                {
                    result.Add("UPDATE");
                }

                if (Operations.HasFlag(ValidatedOperations.Delete))
                {
                    result.Add("DELETE");
                }

                return result;
            }
        }
    }
}
