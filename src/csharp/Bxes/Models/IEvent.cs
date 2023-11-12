namespace Bxes.Models;

public interface IEvent
{
  long Timestamp { get; }
  string Name { get; }
  IEventLifecycle Lifecycle { get; }

  IEventAttributes Attributes { get; }
}

public interface IEventAttributes : IDictionary<BXesStringValue, BxesValue>;