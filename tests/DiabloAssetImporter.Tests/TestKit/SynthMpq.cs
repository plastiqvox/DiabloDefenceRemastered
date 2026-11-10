using DiabloAssetImporter.Formats;
using War3Net.IO.Mpq;

namespace DiabloAssetImporter.Tests.TestKit;

/// <summary>
/// 시험이 만드는 작은 MPQ(War3Net, 설계서 10.1 <c>Pipeline</c>). 원본과 같은 쪽 플래그(암호화·블록 오프셋 키·압축)로 쓴다 —
/// PKWARE implode·파형 압축은 War3Net 이 쓰지 못해 실제 원본 실행에서만 확인한다(WP0 완료 기준).
/// </summary>
internal static class SynthMpq
{
    public static void Write(string path, IReadOnlyDictionary<string, byte[]> files)
    {
        var mpqFiles = new List<MpqFile>();
        foreach (var (name, bytes) in files.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            var f = MpqFile.New(new MemoryStream(bytes), name, leaveOpen: false);
            f.TargetFlags = MpqFileFlags.Exists | MpqFileFlags.CompressedMulti | MpqFileFlags.Encrypted | MpqFileFlags.BlockOffsetAdjustedKey;
            f.CompressionType = MpqCompressionType.ZLib;
            mpqFiles.Add(f);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Create(path);
        using var a = MpqArchive.Create(fs, mpqFiles, new MpqArchiveCreateOptions
        {
            ListFileCreateMode = MpqFileCreateMode.None, AttributesCreateMode = MpqFileCreateMode.None,
            HashTableSize = (ushort)Math.Max(16, NextPow2(files.Count * 2)),
        }, leaveOpen: true);
    }

    private static int NextPow2(int n)
    {
        var p = 1;
        while (p < n) p <<= 1;
        return p;
    }
}

/// <summary>
/// 표·맵 정의가 펼치는 모든 원본 경로에 합성 파일을 만든다(10.1 "실제 표 전체 경우"): CL2 는 프레임 수 = <paramref name="framesOf"/>(없으면 4),
/// TRN 256바이트, 타일셋은 성질 표와 같은 SOL·쓰는 메가타일 번호보다 큰 TIL.
/// </summary>
internal static class SynthSources
{
    public static Dictionary<string, byte[]> For(ImportTable table, MapDataResult data, Func<string, int>? framesOf = null)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var pal = Encoders.Pal();
        files[table.Monsters.Palette] = pal;
        files[table.Hero.Palette] = pal;
        files[table.Missiles.Palette] = pal;
        if (table.Icons.Palette is { } ip) files[ip] = pal;
        var trnShift = 1;
        foreach (var e in table.Monsters.Monsters.Concat(table.Hero.Hero))
        {
            foreach (var a in e.ResolveAll(null))
                files[a.SourcePath] = UnitCl2(a.Width, framesOf?.Invoke(a.SourcePath) ?? 4, 8);
            foreach (var v in e.Variants.Values)
                if (v is not null) files[v] = Encoders.Trn(trnShift++);
        }
        foreach (var e in table.Missiles.Missiles)
            foreach (var p in e.SourcePaths()) files[p] = FlatCl2(e.Width, e.FramesAreDirs ?? framesOf?.Invoke(p) ?? 14);
        foreach (var e in table.Icons.Icons)
            foreach (var p in e.SourcePaths())
                if (!files.ContainsKey(p)) files[p] = FlatCl2(e.Width, e.FramesAreDirs ?? 8);
        foreach (var e in table.Sounds.Sounds)
            foreach (var p in e.SourcePaths()) files[p] = Encoders.Wav8([128, 255, 0, 64, 200]);
        foreach (var e in table.Music.Music) files[e.Path] = Encoders.Wav8([128, 129, 130]);
        foreach (var t in table.Tilesets.Tilesets)
        {
            var def = data.Tilesets.FirstOrDefault(d => d.Id == t.Id);
            foreach (var (k, v) in Tileset(t, def)) files[k] = v;
            foreach (var p in t.Palettes.Values) files[t.SourcePath(p)] = pal;
        }
        return files;
    }

