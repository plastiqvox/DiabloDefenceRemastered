using System.Globalization;
using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Loading;

/// <summary>
/// 그림 서비스 (설계서 6.3·7.5·7.8, 계약 <see cref="IArtServices"/>, 소유 WP5). <c>RenderServices.CreateArt</c> 가 만든다.
/// <list type="bullet">
/// <item><see cref="PreloadStage"/>: 액트 시작 때(판 시작·계단) 그 액트에서 나올 수 있는 그림·맵 그림을 읽고 나머지를 내린다(<see cref="ActPreload"/>). 방 이동 때는 읽지 않는다.
/// 같은 액트면 아무것도 하지 않는다 — <c>WorldRenderer.Attach</c> 가 방마다 불러도 된다. 로그 <c>ASSETS stage=… textures=… vram_mb=… art_scale=…</c>(R7).</item>
/// <item><see cref="Sprite"/>: 미리 읽은 것만(없으면 예외 — 미리 읽기 계산이 빠진 버그). 자원 파일이 없어 못 읽은 것은 <see cref="TryGetSprite"/> 가 거짓(그리는 쪽이 자리 표시를 그림).</item>
/// </list>
/// </summary>
public sealed class ArtServices : IArtServices, IDisposable
{
    private readonly AppServices _app;
    private readonly Dictionary<(string Sprite, string Variant), SpriteHandle?> _handles = [];
    private readonly HashSet<string> _keep = new(StringComparer.Ordinal);
    private StageDef? _stage;
    private bool _preloaded;

    public ArtServices(GraphicsDevice device, AppServices app, string? assetRoot = null)
    {
        _app = app;
        Store = new TextureStore(device, assetRoot ?? ArtModule.DefaultAssetRoot);
        Fonts = FontLoader.Create(app.Log, out var hasFont);
        HasFont = hasFont;
        Cursor = CursorLoader.Load(device, Path.Combine(Store.AssetRoot, CursorLoader.FileName), app.Log);
    }

    public TextureStore Store { get; }
    public FontSystem Fonts { get; }
    public bool HasFont { get; }
    public MouseCursor? Cursor { get; }

    /// <summary>그림 배율 s(7.1).</summary>
    public int ArtScale => Store.ArtScale;

    /// <summary>지금 읽어 둔 액트(<see cref="PreloadAll"/> 이면 null).</summary>
    public StageDef? Stage => _stage;

    /// <summary>
    /// 액트 단위 미리 읽기(7.5). 같은 액트를 다시 부르면 아무것도 하지 않는다. <paramref name="stage"/> 가 null 이면 <see cref="PreloadAll"/>.
    /// </summary>
    public void PreloadStage(StageDef? stage, HeroClassDef? hero)
    {
        if (stage is null) { PreloadAll(); return; }
        if (_preloaded && ReferenceEquals(_stage, stage)) return;
        Load(ActPreload.Sprites(_app.Defs, stage, hero), ActPreload.Maps(stage));
        _stage = stage;
        _preloaded = true;
        Log(stage.Id);
    }

    /// <summary>모든 그림(미리보기·방 없는 월드). 맵 그림은 그리는 맵을 열 때(<see cref="LoadMapArt"/>).</summary>
    public void PreloadAll()
    {
        if (_preloaded && _stage is null) return;
        Load(ActPreload.Sprites(_app.Defs, null, null), []);
        _stage = null;
        _preloaded = true;
        Log("all");
    }

    private void Load(IReadOnlyList<SpriteUse> uses, IReadOnlyList<MapDef> maps)
    {
        _handles.Clear();
        _keep.Clear();
        foreach (var use in uses) _handles[(use.Sprite.Id, use.Variant)] = LoadUse(use);
        foreach (var map in maps) LoadMapArt(map);
        Store.Retain(_keep);
    }

    private SpriteHandle? LoadUse(SpriteUse use)
    {
        var sprite = use.Sprite;
        if (sprite.Source == SpriteSource.Grid)
        {
            _keep.Add(sprite.File);
            var tex = Store.Load(sprite.File, sprite.ColorKey);
            return tex is null ? null : new SpriteHandle([tex], null, null);
        }
        var meta = Store.Meta(SpriteMeta.MetaFile(sprite.File));
        if (meta is null || !meta.TryGetAction(sprite.Action, out var action)) return null;
        var pages = new Texture2D[meta.Pages.Count];
        for (var p = 0; p < pages.Length; p++)
        {
            var rel = SpriteMeta.PageFile(sprite.File, p, use.Variant);
            _keep.Add(rel);
            if (Store.Load(rel) is not { } tex) return null;
            pages[p] = tex;
        }
        return new SpriteHandle(pages, meta, action);
    }

