namespace Lingopi.Core.BusContracts.Rpc;

public record Error
{
    public Error()
    {
    }

    public Error(Enum error, string? message = null)
    {
        ErrorCode = Convert.ToInt32(error);
        Message = message;
    }

    public int ErrorCode { get; set; }
    public string? Message { get; set; }

    public bool Is(Enum e) => Convert.ToInt32(e) == ErrorCode;
}
