using System.Text.Json;
using DiabloDefenceRemastered.Core.Sim.Director;
using System.Text.Json.Serialization;

namespace DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

// 동작 부품(패턴·훅 안쪽, 설계서 6.6 — 레지스트리 종류 EnemyAction): melee_slam, projectile_spread, telegraphed_blast, dash, summon, ground_hazard, buff_area.

// ── melee_slam ─────────────────────────────────────────────────────

public sealed record MeleeSlamParams
{
    /// <summary>범위 피해 반경(없으면 단일 대상 근접).</summary>
    public float? Radius { get; init; }
    public float? Damage { get; init; }
    public float Knockback { get; init; }
    public ShakeDef? Shake { get; init; }
    /// <summary>단일 대상일 때 맞는 거리 = 두 반경 + 이 값(기본 16 = 근접 6 + 10).</summary>
    public float Reach { get; init; } = 16f;
}

/// <summary>내려치기: 반경이 있으면 자기 둘레 범위, 없으면 단일 대상 근접. 넉백·흔들림.</summary>
public sealed class MeleeSlam(MeleeSlamParams p) : IEnemyAction, IDamageInfo
{
    public MeleeSlamParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => p.Radius;

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        var w = ctx.World;
        var reach = p.Radius is { } r ? r + w.Hero.Radius : ctx.Contact + p.Reach;
        if (ctx.DistToHero <= reach) ctx.HitHero(ctx.FinalDamage(p.Damage), ctx.DirToHero, p.Knockback);
        if (p.Shake is { } s) w.Presentation.Shake(new ShakeEvent(w.Tick, s.Px, s.Duration));
    }
}

// ── projectile_spread ──────────────────────────────────────────────

public sealed record ProjectileSpreadParams
{
    public required DefRef<ProjectileDef> Projectile { get; init; }
    public int Count { get; init; } = 1;
    /// <summary>전체 퍼짐각(도). 360 = 사방.</summary>
    public float Arc { get; init; }
    /// <summary><c>hero</c>(주인공 쪽) 또는 <c>facing</c>(바라보는 쪽).</summary>
    public string Aim { get; init; } = "hero";
    public float? Damage { get; init; }
    public float Speed { get; init; }
}

/// <summary>부채꼴 투사체. <c>clear_sight</c> 패턴이면 투사체의 <c>SightGated</c> 가 참.</summary>
public sealed class ProjectileSpread(ProjectileSpreadParams p) : IEnemyAction, IDamageInfo
{
    public ProjectileSpreadParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => null;

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        var dir = p.Aim == "hero" ? ctx.DirToHero : ctx.Self.Facing;
        var baseAngle = MathF.Atan2(dir.Y, dir.X);
        var n = Math.Max(1, p.Count);
        var damage = ctx.FinalDamage(p.Damage);
        var arc = p.Arc * MathX.DegToRad;
        for (var i = 0; i < n; i++)
        {
            float a;
            if (p.Arc >= 360f) a = baseAngle + MathF.Tau * i / n;
            else a = n == 1 ? baseAngle : baseAngle - arc * 0.5f + arc * i / (n - 1);
            EnemyShots.Fire(ctx, p.Projectile.Def, MathX.FromAngle(a), damage, action.SightGated, p.Speed);
        }
    }
}

// ── telegraphed_blast ──────────────────────────────────────────────

/// <summary>예고 지점 고르기: JSON <c>"self"</c> 또는 <c>{ "hero": true, "around": n, "radius": r }</c>.</summary>
[JsonConverter(typeof(BlastPointsConverter))]
public sealed record BlastPoints
{
    public bool Self { get; init; }
    public bool Hero { get; init; }
    public int Around { get; init; }
    public float Radius { get; init; }

    public static BlastPoints AtSelf { get; } = new() { Self = true };
}

public sealed class BlastPointsConverter : JsonConverter<BlastPoints>
{
    public override BlastPoints Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            return s == "self" ? BlastPoints.AtSelf : throw new JsonException($"points 는 \"self\" 또는 객체여야 한다: '{s}'");
        }
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("points 는 \"self\" 또는 객체여야 한다");
        bool hero = false; int around = 0; float radius = 0f;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var key = reader.GetString();
            reader.Read();
            switch (key)
            {
                case "hero": hero = reader.GetBoolean(); break;
                case "around": around = reader.GetInt32(); break;
                case "radius": radius = reader.GetSingle(); break;
                default: throw new JsonException($"points 의 모르는 속성 '{key}'");
            }
        }
        return new BlastPoints { Hero = hero, Around = around, Radius = radius };
    }

    public override void Write(Utf8JsonWriter writer, BlastPoints value, JsonSerializerOptions options)
    {
        if (value.Self) { writer.WriteStringValue("self"); return; }
        writer.WriteStartObject();
        writer.WriteBoolean("hero", value.Hero);
        writer.WriteNumber("around", value.Around);
        writer.WriteNumber("radius", value.Radius);
        writer.WriteEndObject();
    }
}

