using System.Xml;
using Bxes.Logging;
using Bxes.Models;
using Bxes.Models.Values;
using Bxes.Utils;
using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Xes;

public readonly struct XesReadContext(SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer, ILogger logger)
{
  public ILogger Logger { get; } = logger;
  public SingleFileBxesStreamWriterImpl<FromXesBxesEvent> Writer { get; } = writer;
  public Dictionary<string, BxesValue> EventDefaults { get; } = new();
}

public class XesToBxesConverter(ILogger logger, bool doIndicesPreprocessing) : IBetweenFormatsConverter
{
  public void Convert(string filePath, string outputPath)
  {
    using var writer = new SingleFileBxesStreamWriterImpl<FromXesBxesEvent>(outputPath, BxesConstants.BxesVersion);
    using var fs = File.OpenRead(filePath);

    var context = new XesReadContext(writer, logger);

    if (doIndicesPreprocessing)
    {
      ExtractValuesAndKeyValues(fs, context);
    }

    ConvertXesToBxes(fs, context);
  }

  private static void ExtractValuesAndKeyValues(FileStream fs, XesReadContext context)
  {
    using (var reader = XmlReader.Create(fs))
    {
      while (reader.Read())
      {
        if (reader.NodeType == XmlNodeType.Element)
        {
          PreprocessTag(reader, context);
        }
      }
    }

    context.Writer.HandleEvent(new BxesRecalculateIndicesEvent());
    fs.Seek(0, SeekOrigin.Begin);
  }

  private static void ConvertXesToBxes(FileStream fs, XesReadContext context)
  {
    using var reader = XmlReader.Create(fs);
    
    while (reader.Read())
    {
      if (reader.NodeType == XmlNodeType.Element)
      {
        ProcessTag(reader, context);
      }
    }
  }

  private static void PreprocessTag(XmlReader reader, XesReadContext context)
  {
    switch (reader.Name)
    {
      case XesConstants.TraceTagName:
        ReadTrace(reader, context, bxesEvent =>
        {
          context.Writer.HandleEvent(new BxesValueEvent(new BxesStringValue(bxesEvent.Name)));

          foreach (var attribute in bxesEvent.Attributes)
          {
            context.Writer.HandleEvent(new BxesKeyValueEvent(attribute));
          }
        });
        break;
      case XesConstants.ClassifierTagName:
        var classifier = ReadClassifier(reader);
        
        context.Writer.HandleEvent(new BxesValueEvent(classifier.Name));
        foreach (var value in classifier.Keys)
        {
          context.Writer.HandleEvent(new BxesValueEvent(value));
        }
        
        break;
      case XesConstants.ExtensionTagName:
        var extension = ReadExtension(reader);
        context.Writer.HandleEvent(new BxesValueEvent(extension.Name));
        context.Writer.HandleEvent(new BxesValueEvent(extension.Prefix));
        context.Writer.HandleEvent(new BxesValueEvent(extension.Uri));

        break;
      case XesConstants.GlobalTagName:
        if (ReadGlobalInternal(reader, context) is { } global)
        {
          foreach (var attribute in global.Globals)
          {
            context.Writer.HandleEvent(new BxesKeyValueEvent(attribute));
          } 
        }

        break;
      case XesConstants.StringTagName:
      case XesConstants.DateTagName:
      case XesConstants.IntTagName:
      case XesConstants.FloatTagName:
      case XesConstants.BoolTagName:
      case XesConstants.IdTagName:
        if (ReadPropertyInternal(reader, context) is { } property)
        {
          context.Writer.HandleEvent(new BxesKeyValueEvent(property));
        }
        
        break;
    }
  }

  private static void ProcessTag(XmlReader reader, XesReadContext context)
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
      case XesConstants.IdTagName:
        ReadProperty(reader, context);
        break;
    }
  }

  private static void ReadClassifier(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    writer.HandleEvent(new BxesLogMetadataClassifierEvent(ReadClassifier(reader)));
  }

  private static BxesClassifier ReadClassifier(XmlReader reader)
  {
    var name = reader.GetAttribute(XesConstants.ClassifierNameAttribute);
    var keys = reader.GetAttribute(XesConstants.ClassifierKeysAttribute);

    if (name is null) throw new XesReadException(reader, "Failed to read name in classifier");
    if (keys is null) throw new XesReadException(reader, "Failed to read keys in classifier");

    return new BxesClassifier
    {
      Name = new BxesStringValue(name),
      Keys = keys.Split().Select(key => new BxesStringValue(key)).ToList()
    };
  }

  private static void ReadExtension(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    writer.HandleEvent(new BxesLogMetadataExtensionEvent(ReadExtension(reader)));
  }

  private static BxesExtension ReadExtension(XmlReader reader)
  {
    var name = reader.GetAttribute(XesConstants.ExtensionNameAttribute);
    var prefix = reader.GetAttribute(XesConstants.ExtensionPrefixAttribute);
    var uri = reader.GetAttribute(XesConstants.ExtensionUriAttribute);

    if (name is null) throw new XesReadException(reader, "Failed to read name for extension");
    if (prefix is null) throw new XesReadException(reader, "Failed to read prefix for extension");
    if (uri is null) throw new XesReadException(reader, "Failed to read uri for extension");

    return new BxesExtension
    {
      Name = new BxesStringValue(name),
      Prefix = new BxesStringValue(name),
      Uri = new BxesStringValue(name)
    };
  }

  private static void ReadGlobal(XmlReader reader, XesReadContext context)
  {
    context.Writer.HandleEvent(new BxesLogMetadataGlobalEvent(ReadGlobalInternal(reader, context)));
  }

  private static BxesGlobal ReadGlobalInternal(XmlReader reader, XesReadContext context)
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

    return new BxesGlobal
    {
      Kind = entityKind,
      Globals = defaults
    };
  }

  private static void ReadProperty(XmlReader reader, XesReadContext context)
  {
    if (ReadPropertyInternal(reader, context) is { } attributeKeyValue)
    {
      var @event = new BxesLogMetadataPropertyEvent(attributeKeyValue);
      context.Writer.HandleEvent(@event);
    }
    else
    {
      context.Logger.LogWarning(reader, "Failed to read property tag");
    }
  }

  private static AttributeKeyValue? ReadPropertyInternal(XmlReader reader, XesReadContext context)
  {
    if (XesReadUtil.ParseAttribute(reader, context) is { Key: { } key, Value.BxesValue: { } value })
    {
      return new AttributeKeyValue(new BxesStringValue(key), value);
    }

    return null;
  }

  private static void ReadTrace(XmlReader reader, XesReadContext context)
  {
    context.Writer.HandleEvent(new BxesTraceVariantStartEvent(1, new List<AttributeKeyValue>()));
    
    ReadTrace(reader, context, @event =>
    {
      context.Writer.HandleEvent(new BxesEventEvent<FromXesBxesEvent>(@event));
    });
  }

  private static void ReadTrace(
    XmlReader reader, XesReadContext context, Action<FromXesBxesEvent> eventHandler)
  {
    while (reader.Read())
    {
      if (reader is { NodeType: XmlNodeType.Element, Name: XesConstants.EventTagName })
      {
        if (ReadEvent(reader, context) is { } fromXesBxesEvent)
        {
          eventHandler(fromXesBxesEvent);
        }
      }
    }
  }

  private static FromXesBxesEvent? ReadEvent(XmlReader reader, XesReadContext context)
  {
    if (FromXesBxesEventFactory.CreateFrom(reader, context) is { } @event)
    {
      return @event;
    }

    context.Logger.LogWarning(reader, "Failed to read xes event");
    return null;
  }
}