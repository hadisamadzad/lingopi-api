namespace Lingopi.Core.Utilities.LockManager;

public record RedisConfig
{
    public const string Key = "Redis";

    public required string SingleNode { get; set; }
    public required string[] ClusterNodes { get; set; }
    public bool ClusterEnabled { get; set; }

    public string[] Connections => ClusterEnabled ? ClusterNodes : [SingleNode];
}
