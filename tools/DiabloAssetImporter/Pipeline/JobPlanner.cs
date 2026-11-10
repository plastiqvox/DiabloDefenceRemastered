using System.Text.Json;
using DiabloAssetImporter.Bake;
using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Upscale;

namespace DiabloAssetImporter.Pipeline;

/// <summary>표·맵 정의 → 작업 목록(이름 서수 순). 원본 바이트는 아직 읽지 않는다.</summary>
internal static class JobPlanner
{
    public static List<ImportJob> Plan(ImportTable table, IReadOnlyList<ArenaGrid> arenas, UpscaleTable? upscaleApply)
    {
        var jobs = new List<ImportJob>();
        foreach (var e in table.Monsters.Monsters)
            jobs.Add(Unit("monster", e, table.Monsters.Palette, table.Monsters.ShadowAlpha, upscaleApply?.Applies(e) == true));
        foreach (var e in table.Hero.Hero)
            jobs.Add(Unit("hero", e, table.Hero.Palette, table.Hero.ShadowAlpha, false));
        foreach (var e in table.Missiles.Missiles)
            jobs.Add(Missile(e, table.Missiles.Palette, table.Missiles.ShadowAlpha));
        foreach (var e in table.Icons.Icons)
            jobs.Add(Icon(e, table.Icons.Palette ?? table.Missiles.Palette, table.Icons));
        foreach (var e in table.Sounds.Sounds)
            foreach (var src in e.SourcePaths())
                jobs.Add(Sound(src, e.Bundle, e.Prefer));
        foreach (var e in table.Music.Music)
            jobs.Add(Sound(e.Path, e.Bundle, e.Prefer));
        foreach (var grid in arenas)
        {
            var ts = table.Tilesets.Tilesets.First(t => t.Id == grid.Tileset.Id);
            jobs.Add(Arena(grid, ts, upscaleApply?.Applies(ts.Id) == true));
        }
        jobs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return jobs;
    }

    private static string Spec(params object?[] parts) =>
        string.Join("\n", parts.Select(p => p is string s ? s : JsonSerializer.Serialize(p, AssetJson.WriteOptions)));

    private static ImportJob Unit(string kind, ImportUnitEntry e, string palette, float? headShadow, bool upscale)
    {
        var actions = e.ResolveAll(headShadow);
        var variants = e.Variants.OrderBy(v => v.Key, StringComparer.Ordinal).ToList();
        var sources = new List<string> { palette };
        sources.AddRange(actions.Select(a => a.SourcePath));
        sources.AddRange(variants.Where(v => v.Value is not null).Select(v => v.Value!));
        return new ImportJob(e.Out, kind, e.Bundle, [], Distinct(sources), Spec(kind, palette, headShadow, e), upscale, (bag, up) =>
        {
            var decoded = actions.Select(a => UnitBaker.DecodeUnitAction(e.Out[3..], a, bag.Get(a.SourcePath))).ToList();
            var pal = Palette.Read(bag.Get(palette));
            var trns = variants.Select(v => v.Value is null ? null : Palette.ReadTrn(bag.Get(v.Value))).ToList();
            var sheet = UnitBaker.Bake(decoded, pal, trns, up, e.Out);
            return JobOutput.Of(SheetFiles(e.Out, variants.Select(v => v.Key).ToList(), sheet));
        });
    }

    private static ImportJob Missile(ImportMissileEntry e, string palette, float? headShadow)
    {
        var variants = e.Variants.OrderBy(v => v.Key, StringComparer.Ordinal).ToList();
        var files = e.SourcePaths();
        var sources = new List<string> { palette };
        sources.AddRange(files);
        sources.AddRange(variants.Where(v => v.Value is not null).Select(v => v.Value!));
        return new ImportJob(e.Out, "missile", e.Bundle, [], Distinct(sources), Spec("missile", palette, headShadow, e), false, (bag, _) =>
        {
            var decoded = UnitBaker.DecodeMissile(e.Out[3..], e, [.. files.Select(bag.Get)], headShadow);
            var pal = Palette.Read(bag.Get(palette));
            var trns = variants.Select(v => v.Value is null ? null : Palette.ReadTrn(bag.Get(v.Value))).ToList();
            var sheet = UnitBaker.Bake([decoded], pal, trns, null, e.Out);
            return JobOutput.Of(SheetFiles(e.Out, variants.Select(v => v.Key).ToList(), sheet));
        });
    }

