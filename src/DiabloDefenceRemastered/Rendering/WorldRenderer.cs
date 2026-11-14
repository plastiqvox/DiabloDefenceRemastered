using System.Globalization;
using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
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
/// <item>층(7.3): 0 바닥 → 0b 맵 밖 뒤쪽 입장 유닛·뒷둘레 벽 → 1 장판·예고·발밑 원 → 2 시체 → 3 획득물(원작 바닥 그림·대체) → 4 y 정렬(유닛·안쪽 벽·바닥 위 조각·덧그림·상인·
/// 문 봉인 창살(단계 2 wp5) — 벽은 늘 불투명) → 5 앞둘레 턱·맵 밖 앞쪽 입장 유닛·창살 그림이 없는 문의 코드 봉인 → 6 빛·안개 곱하기 → 벽 뒤 유닛 윤곽선(<see cref="UnitOutlines"/>)
/// → 7 발광(투사체·이펙트·발광·번쩍임·예고 테두리·보석 빛) → 8 화면 효과 → (<c>PresentRT</c>) 9 월드 UI(보상 물체 둘레 아이콘·문 위 아이콘 포함).</item>
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
    private static readonly Rgb EnemyHalo = new(255, 150, 60);

    private readonly AppServices _app;
    private readonly WorldFrame _frame = new();
    private readonly List<DrawItem> _items = new(1024);
    private readonly List<GlowDraw> _glows = new(256);
    private readonly List<GlowDraw> _additive = new(256);
    private readonly List<GlowDraw> _alpha7 = new(256);
    private readonly List<Telegraph> _telegraphs = new(64);
    private readonly UnitOutlines _outlines = new();
    private readonly UiCanvas _ui;
    private World? _world;
    private ArenaRenderer? _arena;
    private LightRenderer? _light;
    /// <summary>셰이더 빛(7.7 — 셰이더 파일을 못 읽으면 null, 빛 텍스처로).</summary>
    private ShaderLight? _shaderLight;
    private bool _shaderTried;
    private RenderTarget2D? _worldRT;
    private RenderTarget2D? _upRT;
    private int _scale;
    private int _enemiesDrawn, _enemiesHidden;
    /// <summary>이번 프레임 주인공 그림 묶음(겉모습 — <see cref="HeroLookArt"/>, v0.9.1 V10). 직업이 없으면 null.</summary>
    private SpriteSetDef? _heroSet;
    /// <summary>문 봉인 창살 그림(액트마다 한 번 — <see cref="GateGrateArt.Build"/>). null 이면 모든 문이 코드 그림 봉인.</summary>
    private GateGrateArt? _grateArt;
    /// <summary>지금 방의 나무 문 그림(2026-11-13 — 있으면 창살 대신). 맵이 바뀔 때마다.</summary>
    private GateDoorArt? _doorArt;
    private StageDef? _grateArtStage;
    private bool _grateArtBuilt;
    private readonly List<GrateDraw> _grateDraws = new(32);

    /// <summary>상인 서기 반복 프레임 시간(초 — 원작 값을 원본으로 확인하지 못한 첫 값, 그림 묶음에 frame_time 을 적지 않음: 13장 WP5 단계 2 기록).</summary>
    public const float MerchantFrameTime = 0.15f;

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
        _shaderLight?.Attach(world.Arena);
        // 창살 그림은 액트 맵 풀에서 빌려 오므로 액트마다 한 번(액트 없는 월드는 그 맵만 보고 매번)
        // 나무 문 그림은 그 방 맵 그림에 함께 구워 있으므로 맵마다(없으면 창살)
        _doorArt = world.Map is { } doorMap ? GateDoorArt.Build(doorMap, art) : null;
        if (!_grateArtBuilt || world.Stage is null || !ReferenceEquals(_grateArtStage, world.Stage))
        {
            _grateArt = GateGrateArt.Build(world, art);
            _grateArtStage = world.Stage;
            _grateArtBuilt = true;
        }
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
        _heroSet = world.HeroClass is { } heroClass ? HeroLookArt.Pick(heroClass, hero, world.Defs, SetDrawable) : null;
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
        _outlines.Clear();
        _shaderLight?.BeginFrame();
        _arena?.Prepare(f.View.Inflate(64));

        var toTarget = Matrix.CreateTranslation(-cam.X, -cam.Y, 0f) * Matrix.CreateScale(s);
        var prim = g.Primitives;
        _lightTex = prim.Light;

        // ── 0~5층 ───────────────────────────────────────────────
        device.SetRenderTarget(_worldRT);
        device.Clear(Color.Black);
        // 셰이더 그림자면 원작 구운 그림자 픽셀을 버리고 그린다(그림자는 빛 지도에서 빛 반대쪽으로 다시 — ShaderLight.DrawShadows)
        var noShadow = _shaderLight?.NoShadowEffect(toTarget, new Int2(_worldRT!.Width, _worldRT.Height));
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, noShadow, toTarget);
        _arena?.DrawFloor(batch, f.View, prim);
        DrawOutsideEnemies(batch, f, back: true);
        _arena?.DrawGroup(batch, WallGroup.Back, prim, _shaderLight);
        DrawLayer1(batch, f, prim);
        DrawCorpses(batch, f);
        DrawPickups(batch, f, prim);
        DrawLayer4(batch, f, prim);
        _arena?.DrawGroup(batch, WallGroup.Front, prim, _shaderLight);
        DrawOutsideEnemies(batch, f, back: false);
        DrawGateSeals(batch, f, prim);
        batch.End();

        // ── 셰이더 빛: 벽·유닛이 서 있는 바닥 y 버퍼(위에서 모은 그림을 같은 차례로) ───
        if (_shaderLight is not null && _arena is not null)
            _shaderLight.DrawGround(batch, new Int2(_worldRT!.Width, _worldRT.Height), toTarget, cam);

        // ── 6층 빛·안개 ─────────────────────────────────────────
        CollectEffectLights(f);
        _light!.Build(batch, f, _arena, _app.Options.Reveal);
        device.SetRenderTarget(_worldRT);
        batch.Begin(SpriteSortMode.Deferred, FogBlend.Multiply, SamplerState.LinearClamp);
        batch.Draw(_light.LightRT, new Rectangle(0, 0, _worldRT!.Width, _worldRT.Height), Color.White);
        batch.End();

        // ── 6층 뒤: 벽 뒤 유닛 윤곽선(빛에 어두워지지 않게 곱한 뒤, 발광 앞) ─────
        if (_arena is not null) _outlines.Draw(device, batch, prim, _arena, f.View, toTarget, _worldRT.Width, _worldRT.Height);

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
            // 깊이·스텐실은 벽 뒤 윤곽선(UnitOutlines)만 쓴다
            _worldRT = new RenderTarget2D(device, size.X, size.Y, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            _scale = s;
        }
        if (!_shaderTried)
        {
            _shaderTried = true;
            _shaderLight = ShaderLight.TryLoad(device, _app.Log);
            _shaderLight?.Attach(world.Arena);
        }
        // 셰이더 빛이면 LightRT 를 WorldRT 크기로(전체 해상도), 아니면 480×270
        var lightSize = _shaderLight is not null ? new Int2(_worldRT.Width, _worldRT.Height) : new Int2(LightRenderer.Width, LightRenderer.Height);
        if (_light is null || _light.LightSize != lightSize)
        {
            _light?.Dispose();
            _light = new LightRenderer(device, prim, lightSize, _shaderLight);
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
        if (_heroSet is not { } set) return;
        if (!UnitFrame(set, hero.Anim.Clip, hero.Anim, hero.Facing, "", 1f, f.HeroDraw, out var h, out var page, out var src, out var dest)) return;
        DrawUnit(batch, h.Pages[page], src, dest, Rgb.White, 1f);
        _shaderLight?.AddGround(h.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, f.HeroDraw.Y);
        if (_arena is not null)
            _outlines.Add(h.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, DrawOrder.UnitSortY(_arena.Table, _arena.Iso, f.HeroDraw), f.View, UnitOutlines.Kind.Hero);
        var shape = new ShapeDraw(h.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, f.HeroDraw, 1f, Forced: true, MinLight: !FrontWallCovers(dest), FogShape: false, Caster: !hero.Dead);
        f.Shapes.Add(shape);
        // 보스 묶음 B(6.20 ㉑): 순간이동 시전 중·도착 무적 동안 푸른 가산 덧칠 — 피격 번쩍임과 겹치면 덧칠만.
        // 그림 색조 덧칠은 그림 색에 곱해져 붉은 옷 위에서는 거의 안 보여(캡처) 몸 둘레 푸른 빛(빛 텍스처)을 같은 색·알파로 한 겹 더 깐다(기획 38.2 "주인공에 푸른 빛")
        if (!hero.Dead && (hero.BlinkCast > 0f || hero.BlinkInvulnTime > 0f))
        {
            _glows.Add(new GlowDraw(h.Pages[page], src, dest, BlinkGlow, BlinkGlowAlpha));
            if (_lightTex is { } light)
            {
                var c = dest.Center;
                var half = MathF.Max(dest.Width, dest.Height) * 0.6f;
                _additive.Add(new GlowDraw(light, new RectI(0, 0, light.Width, light.Height), new RectF(c.X - half, c.Y - half, c.X + half, c.Y + half), BlinkGlow, BlinkGlowAlpha));
            }
        }
        else if (hero.HitFlashTime > 0f) _glows.Add(new GlowDraw(h.Pages[page], src, dest, Rgb.White, 0.8f));
        // 냉기 둔화(2026-11-14, 기획 9.3): 둔화 동안 몸에 하늘색 덧칠 + 둘레 빛(순간이동 덧칠과 같은 방식 — 붉은 옷 위에서도 보이게)
        else if (!hero.Dead && hero.ChillTime > 0f && f.World.Defs.Rules.Hero?.Chill is { } chill)
        {
            _glows.Add(new GlowDraw(h.Pages[page], src, dest, chill.Tint, ChillGlowAlpha));
            if (_lightTex is { } light)
            {
                var c = dest.Center;
                var half = MathF.Max(dest.Width, dest.Height) * 0.55f;
                _additive.Add(new GlowDraw(light, new RectI(0, 0, light.Width, light.Height), new RectF(c.X - half, c.Y - half, c.X + half, c.Y + half), chill.Tint, ChillGlowAlpha));
            }
        }
    }

    private const float ChillGlowAlpha = 0.45f;
    /// <summary>우두머리 이름판 둘째 줄(종류·접두사) 색.</summary>
    private static readonly Rgb NamePlateAffixColor = new(215, 210, 195);

    /// <summary>순간이동 덧칠 색(150,190,255 — 숙련 아이콘 색조와 같음).</summary>
    private static readonly Rgb BlinkGlow = new(150, 190, 255);
    /// <summary>
    /// 덧칠 알파. 가산 묶음은 미리 곱한 색에 알파를 한 번 더 곱해(SourceAlpha) 실제 세기 ≈ 알파² 다 — 설계 α0.35 를 그대로 넣으면 0.12 라 캡처에서 보이지 않아
    /// 0.6(실제 ≈ 0.36)으로 둔다.
    /// </summary>
    private const float BlinkGlowAlpha = 0.6f;

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
        _shaderLight?.AddGround(tex, new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, draw.Y);
        // 벽 뒤 윤곽(안개에 반 넘게 가린 적·맵 밖 입장 중인 적은 뺌)
        if (_arena is not null && a >= 0.5f && _arena.Iso.IsInsideMap(e.Pos))
            _outlines.Add(tex, new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, DrawOrder.UnitSortY(_arena.Table, _arena.Iso, draw), f.View,
                          def.Rank is DefRank.Miniboss or DefRank.Boss ? UnitOutlines.Kind.Elite : UnitOutlines.Kind.Enemy);
        var forced = fogA >= 1f && f.Fog && (def.Rank is DefRank.Miniboss or DefRank.Boss || e.Visual.ForceShown);
        f.Shapes.Add(new ShapeDraw(tex, new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, draw, a, forced, !FrontWallCovers(dest), FogShape: true,
                                   Caster: _arena is not null && _arena.Iso.IsInsideMap(e.Pos)));
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
            if (def.Light is { } light) f.ShortLights.Add(new PointLight(pos, hz.Radius * light.RadiusMul, light.Color, 1f));
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

    /// <summary>
    /// 3층 획득물(설계서 6.9·6.19, 기획 11.1·11.4): <see cref="PickupDef.Floor"/> 원작 그림이 있으면 그것(<see cref="PickupArt.FloorFrame"/> — 떨어짐 애니 → 마지막 프레임, 보석은 그 프레임 하나,
    /// 열린 상자는 빈 상자), 못 쓰면 <see cref="PickupDef.Draw"/> 대체(exe PNG·코드 마름모 — 열린 상자는 흐리게). 경험치 보석은 <c>draw.color</c> 가산 빛(7층, 원작 돌이면 0.6배).
    /// 시야 밖이면 빛까지 그리지 않는다(기획 11.1 시야 규칙).
    /// </summary>
    private void DrawPickups(SpriteBatch batch, WorldFrame f, Primitives prim)
    {
        var w = f.World;
        foreach (var p in w.Pickups.Items)
        {
            if (p.Removed) continue;
            var pos = Vector2.Lerp(p.PrevPos, p.Pos, _alphaInterp);
            var fogA = f.Fog ? FogMath.PosAlpha(w.Visibility, pos, f.StageTick, f.HeroDraw) : 1f;
            if (fogA <= 0f) continue;
            var opened = PickupArt.IsOpenedObject(p);
            var floorDrawn = false;
            if (p.Def.Floor is { } floor && floor.Sprite.Def is var fs && Art.TryGetSprite(fs, "", out var fh))
            {
                var frame1 = PickupArt.FloorFrame(floor, fs.Frames, p.Age, opened);
                if (Art.Source(fs, fh, floor.Dir ?? 0, frame1, out var page, out var src, out var dest, ArtScale.RoundToTarget(pos, _scale), floor.Scale ?? 1f))
                {
                    DrawUnit(batch, fh.Pages[page], src, dest, floor.Tint ?? Rgb.White, fogA);
                    f.Shapes.Add(new ShapeDraw(fh.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, pos, fogA, false, true, FogShape: true));
                    // 보석 돌: 어두운 회색 돌에 색을 곱하면 탁해서(미리보기) 같은 그림을 등급 색으로 가산 한 겹 더 — 회색조 변종 대신(13장 WP5 단계 2 기록)
                    if (p.Def.Kind == PickupKind.Xp && floor.Tint is { } gemTint) _glows.Add(new GlowDraw(fh.Pages[page], src, dest, gemTint, GemLift * fogA));
                    floorDrawn = true;
                }
            }
            if (!floorDrawn) DrawPickupFallback(batch, f, prim, p, pos, opened ? fogA * 0.45f : fogA);
            GemGlow(p.Def, pos, PickupArt.GlowScale(p.Def, floorDrawn) * fogA);
        }
    }

    /// <summary>대체 그림(<c>draw</c> — exe PNG 반복 그림 또는 코드 마름모). 단계 1 그대로.</summary>
    private void DrawPickupFallback(SpriteBatch batch, WorldFrame f, Primitives prim, Pickup p, Vector2 pos, float alpha)
    {
        var d = p.Def.Draw;
        var tint = d.Tint ?? d.Color ?? Rgb.White;
        if (d.Sprite?.Def is { } sprite && Art.TryGetSprite(sprite, "", out var h))
        {
            var frame1 = d.Frame ?? Math.Min(sprite.Frames, 1 + (int)(p.Age / ProjectileFrameTime));
            if (Art.Source(sprite, h, 0, frame1, out var page, out var src, out var dest, ArtScale.RoundToTarget(pos, _scale), d.Scale))
            {
                DrawUnit(batch, h.Pages[page], src, dest, tint, alpha);
                f.Shapes.Add(new ShapeDraw(h.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, pos, alpha, false, true, FogShape: true));
            }
            return;
        }
        // 코드 그림(보석 "gem" 등): 작은 마름모
        var size = d.Size is { } sz ? new Vector2(sz.X, sz.Y) : new Vector2(10, 14);
        size *= d.Scale;
        var rect = new RectF(pos.X - size.X * 0.5f, pos.Y - size.Y, pos.X + size.X * 0.5f, pos.Y);
        BatchX.Blit(batch, prim.Diamond, rect, new Rectangle(0, 0, prim.Diamond.Width, prim.Diamond.Height), tint.ToXna(alpha));
        f.Shapes.Add(new ShapeDraw(prim.Diamond, new Rectangle(0, 0, prim.Diamond.Width, prim.Diamond.Height), rect, pos, alpha, false, true, FogShape: true));
    }

    /// <summary>
    /// 경험치 보석 가산 빛(7층 — 어둠·안개 곱하기 뒤라 어두운 바닥에서도 보인다). 크기 = 마름모 크기의 3.2배 지름, 밝기 0.8 — <paramref name="k"/> 배(원작 돌 0.6 × 안개 알파).
    /// 첫 값 — <c>--preview sprites</c> 로 맞춤(기획 v0.9.3 "은은하게").
    /// </summary>
    private void GemGlow(PickupDef def, Vector2 pos, float k)
    {
        if (k <= 0f || _lightTex is null || def.Draw.Color is not { } color) return;
        var size = def.Draw.Size is { } sz ? MathF.Max(sz.X, sz.Y) : 14f;
        var half = size * 1.6f * MathF.Sqrt(k);
        var c = pos - new Vector2(0f, size * 0.5f);
        _additive.Add(new GlowDraw(_lightTex!, new RectI(0, 0, _lightTex!.Width, _lightTex.Height), new RectF(c.X - half, c.Y - half, c.X + half, c.Y + half), color, 0.8f * k));
    }

    /// <summary>원작 돌 보석 위에 같은 그림을 등급 색으로 가산하는 세기(첫 값 — 미리보기로 맞춤).</summary>
    public const float GemLift = 1f;

    /// <summary>빛 텍스처(<see cref="Primitives.Light"/>) — 3층에서 7층 목록에 넣을 때 쓴다.</summary>
    private Texture2D? _lightTex;

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
        if (MerchantSpot(w) is { } shopSpot)
            _items.Add(new DrawItem(_arena is null ? shopSpot.Y : DrawOrder.UnitSortY(_arena.Table, _arena.Iso, shopSpot), int.MaxValue, DrawKind.Merchant, 0));
        CollectGrates(f);
        System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_items).Sort(DrawItem.Comparer.Compare);

        foreach (var it in _items)
        {
            switch (it.Kind)
            {
                case DrawKind.Hero: DrawHero(batch, f); break;
                case DrawKind.Enemy: DrawEnemy(batch, f, enemies[it.Index]); break;
                case DrawKind.Merchant: DrawMerchant(batch, f, prim); break;
                case DrawKind.Grate: DrawGrate(batch, it, _grateDraws); break;
                default:
                    // 벽은 늘 불투명(2026-11-13 — 가려진 유닛은 UnitOutlines 윤곽선)
                    _arena!.DrawItem(batch, it.Index, prim, 1f);
                    if (_shaderLight is not null) _arena.AddGround(_shaderLight, it.Index);
                    break;
            }
        }
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

    // ── 4층 상인·창살(단계 2 wp5) ──────────────────────────────────

    /// <summary>상인이 서는 자리(기획 11.7 "상인이 서는 자리"): 상인 방(<c>Room.Shop</c>)의 보상·문 단계에 <c>Room.RewardSpot</c>. 아니면 null.</summary>
    private static Vector2? MerchantSpot(World w) =>
        w.Room is { Shop: not null, Phase: RoomPhase.Reward or RoomPhase.Doors } room ? room.RewardSpot : null;

    /// <summary>상인 서기 반복(충돌 없음 — 그림만). 그림이 없으면 코드 그림(어두운 겉옷 + 머리).</summary>
    private void DrawMerchant(SpriteBatch batch, WorldFrame f, Primitives prim)
    {
        var w = f.World;
        if (MerchantSpot(w) is not { } spot || w.Room!.Shop is not { } shop) return;
        var fogA = f.Fog ? FogMath.PosAlpha(w.Visibility, spot, f.StageTick, f.HeroDraw) : 1f;
        if (fogA <= 0f) return;
        var set = w.Defs.Merchants.TryGet(shop.MerchantId, out var m) ? m.Sprite?.Def : null;
        if (set?.Clips.Idle?.Sprite.Def is { } sprite && Art.TryGetSprite(sprite, "", out var h))
        {
            var frame1 = 1 + (int)(f.StageTick * World.Dt / MerchantFrameTime) % Math.Max(1, sprite.Frames);
            if (Art.Source(sprite, h, 0, frame1, out var page, out var src, out var dest, ArtScale.RoundToTarget(spot, _scale), 1f))
            {
                DrawUnit(batch, h.Pages[page], src, dest, Rgb.White, fogA);
                f.Shapes.Add(new ShapeDraw(h.Pages[page], new Rectangle(src.X0, src.Y0, src.Width, src.Height), dest, spot, fogA, false, !FrontWallCovers(dest), FogShape: true));
            }
            return;
        }
        var body = new RectF(spot.X - 10, spot.Y - 44, spot.X + 10, spot.Y);
        BatchX.Blit(batch, prim.Pixel, body, new Rectangle(0, 0, 1, 1), new Rgb(70, 52, 40).ToXna(fogA));
        BatchX.Centered(batch, prim.Circle, spot - new Vector2(0f, 52f), new Vector2(16f, 16f), new Rgb(190, 150, 120).ToXna(fogA));
    }

    /// <summary>창살 기둥 하나(이번 프레임 — <see cref="CollectGrates"/>): 원본 그림·자리(내려간 만큼 잘림)·적 드나듦.</summary>
    private readonly record struct GrateDraw(Texture2D Page, Rectangle Src, RectF Dest, bool EnemyPassing);

    /// <summary>
    /// 문 봉인 창살(v0.9.4 V13, 7.11)을 4층 정렬 목록에: 문 틈 메가마다 변의 조각(<see cref="GateGrates.PieceName"/>)의 기둥 그림을 그 하위 칸 자리에.
    /// 출구로 열린 문은 <see cref="GateGrates.Sink"/> 만큼 바닥선(기둥 그림 아래 끝)에서 잘려 아래로 내려간다. 그림이 없는 조각은 <see cref="DrawGateSeals"/> 의 코드 그림.
    /// </summary>
    private void CollectGrates(WorldFrame f)
    {
        _grateDraws.Clear();
        _placed.Clear();
        var w = f.World;
        if ((_grateArt is null && _doorArt is null) || _arena is null || w.Room is not { } room) return;
        var iso = _arena.Iso;
        foreach (var (id, gate) in w.Gates)
        {
            var from = _placed.Count;
            var sink = GateGrates.Sink(room, id, f.StageTick);
            var passing = GateGrates.EnemyPassing(gate, iso, w.Enemies.Items);   // 적이 드나드는 동안: 창살은 흐리게, 문은 열림
            if (_doorArt is not null && _doorArt.Place(gate, iso, open: sink > 0f || passing, _placed))
                passing = false;   // 나무 문(2026-11-13): 열린 문 그림으로 지나가므로 흐리게 하지 않는다
            else if (_grateArt is null || !_grateArt.Place(gate, iso, sink, _placed)) continue;
            if (_placed.Count == from) continue;
            for (var i = from; i < _placed.Count; i++)
            {
                var p = _placed[i];
                if (!WallFade.Overlaps(p.Full, f.View)) continue;
                _grateDraws.Add(new GrateDraw(p.Page, p.Src, p.Dest, passing));
                var sortY = DrawOrder.CellSortY(iso, new ArenaCell(p.Tx, p.Ty, p.Kind, 0));
                _items.Add(new DrawItem(sortY, DrawOrder.CellTiebreak(iso, p.Tx, p.Ty), DrawKind.Grate, _grateDraws.Count - 1));
            }
        }
    }

    private readonly List<GateGrateArt.Placed> _placed = new(32);

    /// <summary>
    /// 창살 기둥 하나 — 벽처럼 늘 불투명(2026-11-13 사용자 결정, 기획 10.7 — 가려진 유닛은 <see cref="UnitOutlines"/>).
    /// 적이 드나드는 문만 <see cref="WallFade.Alpha"/> 로 흐리게(<see cref="GateGrates.EnemyPassing"/>).
    /// </summary>
    private static void DrawGrate(SpriteBatch batch, in DrawItem it, List<GrateDraw> draws)
    {
        var g = draws[it.Index];
        var a = g.EnemyPassing ? WallFade.Alpha : 1f;
        BatchX.Blit(batch, g.Page, g.Dest, g.Src, new Color(a, a, a, a));
    }

    /// <summary>그 묶음을 그릴 수 있나(서기·걷기 클립 그림을 미리 읽었고 자원 파일이 있음 — 겉모습 고르기).</summary>
    private bool SetDrawable(SpriteSetDef set) =>
        (set.Clips.Idle ?? set.Clips.Walk)?.Sprite.Def is { } s && Art.TryGetSprite(s, "", out _);

    // ── 5층 문 봉인(7.11) ───────────────────────────────────────

    /// <summary>
    /// 단계 1 코드 그림 봉인(붉은 바닥 빛·낮은 창살·룬) — v0.9.4 V13 부터는 원작 창살 그림(<see cref="CollectGrates"/>)을 쓸 수 없는 문만(자원 없음·창살 맵이 없는 액트).
    /// </summary>
    private void DrawGateSeals(SpriteBatch batch, WorldFrame f, Primitives prim)
    {
        var w = f.World;
        if (w.Room is not { } room || w.Arena is not { } grid) return;
        var pulse = 0.85f + 0.15f * MathF.Sin(f.StageTick * World.Dt * 2.2f);
        var openFade = room.Phase >= RoomPhase.Doors ? Math.Clamp(1f - (f.StageTick - room.PhaseTick) / 18f, 0f, 1f) : 1f;
        foreach (var (id, gate) in w.Gates)
        {
            if (_doorArt is not null && _doorArt.Has(gate.Side)) continue;                                      // 원작 나무 문(4층 — CollectGrates)
            if (_grateArt is not null && _grateArt.TryGet(GateGrates.PieceName(gate.Side), out _)) continue;   // 원작 창살(4층 — CollectGrates)
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
            f.ShortLights.Add(new PointLight(fx.Event.Pos, radius, light.Color, 1f - age / Math.Max(0.01f, duration)));
        }
        // 날아가는 투사체 빛(2026-11-13): 바닥 자리 위 light.height 에 뜬 점 빛 — 셰이더 빛이 3D 거리로 바닥·벽에 비춘다
        foreach (var p in f.World.Projectiles.Items)
        {
            if (p.Removed || p.Def.Light is not { } pl) continue;
            f.ShortLights.Add(new PointLight(Vector2.Lerp(p.PrevPos, p.Pos, _alphaInterp), pl.Radius * (p.Scale > 0f ? p.Scale : 1f), pl.Color, pl.Intensity, pl.Height));
        }
        // 몸에서 빛을 내는 적(불타는 해골 — 2026-11-13): 어둠 속에서 다가오는 게 바닥·벽에 비쳐 보인다
        // 우두머리 불꽃·냉기·번개 강화(2026-11-14)는 접두사 light — 정의 빛이 없을 때
        foreach (var e in f.World.Enemies.Items)
        {
            if (!e.IsAlive || (e.Def.Light ?? AffixLight(f.World, e)) is not { } el) continue;
            f.ShortLights.Add(new PointLight(Vector2.Lerp(e.PrevPos, e.Pos, _alphaInterp), el.Radius, el.Color, el.Intensity, el.Height));
        }
    }

    private static CarriedLightDef? AffixLight(World w, Enemy e)
    {
        for (var i = 0; i < 2; i++)
            if (e.AffixIdx[i] >= 0 && w.Defs.Affixes[e.AffixIdx[i]].Light is { } light) return light;
        return null;
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
            if (p.Faction == Faction.Enemy)
            {
                _additive.Add(new GlowDraw(h.Pages[page], src, Grow(dest, 1.15f), EnemyRim, 0.35f));
                _additive.Add(draw with { Color = EnemyHalo, Alpha = 0.8f });   // 탄 그림 자체를 따뜻한 색으로 한 겹 더 — 어두운 화살대가 바닥과 갈리게
                // 적 탄 둘레 빛 — 가는 화살이 바닥 무늬에 묻히지 않게(플레이 관찰). 크기는 판정 반경을 따르고, 자리는 그림 가운데(띄워 그린 높이 포함)
                var half = Math.Max(16f, p.Def.Radius * 3.5f);
                var c = dest.Center;
                _additive.Add(new GlowDraw(prim.Light, new RectI(0, 0, prim.Light.Width, prim.Light.Height),
                    new RectF(c.X - half, c.Y - half, c.X + half, c.Y + half), EnemyHalo, 0.6f));
            }
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
        // 판정 점(보스 묶음 A 이음새, 6.20 ⑤): hero_hit_radius 가 있는 적 탄이 주인공 120px 안에 있으면 발 자리에 흰 점(r3)과 엷은 테(r6) — "판정만 작게" 를 읽게
        if (HitDotVisible(w))
        {
            BatchX.Centered(batch, prim.Circle, f.HeroDraw, new Vector2(6f), Rgb.White.ToXna(0.9f));
            BatchX.Centered(batch, prim.Ring, f.HeroDraw, new Vector2(12f), Rgb.White.ToXna(0.45f));
        }
        batch.End();

        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, null, null, null, toTarget);
        foreach (var d in _additive) BatchX.Blit(batch, d.Texture, d.Dest, new Rectangle(d.Src.X0, d.Src.Y0, d.Src.Width, d.Src.Height), d.Color.ToXna(d.Alpha));
        foreach (var d in _glows) BatchX.Blit(batch, d.Texture, d.Dest, new Rectangle(d.Src.X0, d.Src.Y0, d.Src.Width, d.Src.Height), d.Color.ToXna(d.Alpha));
        batch.End();
    }

    /// <summary>판정 점을 그릴까: <c>hero_hit_radius</c> 가 있는 적 탄이 주인공에서 120px 안(6.20 ⑤). 시뮬레이션과 무관.</summary>
    private static bool HitDotVisible(World w)
    {
        if (w.Hero.Dead) return false;
        foreach (var p in w.Projectiles.Items)
            if (!p.Removed && p.Faction == Faction.Enemy && p.Def.HeroHitRadius is not null && Vector2.DistanceSquared(p.Pos, w.Hero.Pos) <= 120f * 120f) return true;
        return false;
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
            // 디아2식 이름판(2026-11-14, 기획 9.1·9.2): 정예 = 파란 "<접두사> <종류>", 우두머리 = 금색 고유 이름 + 밑에 접두사 줄
            var (name, line2) = Core.Sim.Enemies.UniqueNames.Plate(w, e);
            if (string.IsNullOrEmpty(name)) continue;
            var color = e.Rank == EnemyRank.Unique ? new Rgb(230, 190, 80) : new Rgb(110, 150, 255);
            var p = draw - f.Camera - new Vector2(0, e.BodyTop > 0 ? e.BodyTop + 8 : 84);
            if (line2 is not null)
            {
                p.Y -= 12f;
                var m2 = _ui.MeasureText(line2, 10f);
                _ui.Text(line2, new Vector2(p.X - m2.X * 0.5f, p.Y + 14f), 10f, NamePlateAffixColor, a);
            }
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
        DrawRewardOsd(f, art);
        DrawGateIcons(f, art);
        _ui.End();
    }

    /// <summary>
    /// 보상 물체 둘레 아이콘(v0.9.1 V8, 기획 11.4 — <see cref="RewardOsd"/>): 방 안 보상 물체·정예 상자 위쪽 호, 주인공이 300px 안이면 짧은 글자. 시야 밖이면 그리지 않는다.
    /// </summary>
    private void DrawRewardOsd(WorldFrame f, ArtServices art)
    {
        var w = f.World;
        foreach (var p in w.Pickups.Items)
        {
            if (p.Removed || (p.Reward is null && p.Def.Kind != PickupKind.Chest)) continue;
            var pos = Vector2.Lerp(p.PrevPos, p.Pos, _alphaInterp);
            var fogA = f.Fog ? FogMath.PosAlpha(w.Visibility, pos, f.StageTick, f.HeroDraw) : 1f;
            if (fogA <= 0f) continue;
            var icons = RewardOsd.Icons(p, w);
            if (icons.Count == 0) continue;
            var anchor = pos - f.Camera - new Vector2(0f, RewardOsd.ArcLift);
            RewardIcons.Osd(_ui, art, icons, anchor, f.StageTick, Vector2.Distance(f.HeroDraw, pos) <= RewardOsd.LabelRange, fogA);
        }
    }

    /// <summary>열린 출구 문 위(벽 높이 위) 보상 아이콘 48px(<see cref="RewardIcons.Door"/>), 주인공이 300px 안이면 그 아래 이름·층(7.11).</summary>
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
            if (exit.Reward is { } reward)
            {
                // 48px 칸에 비율대로 맞춤(v0.9 — 상인 문은 상인 그림 첫 프레임, 회복 문은 붉은 물약). 테 = 정예 방 붉게
                var frame = exit.NextKind == RoomKind.Elite ? new Rgb(220, 40, 40) : new Rgb(200, 170, 90);
                _ui.Fill(new RectF(top.X - 26, top.Y - 26, top.X + 26, top.Y + 26), frame, 0.9f);
                _ui.Fill(new RectF(top.X - 24, top.Y - 24, top.X + 24, top.Y + 24), new Rgb(24, 20, 18), 0.85f);
                RewardIcons.Door(_ui, art, w.Defs, reward, new RectF(top.X - 23, top.Y - 23, top.X + 23, top.Y + 23));
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
        _outlines.Dispose();
        _shaderLight?.Dispose();
    }

    /// <summary>7층 그리기 하나(텍스처·원본·자리·색·알파).</summary>
    private readonly record struct GlowDraw(Texture2D Texture, RectI Src, RectF Dest, Rgb Color, float Alpha);
}
