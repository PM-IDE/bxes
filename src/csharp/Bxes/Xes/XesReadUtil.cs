using System.Diagnostics;
using System.Xml;
using Bxes.Models;
using Bxes.Models.Values;

namespace Bxes.Xes;

public readonly struct AttributeValueParseResult
{
  public required string Value { get; init; }
  public required BxesValue BxesValue { get; init; }


  public static AttributeValueParseResult Create(string value, BxesValue parsedValue) => new()
  {
    Value = value,
    BxesValue = parsedValue
  };
}

public readonly struct AttributeParseResult
{
  public required string? Key { get; init; }
  public required AttributeValueParseResult? Value { get; init; }

  public bool IsEmpty => Key is null && Value is null;
  

  public static AttributeParseResult Empty() => new()
  {
    Key = null,
    Value = null
  };

  public static AttributeParseResult KeyValue(string key, AttributeValueParseResult value) => new()
  {
    Key = key,
    Value = value
  };

  public static AttributeParseResult OnlyValue(AttributeValueParseResult value) => new()
  {
    Key = null,
    Value = value
  };
}

public static class XesReadUtil
{
  public static AttributeParseResult ParseAttribute(XmlReader reader)
  {
    var key = reader.GetAttribute(XesConstants.KeyAttributeName);
    var value = reader.GetAttribute(XesConstants.ValueAttributeName);

    if (key is null && value is null) return AttributeParseResult.Empty();
    if (key is { } && value is null) throw new XesReadException("Attribute contains key and no value");

    Debug.Assert(value is { });

    if (reader.Name is XesConstants.ListTagName)
    {
      switch (key)
      {
        case XesConstants.ArtifactMoves:
          return AttributeParseResult.KeyValue(key, AttributeValueParseResult.Create(value, ReadArtifact(reader)));
        case XesConstants.CostDrivers:
          throw new NotImplementedException();
        default:
          throw new XesReadException($"Failed to parse list {key}");
      }
    }
    
    BxesValue bxesValue = reader.Name switch
    {
      XesConstants.StringTagName => new BxesStringValue(value),
      XesConstants.DateTagName => new BxesTimeStampValue(DateTime.Parse(value).Ticks),
      XesConstants.IntTagName => new BxesInt64Value(long.Parse(value)),
      XesConstants.FloatTagName => new BxesFloat64Value(double.Parse(value)),
      XesConstants.BoolTagName => new BxesBoolValue(bool.Parse(value)),
      XesConstants.IdTagName => new BxesGuidValue(Guid.Parse(value)),
      _ => throw new XesReadException($"Failed to create value for type {reader.Name}")
    };

    return AttributeParseResult.KeyValue(key, AttributeValueParseResult.Create(value, bxesValue));
  }

  private static BxesArtifactModelsListValue ReadArtifact(XmlReader reader)
  {
    var items = new List<BxesArtifactItem>();
    while (reader.Read())
    {
      if (reader.NodeType is not XmlNodeType.Element || reader.Name != XesConstants.ValuesTagName) continue;

      while (reader.Read())
      {
        if (reader.NodeType is not XmlNodeType.Element || reader.Name != XesConstants.StringTagName) continue;

        if (reader.GetAttribute(XesConstants.ArtifactItemModel) is not { } model)
        {
          throw new XesReadException($"{XesConstants.ArtifactItemModel} was not specified");
        }

        string? instance = null;
        string? transition = null!;

        var subtreeReader = reader.ReadSubtree();

        while (subtreeReader.Read())
        {
          if (reader.NodeType is XmlNodeType.Element && reader.Name == XesConstants.StringTagName)
          {
            var parsedAttribute = ParseAttribute(reader);
            if (parsedAttribute is { Key: { }, Value: { } value })
            {
              switch (parsedAttribute.Key)
              {
                case XesConstants.ArtifactItemInstance:
                  instance = value.Value;
                  break;
                case XesConstants.ArtifactItemTransition:
                  transition = value.Value;
                  break;
              } 
            }
            else
            {
              //todo: replace with logging
              throw new XesReadException("Failed to read artifact attribute");
            }
          }
        }

        if (instance is null || transition is null)
        {
          throw new XesReadException($"Expected not null instance and transition, got {instance}, {transition}");
        }

        items.Add(new BxesArtifactItem
        {
          Model = model,
          Instance = instance,
          Transition = transition
        });
      }

      break;
    }

    return new BxesArtifactModelsListValue(items);
  }
}