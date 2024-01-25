namespace Bxes.IntegrationTests;

public class DifferentImplXesToBxesTest
{
  private readonly List<IBxesImplExecutor> myExecutors = new()
  {
    new RusFicusImplExecutor(),
    new CSharpImplExecutor()
  };


  [Test]
  public void ExecuteTest()
  {
    var sourceFolder = Environment.GetEnvironmentVariable(EnvVars.SourceFolderPath);
    Assert.That(sourceFolder, Is.Not.Null);
    
    foreach (var directory in Directory.GetDirectories(sourceFolder))
    {
      foreach (var xesFile in Directory.EnumerateFiles(directory))
      {
        GoldBasedTestExecutor.Execute(myExecutors, xesFile);
      }
    }
  }
}