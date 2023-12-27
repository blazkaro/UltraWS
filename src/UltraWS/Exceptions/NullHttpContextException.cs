namespace UltraWS.Exceptions;

internal class NullHttpContextException : Exception
{
    public NullHttpContextException()
    {
    }

    public NullHttpContextException(string? message) : base(message)
    {
    }
}