public sealed record TelegraphedBlastParams
{
    public BlastPoints Points { get; init; } = BlastPoints.AtSelf;
    public TelegraphDef? Telegraph { get; init; }
    public float Radius { get; init; } = 50f;
    public float? Damage { get; init; }
    public EffectRef? Effect { get; init; }
    public float Knockback { get; init; }
}

/// <summary>예고 뒤 폭발(피의 땅·불비·불타는 접두사 죽음 폭발): 지점마다 원 예고(<c>BlastSpec</c> 를 실음) — 터지기는 WP3 Hazards.</summary>
public sealed class TelegraphedBlast(TelegraphedBlastParams p) : IEnemyAction, IDamageInfo
{
    public TelegraphedBlastParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => p.Radius;

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        var w = ctx.World;
        var damage = ctx.FinalDamage(p.Damage);
        if (p.Points.Self || !p.Points.Hero && p.Points.Around == 0)
        {
            Spawn(ctx, ctx.Self.Pos, damage);
            return;
        }
        var detour = w.Defs.Rules.Spawn?.PathDetour ?? 1.5f;
        var hero = ctx.HeroPos;
        if (p.Points.Hero) Spawn(ctx, hero, damage);
        var start = ctx.Rng.Angle();
        for (var i = 0; i < p.Points.Around; i++)
        {
            var wish = hero + MathX.FromAngle(start + MathF.Tau * i / p.Points.Around) * p.Points.Radius;
            if (EnemyPlacement.TryPlace(w.Nav, hero, wish, 0f, new Nav.PlaceReach(hero, detour, FromHero: true), out var at)) Spawn(ctx, at, damage);
        }
    }

    private void Spawn(in EnemyCtx ctx, Vector2 at, int damage)
    {
        var w = ctx.World;
        var effect = p.Telegraph is { } t ? new EffectRef { Effect = t.Effect } : null;
        w.Spawner.SpawnTelegraph(new TelegraphSpawn(effect, TelegraphShape.Circle, at, at, p.Radius, p.Telegraph?.Time ?? 0f, Faction.Enemy, ctx.Self.Id,
                                                    new BlastSpec(p.Radius, damage, p.Effect, p.Knockback)));
    }
}

// ── dash ───────────────────────────────────────────────────────────

public sealed record DashParams
{
    public float Speed { get; init; } = 300f;
    public float Time { get; init; } = 0.8f;
    public float? Damage { get; init; }
    public TelegraphDef? Telegraph { get; init; }
}

/// <summary>보스 돌진(여러 틱 동작 — <c>actions</c> 의 마지막에만): 직선, 닿으면 한 번 피해, 시간 또는 막힐 때까지.</summary>
public sealed class Dash(DashParams p) : IEnemyAction, IDamageInfo
{
    public DashParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => null;
    public bool MultiTick => true;

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        action.Dir = ctx.DirToHero;
        action.T = 0f;
        action.Hit = false;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref ActionState action)
    {
        var self = ctx.Self;
        action.T += ctx.Dt;
        self.Intent = MoveIntent.Straight(action.Dir, p.Speed / MathF.Max(1f, self.MoveSpeed));
        if (!action.Hit && ctx.DistToHero <= ctx.Contact + 4f)
        {
            action.Hit = true;
            ctx.HitHero(ctx.FinalDamage(p.Damage), action.Dir, 0f);
        }
        return action.T >= p.Time || (action.T > 0.1f && self.MoveBlockedTime > 0.05f) ? ActionStatus.Done : ActionStatus.Running;
    }
}

// ── summon ─────────────────────────────────────────────────────────

public sealed record SummonEntry
{
    public required DefRef<EnemyDef> Enemy { get; init; }
    public int Count { get; init; } = 1;
}

public sealed record SummonParams
{
    public IReadOnlyList<SummonEntry> Spawns { get; init; } = [];
    public float Radius { get; init; } = 120f;
    /// <summary>이 적이 불러낸 살아 있는 적 동시 상한.</summary>
    public int AliveCap { get; init; } = 30;
    public EffectRef? Effect { get; init; }
}

