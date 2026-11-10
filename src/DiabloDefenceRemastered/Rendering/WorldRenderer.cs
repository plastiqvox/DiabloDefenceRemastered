using System.Globalization;
using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Loading;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>프레임 하나의 그리기 수(로그 <c>FOG</c> 줄·측정 <c>draw_calls</c>).</summary>
public readonly record struct WorldDrawStats(int DrawCalls, int Lights, int EnemiesDrawn, int EnemiesHidden, int InSight, int Explored);

/// <summary>
/// 월드 그리기 (설계서 7, 소유 WP5): 원작 타일 아레나(바닥 조각·벽 기둥·가림, 7.6)·유닛 y 정렬·빛·안개·표시 이펙트·시체·피해 숫자.
/// PlayScene(WP6)이 만들고 프레임마다 <see cref="Fx"/>.Consume → <c>Audio.Consume</c>(7.9) → <c>world.Presentation.Clear()</c> → <see cref="Draw"/> 순으로 부른다.
/// <list type="bullet">
/// <item>층(7.3): 0 바닥 → 0b 맵 밖 뒤쪽 입장 유닛·뒷둘레 벽 → 1 장판·예고·발밑 원 → 2 시체 → 3 획득물 → 4 y 정렬(유닛·안쪽 벽·바닥 위 조각·덧그림, 주인공 앞 벽 반투명)
/// → 5 앞둘레 턱·맵 밖 앞쪽 입장 유닛·문 봉인 → 6 빛·안개 곱하기 → 7 발광(투사체·이펙트·발광·번쩍임·예고 테두리) → 8 화면 효과 → (<c>PresentRT</c>) 9 월드 UI.</item>
/// <item>그림 배율 s(7.1): <c>WorldRT</c> = (960·s)×(540·s), 월드 → 타깃 행렬에 s 를 곱하고 모든 월드 그리기는 <c>PointClamp</c>. 빛·안개 타깃은 480×270 그대로.</item>
/// <item>안개(7.10): <c>--reveal</c>(<see cref="AppOptions.Reveal"/>)이거나 시야가 꺼졌으면 건너뜀. 알파 0 인 대상은 항목을 아예 모으지 않는다.</item>
/// <item><see cref="Attach"/>(v0.7.1, 7.11): 새 월드 = 맵 그림 묶음·안개 텍스처·칸 바뀜 값을 다시 만든다. 액트가 바뀌면 그림을 다시 미리 읽는다(같은 액트면 그대로).
/// 표시 이펙트 버리기는 <see cref="FxSystem.Attach"/>(PlayScene 이 이 다음에 부름).</item>
/// </list>
/// </summary>
public sealed class WorldRenderer : IWorldBound, IDisposable
{
    private const float ProjectileFrameTime = 0.05f;
    private static readonly Rgb EnemyRim = new(255, 60, 40);

    private readonly AppServices _app;
    private readonly WorldFrame _frame = new();
    private readonly List<DrawItem> _items = new(1024);
    private readonly List<GlowDraw> _glows = new(256);
    private readonly List<GlowDraw> _additive = new(256);
    private readonly List<GlowDraw> _alpha7 = new(256);
    private readonly List<Telegraph> _telegraphs = new(64);
    private readonly UiCanvas _ui;
    private World? _world;
    private ArenaRenderer? _arena;
    private LightRenderer? _light;
    private RenderTarget2D? _worldRT;
    private RenderTarget2D? _upRT;
    private int _scale;
    private RectF _heroRect;
    private int _enemiesDrawn, _enemiesHidden;

    /// <summary>스테이지(월드) 하나에 하나. 맵 그림(<c>arena.json</c>)은 처음 그리는 월드의 <c>World.Arena</c> 를 보고 연다.</summary>
    public WorldRenderer(AppServices app)
    {
        _app = app;
        _ui = new UiCanvas(app);
    }

    /// <summary>이 렌더러가 그리는 표시 이펙트.</summary>
    public FxSystem Fx { get; } = new();

    /// <summary>마지막 프레임의 수(<c>FOG</c> 로그·<c>draw_calls</c>).</summary>
    public WorldDrawStats LastStats { get; private set; }

    /// <summary>마지막 프레임의 <c>LightRT</c>(<c>--preview fog</c> 의 <c>_light.png</c> — 7.7 ④ 뒤).</summary>
    public RenderTarget2D? LightTarget => _light?.LightRT;

    /// <summary>지금 맵 그리기(미리보기·디버그 겹침).</summary>
    public ArenaRenderer? Arena => _arena;

    private ArtServices Art => _app.Art as ArtServices ?? throw new InvalidOperationException("그림 서비스(RenderServices.CreateArt)가 없다");

    /// <summary>새 월드(7.11): 맵 그림·안개 텍스처를 다시 만들고, 액트가 바뀌었으면 그림을 미리 읽는다(같은 액트면 그대로).</summary>
    public void Attach(World world)
    {
        _world = world;
        var art = Art;
        art.PreloadStage(world.Stage, world.HeroClass);
        _arena = world.Arena is { } grid && world.Map is { } map ? new ArenaRenderer(grid, art.LoadMapArt(map)) : null;
        _light?.Fog.Attach(world);
    }

