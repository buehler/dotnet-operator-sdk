using System;
using System.Collections.Generic;

namespace KubeOps.Operator.Webhooks.ConversionWebhook;

public record ConversionReview(string ApiVersion, string Kind, Request Request);

public record Request(Guid Uid, string DesiredAPIVersion, List<object> Objects);
