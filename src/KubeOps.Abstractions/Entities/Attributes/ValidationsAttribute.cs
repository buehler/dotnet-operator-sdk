namespace KubeOps.Abstractions.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ValidationsAttribute(string rule, string message) : Attribute
{
    public string Rule => rule;

    public string Message => message;
}
