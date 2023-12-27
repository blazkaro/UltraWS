namespace UltraWS.Exceptions;

internal class HubMethodException : Exception
{
    public HubMethodException()
    {
    }

    public HubMethodException(string? message) : base(message)
    {
    }
}
