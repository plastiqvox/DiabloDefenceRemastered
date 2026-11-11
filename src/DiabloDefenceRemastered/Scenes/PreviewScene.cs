using System.Globalization;
using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Sim.Core;
using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Loading;
using DiabloDefenceRemastered.Rendering;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 그림 확인 씬(<c>--preview sprites/enemies/effects/tiles/blend/fog</c>, 설계서 7.6·7.10·8.1·9, 소유 WP5). SceneFactory(WP6)가 고른다.
/// 배치는 고정(6.13)이고 앱 틱만 본다 — 같은 앱 시각에 캡처하면 늘 같은 그림이다.
/// <list type="bullet">
/// <item><c>sprites</c>: 첫 쪽 = 단계 2 견본(겉모습 아홉 벌, 바닥 아이템·보석 세 등급·금화·물약, 상인 셋, 상자·책·둘레 아이콘 — 합성 보상·획득물), 그다음 쪽부터
/// 그림 정의마다 한 행 — 방향마다 고정 칸(프레임은 재생) + 방향을 도는 칸 하나, 발 기준점 십자, ID. 4초마다 다음 쪽.</item>
/// <item><c>enemies</c>: 적 정의마다 한 행(그 변종·색조·크기의 걷기) + 몸 높이 선(<c>body_height</c> × 크기 — 기획 열린 질문 7).</item>
/// <item><c>effects</c>: 이펙트 정의를 칸에 반복 재생(intro 포함). <c>blend</c>: 투사체·이펙트를 보통/가산 두 칸으로(기획 열린 질문 10).</item>
/// <item><c>tiles</c>: <c>--map</c>(없으면 <c>--stage</c>·뿌리 액트의 보스 맵, <c>&lt;id&gt;@m</c> = 거울판)의 외곽 상자 전체를 줄여 그림 + 하위 칸 성질 세 색 + 입구·기준점·문 틈. 안개 없음.</item>
/// <item><c>fog</c>(v0.5.1): 같은 맵에 장면 여섯을 1초(60틱)씩 차례로 — 주인공·적 자리 고정, 적 생각·이동 없음(시야만 매 틱 계산). 장면마다 30틱째에 <c>FOG</c> 로그 줄.
/// <c>_light.png</c> 는 캡처(WP7)가 <see cref="SaveLightPng"/> 를 불러 쓴다.</item>
/// </list>
/// </summary>
public sealed class PreviewScene : IScene, IDisposable
{
    public static readonly string[] Targets = ["sprites", "enemies", "effects", "tiles", "blend", "fog"];
    public const int PageTicks = 240;
    public const int SceneTicks = 60;

    private readonly AppServices _app;
    private readonly string _target;
    private readonly UiCanvas _ui;
    private long _now;
    private World? _world;
    private WorldRenderer? _renderer;
    private ArenaRenderer? _arena;
    private RenderTarget2D? _mapRT;
    private GateGrateArt? _tileGrates;
    private int _scene = -1;
    private readonly BeginTickSystem _begin = new();

    /// <param name="target"><see cref="AppOptions.Preview"/> 값.</param>
    public PreviewScene(AppServices app, string target)
    {
        _app = app;
        _target = target.Trim().ToLowerInvariant();
        if (Array.IndexOf(Targets, _target) < 0)
            throw new ArgumentException($"--preview 대상 '{target}' 은 {string.Join("·", Targets)} 중 하나", nameof(target));
        _ui = new UiCanvas(app);
    }

    public bool PausesBelow => false;
    public bool DrawsBelow => false;

    public string Target => _target;

    /// <summary>미리보기 월드(<c>tiles</c>·<c>fog</c>). 나머지는 null.</summary>
    public World? World => _world;

    public void Enter(SceneStack stack)
    {
        Art.PreloadAll();
        if (_target is "tiles" or "fog") _world = CreateWorld();
        if (_target == "fog" && _world is not null)
        {
            _renderer = new WorldRenderer(_app);
            _renderer.Attach(_world);
        }
    }

    public void Exit() => Dispose();

    public void FixedTick(in AppTick t, InputFrame input)
    {
        _now = t.Value;
        if (_target != "fog" || _world is not { } w) return;
        var scene = (int)(_now / SceneTicks % 6);
        if (scene != _scene)
        {
            _scene = scene;
            SetupFogScene(w, scene);
        }
        w.Tick = (int)_now;
        _begin.Tick(w);
        if (_now % SceneTicks == SceneTicks / 2 && _renderer is not null) LogFog(w);
    }

