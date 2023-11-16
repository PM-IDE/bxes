using Bxes.Models;
using Bxes.Reader;
using Bxes.Writer;

namespace Bxes.Tests;

[TestFixture]
public class MultipleFilesSimpleWriteTest
{
  [Test]
  public void SimpleTest1()
  {
    ExecuteSimpleTest(TestLogsProvider.CreateSimpleTestLog1());
  }

  private static void ExecuteSimpleTest(IEventLog log)
  {
    TestUtils.ExecuteWithTempDirectory(testDirectory =>
    {
      TestUtils.ExecuteTestWithLog(log, () =>
      {
        new MultipleFilesBxesWriter().WriteAsync(log, testDirectory).GetAwaiter().GetResult();
        return new MultiFileBxesReader().Read(testDirectory);
      });
    });
  }
}