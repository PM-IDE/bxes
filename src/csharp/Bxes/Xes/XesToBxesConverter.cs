using System.Xml;
using System.Xml.Linq;
using Bxes.Models;
using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Xes;

public class XesReadException(string message) : BxesException
{
  public override string Message { get; } = message;
}

public static class XesConstants
{
  public const string TraceTagName = "trace";
  public const string EventTagName = "event";

  public const string StringTagName = "string";
  public const string DateTagName = "date";
  public const string IntTagName = "int";
  public const string FloatTagName = "float";
  public const string BoolTagName = "boolean";

  public const string KeyAttributeName = "key";
  public const string ValueAttributeName = "value";

  public const string ConceptName = "concept:name";
  public const string TimeTimestamp = "time:timestamp";
  public const string LifecycleTransition = "lifecycle:transition";
}

public readonly struct FromXesBxesEvent : IEvent
{
  public long Timestamp { get; }
  public string Name { get; }
  public IEventLifecycle Lifecycle { get; }
  public IEnumerable<AttributeKeyValue> Attributes { get; }


  public FromXesBxesEvent(XElement element)
  {
    var attributes = new Lazy<List<AttributeKeyValue>>(() => new List<AttributeKeyValue>());
    var initializedName = false;
    var initializedTimestamp = false;
    
    foreach (var child in element.Elements())
    {
      string key = null!;
      string value = null!;
      foreach (var attribute in child.Attributes())
      {
        if (attribute.Name == XesConstants.KeyAttributeName)
        {
          key = attribute.Value;
        }

        if (attribute.Name == XesConstants.ValueAttributeName)
        {
          value = attribute.Value;
        }
      }

      BxesValue bxesValue = child.Name.LocalName switch
      {
        XesConstants.StringTagName => new BxesStringValue(value),
        XesConstants.DateTagName => new BxesTimeStampValue(DateTime.Parse(value).Ticks),
        XesConstants.IntTagName => new BxesInt64Value(long.Parse(value)),
        XesConstants.FloatTagName => new BxesFloat64Value(double.Parse(value)),
        XesConstants.BoolTagName => new BxesBoolValue(bool.Parse(value)),
        _ => throw new XesReadException($"Failed to create value for type {child.Name.LocalName}")
      };

      Lifecycle = new StandardXesLifecycle(StandardLifecycleValues.Unspecified);
      
      switch (key)
      {
        case XesConstants.ConceptName:
          Name = value;
          initializedName = true;
          break;
        case XesConstants.TimeTimestamp:
          Timestamp = ((BxesInt64Value)bxesValue).Value;
          initializedTimestamp = true;
          break;
        case XesConstants.LifecycleTransition:
          Lifecycle = IEventLifecycle.Parse(value);
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

    Attributes = attributes.IsValueCreated switch
    {
      true => attributes.Value,
      false => ArraySegment<AttributeKeyValue>.Empty
    };
  }
  
  public bool Equals(IEvent? other) => other is { } && EventUtil.Equals(this, other);
}

public class XesToBxesConverter
{
  public void Convert(string xesFilePath, string bxesOutputPath)
  {
    using var writer = new SingleFileBxesStreamWriterImpl<FromXesBxesEvent>(bxesOutputPath, BxesConstants.BxesVersion);

    using var fs = File.OpenRead(xesFilePath);
    var reader = XmlReader.Create(fs);

    while (reader.Read())
    {
      if (reader.Name == XesConstants.TraceTagName)
      {
        ReadTrace(reader, writer);
      }
    }
  }

  private void ReadTrace(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    writer.HandleEvent(new BxesTraceVariantStartEvent(1));

    while (reader.Read())
    {
      if (reader is { NodeType: XmlNodeType.Element, Name: XesConstants.EventTagName })
      {
        ReadEvent(reader, writer);
      }
    }
  }

  private void ReadEvent(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    var element = XElement.Load(reader);
    var bxesEvent = new FromXesBxesEvent(element);
    writer.HandleEvent(new BxesEventEvent<FromXesBxesEvent>(bxesEvent));
  }
}