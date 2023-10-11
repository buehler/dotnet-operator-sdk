using Microsoft.AspNetCore.Http;

namespace KubeOps.Operator.Web.Webhooks;

public record AdmissionStatus(string Message, int? Code = StatusCodes.Status200OK);
