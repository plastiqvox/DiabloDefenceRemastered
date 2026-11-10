using DiabloDefenceRemastered.Core.Sim.Enemies;

namespace DiabloDefenceRemastered.Core.Sim.Director;

// 스테이지 이벤트 9종 (설계서 6.12, 기획 13.4 — 레지스트리 종류 StageEvent, 매개변수 이름은 WP4 가 정함, 11.3):
// banner(흔들림 포함 — 기획 13.4 의 screen_shake 를 겸함), march, ring, elite_pack, escort, miniboss, boss, light_change(계약 LightChangeParams), room_supply.
// 시각 at 은 로더가 PartDef.At 으로 꺼낸다(매개변수 형에는 없다 — 로더가 at·type 을 빼고 역직렬화).

/// <summary>이벤트 부품 하나. 감독이 시각이 된 틱에 한 번 부른다.</summary>
public interface IStageEvent
{
    void Fire(World world);
}

/// <summary>입구에서 생기는 이벤트(행진·호위·중간 보스·보스)의 <c>entry:</c> 매개변수.</summary>
public interface IEntryEvent
{
    string AtEntry { get; }
}

public sealed record BannerParams
{
    public required string Text { get; init; }
    public float Duration { get; init; } = 2f;
    /// <summary>문구와 같이 거는 흔들림(경고 "땅이 울린다…" 1px 30s).</summary>
    public ShakeDef? Shake { get; init; }
}

public sealed class BannerEvent(BannerParams p) : IStageEvent
{
    public BannerParams Params => p;

    public void Fire(World world)
    {
        world.Banner.Show(p.Text, world.Tick, p.Duration);
        if (p.Shake is { } s) world.Presentation.Shake(new ShakeEvent(world.Tick, s.Px, s.Duration));
    }
}

public sealed record MarchParams : IEntryEvent
{
    public required DefRef<EnemyDef> Enemy { get; init; }
    public int Count { get; init; } = 1;
    public required string AtEntry { get; init; }
    /// <summary>기준점 쪽(예 <c>center</c>).</summary>
    public string? Toward { get; init; }
    /// <summary>목표 문 쪽 — 그 진입점에 닿으면 직진 시간이 남아도 풀림.</summary>
    public string? TowardEntry { get; init; }
    /// <summary>직진 시간(초, 기본 15).</summary>
    public float Straight { get; init; } = 15f;
}

/// <summary>행진: 입구로 들어와 목표(기준점·목표 문)로 직진한 뒤 보통 행동. 방 월드는 <c>RemapMarch</c>(시작 문이 들어온 문일 때만 바꿈).</summary>
public sealed class MarchEvent(MarchParams p) : IStageEvent
{
    public MarchParams Params => p;

    public void Fire(World world)
    {
        if (world.Resolved is not { } map || !p.Enemy.IsBound) return;
        var at = p.AtEntry;
        var toward = p.TowardEntry;
        if (world.Room is { } room)
        {
            if (toward is not null) (at, toward) = room.RemapMarch(at, toward);
            else at = room.RemapEntry(at);
        }
        if (!map.TryGetEntry(at, out var entry)) return;
        Vector2 target;
        Vector2? release = null;
        if (toward is not null && map.TryGetEntry(toward, out var to)) { target = to.Inside; release = to.Inside; }
        else if (p.Toward is not null && map.TryGetAnchor(p.Toward, out var anchor)) target = anchor;
        else target = map.TryGetAnchor(RoomMapRules.CenterAnchor, out var c) ? c : world.MapSize * 0.5f;
        var dir = MathX.NormalizeOr(target - entry.Inside, new Vector2(0, 1));
        var until = world.Tick + MathX.SecondsToTicks(p.Straight, World.Dt);
        foreach (var e in DirectorSpawns.AtEntry(world, p.Enemy.Def, EnemyRank.Normal, [], entry, p.Count, SpawnSource.March))
            e.Order = new MoveOrder { Kind = OrderKind.March, Dir = dir, UntilTick = until, Target = release };
    }
}

public sealed record RingParams
{
    public required DefRef<EnemyDef> Enemy { get; init; }
    public int Count { get; init; } = 12;
    public float Radius { get; init; } = 400f;
    /// <summary>반경을 줄이는 하한(v0.8.1 — 300).</summary>
    public float MinRadius { get; init; } = 300f;
}

/// <summary>포위 고리: 주인공 둘레 같은 간격. 막힌 자리는 반경을 20px 씩 줄여(하한 300) 찾고, 못 놓은 마리는 놓을 수 있는 각도들에 다시 나눔, 그래도 없으면 생략.</summary>
public sealed class RingEvent(RingParams p) : IStageEvent
{
    public RingParams Params => p;

    public void Fire(World world)
    {
        if (!p.Enemy.IsBound) return;
        var result = DirectorSpawns.Ring(world, p.Enemy.Def, p.Count, p.Radius, p.MinRadius);
        world.DirectorState.LastRing = result;
    }
}

public sealed record ElitePackParams
{
    /// <summary>종류(없으면 지금 구간 가중치에서 정예 대상만 뽑음).</summary>
    public DefRef<EnemyDef>? Enemy { get; init; }
    /// <summary>마릿수(없으면 <c>rules.ranks.pack.pack_size</c>).</summary>
    public int? Count { get; init; }
    public int Affixes { get; init; } = 1;
    /// <summary>우두머리 하나가 섞일 확률(없으면 <c>rules.ranks.pack.unique_chance</c>).</summary>
    public float? UniqueChance { get; init; }
}

