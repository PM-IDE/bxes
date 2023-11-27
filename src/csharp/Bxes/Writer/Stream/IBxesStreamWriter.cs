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

public sealed class BxesKeyValueEvent(AttributeKeyValue metadataKeyValue)
  : BxesStreamEvent
{
  public AttributeKeyValue MetadataKeyValue { get; } = metadataKeyValue;
}

public sealed class BxesValueEvent(BxesValue value) : BxesStreamEvent
{
  public BxesValue Value { get; } = value;
}

public sealed class BxesLogMetadataEvent(IEventLogMetadata metadata) : BxesStreamEvent
{
  public IEventLogMetadata Metadata { get; } = metadata;
}