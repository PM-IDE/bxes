using System.Diagnostics;

namespace Bxes.IntegrationTests;

public class CSharpImplExecutor : ExecutorBase
{
  public override string Name => "csharp";


  protected override Process CreateProcess(string xesLogPath, string bxesLogPath) => new()
  {
    StartInfo = new ProcessStartInfo
    {
      FileName = Environment.GetEnvironmentVariable(EnvVars.CSharpExecutablePath),
      Arguments = $"xes-to-bxes -path {xesLogPath} -output-path {bxesLogPath}"
    }
  };
}