/// <summary>소환: <c>Summoner == 자신</c> 인 살아 있는 적 수를 세서 <c>alive_cap</c> 까지만, 등장 연출(Emerging 0.4s). 분열·소환 상한 360.</summary>
public sealed class Summon(SummonParams p) : IEnemyAction
{
    public const float EmergeSeconds = 0.4f;
    public SummonParams Params => p;

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        var w = ctx.World;
        var self = ctx.Self;
        var alive = 0;
        foreach (var e in w.Enemies.Items)
            if (e.IsAlive && e.Summoner == self.Id) alive++;
        var room = Math.Max(0, p.AliveCap - alive);
        var total = 0;
        foreach (var s in p.Spawns) total += s.Count;
        var want = SpawnGate.Reserve(w, SpawnSource.Summon, Math.Min(room, total));
        var detour = w.Defs.Rules.Spawn?.PathDetour ?? 1.5f;
        var start = ctx.Rng.Angle();
        var k = 0;
        foreach (var s in p.Spawns)
        {
            if (!s.Enemy.IsBound) continue;
            var def = s.Enemy.Def;
            var r = EnemyFactory.BodyRadius(w, def, EnemyRank.Normal);
            for (var i = 0; i < s.Count && k < want; i++, k++)
            {
                var wish = self.Pos + MathX.FromAngle(start + MathF.Tau * k / Math.Max(1, total)) * p.Radius;
                if (!EnemyPlacement.TryPlace(w.Nav, self.Pos, wish, r, new Nav.PlaceReach(self.Pos, detour), out var pos)) continue;
                var e = EnemyFactory.Create(w, def, EnemyRank.Normal, [], pos, new SpawnContext(SpawnSource.Summon, self.Id), LifeState.Emerging);
                e.Summoner = self.Id;
                e.WakeTick = w.Tick + MathX.SecondsToTicks(EmergeSeconds, World.Dt);
                EnemyFx.Effect(w, p.Effect, pos, r);
            }
        }
    }
}

// ── ground_hazard ──────────────────────────────────────────────────

/// <summary>장판 매개변수 — 투사체 <c>on_end</c>(<see cref="GroundHazardDef"/>)와 같은 키 이름(11.3 예외 하나) + 훅 <c>cooldown</c>.</summary>
public sealed record GroundHazardParams
{
    public EffectRef? Effect { get; init; }
    public float Radius { get; init; }
    public float Duration { get; init; }
    public float Tick { get; init; } = 0.5f;
    public float Damage { get; init; }
    public string? CapGroup { get; init; }
    public int Cap { get; init; }
    /// <summary><c>trigger: cooldown</c> 의 쿨(초).</summary>
    public float Cooldown { get; init; }
}

/// <summary>장판 남기기(독 웅덩이·산성 자국): 그 자리에 장판 하나. 상한 묶음은 WP3 가 지킨다(오래된 것부터 지움).</summary>
public sealed class GroundHazard(GroundHazardParams p) : IEnemyAction, ITriggerCooldown, IDamageInfo
{
    public GroundHazardParams Params => p;
    public float Cooldown => p.Cooldown;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => null;

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        var self = ctx.Self;
        ctx.World.Spawner.SpawnHazard(new HazardSpawn(p.Effect, self.Pos, p.Radius, p.Duration, p.Tick, ctx.FinalDamage(p.Damage),
                                                      Faction.Enemy, self.Id, CapGroup: p.CapGroup, Cap: p.Cap));
    }
}

// ── buff_area ──────────────────────────────────────────────────────

public sealed record BuffAreaParams
{
    public float Radius { get; init; } = 300f;
    public required DefRef<BuffDef> Buff { get; init; }
    public float Duration { get; init; } = 6f;
    public string? TargetTag { get; init; }
    public bool IncludeBosses { get; init; }
}

/// <summary>범위 버프(스틸메이스 함성): 반경 안 적에게 duration. include_bosses 거짓이면 자기(중간 보스)와 다른 중간 보스·보스에게 안 걸림.</summary>
public sealed class BuffArea(BuffAreaParams p) : IEnemyAction
{
    public BuffAreaParams Params => p;

    public void Begin(in EnemyCtx ctx, ref ActionState action) =>
        EnemyBuffs.ApplyArea(ctx, p.Radius, p.TargetTag, p.Buff, MathX.SecondsToTicks(p.Duration, World.Dt), p.IncludeBosses, includeSelf: true);
}
