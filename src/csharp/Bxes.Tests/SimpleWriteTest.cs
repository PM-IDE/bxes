using Bxes.Models;
using Bxes.Reader;
using Bxes.Writer;

namespace Bxes.Tests;

[TestFixture]
public class SimpleWriteTest
{
  [Test]
  public void SimpleTest1()
  {
    ExecuteSimpleTest(TestLogsProvider.CreateSimpleTestLog1());
  }

  private void ExecuteSimpleTest(IEventLog log)
  {
    ExecuteTestWithTempFile(testPath =>
    {
      new SingleFileBxesWriter().WriteAsync(log, testPath).GetAwaiter().GetResult();
      var readLog = new SingleFileBxesReader().Read(testPath);

      Assert.That(log.Equals(readLog));
    });
  }

  private void ExecuteTestWithTempFile(Action<string> testAction)
  {
    var tempFilePath = Path.GetTempFileName();

    try
    {
      testAction(tempFilePath);
    }
    finally
    {
      File.Delete(tempFilePath);
    }
  }
}

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
      new BXesStringValue(GenerateRandomString()),
      new BrafLifecycle(GenerateRandomBrafLifecycle()),
      GenerateRandomAttributes()
    );

  private static EventAttributesImpl GenerateRandomAttributes() => GenerateRandomMetadata();

  private static EventLogMetadataImpl GenerateRandomMetadata()
  {
    var metadata = new EventLogMetadataImpl();
    var metadataCount = Random.Shared.Next(5);

    for (var i = 0; i < metadataCount; ++i)
    {
      metadata[new BXesStringValue(GenerateRandomString())] = GenerateRandomBxesValue();
    }

    return metadata;
  }

  private static BxesValue GenerateRandomBxesValue()
  {
    var typeId = Random.Shared.Next(0, 11);
    return typeId switch
    {
      TypeIds.I32 => new BxesInt32Value(Random.Shared.Next(10000)),
      TypeIds.I64 => new BxesInt64Value(Random.Shared.Next(10000)),
      TypeIds.U32 => new BxesUint32Value((uint)Random.Shared.Next(10000)),
      TypeIds.U64 => new BxesUint64Value((ulong)Random.Shared.Next(10000)),
      TypeIds.F32 => new BxesFloat32Value((float)(Random.Shared.Next(10000) + Random.Shared.NextDouble())),
      TypeIds.F64 => new BxesFloat64Value(Random.Shared.Next(10000) + Random.Shared.NextDouble()),
      TypeIds.String => new BXesStringValue(GenerateRandomString()),
      TypeIds.Bool => new BxesBoolValue(GenerateRandomBool()),
      TypeIds.Timestamp => new BxesInt64Value(Random.Shared.Next(10000)),
      TypeIds.BrafLifecycle => new BrafLifecycle(GenerateRandomBrafLifecycle()),
      TypeIds.StandardLifecycle => new StandardXesLifecycle(GenerateStandardLifecycleValues()),
      _ => throw new ArgumentOutOfRangeException()
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

  private static BrafLifecycleValues GenerateRandomBrafLifecycle() => (BrafLifecycleValues)Random.Shared.Next(20);

  private static StandardLifecycleValues GenerateStandardLifecycleValues() =>
    (StandardLifecycleValues)Random.Shared.Next(14);
}