    public void Draw(World world, RenderContext rc, float alpha)
    {
        var g = rc as GameRenderContext ?? throw new InvalidOperationException("WorldRenderer 는 RenderServices.CreateContext 의 맥락에서만 그린다");
        if (!ReferenceEquals(world, _world)) Attach(world);
        var device = g.Device;
        var batch = g.Batch;
        var art = Art;
        var s = art.ArtScale;
        EnsureTargets(device, g.Primitives, s, world);
        var metricsBefore = device.Metrics.DrawCount;
        _alphaInterp = Math.Clamp(alpha, 0f, 1f);

        // ── 카메라(7.2): 보간된 주인공, 생성 띠와 같은 함수, 타깃 픽셀로 내림, 흔들림은 정수 px ──
        var tick = world.Tick;
        var hero = world.Hero;
        var heroDraw = Vector2.Lerp(hero.PrevPos, hero.Pos, alpha);
        var view = CameraMath.ViewRect(heroDraw, world.MapSize, world.ViewSize);
        var camBase = ArtScale.FloorToTarget(view.Min, s);
        var shake = _app.Save.Settings.ScreenShake ? ShakeMath.Offset(world.Seed, tick, Fx.ShakePx(tick)) : Int2.Zero;
        var cam = camBase + new Vector2(shake.X, shake.Y);
        g.SetCamera(camBase);

        var f = _frame;
        f.World = world;
        f.Camera = cam;
        f.HeroDraw = heroDraw;
        f.StageTick = tick;
        f.Fog = !_app.Options.Reveal && world.Visibility.Enabled;
        f.View = new RectF(cam.X, cam.Y, cam.X + world.ViewSize.X, cam.Y + world.ViewSize.Y);
        f.Shapes.Clear();
        f.ShortLights.Clear();
        _glows.Clear();
        _additive.Clear();
        _alpha7.Clear();
        _enemiesDrawn = _enemiesHidden = 0;
        _arena?.Prepare(f.View.Inflate(64));

        var toTarget = Matrix.CreateTranslation(-cam.X, -cam.Y, 0f) * Matrix.CreateScale(s);
        var prim = g.Primitives;

        // ── 0~5층 ───────────────────────────────────────────────
        device.SetRenderTarget(_worldRT);
        device.Clear(Color.Black);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, toTarget);
        _arena?.DrawFloor(batch, f.View, prim);
        DrawOutsideEnemies(batch, f, back: true);
        _arena?.DrawGroup(batch, WallGroup.Back, prim);
        DrawLayer1(batch, f, prim);
        DrawCorpses(batch, f);
        DrawPickups(batch, f, prim);
        DrawLayer4(batch, f, prim);
        _arena?.DrawGroup(batch, WallGroup.Front, prim, i => FrontFade(i, f));
        DrawOutsideEnemies(batch, f, back: false);
        DrawGateSeals(batch, f, prim);
        batch.End();

        // ── 6층 빛·안개 ─────────────────────────────────────────
        CollectEffectLights(f);
        _light!.Build(batch, f, _arena, _app.Options.Reveal);
        device.SetRenderTarget(_worldRT);
        batch.Begin(SpriteSortMode.Deferred, FogBlend.Multiply, SamplerState.LinearClamp);
        batch.Draw(_light.LightRT, new Rectangle(0, 0, _worldRT!.Width, _worldRT.Height), Color.White);
        batch.End();

        // ── 7층 발광(알파 → 가산) ────────────────────────────────
        DrawLayer7(batch, f, prim, toTarget);

