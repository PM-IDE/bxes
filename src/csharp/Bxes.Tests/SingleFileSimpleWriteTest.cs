using Bxes.Models;
using Bxes.Reader;
using Bxes.Writer;

namespace Bxes.Tests;

[TestFixture]
public class SingleFileSimpleWriteTest
{
  [Test]
  public void SimpleTest1()
  {
    ExecuteSimpleTest(TestLogsProvider.CreateSimpleTestLog1());
  }

  private static void ExecuteSimpleTest(IEventLog log)
  {
    TestUtils.ExecuteTestWithTempFile(log, testPath =>
    {
      new SingleFileBxesWriter().WriteAsync(log, testPath).GetAwaiter().GetResult();
      return new SingleFileBxesReader().Read(testPath);
    });
  }
}