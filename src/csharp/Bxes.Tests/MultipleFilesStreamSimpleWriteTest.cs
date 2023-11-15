using Bxes.Models;
using Bxes.Reader;
using Bxes.Writer;

namespace Bxes.Tests;

[TestFixture]
public class MultipleFilesStreamSimpleWriteTest
{
  [Test]
  public void SimpleTest1()
  {
    ExecuteSimpleTest(TestLogsProvider.CreateSimpleTestLog1());
  }

  private void ExecuteSimpleTest(IEventLog log)
  {
    TestUtils.ExecuteWithTempDirectory(testDirectory =>
    {
      TestUtils.ExecuteTestWithLog(log, () =>
      {
        using (var writer = new MultipleFilesBxesStreamWriterImpl<IEvent>(testDirectory, log.Version))
        {
          foreach (var streamEvent in log.ToEventsStream())
          {
            writer.HandleEvent(streamEvent);
          } 
        }

        return new MultiFileBxesReader().Read(testDirectory);
      });
    });
  }
}