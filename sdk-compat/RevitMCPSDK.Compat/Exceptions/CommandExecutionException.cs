namespace RevitMCPSDK.Exceptions;

public class CommandExecutionException : Exception
{
    public int ErrorCode { get; }
    public object ErrorData { get; }

    public CommandExecutionException(string message, int errorCode, object errorData = null) : base(message)
    {
        ErrorCode = errorCode;
        ErrorData = errorData;
    }
}
