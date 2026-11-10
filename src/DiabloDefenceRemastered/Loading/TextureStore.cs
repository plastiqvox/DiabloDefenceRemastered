using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered.Loading;

/// <summary>
/// 텍스처 읽기·내리기 (설계서 7.5, 소유 WP5). 자원 뿌리 기준 상대 경로(<c>d1/monsters/zombie@bluered.png</c>)로 찾는다.
/// <list type="bullet">
/// <item>읽기는 <c>Texture2D.FromStream(device, stream, colorProcessor)</c> 하나 — 색 처리에서 색 키(grid)를 (0,0,0,0) 으로, 그다음 premultiplied(rgb × a)로 바꾼다.
/// 해독과 GPU 올리기를 같이 하므로 <b>주 스레드에서 차례로</b> 부른다(DesktopGL 교착 방지 — 7.5).</item>
/// <item>액트 단위로 읽고 내린다(<see cref="Retain"/> — 이번 액트에 쓰지 않는 것을 내림). 방 이동 때는 읽지 않는다.</item>
/// <item>메모리 어림 <see cref="VramBytes"/> = 폭 × 높이 × 4 의 합(밉맵 없음) — 로그 <c>vram_mb</c>(R7).</item>
/// </list>
/// 그림 배율 s(7.1) = manifest <c>upscale</c> 가 <c>done</c> 이면 <c>output_scale</c>, 아니면 1. manifest 가 없으면(자원 없는 개발 실행) 1.
/// </summary>
public sealed class TextureStore(GraphicsDevice device, string assetRoot) : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SpriteMeta> _metas = new(StringComparer.Ordinal);
    private readonly HashSet<string> _missing = new(StringComparer.Ordinal);
    private AssetManifest? _manifest;
    private bool _manifestRead;

    public string AssetRoot { get; } = assetRoot;
    public GraphicsDevice Device { get; } = device;

    /// <summary>manifest(없으면 null).</summary>
    public AssetManifest? Manifest
    {
        get
        {
            if (_manifestRead) return _manifest;
            _manifestRead = true;
            var path = Path.Combine(AssetRoot, AssetFormat.ManifestPath);
            try { _manifest = File.Exists(path) ? AssetManifest.Read(path) : null; }
            catch (Exception e) when (e is IOException or System.Text.Json.JsonException) { _manifest = null; }
            return _manifest;
        }
    }

    /// <summary>그림 배율 s(7.1).</summary>
    public int ArtScale => Manifest?.Upscale.ArtScale ?? 1;

    /// <summary>파일 하나의 그림 픽셀 배율(manifest 파일 줄 <c>scale</c>, 없으면 1) — grid·exe 그림.</summary>
    public int FileScale(string rel) => Manifest is { } m && m.TryGetFile(rel, out var f) ? f.Scale : 1;

    public int Count => _textures.Count;

    public long VramBytes
    {
        get
        {
            long sum = 0;
            foreach (var t in _textures.Values) sum += (long)t.Width * t.Height * 4;
            return sum;
        }
    }

    public bool Exists(string rel) => File.Exists(Full(rel));

    /// <summary>읽은 것(없으면 null). 읽지 않는다.</summary>
    public Texture2D? Peek(string rel) => _textures.GetValueOrDefault(rel);

    /// <summary>읽기(이미 있으면 그것). 파일이 없으면 null(한 번만 확인 — 같은 빈자리를 프레임마다 디스크에서 찾지 않게).</summary>
    public Texture2D? Load(string rel, Rgb? colorKey = null)
    {
        if (_textures.TryGetValue(rel, out var t)) return t;
        if (_missing.Contains(rel)) return null;
        var full = Full(rel);
        if (!File.Exists(full)) { _missing.Add(rel); return null; }
        using var stream = File.OpenRead(full);
        t = Texture2D.FromStream(Device, stream, data => Premultiply(data, colorKey));
        t.Name = rel;
        _textures[rel] = t;
        return t;
    }

    /// <summary>아틀라스 메타(그 페이지들이 같이 쓰는 <c>&lt;이름&gt;.json</c>). 없으면 null.</summary>
    public SpriteMeta? Meta(string metaRel)
    {
        if (_metas.TryGetValue(metaRel, out var m)) return m;
        if (_missing.Contains(metaRel)) return null;
        var full = Full(metaRel);
        if (!File.Exists(full)) { _missing.Add(metaRel); return null; }
        m = SpriteMeta.Read(full);
        _metas[metaRel] = m;
        return m;
    }

    /// <summary>이 목록에 없는 텍스처를 내린다(액트가 바뀔 때 — 7.5). 메타는 작아서 둔다.</summary>
    public void Retain(IReadOnlySet<string> keep)
    {
        List<string>? drop = null;
        foreach (var key in _textures.Keys)
            if (!keep.Contains(key)) (drop ??= []).Add(key);
        if (drop is null) return;
        foreach (var key in drop)
        {
            _textures[key].Dispose();
            _textures.Remove(key);
        }
    }

    /// <summary>
    /// 색 처리(7.5): 색 키와 같은 픽셀 → (0,0,0,0), 그 밖은 rgb × a(premultiplied). 데이터는 RGBA 바이트.
    /// </summary>
    public static void Premultiply(byte[] data, Rgb? colorKey)
    {
        for (var i = 0; i + 3 < data.Length; i += 4)
        {
            if (colorKey is { } k && data[i] == k.R && data[i + 1] == k.G && data[i + 2] == k.B)
            {
                data[i] = data[i + 1] = data[i + 2] = data[i + 3] = 0;
                continue;
            }
            var a = data[i + 3];
            if (a == 255) continue;
            data[i] = (byte)((data[i] * a + 127) / 255);
            data[i + 1] = (byte)((data[i + 1] * a + 127) / 255);
            data[i + 2] = (byte)((data[i + 2] * a + 127) / 255);
        }
    }

    private string Full(string rel) => Path.Combine(AssetRoot, rel.Replace('/', Path.DirectorySeparatorChar));

    public void Dispose()
    {
        foreach (var t in _textures.Values) t.Dispose();
        _textures.Clear();
    }
}