public sealed class ElitePackEvent(ElitePackParams p) : IStageEvent
{
    public ElitePackParams Params => p;

    public void Fire(World world)
    {
        var def = p.Enemy is { IsBound: true } r ? r.Def : DirectorSpawns.PickEliteKind(world);
        if (def is null) return;
        DirectorSpawns.ElitePack(world, def, p.Count, p.Affixes, p.UniqueChance);
    }
}

public sealed record EscortEventParams : IEntryEvent
{
    public required DefRef<EnemyDef> Enemy { get; init; }
    /// <summary>호위 수(−1 = 정의 기본값, 0 = 호위 없음).</summary>
    public int Escorts { get; init; } = -1;
    public required string AtEntry { get; init; }
}

/// <summary>해골 대장 + 호위: 대장이 입구로 들어온 뒤 생성 훅(<c>escort</c>)이 <c>EscortCount</c> 만큼 데려온다.</summary>
public sealed class EscortEvent(EscortEventParams p) : IStageEvent
{
    public EscortEventParams Params => p;

    public void Fire(World world)
    {
        if (world.Resolved is not { } map || !p.Enemy.IsBound) return;
        var at = world.Room?.RemapEntry(p.AtEntry) ?? p.AtEntry;
        if (!map.TryGetEntry(at, out var entry)) return;
        DirectorSpawns.AtEntry(world, p.Enemy.Def, EnemyRank.Normal, [], entry, 1, SpawnSource.Escort, p.Escorts);
    }
}

public sealed record BossEventParams : IEntryEvent
{
    public required DefRef<EnemyDef> Enemy { get; init; }
    public required string AtEntry { get; init; }
    /// <summary>정의 enrage·arrival 을 덮어씀(6.12).</summary>
    public EnrageDef? Enrage { get; init; }
    public ArrivalDef? Arrival { get; init; }
}

/// <summary>중간 보스·보스 입장: 입구로 걸어 들어옴(그 동안이 예고, 2.4), 보스 막대(등장 순서 — 최종 보스는 위), 등장 문구·흔들림·신호 <c>BossArrived</c>.</summary>
public sealed class BossEvent(BossEventParams p, bool final) : IStageEvent
{
    public BossEventParams Params => p;
    public bool Final => final;

    public void Fire(World world)
    {
        if (world.Resolved is not { } map || !p.Enemy.IsBound) return;
        var at = world.Room?.RemapEntry(p.AtEntry) ?? p.AtEntry;
        if (!map.TryGetEntry(at, out var entry)) return;
        var def = p.Enemy.Def;
        var spawned = DirectorSpawns.AtEntry(world, def, EnemyRank.Normal, [], entry, 1, final ? SpawnSource.Boss : SpawnSource.Miniboss);
        if (spawned.Count == 0) return;
        var e = spawned[0];
        var enrage = p.Enrage ?? def.Enrage;
        if (enrage is not null)
        {
            // --start-at 으로 보스 방을 늦게 시작하면 광폭화 시계를 그만큼 흐른 것으로(v0.8, 9절)
            var late = world.Room is { } room && room.Plan.Kind == RoomKind.Boss ? Math.Max(0, room.ClockTicks - DirectorSystem.GraceTicks(world)) : 0;
            e.EnrageAt = world.Tick + MathX.SecondsToTicks(enrage.After.Seconds, World.Dt) - late;
        }
        if (def.ShowBossBar)
        {
            var bar = new BossBar(e.Id, def.Name, final);
            if (final) world.BossBars.Insert(0, bar);
            else world.BossBars.Add(bar);
        }
        var arrival = p.Arrival ?? def.Arrival;
        if (arrival is not null)
        {
            if (arrival.Banner.Length > 0) world.Banner.Show(arrival.Banner, world.Tick, arrival.Time > 0f ? arrival.Time : 2f);
            if (arrival.Shake is { } s) world.Presentation.Shake(new ShakeEvent(world.Tick, s.Px, s.Duration));
        }
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.BossArrived, e.Pos, def, e.Id));
        if (final) world.DirectorState.BossSpawned = true;
    }
}

/// <summary><c>light_change</c>: 방 안에서 시각이 된 것은 <see cref="LightingState.Apply"/>(방 시작 때 지난 것의 Replay 는 World 생성자 — 3.6).</summary>
public sealed class LightChangeEvent(LightChangeParams p) : IStageEvent
{
    public LightChangeParams Params => p;

    public void Fire(World world) => world.Lighting.Apply(p);
}

public sealed record RoomSupplyParams
{
    /// <summary>방 기준점 ID(<c>room_*</c>) — 13.5-19 가 읽으므로 이름 고정.</summary>
    public required string Anchor { get; init; }
    public required DefRef<PickupDef> Pickup { get; init; }
}

/// <summary>방 보급(6.12): 기준점 자리에 획득물 하나. 같은 기준점에 살아 있는 보급이 있으면 생략(쌓이지 않음). 씨앗을 쓰지 않는다.</summary>
public sealed class RoomSupplyEvent(RoomSupplyParams p) : IStageEvent
{
    public RoomSupplyParams Params => p;

    public void Fire(World world)
    {
        if (world.Resolved is not { } map || !map.TryGetAnchor(p.Anchor, out var pos) || !p.Pickup.IsBound) return;
        foreach (var pk in world.Pickups.Items)
            if (!pk.Removed && string.Equals(pk.SupplyRoom, p.Anchor, StringComparison.Ordinal)) return;
        world.Spawner.SpawnPickup(new PickupSpawn(p.Pickup.Def, pos, SupplyRoom: p.Anchor));
    }
}
