namespace Raft.Store.Domain;

public record NodeAddress(string Host, int Port)
{
    public override string ToString()
    {
        return $"{Host}:{Port}";
    }
}