    public void Draw(RenderContext rc, float alpha)
    {
        // 그림 행·견본 쪽은 화면 전체를 덮지 않는다 — 프레임마다 지우지 않으면 앞 프레임이 남아 가산 빛이 쌓이고(희게 포화) 흔들리는 글자가 번졌다(단계 2 wp5)
        if (_target is "sprites" or "enemies" or "effects" or "blend") rc.Device.Clear(Color.Black);
        switch (_target)
        {
            case "sprites": DrawSprites(rc); break;
            case "enemies": DrawEnemies(rc); break;
            case "effects": DrawEffects(rc, blend: false); break;
            case "blend": DrawEffects(rc, blend: true); break;
            case "tiles": DrawTiles(rc); break;
            case "fog":
                if (_world is not null && _renderer is not null) _renderer.Draw(_world, rc, 1f);
                DrawTitle(rc, $"fog 장면 {_scene + 1}/6 — {FogSceneName(_scene)}{(_app.Options.Reveal ? " (reveal)" : "")}");
                break;
        }
    }

    /// <summary><c>--preview fog</c> 캡처의 <c>_light.png</c>(480×270, 7.7 ④ 뒤 <c>LightRT</c>). 캡처 담당(WP7)이 캡처 파일 이름 옆에 쓴다.</summary>
    public bool SaveLightPng(string path)
    {
        if (_renderer?.LightTarget is not { } light) return false;
        using var stream = File.Create(path);
        light.SaveAsPng(stream, light.Width, light.Height);
        return true;
    }

    private ArtServices Art => _app.Art as ArtServices ?? throw new InvalidOperationException("그림 서비스가 없다");

    // ── 그림 행 ─────────────────────────────────────────────────

    private void DrawSprites(RenderContext rc)
    {
        var sprites = _app.Defs.Sprites.All;
        var perPage = (int)((540f - Top) / RowHeight);
        var total = 1 + Math.Max(1, (sprites.Count + perPage - 1) / perPage);
        var page = (int)(_now / PageTicks % total);
        if (page == 0)
        {
            DrawShowcase(rc, total);
            return;
        }
        DrawRows(rc, "sprites", sprites.Count, (batch, row, y) =>
        {
            var s = sprites[row];
            DrawSpriteRow(batch, s, "", Rgb.White, 1f, y, s.Directions, frameTime: 0.08f);
            return $"{s.Id}  {s.Frame.X}×{s.Frame.Y} d{s.Directions} f{s.Frames}";
        }, page - 1, (page, total));
    }

    private void DrawEnemies(RenderContext rc)
    {
        var enemies = _app.Defs.Enemies.All;
        DrawRows(rc, "enemies", enemies.Count, (batch, row, y) =>
        {
            var e = enemies[row];
            var set = e.Sprites.Def;
            var clip = set.Clips.Walk ?? set.Clips.Idle;
            if (clip is null) return e.Id;
            var sprite = clip.Sprite.Def;
            var cells = DrawSpriteRow(batch, sprite, e.Variant, e.Tint, e.Scale * CellScale(sprite), y, sprite.Directions, clip.FrameTime ?? set.FrameTime);
            // 몸 높이 선: 발에서 body_height × 크기 위
            var top = y + RowFoot - set.BodyHeight * e.Scale * CellScale(sprite);
            batch.Draw(Prim(rc).Pixel, new Rectangle((int)LabelWidth, (int)top, (int)(cells * CellWidth(sprite.Directions)), 1), new Rgb(80, 200, 255).ToXna(0.6f));
            return $"{e.Id}  {set.Id}{(e.Variant.Length > 0 ? "@" + e.Variant : "")} ×{e.Scale:0.##} h{set.BodyHeight:0}";
        });
    }

    private const float LabelWidth = 150f, RowHeight = 84f, RowFoot = 72f, Top = 28f;

    private static float CellWidth(int dirs) => Math.Min(88f, (960f - LabelWidth - 8f) / (Math.Min(dirs, 16) + 1));

    private static float CellScale(SpriteDef s) => Math.Min(1f, Math.Min(80f / Math.Max(1, s.Frame.Y), 88f / Math.Max(1, s.Frame.X)));

