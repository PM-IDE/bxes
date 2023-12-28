using System.Xml;
using Bxes.Logging;
using Bxes.Models;
using Bxes.Models.Values;
using Bxes.Utils;
using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Xes;

public readonly ref struct XesReadContext(SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer, ILogger logger)
{
  public ILogger Logger { get; } = logger;
  public SingleFileBxesStreamWriterImpl<FromXesBxesEvent> Writer { get; } = writer;
  public Dictionary<string, BxesValue> EventDefaults { get; } = new();
}

public class XesToBxesConverter : IBetweenFormatsConverter
{
  public void Convert(string filePath, string outputPath, ILogger logger)
  {
    using var writer = new SingleFileBxesStreamWriterImpl<FromXesBxesEvent>(outputPath, BxesConstants.BxesVersion);

    using var fs = File.OpenRead(filePath);
    using var reader = XmlReader.Create(fs);

    var context = new XesReadContext(writer, logger);

    while (reader.Read())
    {
      if (reader.NodeType == XmlNodeType.Element)
      {
        ProcessTag(reader, context);
      }
    }
  }

  private void ProcessTag(XmlReader reader, XesReadContext context)
  {
    switch (reader.Name)
    {
      case XesConstants.TraceTagName:
        ReadTrace(reader, context);
        break;
      case XesConstants.ClassifierTagName:
        ReadClassifier(reader, context.Writer);
        break;
      case XesConstants.ExtensionTagName:
        ReadExtension(reader, context.Writer);
        break;
      case XesConstants.GlobalTagName:
        ReadGlobal(reader, context);
        break;
      case XesConstants.StringTagName:
      case XesConstants.DateTagName:
      case XesConstants.IntTagName:
      case XesConstants.FloatTagName:
      case XesConstants.BoolTagName:
        ReadProperty(reader, context);
        break;
    }
  }

  private static void ReadClassifier(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    var name = reader.GetAttribute(XesConstants.ClassifierNameAttribute);
    var keys = reader.GetAttribute(XesConstants.ClassifierKeysAttribute);

    if (name is null) throw new XesReadException(reader, "Failed to read name in classifier");
    if (keys is null) throw new XesReadException(reader, "Failed to read keys in classifier");

    writer.HandleEvent(new BxesLogMetadataClassifierEvent(new BxesClassifier
    {
      Name = new BxesStringValue(name),
      Keys = keys.Split().Select(key => new BxesStringValue(key)).ToList()
    }));
  }

  private static void ReadExtension(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    var name = reader.GetAttribute(XesConstants.ExtensionNameAttribute);
    var prefix = reader.GetAttribute(XesConstants.ExtensionPrefixAttribute);
    var uri = reader.GetAttribute(XesConstants.ExtensionUriAttribute);

    if (name is null) throw new XesReadException(reader, "Failed to read name for extension");
    if (prefix is null) throw new XesReadException(reader, "Failed to read prefix for extension");
    if (uri is null) throw new XesReadException(reader, "Failed to read uri for extension");

    writer.HandleEvent(new BxesLogMetadataExtensionEvent(new BxesExtension
    {
      Name = new BxesStringValue(name),
      Prefix = new BxesStringValue(name),
      Uri = new BxesStringValue(name)
    }));
  }

  private static void ReadGlobal(XmlReader reader, XesReadContext context)
  {
    if (reader.GetAttribute(XesConstants.GlobalScopeAttribute) is not { } scope)
      throw new XesReadException(reader, "Failed to find scope attribute in global tag");

    var entityKind = scope switch
    {
      "event" => GlobalsEntityKind.Event,
      "trace" => GlobalsEntityKind.Trace,
      "log" => GlobalsEntityKind.Log,
      _ => throw new XesReadException(reader, $"Unknown scope attribute value {scope}")
    };

    var defaults = new List<AttributeKeyValue>();

    var subtreeReader = reader.ReadSubtree();
    //skip first global tag
    subtreeReader.Read();

    while (subtreeReader.Read())
    {
      if (reader.NodeType == XmlNodeType.Element)
      {
        if (XesReadUtil.ParseAttribute(subtreeReader, context) is { Key: { } key, Value.BxesValue: { } value })
        {
          defaults.Add(new AttributeKeyValue(new BxesStringValue(key), value));

          if (entityKind == GlobalsEntityKind.Event)
          {
            context.EventDefaults[key] = value;
          }
        }
        else
        {
          context.Logger.LogWarning(reader, "Failed to read global tag");
        }
      }
    }

    context.Writer.HandleEvent(new BxesLogMetadataGlobalEvent(new BxesGlobal
    {
      Kind = entityKind,
      Globals = defaults
    }));
  }

  private static void ReadProperty(XmlReader reader, XesReadContext context)
  {
    if (XesReadUtil.ParseAttribute(reader, context) is { Key: { } key, Value.BxesValue: { } value })
    {
      var @event = new BxesLogMetadataPropertyEvent(new AttributeKeyValue(new BxesStringValue(key), value));
      context.Writer.HandleEvent(@event);
    }
    else
    {
      context.Logger.LogWarning(reader, "Failed to read property tag");
    }
  }

  private void ReadTrace(XmlReader reader, XesReadContext context)
  {
    context.Writer.HandleEvent(new BxesTraceVariantStartEvent(1, new List<AttributeKeyValue>()));

    while (reader.Read())
    {
      if (reader is { NodeType: XmlNodeType.Element, Name: XesConstants.EventTagName })
      {
        ReadEvent(reader, context);
      }
    }
  }

  private static void ReadEvent(XmlReader reader, XesReadContext context)
  {
    if (FromXesBxesEventFactory.CreateFrom(reader, context) is { } @event)
    {
      context.Writer.HandleEvent(new BxesEventEvent<FromXesBxesEvent>(@event));
    }
    else
    {
      context.Logger.LogWarning(reader, "Failed to read xes event");
    }
  }
}