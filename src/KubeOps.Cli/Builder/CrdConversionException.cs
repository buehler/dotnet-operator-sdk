namespace KubeOps.Cli.Builder;

public class CrdConversionException : Exception
{
    public CrdConversionException()
    {
    }

    public CrdConversionException(string message)
        : base(message)
    {
    }

    public CrdConversionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
