using Bxes.Models;

namespace Bxes.Writer.Stream;

public interface IBxesStreamWriter : IDisposable
{
  void HandleEvent(BxesStreamEvent @event);
}