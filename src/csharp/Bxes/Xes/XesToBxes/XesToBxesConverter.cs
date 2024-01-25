using System.Xml;
using Bxes.Logging;
using Bxes.Models;
using Bxes.Models.Values;
using Bxes.Utils;
using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Xes.XesToBxes;

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
    var handler = new XesValuesPreprocessor(context.Writer);
    
    using (var reader = XmlReader.Create(fs))
    {
      while (reader.Read())
      {
        if (reader.NodeType == XmlNodeType.Element)
        {
          ProcessTag(reader, context, handler);
        }
      }
    }

    context.Writer.HandleEvent(new BxesRecalculateIndicesEvent());
    fs.Seek(0, SeekOrigin.Begin);
  }

  private static void ConvertXesToBxes(FileStream fs, XesReadContext context)
  {
    using var reader = XmlReader.Create(fs);
    var handler = new XesToBxesHandler(context.Writer);

    while (reader.Read())
    {
      if (reader.NodeType == XmlNodeType.Element)
      {
        ProcessTag(reader, context, handler);
      }
    }
  }

  private static void ProcessTag(XmlReader reader, XesReadContext context, XesElementHandlerBase handler)
  {
    switch (reader.Name)
    {
      case XesConstants.TraceTagName:
        handler.HandleTraceStart();
        ReadTrace(reader.ReadSubtree(), context, handler.HandleEvent);
        break;
      case XesConstants.ClassifierTagName:
        handler.HandleClassifier(ReadClassifier(reader));
        break;
      case XesConstants.ExtensionTagName:
        handler.HandleExtension(ReadExtension(reader));
        break;
      case XesConstants.GlobalTagName:
        if (ReadGlobalInternal(reader, context) is { } global)
        {
          handler.HandleGlobal(global);
        }
        else
        {
          context.Logger.LogWarning(reader, "Failed to read global tag");
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
          handler.HandleProperty(property);
        }
        else
        {
          context.Logger.LogWarning(reader, "Failed to read property tag");
        }
        
        break;
    }
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
      Prefix = new BxesStringValue(prefix),
      Uri = new BxesStringValue(uri)
    };
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

  private static AttributeKeyValue? ReadPropertyInternal(XmlReader reader, XesReadContext context)
  {
    if (XesReadUtil.ParseAttribute(reader, context) is { Key: { } key, Value.BxesValue: { } value })
    {
      return new AttributeKeyValue(new BxesStringValue(key), value);
    }

    return null;
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