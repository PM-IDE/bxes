using Bxes.Models;

namespace Bxes.Writer.Stream;

public interface IBxesStreamWriter : IDisposable
{
  void HandleEvent(BxesStreamEvent @event);
}

public abstract class BxesStreamEvent;

public sealed class BxesTraceVariantStartEvent(uint tracesCount) : BxesStreamEvent
{
  public uint TracesCount { get; } = tracesCount;
}

public sealed class BxesEventEvent<TEvent>(TEvent @event) : BxesStreamEvent
  where TEvent : IEvent
{
  public TEvent Event { get; set; } = @event;
}

public sealed class BxesLogMetadataKeyValueEvent(AttributeKeyValue metadataKeyValue)
  : BxesStreamEvent
{
  public AttributeKeyValue MetadataKeyValue { get; } = metadataKeyValue;
}