    public static byte[] UnitCl2(int width, int frames, int groups, int height = 40) =>
        Encoders.Grouped([.. Enumerable.Range(0, groups).Select(g => (IReadOnlyList<byte[]>)[.. Enumerable.Range(0, frames).Select(f =>
            Encoders.Cl2Frame(Encoders.Figure(width, height + (f % 2), (byte)(130 + g * 4), f + g)))])]);

    public static byte[] FlatCl2(int width, int frames, int height = 40) =>
        Encoders.Flat([.. Enumerable.Range(0, frames).Select(f => Encoders.Cl2Frame(Encoders.Figure(width, height, (byte)(140 + f), f)))]);

    /// <summary>
    /// 합성 타일셋: 메가타일 m(1..max) → 하위 타일 4(m−1)..+3, SOL = 성질 표 글자의 번호(없으면 0 = 바닥). MIN 조각: 바닥 단 = 왼쪽·오른쪽 삼각형(바닥 칸)
    /// 또는 사각형(막힌 칸), 막힌 칸은 위 단까지 평면. 덧그림 CEL 은 프레임 하나.
    /// </summary>
    public static Dictionary<string, byte[]> Tileset(ImportTilesetEntry t, TilesetDef? def)
    {
        var maxMega = 200;
        if (def is not null)
        {
            var used = def.Props.Keys.Concat(def.Floor).Concat(def.Pieces.Values).DefaultIfEmpty(1).Max();
            maxMega = Math.Max(maxMega, used + 2);
        }
        var subtiles = maxMega * 4;
        var til = Enumerable.Range(0, subtiles).ToArray();
        var sol = new byte[subtiles];
        var n = t.PiecesPerSubtile;
        var min = new int[subtiles * n];
        // CEL 프레임: 1 = 왼쪽 삼각형, 2 = 오른쪽 삼각형, 3 = 평면, 4 = 투명 사각형
        for (var m = 1; m <= maxMega; m++)
        {
            var props = def is not null && def.Props.TryGetValue(m, out var p) ? p : "....";
            for (var k = 0; k < 4; k++)
            {
                var s = (m - 1) * 4 + k;
                sol[s] = (byte)ArenaLayout.PropChars.IndexOf(props[k]);
                var floor = (sol[s] & 5) == 0;
                if (floor) { min[s * n + n - 2] = (2 << 12) | 1; min[s * n + n - 1] = (3 << 12) | 2; }
                else
                    for (var piece = 0; piece < n; piece++) min[s * n + piece] = (0 << 12) | 3;
                if (m % 7 == 0) min[s * n] = (1 << 12) | 4;   // 바닥 칸 위 조각(kind 1)도 생기게
            }
        }
        var tri = new IndexedImage(32, 32);
        for (var i = 0; i < 1024; i++) { tri.Index[i] = (byte)(20 + i % 7); tri.Opaque[i] = true; }
        var half = new IndexedImage(32, 32);
        for (var i = 0; i < 1024; i++) { half.Index[i] = 60; half.Opaque[i] = i % 32 < 16; }
        var cel = Encoders.Flat([Encoders.LevelPiece(tri, 2), Encoders.LevelPiece(tri, 3), Encoders.LevelPiece(tri, 0), Encoders.LevelPiece(half, 1)]);
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [t.SourcePath(t.Til)] = Encoders.U16s(til),
            [t.SourcePath(t.Min)] = Encoders.U16s(min),
            [t.SourcePath(t.Sol)] = sol,
            [t.SourcePath(t.Cel)] = cel,
        };
        if (t.Overlay is { } o)
        {
            var frames = o.Frames.Keys.DefaultIfEmpty(1).Max();
            files[t.SourcePath(o.Cel)] = Encoders.Flat([.. Enumerable.Range(0, frames).Select(_ => new byte[] { 0x10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0xC0 })]);
        }
        foreach (var d in t.AuditDun) files[t.SourcePath(d)] = Encoders.U16s([4, 4, .. Enumerable.Repeat(0, 16)]);
        return files;
    }
}
