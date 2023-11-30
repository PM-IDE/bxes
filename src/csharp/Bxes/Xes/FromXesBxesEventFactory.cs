using System.Xml.Linq;
using Bxes.Models.Values;
using Bxes.Models.Values.Lifecycle;
using Bxes.Writer;

namespace Bxes.Xes;

public static class FromXesBxesEventFactory
{
  public static FromXesBxesEvent CreateFrom(XElement element)
  {
    var attributes = new Lazy<List<AttributeKeyValue>>(() => new List<AttributeKeyValue>());
    var initializedName = false;
    var initializedTimestamp = false;

    IEventLifecycle lifecycle = new StandardXesLifecycle(StandardLifecycleValues.Unspecified);
    string name = null!;
    long timestamp = 0;
    
    foreach (var child in element.Elements())
    {
      var (key, value, bxesValue) = XesReadUtil.ParseAttribute(child);

      switch (key)
      {
        case XesConstants.ConceptName:
          name = value;
          initializedName = true;
          break;
        case XesConstants.TimeTimestamp:
          timestamp = ((BxesInt64Value)bxesValue).Value;
          initializedTimestamp = true;
          break;
        case XesConstants.LifecycleTransition:
          lifecycle = IEventLifecycle.Parse(value);
          break;
        default:
          attributes.Value.Add(new AttributeKeyValue(new BxesStringValue(key), bxesValue));
          break;
      }
    }

    if (!initializedName || !initializedTimestamp)
    {
      throw new XesReadException("Failed to initialize name or timestamp");
    }

    return new FromXesBxesEvent
    {
      Timestamp = timestamp,
      Name = name,
      Lifecycle = lifecycle,
      Attributes = attributes.IsValueCreated ? attributes.Value : ArraySegment<AttributeKeyValue>.Empty
    };
  }
}