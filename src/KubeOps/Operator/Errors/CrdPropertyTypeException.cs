namespace KubeOps.Operator.Errors;

internal class CrdPropertyTypeException : Exception
{
    public CrdPropertyTypeException()
        : this("The given property has an invalid type.")
    {
    }

    public CrdPropertyTypeException(string message)
        : base(message)
    {
    }

    public CrdPropertyTypeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
