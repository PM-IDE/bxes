namespace Bxes.Utils;

public interface IPathContainer : IDisposable
{
  string Path { get; }
}

public readonly struct TempFilePathContainer() : IPathContainer
{
  public string Path { get; } = System.IO.Path.GetTempFileName();


  public void Dispose()
  {
    File.Delete(Path);
  }
}

public readonly struct TempFolderContainer() : IPathContainer
{
  public string Path { get; } = Directory.CreateTempSubdirectory().FullName;


  public void Dispose()
  {
    Directory.Delete(Path, true);
  }
}