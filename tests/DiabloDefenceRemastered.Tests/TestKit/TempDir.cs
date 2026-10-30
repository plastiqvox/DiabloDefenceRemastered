namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>시험용 임시 폴더(시스템 임시 폴더 아래, 끝나면 지움). 저장소 안에는 만들지 않는다.</summary>
public sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ddr-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>상대 경로('/' 구분)에 글을 쓰고 전체 경로를 돌려준다.</summary>
    public string Write(string relative, string text)
    {
        var full = System.IO.Path.Combine(Path, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
        return full;
    }

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