    /// <summary>한 행: 방향마다 고정 칸 + 도는 칸. 칸 수를 돌려준다.</summary>
    private int DrawSpriteRow(SpriteBatch batch, SpriteDef sprite, string variant, Rgb tint, float scale, float y, int dirs, float frameTime)
    {
        if (!Art.TryGetSprite(sprite, variant, out _)) return 0;
        var shown = Math.Min(dirs, 16);
        var cw = CellWidth(dirs);
        var frame1 = 1 + (int)(_now * World.Dt / Math.Max(0.01f, frameTime)) % Math.Max(1, sprite.Frames);
        for (var i = 0; i <= shown; i++)
        {
            var dir = i < shown ? i * dirs / shown : (int)(_now / 30 % dirs);
            var foot = new XnaVector2(LabelWidth + cw * (i + 0.5f), y + RowFoot);
            Art.DrawFrame(batch, sprite, variant, dir, frame1, foot, scale, tint.ToXna());
            Cross(batch, foot);
        }
        return shown + 1;
    }

    private Texture2D? _pixel;

    private void Cross(SpriteBatch batch, XnaVector2 foot)
    {
        if (_pixel is null) return;
        var c = new Rgb(255, 60, 60).ToXna(0.9f);
        batch.Draw(_pixel, new Rectangle((int)foot.X - 4, (int)foot.Y, 9, 1), c);
        batch.Draw(_pixel, new Rectangle((int)foot.X, (int)foot.Y - 4, 1, 9), c);
    }