        // ── --debug-nav 겹침(월드 px) ─────────────────────────────
        if (_app.Options.DebugNav.Count > 0)
        {
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, toTarget);
            DebugNavOverlay.Draw(batch, prim, world, _app.Options.DebugNav, f.View, heroDraw, tick);
            batch.End();
        }

        // ── 8층 화면 효과 ───────────────────────────────────────
        DrawScreenEffects(batch, world, prim, tick);

        // ── PresentRT 로: 정수 배율 점 확대 → 레터박스 선형 ──────────
        Present(g, s);

        // ── 9층 월드 UI(PresentRT 해상도) ──────────────────────────
        DrawWorldUi(rc, f, art);

        var field = world.Visibility;
        LastStats = new WorldDrawStats((int)(device.Metrics.DrawCount - metricsBefore), _light.LightCount, _enemiesDrawn, _enemiesHidden,
                                       CountInSight(field), field.ExploredCount);
    }

    private void EnsureTargets(GraphicsDevice device, Primitives prim, int s, World world)
    {
        if (_worldRT is null || _scale != s)
        {
            _worldRT?.Dispose();
            _upRT?.Dispose();
            _upRT = null;
            var size = ArtScale.WorldTarget(s);
            _worldRT = new RenderTarget2D(device, size.X, size.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _scale = s;
        }
        if (_light is null)
        {
            _light = new LightRenderer(device, prim);
            _light.Fog.Attach(world);
        }
    }

    // ── 유닛 그림 ─────────────────────────────────────────────

    /// <summary>묶음의 클립 한 프레임을 그리고 사각형을 돌려준다(그림이 없으면 발 자리 십자 대신 작은 상자).</summary>
    private bool UnitFrame(SpriteSetDef set, ClipKind clip, in AnimClock anim, Vector2 facing, string variant, float scale, Vector2 foot,
                           out SpriteHandle handle, out int page, out RectI src, out RectF dest)
    {
        handle = default;
        page = 0;
        src = default;
        dest = default;
        var c = set.Clips.Get(clip) ?? set.Clips.Idle ?? set.Clips.Walk;
        if (c is null) return false;
        var sprite = c.Sprite.Def;
        if (!Art.TryGetSprite(sprite, variant, out handle)) return false;
        var ft = c.FrameTime ?? set.FrameTime;
        var frame1 = anim.Frame(ft, sprite.Frames) + 1;
        var dir = sprite.Directions > 1 ? FacingMath.ToColumn(facing, sprite.Directions) : 0;
        var pos = ArtScale.RoundToTarget(foot, _scale);
        return Art.Source(sprite, handle, dir, frame1, out page, out src, out dest, pos, scale);
    }

    private void DrawUnit(SpriteBatch batch, Texture2D tex, RectI src, RectF dest, Rgb tint, float alpha) =>
        BatchX.Blit(batch, tex, dest, new Rectangle(src.X0, src.Y0, src.Width, src.Height), tint.ToXna(alpha));

    private void DrawHero(SpriteBatch batch, WorldFrame f)
    {
        var w = f.World;
        var hero = w.Hero;
        _heroRect = new RectF(f.HeroDraw.X - 16, f.HeroDraw.Y - 76, f.HeroDraw.X + 16, f.HeroDraw.Y);
        if (w.HeroClass?.Sprites.Def is not { } set) return;
        if (!UnitFrame(set, hero.Anim.Clip, hero.Anim, hero.Facing, "", 1f, f.HeroDraw, out var h, out var page, out var src, out var dest)) return;
        _heroRect = dest;
        DrawUnit(batch, h.Pages[page], src, dest, Rgb.White, 1f);
        var shape = new ShapeDraw(h.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, f.HeroDraw, 1f, Forced: true, MinLight: !FrontWallCovers(dest), FogShape: false);
        f.Shapes.Add(shape);
        if (hero.HitFlashTime > 0f) _glows.Add(new GlowDraw(h.Pages[page], src, dest, Rgb.White, 0.8f));
    }

    private void DrawEnemy(SpriteBatch batch, WorldFrame f, Enemy e)
    {
        var w = f.World;
        var draw = Vector2.Lerp(e.PrevPos, e.Pos, _alphaInterp);
        var fogA = f.Fog ? FogMath.EnemyAlpha(w, e, f.StageTick, f.HeroDraw, draw) : 1f;
        if (fogA <= 0f) { _enemiesHidden++; return; }
        var def = e.Def;
        var a = fogA * Math.Clamp(e.Visual.AlphaMul, 0f, 1f);
        if (!UnitFrame(def.Sprites.Def, e.Anim.Clip, e.Anim, e.Facing, def.Variant, def.Scale, draw, out var h, out var page, out var src, out var dest)) return;
        _enemiesDrawn++;
        var tex = h.Pages[page];
        DrawUnit(batch, tex, src, dest, def.Tint, a);
        var forced = fogA >= 1f && f.Fog && (def.Rank is DefRank.Miniboss or DefRank.Boss || e.Visual.ForceShown);
        f.Shapes.Add(new ShapeDraw(tex, new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, draw, a, forced, !FrontWallCovers(dest), FogShape: true));
        if (e.Visual.GlowAlpha > 0f) _glows.Add(new GlowDraw(tex, src, dest, e.Visual.GlowColor, e.Visual.GlowAlpha * fogA));
        if (e.HitFlashTime > 0f) _glows.Add(new GlowDraw(tex, src, dest, Rgb.White, 0.8f * fogA));
        if (e.Visual.FlashAlpha > 0f) _glows.Add(new GlowDraw(tex, src, dest, e.Visual.FlashColor, e.Visual.FlashAlpha * fogA));
    }

    /// <summary>보간 값(PlayScene 이 <see cref="Draw"/> 에 넘긴 alpha) — 그리는 함수들이 같은 값을 쓰게 프레임 머리에서 넣는다.</summary>
    private float _alphaInterp;

    /// <summary>맵 밖 입장 중 적: 뒤쪽 바깥(nw·ne 변 밖)은 0b 층(뒷벽보다 먼저), 앞쪽 바깥은 5층(앞 턱 뒤) — 7.3.</summary>
    private void DrawOutsideEnemies(SpriteBatch batch, WorldFrame f, bool back)
    {
        if (_arena is null) return;
        var iso = _arena.Iso;
        foreach (var e in f.World.Enemies.Items)
        {
            if (e.Removed || e.Dead) continue;
            if (iso.IsInsideMap(e.Pos)) continue;
            var t = iso.ToTile(e.Pos);
            var isBack = t.Tx < -0.5f || t.Ty < -0.5f;
            if (isBack != back) continue;
            DrawEnemy(batch, f, e);
        }
    }

    // ── 1층: 장판·예고 채움·발밑 원 ─────────────────────────────

    private void DrawLayer1(SpriteBatch batch, WorldFrame f, Primitives prim)
    {
        var w = f.World;
        var tick = f.StageTick;
        foreach (var hz in w.Hazards.Items)
        {
            if (hz.Removed || hz.Effect is not { } def) continue;
            var pos = Vector2.Lerp(hz.PrevPos, hz.Pos, _alphaInterp);
            var age = (tick - hz.SpawnTick) * World.Dt;
            var scale = hz.EffectScale > 0f ? hz.EffectScale : EffectPlayback.Scale(def, hz.Radius, 1f);
            var tint = Mul(def.Tint, hz.Tint);
            if (def.Scatter is { } scatter)
            {
                var key = StableHash.Of((uint)hz.SpawnTick, hz.Id.Key);
                var count = ScatterLayout.Count(scatter, hz.Radius);
                var frames = EffectPlayback.Range(def.Frames, def.Sprite?.Def).Last;
                for (var i = 0; i < count; i++)
                    DrawEffectSprite(batch, f, def, age, pos + ScatterLayout.Offset(scatter, key, i, hz.Radius), scale, tint, Vector2.Zero,
                                     ScatterLayout.StartFrame(key, i, frames));
            }
            else DrawEffectSprite(batch, f, def, age, pos, scale, tint, Vector2.Zero, 0);
            if (def.Light is { } light) f.ShortLights.Add((pos, hz.Radius * light.RadiusMul, light.Color, 1f));
            f.Shapes.Add(new ShapeDraw(prim.Circle, new Rectangle(0, 0, prim.Circle.Width, prim.Circle.Height),
                                       new RectF(pos.X - hz.Radius, pos.Y - hz.Radius, pos.X + hz.Radius, pos.Y + hz.Radius), pos, 1f, false, false, FogShape: true));
        }

        _telegraphs.Clear();
        foreach (var t in w.Telegraphs.Items)
        {
            if (t.Removed) continue;
            _telegraphs.Add(t);
            var color = t.Tint == default ? new Rgb(255, 40, 40) : t.Tint;
            var baseAlpha = t.Effect?.Alpha is > 0f and var ea ? ea : 0.35f;
            if (t.Shape == TelegraphShape.Circle)
            {
                var d = new Vector2(t.Size * 2f);
                BatchX.Centered(batch, prim.Circle, t.Pos, d, color.ToXna(baseAlpha * 0.5f));
                BatchX.Centered(batch, prim.Circle, t.Pos, d * t.Progress, color.ToXna(baseAlpha));
                f.Shapes.Add(new ShapeDraw(prim.Circle, new Rectangle(0, 0, prim.Circle.Width, prim.Circle.Height),
                                           new RectF(t.Pos.X - t.Size, t.Pos.Y - t.Size, t.Pos.X + t.Size, t.Pos.Y + t.Size), t.Pos, 1f, false, false, FogShape: true));
            }
            else
            {
                BatchX.Line(batch, prim.Pixel, t.Pos, t.End, t.Size * 2f, color.ToXna(baseAlpha * 0.5f));
                BatchX.Line(batch, prim.Pixel, t.Pos, Vector2.Lerp(t.Pos, t.End, t.Progress), t.Size * 2f, color.ToXna(baseAlpha));
            }
        }

        // 발밑 원(오라·정예·아군) — 적 안개 알파를 따른다
        foreach (var e in w.Enemies.Items)
        {
            if (e.Removed || e.Dead || e.Visual.FootRingAlpha <= 0f) continue;
            var draw = Vector2.Lerp(e.PrevPos, e.Pos, _alphaInterp);
            var fogA = f.Fog ? FogMath.EnemyAlpha(w, e, tick, f.HeroDraw, draw) : 1f;
            if (fogA <= 0f) continue;
            BatchX.Centered(batch, prim.Ring, draw, new Vector2(e.Radius * 2.6f, e.Radius * 1.3f), e.Visual.FootRingColor.ToXna(e.Visual.FootRingAlpha * fogA));
        }
    }

    // ── 2층 시체·3층 획득물 ─────────────────────────────────────

    private void DrawCorpses(SpriteBatch batch, WorldFrame f)
    {
        var w = f.World;
        for (var i = 0; i < Fx.CorpseCount; i++)
        {
            ref readonly var c = ref Fx.CorpseAt(i);
            var fogA = f.Fog ? FogMath.PosAlpha(w.Visibility, c.Pos, f.StageTick, f.HeroDraw) : 1f;
            if (fogA <= 0f) continue;
            var set = c.Sprites;
            var clip = set.Clips.Die;
            if (clip is null) continue;
            var anim = new AnimClock { Clip = ClipKind.Die, Time = (f.StageTick - c.Tick) * World.Dt, Rate = 1f, Loop = false };
            if (!UnitFrame(set, ClipKind.Die, anim, c.Facing, c.Variant, c.Scale, c.Pos, out var h, out var page, out var src, out var dest)) continue;
            var a = fogA * Math.Clamp(c.Alpha <= 0f ? 1f : c.Alpha, 0f, 1f);
            DrawUnit(batch, h.Pages[page], src, dest, c.Tint == default ? Rgb.White : c.Tint, a);
            f.Shapes.Add(new ShapeDraw(h.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, c.Pos, a, false, true, FogShape: true));
        }
    }

    private void DrawPickups(SpriteBatch batch, WorldFrame f, Primitives prim)
    {
        var w = f.World;
        foreach (var p in w.Pickups.Items)
        {
            if (p.Removed) continue;
            var pos = Vector2.Lerp(p.PrevPos, p.Pos, _alphaInterp);
            var fogA = f.Fog ? FogMath.PosAlpha(w.Visibility, pos, f.StageTick, f.HeroDraw) : 1f;
            if (fogA <= 0f) continue;
            var d = p.Def.Draw;
            var tint = d.Tint ?? d.Color ?? Rgb.White;
            if (d.Sprite?.Def is { } sprite && Art.TryGetSprite(sprite, "", out var h))
            {
                var frame1 = d.Frame ?? Math.Min(sprite.Frames, 1 + (int)(p.Age / ProjectileFrameTime));
                if (Art.Source(sprite, h, 0, frame1, out var page, out var src, out var dest, ArtScale.RoundToTarget(pos, _scale), d.Scale))
                {
                    DrawUnit(batch, h.Pages[page], src, dest, tint, fogA);
                    f.Shapes.Add(new ShapeDraw(h.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, pos, fogA, false, true, FogShape: true));
                }
                continue;
            }
            // 코드 그림(보석 "gem" 등): 작은 마름모
            var size = d.Size is { } sz ? new Vector2(sz.X, sz.Y) : new Vector2(10, 14);
            size *= d.Scale;
            var rect = new RectF(pos.X - size.X * 0.5f, pos.Y - size.Y, pos.X + size.X * 0.5f, pos.Y);
            BatchX.Blit(batch, prim.Diamond, rect, new Rectangle(0, 0, prim.Diamond.Width, prim.Diamond.Height), tint.ToXna(fogA));
            f.Shapes.Add(new ShapeDraw(prim.Diamond, new Rectangle(0, 0, prim.Diamond.Width, prim.Diamond.Height), rect, pos, fogA, false, true, FogShape: true));
        }
    }

    // ── 4층 y 정렬 ────────────────────────────────────────────

    private void DrawLayer4(SpriteBatch batch, WorldFrame f, Primitives prim)
    {
        var w = f.World;
        _items.Clear();
        var heroSort = _arena is null ? f.HeroDraw.Y : DrawOrder.UnitSortY(_arena.Table, _arena.Iso, f.HeroDraw);
        _items.Add(new DrawItem(heroSort, -1, DrawKind.Hero, 0));
        var enemies = w.Enemies.Items;
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (e.Removed || e.Dead) continue;
            if (_arena is not null && !_arena.Iso.IsInsideMap(e.Pos)) continue;   // 맵 밖 입장 중은 0b·5층
            var draw = Vector2.Lerp(e.PrevPos, e.Pos, _alphaInterp);
            if (draw.X < f.View.X0 - 200 || draw.X > f.View.X1 + 200 || draw.Y < f.View.Y0 - 100 || draw.Y > f.View.Y1 + 300) continue;
            var sort = _arena is null ? draw.Y : DrawOrder.UnitSortY(_arena.Table, _arena.Iso, draw);
            _items.Add(new DrawItem(sort, (int)e.Id.Key, DrawKind.Enemy, i));
        }
        _arena?.CollectLayer4(f.View, _items);
        System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_items).Sort(DrawItem.Comparer.Compare);

        // 주인공 사각형을 먼저 알아야 앞 벽 반투명을 판정한다 — 그림 사각형만 미리 계산
        PeekHeroRect(f);
        foreach (var it in _items)
        {
            switch (it.Kind)
            {
                case DrawKind.Hero: DrawHero(batch, f); break;
                case DrawKind.Enemy: DrawEnemy(batch, f, enemies[it.Index]); break;
                default:
                    var item = _arena!.Items[it.Index];
                    var fade = WallFade.Fades(false, it.SortY, _arena.ItemRect(item), _arena.Iso.CellCenter(item.Tx, item.Ty), f.HeroDraw, _heroRect,
                                              WallFade.Radius(w.Map!.WallFade, w.Lighting.HeroLightRadius), w.Map.WallFade.Mode);
                    _arena.DrawItem(batch, it.Index, prim, fade ? WallFade.Alpha : 1f);
                    break;
            }
        }
    }

    private void PeekHeroRect(WorldFrame f)
    {
        var w = f.World;
        _heroRect = new RectF(f.HeroDraw.X - 16, f.HeroDraw.Y - 76, f.HeroDraw.X + 16, f.HeroDraw.Y);
        if (w.HeroClass?.Sprites.Def is { } set
            && UnitFrame(set, w.Hero.Anim.Clip, w.Hero.Anim, w.Hero.Facing, "", 1f, f.HeroDraw, out _, out _, out _, out var dest))
            _heroRect = dest;
    }

    private float FrontFade(int index, WorldFrame f)
    {
        var w = f.World;
        var it = _arena!.Items[index];
        var fade = WallFade.Fades(true, 0f, _arena.ItemRect(it), _arena.Iso.CellCenter(it.Tx, it.Ty), f.HeroDraw, _heroRect,
                                  WallFade.Radius(w.Map!.WallFade, w.Lighting.HeroLightRadius), w.Map.WallFade.Mode);
        return fade ? WallFade.Alpha : 1f;
    }

    /// <summary>앞(화면 아래) 안쪽 벽·앞 턱이 겹친 유닛(7.7 ④ — 최소 밝기·발광을 건너뜀).</summary>
    private bool FrontWallCovers(RectF unit)
    {
        if (_arena is null) return false;
        foreach (var i in _arena.VisibleWalls)
        {
            var it = _arena.Items[i];
            if (it.Group == WallGroup.Back) continue;
            var r = _arena.ItemRect(it);
            if (_arena.SortY(it) > unit.Y1 && WallFade.Overlaps(r, unit)) return true;
        }
        return false;
    }

    // ── 5층 문 봉인(7.11) ───────────────────────────────────────

    private void DrawGateSeals(SpriteBatch batch, WorldFrame f, Primitives prim)
    {
        var w = f.World;
        if (w.Room is not { } room || w.Arena is not { } grid) return;
        var pulse = 0.85f + 0.15f * MathF.Sin(f.StageTick * World.Dt * 2.2f);
        var openFade = room.Phase >= RoomPhase.Doors ? Math.Clamp(1f - (f.StageTick - room.PhaseTick) / 18f, 0f, 1f) : 1f;
        foreach (var (id, gate) in w.Gates)
        {
            var sealedNow = GateSeals.IsSealed(room, id);
            var a = pulse * (sealedNow ? 1f : openFade);
            if (a <= 0.01f) continue;
            // ① 문 틈 바닥의 은은한 붉은 빛 — 칸마다 부드러운 방사형(칸보다 조금 커 이웃 칸과 이어진다). 칸마다 꽉 찬 마름모·벽 높이 사각형을 쌓던
            //    옛 그림은 문 자리에 계단 모양 붉은 덩어리로 보였다(플레이 관찰).
            var glow = SealGlow.ToXna(0.3f * a);
            foreach (var cell in gate.Cells)
                BatchX.Centered(batch, prim.Light, grid.Iso.CellCenter(cell.X, cell.Y), new Vector2(88f, 44f), glow);
            // ② 문턱의 낮은 창살(칸마다 둘 + 위 가로대)과 ③ 가운데 바닥 룬(작은 마름모 + 십자) — 모두 문 틈 칸 안
            var shape = GateSeals.Shape(gate, grid.Iso);
            var bar = SealBar.ToXna(0.55f * a);
            var up = new Vector2(0f, -SealBarHeight);
            for (var i = 0; i < shape.Bars.Count; i++)
            {
                BatchX.Line(batch, prim.Pixel, shape.Bars[i], shape.Bars[i] + up, 2f, bar);
                if (i > 0) BatchX.Line(batch, prim.Pixel, shape.Bars[i - 1] + up, shape.Bars[i] + up, 1.5f, bar);
            }
            if (shape.Bars.Count == 0) continue;
            var rune = SealBar.ToXna(0.45f * a);
            var (rc, rx, ry) = (shape.Center, new Vector2(12f, 0f), new Vector2(0f, 6f));
            BatchX.Line(batch, prim.Pixel, rc - rx, rc - ry, 1f, rune);
            BatchX.Line(batch, prim.Pixel, rc - ry, rc + rx, 1f, rune);
            BatchX.Line(batch, prim.Pixel, rc + rx, rc + ry, 1f, rune);
            BatchX.Line(batch, prim.Pixel, rc + ry, rc - rx, 1f, rune);
            BatchX.Line(batch, prim.Pixel, rc - rx * 0.5f, rc + rx * 0.5f, 1f, rune);
            BatchX.Line(batch, prim.Pixel, rc - ry * 0.5f, rc + ry * 0.5f, 1f, rune);
        }
    }

    private static readonly Rgb SealGlow = new(200, 28, 16);
    private static readonly Rgb SealBar = new(255, 70, 40);
    private const float SealBarHeight = 18f;

    // ── 6층 빛 모으기·7층 ───────────────────────────────────────

    private void CollectEffectLights(WorldFrame f)
    {
        foreach (var fx in Fx.Effects)
        {
            if (fx.Event.Def.Light is not { } light) continue;
            var age = (f.StageTick - fx.Event.Tick) * World.Dt;
            var duration = light.Duration ?? fx.Life;
            if (age > duration) continue;
            var radius = (fx.Event.Radius > 0f ? fx.Event.Radius : 48f * fx.Scale) * light.RadiusMul;
            f.ShortLights.Add((fx.Event.Pos, radius, light.Color, 1f - age / Math.Max(0.01f, duration)));
        }
    }

    private void DrawLayer7(SpriteBatch batch, WorldFrame f, Primitives prim, Matrix toTarget)
    {
        var w = f.World;
        // 투사체(속도 방향 열, 주인공 투사체는 가산 한 겹 더, 적 투사체는 붉은 테)
        foreach (var p in w.Projectiles.Items)
        {
            if (p.Removed) continue;
            var sprite = p.Def.Sprite.Def;
            if (!Art.TryGetSprite(sprite, "", out var h)) continue;
            var pos = ArtScale.RoundToTarget(Vector2.Lerp(p.PrevPos, p.Pos, _alphaInterp), _scale);
            var dir = sprite.Directions > 1 && p.Vel.LengthSquared() > 1e-6f ? FacingMath.ToColumn(p.Vel, sprite.Directions) : 0;
            var frame1 = 1 + (int)(p.Age / ProjectileFrameTime) % Math.Max(1, sprite.Frames);
            var scale = p.Def.Scale * (p.Scale > 0f ? p.Scale : 1f);
            if (!Art.Source(sprite, h, dir, frame1, out var page, out var src, out var dest, pos, scale)) continue;
            var tint = Mul(p.Def.Tint, p.Tint == default ? Rgb.White : p.Tint);
            var draw = new GlowDraw(h.Pages[page], src, dest, tint, 1f);
            (p.Def.Blend == BlendKind.Additive ? _additive : _alpha7).Add(draw);
            if (p.Faction == Faction.Enemy) _additive.Add(new GlowDraw(h.Pages[page], src, Grow(dest, 1.15f), EnemyRim, 0.35f));
            else _additive.Add(draw with { Alpha = 0.5f });
        }
        // 표시 이펙트(늘 그림 — 안개 위)
        foreach (var fx in Fx.Effects)
        {
            var def = fx.Event.Def;
            if (def.Sprite is null) continue;
            var age = (f.StageTick - fx.Event.Tick) * World.Dt;
            var tint = Mul(def.Tint, fx.Event.Tint == default ? Rgb.White : fx.Event.Tint);
            CollectEffect(def, age, fx.Event.Pos, fx.Scale, tint, fx.Event.Dir, 0, def.Blend == BlendKind.Additive ? _additive : _alpha7);
        }

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, toTarget);
        foreach (var d in _alpha7) BatchX.Blit(batch, d.Texture, d.Dest, new Rectangle(d.Src.X0, d.Src.Y0, d.Src.Width, d.Src.Height), d.Color.ToXna(d.Alpha));
        // 예고 테두리 한 번 더(알파 0.6 의 2px 선) — 벽 위에서도 범위가 보이게(7.3)
        foreach (var t in _telegraphs)
        {
            var color = (t.Tint == default ? new Rgb(255, 40, 40) : t.Tint).ToXna(0.6f);
            if (t.Shape == TelegraphShape.Circle) BatchX.Centered(batch, prim.Ring, t.Pos, new Vector2(t.Size * 2f), color);
            else
            {
                var n = Vector2.Normalize(new Vector2(-(t.End - t.Pos).Y, (t.End - t.Pos).X)) * t.Size;
                BatchX.Line(batch, prim.Pixel, t.Pos + n, t.End + n, 2f, color);
                BatchX.Line(batch, prim.Pixel, t.Pos - n, t.End - n, 2f, color);
            }
        }
        batch.End();

        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, null, null, null, toTarget);
        foreach (var d in _additive) BatchX.Blit(batch, d.Texture, d.Dest, new Rectangle(d.Src.X0, d.Src.Y0, d.Src.Width, d.Src.Height), d.Color.ToXna(d.Alpha));
        foreach (var d in _glows) BatchX.Blit(batch, d.Texture, d.Dest, new Rectangle(d.Src.X0, d.Src.Y0, d.Src.Width, d.Src.Height), d.Color.ToXna(d.Alpha));
        batch.End();
    }

    private void DrawEffectSprite(SpriteBatch batch, WorldFrame f, EffectDef def, float age, Vector2 pos, float scale, Rgb tint, Vector2 dir, int start)
    {
        _scratch.Clear();
        CollectEffect(def, age, pos, scale, tint, dir, start, _scratch);
        foreach (var d in _scratch) BatchX.Blit(batch, d.Texture, d.Dest, new Rectangle(d.Src.X0, d.Src.Y0, d.Src.Width, d.Src.Height), d.Color.ToXna(d.Alpha));
    }

    private readonly List<GlowDraw> _scratch = new(16);

    private void CollectEffect(EffectDef def, float age, Vector2 pos, float scale, Rgb tint, Vector2 dir, int start, List<GlowDraw> into)
    {
        var sample = EffectPlayback.Sample(def, age, start);
        if (sample.Done) return;
        var sprite = sample.Intro ? def.Intro!.Sprite.Def : def.Sprite?.Def;
        if (sprite is null || !Art.TryGetSprite(sprite, "", out var h)) return;
        var col = sprite.Directions > 1 && dir.LengthSquared() > 1e-6f ? FacingMath.ToColumn(dir, sprite.Directions) : 0;
        if (!Art.Source(sprite, h, col, sample.Frame1, out var page, out var src, out var dest, ArtScale.RoundToTarget(pos, _scale), scale)) return;
        into.Add(new GlowDraw(h.Pages[page], src, dest, tint, Math.Clamp(def.Alpha, 0f, 1f)));
    }

    // ── 8층 ───────────────────────────────────────────────────

    private void DrawScreenEffects(SpriteBatch batch, World w, Primitives prim, int tick)
    {
        var hero = w.Hero;
        if (hero.MaxHp <= 0f || hero.Hp / hero.MaxHp >= 0.3f || hero.Dead) return;
        var k = (0.3f - hero.Hp / hero.MaxHp) / 0.3f;
        var a = (0.25f + 0.1f * MathF.Sin(tick * World.Dt * 6f)) * k;
        var size = ArtScale.WorldTarget(_scale);
        var band = 40 * _scale;
        var red = new Rgb(160, 0, 0);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        for (var i = 0; i < 4; i++)
        {
            var aa = a * (1f - i / 4f);
            var o = i * band / 4;
            var t = band / 4;
            batch.Draw(prim.Pixel, new Rectangle(o, o, size.X - 2 * o, t), red.ToXna(aa));
            batch.Draw(prim.Pixel, new Rectangle(o, size.Y - o - t, size.X - 2 * o, t), red.ToXna(aa));
            batch.Draw(prim.Pixel, new Rectangle(o, o + t, t, size.Y - 2 * o - 2 * t), red.ToXna(aa));
            batch.Draw(prim.Pixel, new Rectangle(size.X - o - t, o + t, t, size.Y - 2 * o - 2 * t), red.ToXna(aa));
        }
        batch.End();
    }

    // ── PresentRT 로 ──────────────────────────────────────────

    private void Present(GameRenderContext g, int s)
    {
        var device = g.Device;
        var present = new Int2(g.PresentSize.X, g.PresentSize.Y);
        var k = PresentMath.IntegerScale(present, s);
        var box = g.Letterbox;
        Texture2D src = _worldRT!;
        if (k > 1)
        {
            var size = ArtScale.WorldTarget(s);
            if (_upRT is null || _upRT.Width != size.X * k)
            {
                _upRT?.Dispose();
                _upRT = new RenderTarget2D(device, size.X * k, size.Y * k, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }
            device.SetRenderTarget(_upRT);
            g.Batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
            g.Batch.Draw(_worldRT, new Rectangle(0, 0, _upRT.Width, _upRT.Height), Color.White);
            g.Batch.End();
            src = _upRT;
        }
        g.RestorePresent();
        g.Batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
        g.Batch.Draw(src, box, Color.White);
        g.Batch.End();
    }

    // ── 9층 월드 UI ───────────────────────────────────────────

    private void DrawWorldUi(RenderContext rc, WorldFrame f, ArtServices art)
    {
        var w = f.World;
        _ui.Begin(rc);
        // 피해 숫자(설정에서 끄면 그리지 않음)
        if (_app.Save.Settings.ShowDamageNumbers)
            foreach (ref readonly var n in Fx.Numbers.Items)
            {
                if (!n.Active) continue;
                var age = f.StageTick - n.StartTick + _alphaInterp;
                var a = DamageNumberPool.Alpha(age);
                if (!n.ToHero && f.Fog)
                    a *= w.Enemies.TryGet(n.Target, out var e) ? FogMath.EnemyAlpha(w, e, f.StageTick, f.HeroDraw, e.Pos)
                                                                : FogMath.PosAlpha(w.Visibility, n.Pos, f.StageTick, f.HeroDraw);
                if (a <= 0f) continue;
                var text = n.Value.ToString(CultureInfo.InvariantCulture);
                var size = n.Crit ? 18f : 14f;
                var color = n.ToHero ? new Rgb(255, 70, 60) : n.Crit ? new Rgb(255, 220, 80) : Rgb.White;
                var p = DamageNumberPool.DrawPos(n, age) - f.Camera;
                var m = _ui.MeasureText(text, size);
                _ui.Text(text, new Vector2(p.X - m.X * 0.5f, p.Y - 40f), size, color, a);
            }
        // 정예·우두머리 이름
        foreach (var e in w.Enemies.Items)
        {
            if (e.Removed || e.Dead || e.Rank == EnemyRank.Normal) continue;
            var draw = Vector2.Lerp(e.PrevPos, e.Pos, _alphaInterp);
            var a = f.Fog ? FogMath.EnemyAlpha(w, e, f.StageTick, f.HeroDraw, draw) : 1f;
            if (a <= 0f) continue;
            var name = e.Def.Name;
            if (string.IsNullOrEmpty(name)) continue;
            var color = e.Rank == EnemyRank.Unique ? new Rgb(230, 190, 80) : new Rgb(110, 150, 255);
            var p = draw - f.Camera - new Vector2(0, e.BodyTop > 0 ? e.BodyTop + 8 : 84);
            var m = _ui.MeasureText(name, 12f);
            _ui.Text(name, new Vector2(p.X - m.X * 0.5f, p.Y), 12f, color, a);
        }
        // 주인공 발밑 체력 막대
        var hero = w.Hero;
        if (hero.MaxHp > 0f && !hero.Dead)
        {
            var hp = Math.Clamp(hero.Hp / hero.MaxHp, 0f, 1f);
            var p = f.HeroDraw - f.Camera + new Vector2(-16, 6);
            _ui.Fill(new RectF(p.X, p.Y, p.X + 32, p.Y + 4), Rgb.Black, 0.7f);
            _ui.Fill(new RectF(p.X, p.Y, p.X + 32 * hp, p.Y + 4), new Rgb(200, 30, 30), 0.95f);
        }
        DrawGateIcons(f, art);
        _ui.End();
    }

    /// <summary>열린 출구 문 위(벽 높이 위) 보상 아이콘 48px, 주인공이 300px 안이면 그 아래 이름·층(7.11).</summary>
    private void DrawGateIcons(WorldFrame f, ArtServices art)
    {
        var w = f.World;
        if (w.Room is not { } room || room.Phase < RoomPhase.Doors || w.Arena is not { } grid) return;
        foreach (var (id, gate) in w.Gates)
        {
            if (!GateSeals.TryGetExit(room, id, out var exit) || gate.Cells.Count == 0) continue;
            var center = Vector2.Zero;
            foreach (var c in gate.Cells) center += grid.Iso.CellCenter(c.X, c.Y);
            center /= gate.Cells.Count;
            var top = center - new Vector2(0, grid.Iso.ColumnHeight + 24) - f.Camera;
            if (exit.Reward?.Icon is { } icon && art.TryGetSprite(icon.Sprite.Def, "", out var h)
                && art.Source(icon.Sprite.Def, h, icon.Dir ?? 0, icon.Frame ?? 1, out _, out var src, out _, Vector2.Zero, 1f))
            {
                var frame = exit.NextKind == RoomKind.Elite ? new Rgb(220, 40, 40) : new Rgb(200, 170, 90);
                _ui.Fill(new RectF(top.X - 26, top.Y - 26, top.X + 26, top.Y + 26), frame, 0.9f);
                _ui.Sprite(icon.Sprite.Def.Id, src, new RectF(top.X - 24, top.Y - 24, top.X + 24, top.Y + 24), icon.Tint ?? Rgb.White);
            }
            if (Vector2.Distance(f.HeroDraw, center) > 300f) continue;
            var label = GateSeals.Label(exit);
            var m = _ui.MeasureText(label, 13f);
            _ui.Text(label, new Vector2(top.X - m.X * 0.5f, top.Y + 30), 13f, Rgb.White);
            if (GateSeals.FloorLabel(exit) is { } floor)
            {
                var fm = _ui.MeasureText(floor, 12f);
                _ui.Text(floor, new Vector2(top.X - fm.X * 0.5f, top.Y + 48), 12f, new Rgb(200, 200, 200));
            }
        }
    }

    // ── 도우미 ────────────────────────────────────────────────

    private static int CountInSight(VisibilityField f)
    {
        if (!f.Enabled) return 0;
        var n = 0;
        for (var ty = 0; ty < f.Size; ty++)
            for (var tx = 0; tx < f.Size; tx++)
                if (f.InSight(tx, ty)) n++;
        return n;
    }

    private static Rgb Mul(Rgb a, Rgb b) => new((byte)(a.R * b.R / 255), (byte)(a.G * b.G / 255), (byte)(a.B * b.B / 255));

    private static RectF Grow(RectF r, float k)
    {
        var c = r.Center;
        var hw = r.Width * 0.5f * k;
        var hh = r.Height * 0.5f * k;
        return new RectF(c.X - hw, c.Y - hh, c.X + hw, c.Y + hh);
    }

    public void Dispose()
    {
        _worldRT?.Dispose();
        _upRT?.Dispose();
        _light?.Dispose();
    }

    /// <summary>7층 그리기 하나(텍스처·원본·자리·색·알파).</summary>
    private readonly record struct GlowDraw(Texture2D Texture, RectI Src, RectF Dest, Rgb Color, float Alpha);
}
