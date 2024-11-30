namespace Raft.Store.Domain.Replication;

public record LogEntry(Command Command, int Term);