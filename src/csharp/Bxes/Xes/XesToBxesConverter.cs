using System.Xml;
using System.Xml.Linq;
using Bxes.Models;
using Bxes.Models.Values;
using Bxes.Writer;
using Bxes.Writer.Stream;

namespace Bxes.Xes;

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