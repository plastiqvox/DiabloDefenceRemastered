using DiabloAssetImporter.Cli;
using DiabloAssetImporter.Pipeline;

namespace DiabloAssetImporter.Tests.TestKit;

/// <summary>시험용 임시 폴더(시스템 임시 폴더 아래, 저장소 밖). 끝나면 지운다.</summary>
internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ddr-imp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }
    public string Sub(string name) => Directory.CreateDirectory(System.IO.Path.Combine(Path, name)).FullName;

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

/// <summary>저장소의 실제 표·정의 폴더(시험 출력 폴더에서 위로 올라가 찾음).</summary>
internal static class Repo
{
    public static string Root { get; } = RepoPaths.FindRoot(AppContext.BaseDirectory) ?? throw new InvalidOperationException("저장소 뿌리를 못 찾음");
    public static string TableDir => RepoPaths.TableDir(Root);
    public static string DataDir => RepoPaths.DataDir(Root);

    public static ImportTable Table() => ImportTable.Load(TableDir).Table ?? throw new InvalidOperationException("표를 못 읽음");

    /// <summary>실제 표를 시험 폴더에 복사(고쳐 쓰는 시험용).</summary>
    public static string CopyTable(string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var f in ImportTable.FileNames) File.Copy(Path.Combine(TableDir, f), Path.Combine(dir, f), true);
        return dir;
    }
}

/// <summary>합성 MPQ + 실제(또는 바꾼) 표·정의로 가져오기를 돌리는 판.</summary>
internal sealed class ImportFixture : IDisposable
{
    private readonly TempDir _tmp = new();

    public ImportFixture(Func<Dictionary<string, byte[]>, Dictionary<string, byte[]>>? edit = null, string? tableDir = null)
    {
        TableDir = tableDir ?? Repo.CopyTable(_tmp.Sub("table"));
        var table = ImportTable.Load(TableDir).Table!;
        Files = SynthSources.For(table, MapDataReader.Read(Repo.DataDir));
        if (edit is not null) Files = edit(Files);
        SourceDir = _tmp.Sub("src");
        Diabdat = Path.Combine(SourceDir, "DIABDAT.MPQ");
        SynthMpq.Write(Diabdat, Files);
        Out = _tmp.Sub("out");
    }

    public string TableDir { get; }
    public Dictionary<string, byte[]> Files { get; }
    public string SourceDir { get; }
    public string Diabdat { get; }
    public string Out { get; }
    public string Tmp => _tmp.Path;
    public StringWriter Log { get; } = new();

    public ImportRequest Request(string? outDir = null) => new()
    {
        Diabdat = Diabdat, OutDir = outDir ?? Out, TableDir = TableDir, DataDir = Repo.DataDir, Jobs = 2, Log = Log,
    };

    public ImportResult Run(Func<ImportRequest, ImportRequest>? tweak = null, string? outDir = null)
    {
        var req = Request(outDir);
        return Importer.Run(tweak is null ? req : tweak(req));
    }

    public string ManifestText(string? outDir = null) => File.ReadAllText(Path.Combine(outDir ?? Out, AssetFormat.ManifestPath));

    public void Dispose() => _tmp.Dispose();
}
