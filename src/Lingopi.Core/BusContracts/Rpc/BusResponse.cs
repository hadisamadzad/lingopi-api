namespace Lingopi.Core.BusContracts.Rpc;

public record BusResponse
{
    public virtual Error? Error { get; set; }

    public virtual bool HasError() => Error != null;
}