    private static ImportJob Icon(ImportIconEntry e, string palette, IconsTable head)
    {
        var files = e.SourcePaths();
        var sources = new List<string> { palette };
        sources.AddRange(files);
        return new ImportJob(e.Out, "icon", e.Bundle, [], Distinct(sources), Spec("icon", palette, head.Size, head.Background, e), false, (bag, _) =>
        {
            var decoded = e.Cel is { } cel ? DecodeCelIcon(e, bag.Get(cel)) : UnitBaker.DecodeMissile(e.Out[3..],
                new ImportMissileEntry { Out = e.Out, Cl2 = e.Cl2!, Width = e.Width, Dirs = e.Dirs, FramesAreDirs = e.FramesAreDirs, ShadowAlpha = e.ShadowAlpha },
                [.. files.Select(bag.Get)], null);
            var img = IconBaker.Bake(e, decoded, Palette.Read(bag.Get(palette)), head.Size, head.Background);
            return JobOutput.Of([new OutFile(e.Out + ".png", Png.Encode(img))]);
        });
    }

    /// <summary>CEL 아이콘: 프레임 전부를 폭 그대로 풀어 방향 1개짜리 묶음으로(칸 = 폭 × 가장 큰 높이). 원작 마법 아이콘은 투명 없는 56×56 이다.</summary>
    internal static UnitBaker.DecodedAction DecodeCelIcon(ImportIconEntry e, byte[] cel)
    {
        List<IndexedImage> images;
        try { images = [.. Cel.Frames(cel).Select(f => Cel.Decode(f.Span, e.Width))]; }
        catch (InvalidDataException ex) { throw new ImportCheckException($"{e.Cel}: {ex.Message}"); }
        if (images.Count == 0) throw new ImportCheckException($"{e.Cel}: 프레임이 없다");
        return new UnitBaker.DecodedAction("default", new Int2(e.Width, images.Max(i => i.Height)), 1, images.Count, images, e.ShadowAlpha);
    }

    private static ImportJob Sound(string src, ContentBundle? bundle, IReadOnlyList<ContentBundle> prefer)
    {
        var outName = AssetFormat.SoundOutput(src);
        return new ImportJob(outName, "sound", bundle, prefer, [src], Spec("sound", src), false, (bag, _) =>
        {
            Wav.Converted c;
            try { c = Wav.ToPcm16(bag.Get(src)); }
            catch (InvalidDataException ex) { throw new ImportCheckException($"{src}: {ex.Message}"); }
            return new JobOutput([new OutFile(outName, c.Bytes)], [new SoundInfo { Path = outName, Samples = c.Samples, Rate = c.Rate, Channels = c.Channels }]);
        });
    }

    private static ImportJob Arena(ArenaGrid grid, ImportTilesetEntry ts, bool upscale)
    {
        var palPath = ts.SourcePath(ts.Palettes[grid.Map.Palette]);
        var sources = new List<string>(TilesetSource.SourcePaths(ts)) { palPath };
        var art = grid.Map.Art.TrimEnd('/');
        return new ImportJob(art, "arena", ts.Bundle, [], Distinct(sources), Spec("arena", grid.Map.Id, grid.LayoutHash, ts), upscale, (bag, up) =>
        {
            var src = TilesetSource.From(ts, bag);
            var files = ArenaBaker.Bake(grid, src, Palette.Read(bag.Get(palPath)), up);
            return JobOutput.Of(files.Select(f => new OutFile(f.Path, f.Bytes, f.Scale)));
        });
    }

    private static IEnumerable<OutFile> SheetFiles(string outName, IReadOnlyList<string> variantNames, PackedSheet sheet)
    {
        var page0 = outName + ".png";
        for (var v = 0; v < variantNames.Count; v++)
            for (var p = 0; p < sheet.VariantPages[v].Count; p++)
                yield return new OutFile(SpriteMeta.PageFile(page0, p, variantNames[v]), Png.Encode(sheet.VariantPages[v][p]), sheet.Meta.Scale);
        yield return new OutFile(SpriteMeta.MetaFile(page0), AssetJson.Serialize(sheet.Meta), sheet.Meta.Scale);
    }

    private static List<string> Distinct(List<string> list) => [.. list.Distinct(StringComparer.OrdinalIgnoreCase)];
}
