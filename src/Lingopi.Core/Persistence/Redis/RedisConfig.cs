namespace Lingopi.Core.Persistence.Redis;

public record RedisConfig
{
    public const string Key = "Redis";

    public string? SingleNode { get; set; }
    public string[]? ClusterNodes { get; set; }
    public bool ClusterEnabled { get; set; }

    public string[] Connections => ClusterEnabled ? ClusterNodes! : [SingleNode!];
}
