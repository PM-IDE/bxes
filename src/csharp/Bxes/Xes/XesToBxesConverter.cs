using System.Xml;
using Bxes.Models;
using Bxes.Models.Values;
using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Xes;

public class XesToBxesConverter : IBetweenFormatsConverter
{
  public void Convert(string filePath, string outputPath)
  {
    using var writer = new SingleFileBxesStreamWriterImpl<FromXesBxesEvent>(outputPath, BxesConstants.BxesVersion);

    using var fs = File.OpenRead(filePath);
    using var reader = XmlReader.Create(fs);

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
    var name = reader.GetAttribute(XesConstants.ClassifierNameAttribute);
    var keys = reader.GetAttribute(XesConstants.ClassifierKeysAttribute);

    if (name is null) throw new XesReadException("Failed to read name in classifier");
    if (keys is null) throw new XesReadException("Failed to read keys in classifier");

    writer.HandleEvent(new BxesLogMetadataClassifierEvent(new BxesClassifier
    {
      Name = new BxesStringValue(name),
      Keys = keys.Split().Select(key => new BxesStringValue(key)).ToList()
    }));
  }

  private void ReadExtension(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    var name = reader.GetAttribute(XesConstants.ExtensionNameAttribute);
    var prefix = reader.GetAttribute(XesConstants.ExtensionPrefixAttribute);
    var uri = reader.GetAttribute(XesConstants.ExtensionUriAttribute);

    if (name is null) throw new XesReadException("Failed to read name for extension");
    if (prefix is null) throw new XesReadException("Failed to read prefix for extension");
    if (uri is null) throw new XesReadException("Failed to read uri for extension");

    writer.HandleEvent(new BxesLogMetadataExtensionEvent(new BxesExtension
    {
      Name = new BxesStringValue(name),
      Prefix = new BxesStringValue(name),
      Uri = new BxesStringValue(name)
    }));
  }

  private void ReadGlobal(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    if (reader.GetAttribute(XesConstants.GlobalScopeAttribute) is not { } scope)
      throw new XesReadException("Failed to find scope attribute in global tag");

    var entityKind = scope switch
    {
      "event" => GlobalsEntityKind.Event,
      "trace" => GlobalsEntityKind.Trace,
      "log" => GlobalsEntityKind.Log,
      _ => throw new XesReadException($"Unknown scope attribute value {scope}")
    };

    var defaults = new List<AttributeKeyValue>();

    var subtreeReader = reader.ReadSubtree();
    //skip first global tag
    subtreeReader.Read();

    while (subtreeReader.Read())
    {
      if (reader.NodeType == XmlNodeType.Element)
      {
        if (XesReadUtil.ParseAttribute(subtreeReader) is { Key: { } key, Value.BxesValue: { } value })
        {
          defaults.Add(new AttributeKeyValue(new BxesStringValue(key), value));
        }
        else
        {
          throw new XesReadException("Failed to read global tag");
        }
      }
    }

    writer.HandleEvent(new BxesLogMetadataGlobalEvent(new BxesGlobal
    {
      Kind = entityKind,
      Globals = defaults
    }));
  }

  private void ReadProperty(XmlReader reader, SingleFileBxesStreamWriterImpl<FromXesBxesEvent> writer)
  {
    if (XesReadUtil.ParseAttribute(reader) is { Key: { } key, Value.BxesValue: { } value })
    {
      writer.HandleEvent(new BxesLogMetadataPropertyEvent(new AttributeKeyValue(new BxesStringValue(key), value)));
    }
    else
    {
      throw new XesReadException("Failed to read global tag");
    }
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
    if (FromXesBxesEventFactory.CreateFrom(reader) is { } @event)
    {
      writer.HandleEvent(new BxesEventEvent<FromXesBxesEvent>(@event));
    }
    else
    {
      throw new XesReadException("Failed to read xes event");
    }
  }
}