    /// <summary>
    /// 맵 그림 읽기(<c>arena.json</c>·바닥 조각·기둥 아틀라스). 없으면 null(자원 없는 개발 실행 — <c>ArenaRenderer</c> 가 코드 마름모로 그림).
    /// 이번 액트에 남길 목록에 넣으므로 액트 미리 읽기 밖에서 불러도 다음 <see cref="PreloadStage"/> 까지 남는다.
    /// </summary>
    public MapArt? LoadMapArt(MapDef map)
    {
        if (string.IsNullOrEmpty(map.Art)) return null;
        var art = map.Art.TrimEnd('/');
        var arenaRel = $"{art}/{ArenaArt.FileName}";
        if (!Store.Exists(arenaRel)) return null;
        var arena = ArenaArt.Read(Path.Combine(Store.AssetRoot, arenaRel));
        var floors = new Texture2D?[arena.Floor.Count];
        for (var i = 0; i < floors.Length; i++)
        {
            var rel = $"{art}/{arena.Floor[i].File}";
            _keep.Add(rel);
            floors[i] = Store.Load(rel);
        }
        var columnsBase = $"{art}/{arena.Columns}.png";
        var meta = Store.Meta(SpriteMeta.MetaFile(columnsBase));
        var pages = new Texture2D[meta?.Pages.Count ?? 0];
        for (var p = 0; p < pages.Length; p++)
        {
            var rel = SpriteMeta.PageFile(columnsBase, p);
            _keep.Add(rel);
            pages[p] = Store.Load(rel) ?? throw new FileNotFoundException($"기둥 아틀라스 페이지가 없다: {rel}");
        }
        SpriteMetaAction? action = null;
        if (meta is not null && meta.TryGetAction(null, out var a)) action = a;
        return new MapArt(arena, floors, pages, meta, action);
    }

    public SpriteHandle Sprite(SpriteDef sprite, string variant)
    {
        if (!_handles.TryGetValue((sprite.Id, variant), out var handle))
            throw new InvalidOperationException($"그림 '{sprite.Id}'(변종 '{variant}')을 미리 읽지 않았다 — 액트 미리 읽기 계산(ActPreload)이 빠뜨렸다");
        return handle ?? throw new FileNotFoundException($"그림 '{sprite.Id}'(변종 '{variant}')의 자원 파일이 없다 — 가져오기를 다시 돌린다");
    }

    /// <summary>미리 읽었고 자원 파일도 있으면 참. 미리 읽지 않았으면 예외(버그).</summary>
    public bool TryGetSprite(SpriteDef sprite, string variant, out SpriteHandle handle)
    {
        if (!_handles.TryGetValue((sprite.Id, variant), out var h))
            throw new InvalidOperationException($"그림 '{sprite.Id}'(변종 '{variant}')을 미리 읽지 않았다 — 액트 미리 읽기 계산(ActPreload)이 빠뜨렸다");
        handle = h ?? default;
        return h is not null;
    }

    /// <summary>
    /// 프레임 하나 그리기(WP6 처럼 WP5 밖에서 그리는 쪽). <paramref name="pos"/> = 발 자리(그 배치의 좌표계), <paramref name="scale"/> = 그 좌표계 1 단위의 크기 배율.
    /// 자원이 없으면 그리지 않는다.
    /// </summary>
    public void DrawFrame(SpriteBatch batch, SpriteDef sprite, string variant, int dir, int frame, XnaVector2 pos, float scale, Color color)
    {
        if (!TryGetSprite(sprite, variant, out var h)) return;
        if (Source(sprite, h, dir, frame, out var page, out var src, out var dest, pos.ToNumerics(), scale))
            batch.Draw(h.Pages[page], new Rectangle((int)MathF.Round(dest.X0), (int)MathF.Round(dest.Y0), (int)MathF.Round(dest.Width), (int)MathF.Round(dest.Height)),
                       new Rectangle(src.X0, src.Y0, src.Width, src.Height), color);
    }

    /// <summary>
    /// 그림 한 프레임의 페이지·원본 사각형(그림 픽셀)·자리(<paramref name="pos"/> 좌표계). 빈 프레임이면 거짓. 방향·프레임은 범위로 자른다.
    /// </summary>
    public bool Source(SpriteDef sprite, in SpriteHandle h, int dir, int frame1, out int page, out RectI src, out RectF dest, Vector2 pos, float scale)
    {
        dir = Math.Clamp(dir, 0, Math.Max(0, sprite.Directions - 1));
        frame1 = Math.Clamp(frame1, 1, Math.Max(1, sprite.Frames));
        var origin = SpriteLayout.Origin(sprite, dir);
        if (h.Action is { } action && h.Meta is { } meta)
        {
            var r = action.Rect(Math.Min(dir, action.Directions - 1), Math.Min(frame1, action.Frames));
            page = r.Page;
            src = new RectI(r.X, r.Y, r.X + r.W, r.Y + r.H);
            dest = SpriteLayout.Atlas(pos, origin, r, scale, meta.Scale);
            return !r.IsEmpty;
        }
        page = 0;
        src = SpriteLayout.GridSource(sprite.Frame, dir, frame1, Store.FileScale(sprite.File));
        dest = SpriteLayout.Grid(pos, origin, sprite.Frame, scale);
        return true;
    }

    private void Log(string stage)
    {
        var mb = Store.VramBytes / (1024.0 * 1024.0);
        _app.Log.Info(string.Create(CultureInfo.InvariantCulture,
            $"ASSETS stage={stage} textures={Store.Count} vram_mb={mb:F1} art_scale={Store.ArtScale}"));
    }

    public void Dispose() => Store.Dispose();
}

/// <summary>
/// 맵 그림 하나(<c>arena.json</c> + 바닥 조각 + 기둥 아틀라스, 7.6). 바닥 조각 텍스처는 파일이 없으면 null(그 조각만 비움).
/// </summary>
public sealed record MapArt(ArenaArt Arena, IReadOnlyList<Texture2D?> Floors, IReadOnlyList<Texture2D> ColumnPages, SpriteMeta? ColumnMeta, SpriteMetaAction? Columns);
