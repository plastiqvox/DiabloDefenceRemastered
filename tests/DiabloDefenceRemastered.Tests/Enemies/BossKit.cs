using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 보스 묶음 A 시험 도우미(설계서 6.20): 합성 탄(<c>hero_hit_radius</c> 6), 패턴·순서 정의 만들기, 가짜 이웃 월드(<see cref="Wp4Defs.World"/> — 투사체·예고는 기록만)에서 조건까지 돌리기.
/// </summary>
public static class BossKit
{
    public static readonly ProjectileDef Orb = new()
    {
        Id = "pj_t_orb", Sprite = DefRef<SpriteDef>.To(TestDefs.Sprite("t_orb")), Radius = 5f, HeroHitRadius = 6f, Speed = 120f, Range = 640f,
    };

    public static readonly ProjectileDef Orb2 = Orb with { Id = "pj_t_orb2" };

    public static readonly TelegraphDef Circle = new() { Effect = DefRef<EffectDef>.To(new EffectDef { Id = "fx_t_circle" }), Time = 0.5f };

    public static TelegraphDef Line(float time = 0.5f, float? length = null) => new()
    {
        Effect = DefRef<EffectDef>.To(new EffectDef { Id = "fx_t_line" }), Time = time, Shape = TelegraphShape.Line, Length = length,
    };

    public static PartDef Barrage(BarrageParams p) => A("barrage", p);

    public static BarrageParams Ring(int count, ProjectileDef? def = null) => new()
    {
        Shape = BarrageShape.Ring, Projectile = DefRef<ProjectileDef>.To(def ?? Orb), Count = count, Speed = 120f, Damage = 14f,
    };

    public static BarrageParams Fan(int count, float arc, ProjectileDef? def = null) => new()
    {
        Shape = BarrageShape.Fan, Projectile = DefRef<ProjectileDef>.To(def ?? Orb), Count = count, Arc = arc, Speed = 150f, Damage = 14f,
    };

    /// <summary>조건 없는 패턴(순서용) — 공격 클립, 예고(기본 원 0.5s).</summary>
    public static PatternDef Pattern(string id, TelegraphDef? telegraph, params PartDef[] actions) => new()
    {
        Id = id, Clip = ClipKind.Attack, Telegraph = telegraph, Actions = actions,
    };

    /// <summary>쿨 패턴(순서 밖).</summary>
    public static PatternDef Cooled(string id, float cooldown, TelegraphDef? telegraph, params PartDef[] actions) =>
        Pattern(id, telegraph, actions) with { Cooldown = cooldown };

    public static SequenceDef Seq(float start, params (string Pattern, float Wait)[] steps) => new()
    {
        Start = start, Steps = [.. steps.Select(s => new SequenceStepDef { Pattern = s.Pattern, Wait = s.Wait })],
    };

    /// <summary>패턴 하나를 쿨 0.1 로 바로 쓰는 적(보스 등급, 반경 30 — 해골왕과 같은 몸).</summary>
    public static EnemyDef Single(string id, PatternDef pattern) =>
        Enemy(id) with { Rank = DefRank.Boss, Radius = 30f, Hp = 10000f, Speed = 55f, Patterns = [pattern with { Cooldown = pattern.Cooldown ?? 100f }] };

    /// <summary>조건까지 틱(최대 <paramref name="max"/>). 닿았으면 참.</summary>
    public static bool StepUntil(TestWorld t, Func<bool> done, int max = 600)
    {
        for (var i = 0; i < max; i++)
        {
            if (done()) return true;
            t.Step();
        }
        return done();
    }

    /// <summary>방향의 각(도, −180..180).</summary>
    public static float Deg(Vector2 v) => MathF.Atan2(v.Y, v.X) * 180f / MathF.PI;

    /// <summary>두 방향 사이 각(도, 0..180).</summary>
    public static float Between(Vector2 a, Vector2 b) =>
        MathF.Acos(Math.Clamp(Vector2.Dot(Vector2.Normalize(a), Vector2.Normalize(b)), -1f, 1f)) * 180f / MathF.PI;

    /// <summary>각 차(도, −180..180).</summary>
    public static float Wrap(float deg)
    {
        while (deg > 180f) deg -= 360f;
        while (deg <= -180f) deg += 360f;
        return deg;
    }

    public static Enemy SpawnBoss(TestWorld t, string id, Vector2 pos) =>
        EnemyFactory.Create(t.World, t.World.Defs.Enemies.Get(id), EnemyRank.Normal, [], pos, new SpawnContext(SpawnSource.Boss, EntityId.None));
}
