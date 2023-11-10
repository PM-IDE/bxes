namespace Bxes;

public interface IBxesWriter
{
  Task WriteAsync(IEventLog log);
}

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

public class BXesEventEvent : BxesStreamEvent
{
  public IEvent Event { get; set; }

  
  public BXesEventEvent(IEvent @event)
  {
    Event = @event;
  }
}

