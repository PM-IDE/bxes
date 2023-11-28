namespace Bxes.Models.Values.Lifecycle;

public abstract class EventLifecycle<TLifecycleValue>(TLifecycleValue value)
  : BxesValue<TLifecycleValue>(value), IEventLifecycle;