using System.Xml;
using System.Xml.Linq;
using Bxes.Models;
using Bxes.Models.Values;
using Bxes.Models.Values.Lifecycle;
using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Xes;

public class XesReadException(string message) : BxesException
{
  public override string Message { get; } = message;
}

public static class XesConstants
{
  public const string DefaultName = "name";
  
  public const string TraceTagName = "trace";
  public const string EventTagName = "event";
  public const string ExtensionTagName = "extension";
  public const string ClassifierTagName = "classifier";
  public const string GlobalTagName = "global";

  public const string ClassifierNameAttribute = DefaultName;
  public const string ClassifierKeysAttribute = "keys";

  public const string ExtensionNameAttribute = DefaultName;
  public const string ExtensionPrefixAttribute = "prefix";
  public const string ExtensionUriAttribute = "uri";

  public const string GlobalScopeAttribute = "scope";

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

public static class XesReadUtil
{
  public static (string Key, string Value, BxesValue ParsedValue) ParseAttribute(XElement child)
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

    return (key, value, bxesValue);
  }
}

public readonly struct FromXesBxesEvent : IEvent
{
  public required long Timestamp { get; init; }
  public required string Name { get; init; }
  public required IEventLifecycle Lifecycle { get; init; }
  public required IList<AttributeKeyValue> Attributes { get; init; }
  

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
      if (reader.NodeType == XmlNodeType.Element)
      {
        ProcessTag(reader, writer);
      }
    }
  }

  private void ProcessTag(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    switch (reader.Name)
    {
      case XesConstants.TraceTagName:
        ReadTrace(reader, writer);
        break;
      case XesConstants.ClassifierTagName:
        ReadClassifier(reader, writer);
        break;
      case XesConstants.ExtensionTagName:
        ReadExtension(reader, writer);
        break;
      case XesConstants.GlobalTagName:
        ReadGlobal(reader, writer);
        break;
      case XesConstants.StringTagName:
      case XesConstants.DateTagName:
      case XesConstants.IntTagName:
      case XesConstants.FloatTagName:
      case XesConstants.BoolTagName:
        ReadProperty(reader, writer);
        break;
    }
  }

  private void ReadClassifier(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    var element = XElement.Load(reader);
    
    BxesStringValue? name = null;
    List<BxesStringValue>? keys = null;
    foreach (var attribute in element.Attributes())
    {
      if (attribute.Name == XesConstants.ClassifierNameAttribute)
      {
        name = new BxesStringValue(attribute.Value);
      }
      else if (attribute.Name == XesConstants.ClassifierKeysAttribute)
      {
        keys = attribute.Value.Split().Select(key => new BxesStringValue(key)).ToList();
      }
    }

    if (name is null) throw new XesReadException("Failed to read name in classifier");
    if (keys is null) throw new XesReadException("Failed to read keys in classifier");
    
    writer.HandleEvent(new BxesLogMetadataClassifierEvent(new BxesClassifier
    {
      Name = name,
      Keys = keys
    }));
  }
  
  private void ReadExtension(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    var element = XElement.Load(reader);

    BxesStringValue? name = null;
    BxesStringValue? prefix = null;
    BxesStringValue? uri = null;

    foreach (var attribute in element.Attributes())
    {
      if (attribute.Name == XesConstants.ExtensionNameAttribute)
      {
        name = new BxesStringValue(attribute.Value);
      }
      else if (attribute.Name == XesConstants.ExtensionPrefixAttribute)
      {
        prefix = new BxesStringValue(attribute.Value);
      }
      else if (attribute.Name == XesConstants.ExtensionUriAttribute)
      {
        uri = new BxesStringValue(attribute.Value);
      }
    }

    if (name is null) throw new XesReadException("Failed to read name for extension");
    if (prefix is null) throw new XesReadException("Failed to read prefix for extension");
    if (uri is null) throw new XesReadException("Failed to read uri for extension");
    
    writer.HandleEvent(new BxesLogMetadataExtensionEvent(new BxesExtension
    {
      Name = name,
      Prefix = prefix,
      Uri = uri
    }));
  }

  private void ReadGlobal(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    var element = XElement.Load(reader);

    if (element.Attribute(XesConstants.GlobalScopeAttribute) is not { } scopeAttribute) 
      throw new XesReadException("Failed to find scope attribute in global tag");

    var entityKind = scopeAttribute.Value switch
    {
      "event" => GlobalsEntityKind.Event,
      "trace" => GlobalsEntityKind.Trace,
      "log" => GlobalsEntityKind.Log,
      _ => throw new XesReadException($"Unknown scope attribute value {scopeAttribute.Value}")
    };

    var defaults = new List<AttributeKeyValue>();
    foreach (var child in element.Elements())
    {
      var (key, _, value) = XesReadUtil.ParseAttribute(child);
      defaults.Add(new AttributeKeyValue(new BxesStringValue(key), value));
    }
    
    writer.HandleEvent(new BxesLogMetadataGlobalEvent(new BxesGlobal
    {
      Kind = entityKind,
      Globals = defaults
    }));
  }
  
  private void ReadProperty(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    var (key, _, value) = XesReadUtil.ParseAttribute(XElement.Load(reader));
    writer.HandleEvent(new BxesLogMetadataPropertyEvent(new AttributeKeyValue(new BxesStringValue(key), value)));
  }

  private void ReadTrace(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    writer.HandleEvent(new BxesTraceVariantStartEvent(1, new List<AttributeKeyValue>()));

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
    writer.HandleEvent(new BxesEventEvent<FromXesBxesEvent>(FromXesBxesEventFactory.CreateFrom(element)));
  }
}