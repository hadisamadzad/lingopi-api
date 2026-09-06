namespace Lingopi.Core.BusContracts.Rpc.Sample;

public record SampleBusRequest : IBusRequest
{
    public required string Text { get; set; }
}
