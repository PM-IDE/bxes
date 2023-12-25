using System.Runtime.InteropServices;
using System.Xml;
using Bxes.Models;
using Bxes.Models.Values;

namespace Bxes.Xes;

public static class XesReadUtil
{
  public static (string Key, string Value, BxesValue ParsedValue) ParseAttribute(XmlReader reader)
  {
    var key = reader.GetAttribute(XesConstants.KeyAttributeName) ?? throw new XesReadException("No key at attribute");
    var value = reader.GetAttribute(XesConstants.ValueAttributeName) ?? throw new XesReadException("No value at attribute");

    if (reader.Name is XesConstants.ListTagName)
    {
      switch (key)
      {
        case XesConstants.ArtifactMoves:
          return (key, value, ReadArtifact(reader));
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

    return (key, value, bxesValue);
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
            var (artifactKey, artifactValue, _) = ParseAttribute(reader);
            switch (artifactKey)
            {
              case XesConstants.ArtifactItemInstance:
                instance = artifactValue;
                break;
              case XesConstants.ArtifactItemTransition:
                transition = artifactValue;
                break;
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