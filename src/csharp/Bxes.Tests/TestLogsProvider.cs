using Bxes.Models;
using Bxes.Writer;

namespace Bxes.Tests;

public static class TestLogsProvider
{
  public static IEventLog CreateSimpleTestLog1()
  {
    var variants = new List<ITraceVariant>();
    var variantsCount = Random.Shared.Next(10);
    for (var i = 0; i < variantsCount; ++i)
    {
      variants.Add(CreateRandomVariant());
    }

    return new InMemoryEventLog(123, GenerateRandomMetadata(), variants);
  }

  private static ITraceVariant CreateRandomVariant()
  {
    var eventsCount = Random.Shared.Next(100);
    var events = new List<InMemoryEventImpl>();

    for (var i = 0; i < eventsCount; ++i)
    {
      events.Add(CreateRandomEvent());
    }

    return new TraceVariantImpl((uint)Random.Shared.Next(10000), events);
  }

  private static InMemoryEventImpl CreateRandomEvent() =>
    new(
      Random.Shared.Next(10123123),
      new BxesStringValue(GenerateRandomString()),
      new BrafLifecycle(GenerateRandomEnum<BrafLifecycleValues>()),
      GenerateRandomAttributes()
    );

  private static IEnumerable<AttributeKeyValue> GenerateRandomAttributes() => 
    GenerateRandomMetadata();

  private static IEnumerable<AttributeKeyValue> GenerateRandomMetadata()
  {
    var metadata = new List<AttributeKeyValue>();
    var metadataCount = Random.Shared.Next(5);

    for (var i = 0; i < metadataCount; ++i)
    {
      metadata.Add(new(new BxesStringValue(GenerateRandomString()), GenerateRandomBxesValue()));
    }

    return metadata;
  }

  private static BxesValue GenerateRandomBxesValue()
  {
    var typeId = (TypeIds)Random.Shared.Next(Enum.GetValues<TypeIds>().Length);
    return typeId switch
    {
      TypeIds.I32 => new BxesInt32Value(Random.Shared.Next(10000)),
      TypeIds.I64 => new BxesInt64Value(Random.Shared.Next(10000)),
      TypeIds.U32 => new BxesUint32Value((uint)Random.Shared.Next(10000)),
      TypeIds.U64 => new BxesUint64Value((ulong)Random.Shared.Next(10000)),
      TypeIds.F32 => new BxesFloat32Value((float)(Random.Shared.Next(10000) + Random.Shared.NextDouble())),
      TypeIds.F64 => new BxesFloat64Value(Random.Shared.Next(10000) + Random.Shared.NextDouble()),
      TypeIds.String => new BxesStringValue(GenerateRandomString()),
      TypeIds.Bool => new BxesBoolValue(GenerateRandomBool()),
      TypeIds.Timestamp => new BxesInt64Value(Random.Shared.Next(10000)),
      TypeIds.BrafLifecycle => new BrafLifecycle(GenerateRandomEnum<BrafLifecycleValues>()),
      TypeIds.StandardLifecycle => new StandardXesLifecycle(GenerateRandomEnum<StandardLifecycleValues>()),
      TypeIds.Artifact => GenerateRandomArtifact(),
      TypeIds.Drivers => GenerateRandomDrivers(),
      TypeIds.Guid => GenerateGuidValue(),
      TypeIds.SoftwareEventType => new BxesSoftwareEventTypeValue(GenerateRandomEnum<SoftwareEventTypeValues>()),
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  private static BxesGuidValue GenerateGuidValue() => new(Guid.NewGuid());

  private static BxesDriversListValue GenerateRandomDrivers()
  {
    var artifactCount = Random.Shared.Next(100);
    var drivers = new List<BxesDriver>();

    for (var i = 0; i < artifactCount; ++i)
    {
      drivers.Add(GenerateRandomDriver());
    }

    return new BxesDriversListValue(drivers);
  }

  private static BxesDriver GenerateRandomDriver()
  {
    return new BxesDriver
    {
      Amount = Random.Shared.NextDouble(),
      Name = GenerateRandomString(),
      Type = GenerateRandomString()
    };
  }
  
  private static BxesArtifactModelsListValue GenerateRandomArtifact()
  {
    var artifactsCount = Random.Shared.Next(100);
    var models = new List<BxesArtifactItem>();

    for (var i = 0; i < artifactsCount; ++i)
    {
      models.Add(GenerateRandomArtifactItem());
    }

    return new BxesArtifactModelsListValue(models);
  }

  private static BxesArtifactItem GenerateRandomArtifactItem()
  {
    return new BxesArtifactItem
    {
      Instance = GenerateRandomString(),
      Transition = GenerateRandomString()
    };
  }

  private static bool GenerateRandomBool()
  {
    return Random.Shared.Next(2) == 1;
  }

  private static string GenerateRandomString()
  {
    var length = Random.Shared.Next(100);
    return new string(Enumerable.Range(0, length).Select(_ => GenerateRandomChar()).ToArray());
  }

  private static char GenerateRandomChar() => (char)('a' + Random.Shared.Next('z' - 'a' + 1));

  private static T GenerateRandomEnum<T>() where T : struct, Enum => 
     Enum.GetValues<T>()[Random.Shared.Next(Enum.GetValues<T>().Length)];
}