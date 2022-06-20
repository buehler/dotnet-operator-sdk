using System;
using System.Collections.Generic;

namespace KubeOps.Operator.Webhooks.ConversionWebhook;

public record ConversionResponse(string ApiVersion, string Kind, Response Response);

public record Response(Guid Uid, Result Result, List<object>? ConvertedObjects);

public record ConversionResponse<T>(string ApiVersion, string Kind, Response<T> Response);

public record Response<T>(Guid Uid, Result Result, List<T> ConvertedObjects);

public class Result
{
    public Result(string status, string? message = null)
    {
        Status = status;
        Message = message;
    }

    public string Status { get; }
    public string? Message { get; }
    public static Result Success() => new Result("Success");
    public static Result Failed(string message) => new Result("Failed", message);

}



