using Bxes.Models;

namespace Bxes.Writer;

public interface IBxesStreamWriter
{
  Task HandleEvent(BxesStreamEvent @event);
}

public abstract class BxesStreamEvent
{
}

public class BXesTraceStartEvent : BxesStreamEvent
{
}

public class BXesEventEvent<TEvent> : BxesStreamEvent where TEvent : IEvent
{
  public TEvent Event { get; set; }


  public BXesEventEvent(TEvent @event)
  {
    Event = @event;
  }
}