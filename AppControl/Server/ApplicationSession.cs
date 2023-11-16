namespace AppControl.Server;

public class ApplicationSession(long id, string versionTag, DateTime started, DateTime? stopped, string exitReason)
{
    public ApplicationSession(long id, string versionTag, DateTime started) : this(id, versionTag, started, null, null)
    {
    }

    public long Id { get; } = id;
    public string VersionTag { get; } = versionTag;
    public DateTime Started { get; } = started;
    public DateTime? Stopped { get; } = stopped;
    public string ExitReason { get; } = exitReason;
}