    /// <param name="rowPage">행 쪽을 정해 줌(첫 쪽이 견본인 <c>sprites</c>) — 없으면 앱 시각으로 돈다. <paramref name="shown"/> = 제목에 보일 (쪽, 전체).</param>
    private void DrawRows(RenderContext rc, string title, int count, Func<SpriteBatch, int, float, string> row, int? rowPage = null, (int Page, int Total)? shown = null)
    {
        var perPage = (int)((540f - Top) / RowHeight);
        var pages = Math.Max(1, (count + perPage - 1) / perPage);
        var page = rowPage ?? (int)(_now / PageTicks % pages);
        var labels = new List<(float Y, string Text)>();
        _pixel = Prim(rc).Pixel;
        rc.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, VirtualToPresent(rc));
        for (var i = 0; i < perPage; i++)
        {
            var index = page * perPage + i;
            if (index >= count) break;
            var y = Top + i * RowHeight;
            labels.Add((y, row(rc.Batch, index, y)));
        }
        rc.Batch.End();
        _ui.Begin(rc);
        var (sp, st) = shown ?? (page + 1, pages);
        _ui.Text($"--preview {title}  {sp}/{st}  ({count})", new Vector2(8, 6), 14f, Rgb.White);
        foreach (var (y, text) in labels) _ui.Text(text, new Vector2(8, y + RowFoot - 14), 11f, new Rgb(220, 220, 220));
        if (count == 0) _ui.Text("정의가 없다", new Vector2(8, 40), 14f, new Rgb(255, 120, 120));
        _ui.End();
    }

    // ── sprites 첫 쪽: 단계 2 견본(wp5) ─────────────────────────────

    private World? _showWorld;

    /// <summary>
    /// 단계 2 견본 한 화면(설계서 9 <c>--preview sprites</c>, 13장 WP5 단계 2 기록 — 캡처 판정용): ① 겉모습 아홉 벌(서기, 남쪽) ② 바닥 아이템 — 작은 영혼석(마름모)·영혼석·큰 영혼석(돌 + 색)·
    /// 금화·체력 물약·마력 폭주·책(놓인 모습)과 보석 빛 ③ 상인 셋(서기 반복)·상자(닫힘·열림)·책 받침 ④ 둘레 아이콘 견본(합성 <c>Pickup</c> — 닫힌 상자·연 뒤 책·물약 칸 가득·마법서·정예 상자).
    /// 칸마다 발 기준점 십자. 원작 그림이 없으면 그 칸은 대체 그림(획득물 <c>draw</c>)이나 빈 칸.
    /// </summary>
    private void DrawShowcase(RenderContext rc, int pages)
    {
        var defs = _app.Defs;
        var prim = Prim(rc);
        _pixel = prim.Pixel;
        var w = _showWorld ??= new World(defs, _app.Options.Seed, null, HeroClass());
        var labels = new List<(Vector2 Pos, string Text)>();
        var glows = new List<(Vector2 Pos, Rgb Color, float Half, float Alpha)>();
        var lifts = new List<(SpriteDef Sprite, int Frame, XnaVector2 Foot, Rgb Tint)>();
        var batch = rc.Batch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, VirtualToPresent(rc));

        // ① 겉모습 아홉 벌
        var hero = HeroClass();
        const float y1 = 130f;
        if (hero is not null)
        {
            var keys = hero.Looks.Count > 0 ? hero.Looks.Keys.ToList() : [hero.Look];
            for (var i = 0; i < keys.Count; i++)
            {
                var set = HeroLookArt.Pick(hero, keys[i], s => s.Clips.Idle?.Sprite.Def is { } sp && Art.TryGetSprite(sp, "", out _));
                var foot = new XnaVector2(60f + i * 100f, y1);
                if (set.Clips.Idle?.Sprite.Def is { } idle)
                {
                    var frame = 1 + (int)(_now * World.Dt / (set.Clips.Idle.FrameTime ?? set.FrameTime)) % Math.Max(1, idle.Frames);
                    Art.DrawFrame(batch, idle, "", 0, frame, foot, 1f, Color.White);
                }
                Cross(batch, foot);
                labels.Add((new Vector2(foot.X - 30f, y1 + 4f), $"{keys[i]} → {set.Id}"));
            }
        }

        // ② 바닥 아이템(놓인 모습)과 보석
        const float y2 = 250f;
        string[] items = ["gem_blue", "gem_green", "gem_red", "gold", "healing_potion", "mana_surge", "pk_book"];
        for (var i = 0; i < items.Length; i++)
        {
            if (!defs.Pickups.TryGet(items[i], out var p)) continue;
            var foot = new Vector2(70f + i * 120f, y2);
            var floorDrawn = false;
            if (p.Floor is { } floor && floor.Sprite.Def is var fs && Art.TryGetSprite(fs, "", out _))
            {
                Art.DrawFrame(batch, fs, "", 0, PickupArt.FloorFrame(floor, fs.Frames, 999f, false), foot.ToXna(), floor.Scale ?? 1f, (floor.Tint ?? Rgb.White).ToXna());
                if (p.Kind == PickupKind.Xp && floor.Tint is { } lift) lifts.Add((fs, PickupArt.FloorFrame(floor, fs.Frames, 999f, false), foot.ToXna(), lift));
                floorDrawn = true;
            }
            else if (p.Draw.Sprite?.Def is { } ds)
                Art.DrawFrame(batch, ds, "", 0, p.Draw.Frame ?? ds.Frames, foot.ToXna(), p.Draw.Scale, (p.Draw.Tint ?? Rgb.White).ToXna());
            else
            {
                var size = p.Draw.Size is { } sz ? new Vector2(sz.X, sz.Y) : new Vector2(10, 14);
                BatchX.Blit(batch, prim.Diamond, new RectF(foot.X - size.X * 0.5f, foot.Y - size.Y, foot.X + size.X * 0.5f, foot.Y),
                            new Rectangle(0, 0, prim.Diamond.Width, prim.Diamond.Height), (p.Draw.Color ?? Rgb.White).ToXna());
            }
            var k = PickupArt.GlowScale(p, floorDrawn);
            if (k > 0f && p.Draw.Color is { } gc)
            {
                var size = p.Draw.Size is { } sz2 ? MathF.Max(sz2.X, sz2.Y) : 14f;
                glows.Add((foot - new Vector2(0f, size * 0.5f), gc, size * 1.6f * MathF.Sqrt(k), 0.8f * k));
            }
            Cross(batch, foot.ToXna());
            labels.Add((new Vector2(foot.X - 40f, y2 + 4f), $"{p.Id}{(floorDrawn ? "" : " (대체)")}"));
        }

        // ③ 상인 셋·상자 닫힘/열림·책 받침
        const float y3 = 385f;
        var x = 70f;
        foreach (var m in defs.Merchants.All)
        {
            var foot = new XnaVector2(x, y3);
            if (m.Sprite?.Def.Clips.Idle?.Sprite.Def is { } idle)
                Art.DrawFrame(batch, idle, "", 0, 1 + (int)(_now * World.Dt / WorldRenderer.MerchantFrameTime) % Math.Max(1, idle.Frames), foot, 1f, Color.White);
            Cross(batch, foot);
            labels.Add((new Vector2(x - 30f, y3 + 4f), $"{m.Name} {m.Sprite?.Id}"));
            x += 110f;
        }
        foreach (var (id, opened, text) in new[] { ("obj_chest", false, "상자 닫힘"), ("obj_chest", true, "상자 열림(빔)"), ("obj_tome", false, "책 받침") })
        {
            var foot = new XnaVector2(x, y3);
            if (defs.Pickups.TryGet(id, out var p) && p.Floor is { } floor && floor.Sprite.Def is var fs && Art.TryGetSprite(fs, "", out _))
                Art.DrawFrame(batch, fs, "", 0, PickupArt.FloorFrame(floor, fs.Frames, 999f, opened), foot, 1f, Color.White);
            Cross(batch, foot);
            labels.Add((new Vector2(x - 30f, y3 + 4f), text));
            x += 110f;
        }
        batch.End();

        if (glows.Count > 0 || lifts.Count > 0)
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null, null, VirtualToPresent(rc));
            foreach (var (pos, color, half, alpha) in glows)
                BatchX.Centered(batch, prim.Light, pos, new Vector2(half * 2f), color.ToXna(alpha));
            foreach (var (sprite, frame, foot, tint) in lifts) Art.DrawFrame(batch, sprite, "", 0, frame, foot, 1f, tint.ToXna(WorldRenderer.GemLift));
            batch.End();
        }

        // ④ 둘레 아이콘 견본(합성 획득물 — 무기 칸 0 = 매직 미사일 Lv3)
        _ui.Begin(rc);
        _ui.Text($"--preview sprites  1/{pages}  단계 2 견본 — 겉모습 아홉 벌 · 바닥 아이템·보석 · 상인·물체 · 둘레 아이콘", new Vector2(8, 6), 13f, Rgb.White);
        foreach (var (pos, text) in labels) _ui.Text(text, pos, 10f, new Rgb(220, 220, 220));
        if (defs.Weapons.TryGet("magic_missile", out var mm) && w.Hero.Weapons[0].IsEmpty) w.Hero.Weapons[0] = new WeaponSlot { Def = mm, Level = 3, MaxedAtTick = -1 };
        var samples = new List<(Pickup P, string Text, bool Full)>();
        Pickup Make(string id, RewardDef? reward, RewardPickupStage stage, int chosen, int value)
        {
            var p = new Pickup();
            p.Reset();
            p.Def = defs.Pickups.Get(id);
            p.Reward = reward;
            p.RewardStage = stage;
            p.RewardChosen = chosen;
            p.Value = value;
            return p;
        }
        if (defs.Rewards.TryGet("reward_chest", out var chest))
        {
            samples.Add((Make(chest.Object!.Id, chest, RewardPickupStage.Closed, -1, 0), "닫힌 상자", false));
            if (chest.Drop is { } drop)
            {
                samples.Add((Make(drop.Id, chest, RewardPickupStage.Dropped, 0, 45), "연 뒤 책", false));
                samples.Add((Make(drop.Id, chest, RewardPickupStage.Dropped, 0, 45), "책 — 물약 칸 가득", true));
            }
        }
        if (defs.Rewards.TryGet("reward_tome", out var tome)) samples.Add((Make(tome.Object!.Id, tome, RewardPickupStage.Closed, -1, 0), "마법서", false));
        if (defs.Pickups.All.FirstOrDefault(p => p.Kind == PickupKind.Chest) is { } elite) samples.Add((Make(elite.Id, null, RewardPickupStage.Closed, -1, 3), "정예 상자", false));
        for (var i = 0; i < samples.Count; i++)
        {
            var (p, text, full) = samples[i];
            w.Hero.Potions = full ? HeroPotions.Capacity(w) : 0;
            var anchor = new Vector2(100f + i * 190f, 500f - RewardOsd.ArcLift);
            RewardIcons.Osd(_ui, Art, RewardOsd.Icons(p, w), anchor, (int)_now, labels: true, 1f);
            var m = _ui.MeasureText(text, 11f);
            _ui.Text(text, new Vector2(anchor.X - m.X * 0.5f, 512f), 11f, new Rgb(255, 210, 120));
        }
        w.Hero.Potions = 0;
        _ui.End();
    }

    // ── 이펙트·혼합 ───────────────────────────────────────────────

    private void DrawEffects(RenderContext rc, bool blend)
    {
        var effects = _app.Defs.Effects.All.Where(e => e.Sprite is not null).ToList();
        var projectiles = blend ? _app.Defs.Projectiles.All : [];
        var count = effects.Count + projectiles.Count;
        const int cols = 6, rowsPerPage = 4;
        var perPage = blend ? rowsPerPage * (cols / 2) : rowsPerPage * cols;
        var pages = Math.Max(1, (count + perPage - 1) / perPage);
        var page = (int)(_now / PageTicks % pages);
        var cw = 960f / cols;
        var ch = (540f - Top) / rowsPerPage;
        var labels = new List<(Vector2 Pos, string Text)>();
        var additive = new List<(EffectDef? Fx, ProjectileDef? Pj, Vector2 Pos)>();
        rc.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, VirtualToPresent(rc));
        for (var i = 0; i < perPage; i++)
        {
            var index = page * perPage + i;
            if (index >= count) break;
            var slot = blend ? i * 2 : i;
            var pos = new Vector2(cw * (slot % cols + 0.5f), Top + ch * (slot / cols) + ch * 0.6f);
            var fx = index < effects.Count ? effects[index] : null;
            var pj = fx is null ? projectiles[index - effects.Count] : null;
            DrawOne(rc.Batch, fx, pj, pos);
            labels.Add((pos + new Vector2(-cw * 0.45f, ch * 0.32f), fx?.Id ?? pj!.Id));
            if (blend)
            {
                var right = pos + new Vector2(cw, 0);
                additive.Add((fx, pj, right));
                labels.Add((right + new Vector2(-cw * 0.45f, ch * 0.32f), "가산"));
            }
        }
        rc.Batch.End();
        if (additive.Count > 0)
        {
            rc.Batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, null, null, null, VirtualToPresent(rc));
            foreach (var (fx, pj, pos) in additive) DrawOne(rc.Batch, fx, pj, pos);
            rc.Batch.End();
        }
        _ui.Begin(rc);
        _ui.Text($"--preview {(blend ? "blend" : "effects")}  {page + 1}/{pages}  ({count})", new Vector2(8, 6), 14f, Rgb.White);
        foreach (var (p, text) in labels) _ui.Text(text, p, 11f, new Rgb(220, 220, 220));
        _ui.End();
    }

    private void DrawOne(SpriteBatch batch, EffectDef? fx, ProjectileDef? pj, Vector2 pos)
    {
        var t = _now * World.Dt;
        if (fx is not null)
        {
            var length = EffectPlayback.IntroLength(fx) + EffectPlayback.CycleLength(fx) * (fx.Loop ? 3f : 1f) + 0.3f;
            var sample = EffectPlayback.Sample(fx, t % length);
            if (sample.Done) return;
            var sprite = sample.Intro ? fx.Intro!.Sprite.Def : fx.Sprite!.Def;
            var scale = Math.Min(fx.Scale ?? 1f, 80f / Math.Max(1, sprite.Frame.Y));
            Art.DrawFrame(batch, sprite, "", 0, sample.Frame1, pos.ToXna(), scale, fx.Tint.ToXna(fx.Alpha));
        }
        else if (pj is not null)
        {
            var sprite = pj.Sprite.Def;
            var dir = (int)(_now / 20 % Math.Max(1, sprite.Directions));
            var frame1 = 1 + (int)(t / 0.05f) % Math.Max(1, sprite.Frames);
            Art.DrawFrame(batch, sprite, "", dir, frame1, pos.ToXna(), pj.Scale, pj.Tint.ToXna());
        }
    }

    // ── 타일 ──────────────────────────────────────────────────

    private void DrawTiles(RenderContext rc)
    {
        if (_world is not { Arena: { } grid } w)
        {
            DrawTitle(rc, "--preview tiles: 맵이 없다(--map·--stage·액트 보스 맵)");
            return;
        }
        var g = rc as GameRenderContext ?? throw new InvalidOperationException("RenderServices.CreateContext 의 맥락이 아니다");
        var box = grid.Iso.BoxSize;
        var bw = (int)box.X;
        var bh = (int)box.Y;
        if (_mapRT is null)
        {
            _arena = new ArenaRenderer(grid, Art.LoadMapArt(w.Map!));
            var k = Math.Min(1f, 4096f / Math.Max(bw, bh));
            _mapRT = new RenderTarget2D(rc.Device, (int)(bw * k), (int)(bh * k), false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            var whole = new RectF(0, 0, bw, bh);
            rc.Device.SetRenderTarget(_mapRT);
            rc.Device.Clear(Color.Black);
            var batch = rc.Batch;
            var m = Matrix.CreateScale(k);
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, m);
            _arena.Prepare(whole);
            _arena.DrawFloor(batch, whole, g.Primitives);
            _arena.DrawGroup(batch, WallGroup.Back, g.Primitives);
            var items = new List<DrawItem>();
            _arena.CollectLayer4(whole, items);
            System.Runtime.InteropServices.CollectionsMarshal.AsSpan(items).Sort(DrawItem.Comparer.Compare);
            foreach (var it in items) _arena.DrawItem(batch, it.Index, g.Primitives, 1f);
            _arena.DrawGroup(batch, WallGroup.Front, g.Primitives);
            DebugNavOverlay.Draw(batch, g.Primitives, w, ["tiles", "entries"], whole, w.Hero.Pos, 0);
            batch.End();
            g.RestorePresent();
        }
        var lb = g.Letterbox;
        var fit = Math.Min(lb.Width / (float)_mapRT.Width, (lb.Height - 24f) / _mapRT.Height);
        var dw = (int)(_mapRT.Width * fit);
        var dh = (int)(_mapRT.Height * fit);
        var ox = lb.X + (lb.Width - dw) / 2;
        var oy = lb.Y + 24 + (lb.Height - 24 - dh) / 2;
        rc.Batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
        rc.Batch.Draw(_mapRT, new Rectangle(ox, oy, dw, dh), Color.White);
        rc.Batch.End();
        // 문 봉인 창살(v0.9.4 V13, 단계 2 wp5): 2.5초 주기로 닫힘 1초 → 0.6초 동안 내려감 → 열림. 맵 그림 위에 겹쳐 그린다(정렬 없음 — 문 틈은 둘레라 가릴 것이 앞 턱뿐)
        var grateState = "";
        _tileGrates ??= GateGrateArt.Build(w, Art, _app.Defs.Maps.All.SelectMany(m => m.Mirrored is { } mm ? new[] { m, mm } : [m]));
        if (_tileGrates is { } grates)
        {
            var cycle = (int)(_now % 150);
            var sink = Math.Clamp((cycle - 60) / (float)GateGrates.OpenTicks, 0f, 1f);
            grateState = sink <= 0f ? "  창살 닫힘" : sink >= 1f ? "  창살 열림" : string.Create(CultureInfo.InvariantCulture, $"  창살 내려감 {sink:0.00}");
            var placed = new List<GateGrateArt.Placed>();
            foreach (var (_, gate) in w.Gates) grates.Place(gate, grid.Iso, sink, placed);
            var scale = _mapRT.Width / (float)bw * fit;
            rc.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null,
                           Matrix.CreateScale(scale) * Matrix.CreateTranslation(ox, oy, 0f));
            foreach (var p in placed) BatchX.Blit(rc.Batch, p.Page, p.Dest, p.Src, Color.White);
            rc.Batch.End();
        }
        else grateState = "  창살 그림 없음(코드 봉인)";
        DrawTitle(rc, string.Create(CultureInfo.InvariantCulture,
            $"--preview tiles  {w.Map!.Id}  {bw}×{bh}  {(_arena?.Art is null ? "코드 그림(자원 없음)" : "arena.json")}  걷기=빨강 투사체=파랑 시야=노랑{grateState}"));
    }

    private void DrawTitle(RenderContext rc, string text)
    {
        _ui.Begin(rc);
        _ui.Text(text, new Vector2(8, 4), 13f, Rgb.White);
        _ui.End();
    }

    // ── 맵·월드 ───────────────────────────────────────────────

    private World? CreateWorld()
    {
        var defs = _app.Defs;
        var stage = _app.Options.StageId is { } sid && defs.Stages.TryGet(sid, out var st) ? st : RootAct(defs);
        MapDef? map = null;
        if (_app.Options.Map is { } mapId)
        {
            var mirror = mapId.EndsWith("@m", StringComparison.Ordinal);
            if (defs.Maps.TryGet(mirror ? mapId[..^2] : mapId, out var m)) map = mirror ? m.Mirrored : m;
            if (map is null) { _app.Log.Error($"PREVIEW --map {mapId}: 없는 맵(또는 거울판이 없는 맵)"); return null; }
        }
        map ??= stage?.RoomMaps.Boss?.Def ?? (stage?.RoomMaps.Normal.Count > 0 ? stage.RoomMaps.Normal[0].Def : null) ?? (defs.Maps.Count > 0 ? defs.Maps[0] : null);
        if (map is null) return null;
        var pool = new RoomMapPoolDef { Boss = DefRef<MapDef>.To(map) };
        var previewStage = stage is null ? new StageDef { Id = "preview", RoomMaps = pool } : stage with { RoomMaps = pool };
        var world = new World(defs, _app.Options.Seed, previewStage, HeroClass());
        world.Presentation.Enabled = true;
        _begin.Tick(world);
        return world;
    }

    private HeroClassDef? HeroClass()
    {
        var defs = _app.Defs;
        if (_app.Options.HeroClass is { } id && defs.HeroClasses.TryGet(id, out var h)) return h;
        return defs.Rules.Hero?.Class.Def;
    }

    /// <summary>뿌리 액트 = 다른 액트의 <c>next_act</c> 가 가리키지 않는 첫 액트(정의 순).</summary>
    private static StageDef? RootAct(GameDefs defs)
    {
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in defs.Stages.All) if (s.NextAct is { } n) targets.Add(n.Id);
        foreach (var s in defs.Stages.All) if (!targets.Contains(s.Id)) return s;
        return defs.Stages.Count > 0 ? defs.Stages[0] : null;
    }

    // ── 안개 장면(9절 --preview fog) ───────────────────────────────

    private static string FogSceneName(int scene) => scene switch
    {
        0 => "① 뒷벽 앞", 1 => "② 문 앞", 2 => "③ 기둥 옆", 3 => "④ 시야 경계", 4 => "⑤ 앞 턱 앞", _ => "⑥ 어두운 바닥",
    };

    /// <summary>
    /// 장면 자리(하위 칸 좌표 — 맵 크기 N 에 맞춰 비례, 대성당 40×40 기준으로 고름). 적은 정의의 첫 적(원거리 장면은 이름에 <c>bow</c>·<c>archer</c> 가 든 적)을 쓴다.
    /// 적 정의가 없으면(WP4 데이터 전) 주인공·땅·벽만 보인다.
    /// </summary>
    private void SetupFogScene(World w, int scene)
    {
        var grid = w.Arena!;
        var n = grid.Size;
        var iso = grid.Iso;
        Vector2 T(float tx, float ty) => iso.ToWorld(new TileCoord(tx * n / 40f, ty * n / 40f));
        w.Enemies.Clear();
        _renderer?.Fx.Clear();
        var radius = w.Lighting.HeroLightRadius > 0 ? w.Lighting.HeroLightRadius : 420f;
        var (hero, enemies) = scene switch
        {
            0 => (T(14, 3), new[] { T(11, 1.5f), T(17, 1.5f) }),
            1 => (T(4, 19.5f), new[] { T(-3, 19.5f), T(-3, 12f) }),
            2 => (T(11.5f, 18.5f), new[] { T(16.5f, 18.5f) }),
            3 => (T(19.5f, 19.5f), new[] { T(19.5f, 19.5f) + new Vector2(radius + 10f, 0), T(19.5f, 19.5f) + new Vector2(-(radius - 20f), 0), T(19.5f, 19.5f) + new Vector2(0, 30f) }),
            4 => (T(34, 26), new[] { T(36, 30) }),
            _ => (T(19.5f, 19.5f), new[] { T(19.5f, 19.5f) + new Vector2(radius - 60f, 0) }),
        };
        w.Hero.Pos = w.Hero.PrevPos = hero;
        var def = PickEnemy(scene == 1);
        if (def is null) return;
        foreach (var p in enemies)
        {
            var e = w.Enemies.Add();
            e.Def = def;
            e.Pos = e.PrevPos = p;
            e.Radius = def.Radius ?? def.Sprites.Def.BaseRadius * def.Scale;
            e.Life = LifeState.Active;
            e.Visual = new EnemyVisual { AlphaMul = 1f };
            e.Facing = Vector2.Normalize(hero - p + new Vector2(0.001f, 0));
            e.Anim.Play(ClipKind.Idle, loop: true);
            e.BodyTop = def.Sprites.Def.BodyHeight * def.Scale;
        }
    }

    private EnemyDef? PickEnemy(bool ranged)
    {
        var all = _app.Defs.Enemies.All;
        if (ranged)
            foreach (var e in all)
                if (e.Id.Contains("bow", StringComparison.Ordinal) || e.Id.Contains("archer", StringComparison.Ordinal)) return e;
        return all.Count > 0 ? all[0] : null;
    }

    private void LogFog(World w)
    {
        var s = _renderer!.LastStats;
        var hidden = _app.Options.Reveal ? 0 : s.EnemiesHidden;
        _app.Log.Info(string.Create(CultureInfo.InvariantCulture,
            $"FOG stage_tick={w.Tick} in_sight={s.InSight} explored={s.Explored} enemies_drawn={s.EnemiesDrawn} enemies_hidden={hidden} reveal={(_app.Options.Reveal ? 1 : 0)}"));
    }

    // ── 도우미 ────────────────────────────────────────────────

    private static Primitives Prim(RenderContext rc) =>
        (rc as GameRenderContext ?? throw new InvalidOperationException("RenderServices.CreateContext 의 맥락이 아니다")).Primitives;

    /// <summary>가상 960×540 → PresentRT(레터박스) 행렬.</summary>
    private static Matrix VirtualToPresent(RenderContext rc)
    {
        var lb = PresentMath.Letterbox(new Int2(rc.PresentSize.X, rc.PresentSize.Y));
        var k = lb.Width / 960f;
        return Matrix.CreateScale(k) * Matrix.CreateTranslation(lb.X0, lb.Y0, 0f);
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
        _mapRT?.Dispose();
        _mapRT = null;
    }
}
