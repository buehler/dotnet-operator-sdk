namespace KubeOps.Abstractions.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ValidateAttribute(string rule, string? fieldPath = null, string? message = null, string? messageExpression = null, string? reason = null) : Attribute
{
    public string Rule => rule;

    public string? Message => message;

    public string? MessageExpression => messageExpression;

    public string? Reason => reason;

    public string? FieldPath => fieldPath;
}
