namespace Lingopi.Core.BusContracts.Rpc.Sample;

public record SampleBusResponseWithCustomError : BusResponse
{
    public required string Text { get; set; }
    public new CustomError? Error { get; set; }

    public override bool HasError()
    {
        if (Error != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
