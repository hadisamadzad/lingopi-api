namespace Lingopi.Core.BusContracts.Rpc.Sample;

public record CustomError : Error
{
    public required string CustomProperty { get; set; }
}
