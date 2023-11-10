namespace Bxes;

public interface IBxesReader
{
  Task<IEventLog> ReadAsync(ReadOnlySpan<char> path);
}