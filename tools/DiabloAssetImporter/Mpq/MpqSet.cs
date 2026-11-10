using War3Net.IO.Mpq;

namespace DiabloAssetImporter.Mpq;

/// <summary>
/// MPQ 하나(War3Net 감싸기). 원본은 <c>FileAccess.Read</c>·<c>FileShare.Read</c> 로만 연다. 읽기는 주 스레드에서 차례로(1.4 "MPQ 읽기와 스레드").
/// </summary>
internal sealed class MpqSource : IDisposable
{
    private readonly FileStream _file;
    private readonly MpqArchive _archive;

    private MpqSource(string path, FileStream file, MpqArchive archive)
    {
        Path = path;
        _file = file;
        _archive = archive;
        Name = System.IO.Path.GetFileName(path).ToLowerInvariant();
    }

    public string Path { get; }
    /// <summary>manifest 에 적는 이름(소문자 파일 이름 — <c>diabdat.mpq</c>).</summary>
    public string Name { get; }

    public static MpqSource Open(string path)
    {
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            var a = MpqArchive.Open(fs, loadListFile: false);
            return new MpqSource(path, fs, a);
        }
        catch
        {
            fs.Dispose();
            throw;
        }
    }

    public bool Exists(string name) => _archive.FileExists(name);

    /// <summary>항목 바이트(복호·압축 풀기는 War3Net). 없으면 null.</summary>
    public byte[]? Read(string name)
    {
        if (!_archive.FileExists(name)) return null;
        using var s = _archive.OpenFile(name);
        using var ms = new MemoryStream((int)Math.Max(0, s.Length));
        s.CopyTo(ms);
        return ms.ToArray();
    }

    public void Dispose()
    {
        _archive.Dispose();
        _file.Dispose();
    }
}
