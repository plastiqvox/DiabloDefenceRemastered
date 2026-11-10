using DiabloDefenceRemastered.Core.Sim.Enemies;

namespace DiabloDefenceRemastered.Core.Sim.Director;

/// <summary>감독 상태 (World 조각, 방 수명). 시험·로그가 계측 값을 읽는다.</summary>
public sealed class DirectorState
{
    public bool Started;
    /// <summary>방 월드의 방 이벤트 일정(방 시계 순).</summary>
    public RoomSchedule? Schedule;
    public int NextEvent;
    /// <summary>생존이 끝나 남은 방 이벤트를 버렸다.</summary>
    public bool EventsClosed;
    /// <summary>일반 생성 누적기(마리).</summary>
    public float Accumulator;
    public EnemyDef? NextDef;
    public int NextSize;
    /// <summary>일반 생성 무리 순번(방 체류 압박의 every_nth).</summary>
    public int Groups;
    public bool EliteChecked;
    /// <summary>최종 보스가 나왔다(<c>during: boss</c> 구간).</summary>
    public bool BossSpawned;
    /// <summary>일반 생성 자리 수·그중 입구 대체 수(R23 계측).</summary>
    public int BandSpawns;
    public int EntryFallbacks;
    /// <summary>일반 생성으로 낸 마리 수(시험).</summary>
    public int IntervalSpawned;
    /// <summary>방 체류 압박으로 낸 무리 수(시험).</summary>
    public int PressureSpawns;
    public RingResult? LastRing;
}

/// <summary>
/// 2단계 <c>Director</c> (설계서 3.5·6.12·3.6). 방 월드: 첫 틱에 방 문구 → 방 이벤트(<c>RoomEvents.Schedule</c>, 방 시계)·정예 판정(일반 방마다 방 시계 <c>elite_chance.at</c> 한 번)·일반 생성.
/// <c>Room.SpawningOpen</c> 이 거짓이면(유예·정리·보상·문, 정예 방 2:30 뒤) 생성·이벤트를 하지 않고, 생존이 끝나면 남은 이벤트를 버린다.
/// 구간·배율은 <see cref="World.DifficultyMinutes"/>, 동시 최대 × <c>Room.AreaScale</c>(반올림). 방 없는 월드는 v0.6 대로 스테이지 시각으로 이벤트를 낸다.
/// 늘 하는 일: 안쪽 방 체류(<c>InnerRoomPresence</c>) 갱신·누적, 방 체류 압박.
/// </summary>
public sealed class DirectorSystem : ISimSystem
{
    public void Tick(World world)
    {
        var st = world.DirectorState;
        if (!st.Started) Start(world, st);
        UpdatePresence(world);
        if (world.Outcome != RunOutcome.Running) return;

        if (world.Room is { } room)
        {
            if (room.Phase > RoomPhase.Survive || world.Events.Previous.RoomSurviveEnded.Count > 0) st.EventsClosed = true;
            if (!room.SpawningOpen) return;
            FireRoomEvents(world, st, room);
            EliteCheck(world, st, room);
        }
        else FireStageEvents(world, st);
        Intervals(world, st);
    }

    public static int GraceTicks(World world) =>
        world.Defs.Rules.Rooms is { } r ? MathX.SecondsToTicks(r.Grace, World.Dt) : 0;

    private static void Start(World world, DirectorState st)
    {
        st.Started = true;
        if (world.Room is { } room && world.Stage is { } stage && world.Defs.Rules.Rooms is { } rules)
        {
            st.Schedule = RoomEvents.Schedule(stage, room.Plan, rules, room.ClockTicks);
            var floor = room.Plan.Def.Floor;
            var text = floor > 0 ? $"{stage.Name} {floor}층 — {room.Plan.Number}" : $"{stage.Name} — {room.Plan.Number}";
            world.Banner.Show(text, world.Tick, RoomBannerSeconds);
        }
    }

    /// <summary>방 문구 시간(초).</summary>
    public const float RoomBannerSeconds = 1.5f;

    private static void FireRoomEvents(World world, DirectorState st, RoomState room)
    {
        if (st.EventsClosed || st.Schedule is not { } schedule) return;
        var events = schedule.Events;
        while (st.NextEvent < events.Count && events[st.NextEvent].ClockTick <= room.ClockTicks)
        {
            Fire(world, events[st.NextEvent].Event);
            st.NextEvent++;
        }
    }

    private static void FireStageEvents(World world, DirectorState st)
    {
        if (world.Stage is not { } stage) return;
        var events = stage.Events;
        var now = world.StageTime;
        while (st.NextEvent < events.Count)
        {
            var e = events[st.NextEvent];
            if (e.At is { } at && at.Seconds > now) break;
            Fire(world, e);
            st.NextEvent++;
        }
    }

    private static void Fire(World world, PartDef e)
    {
        if (e.Instance is IStageEvent ev) ev.Fire(world);
        else if (e.Params is LightChangeParams change) world.Lighting.Apply(change);
    }

    /// <summary>정예 판정(v0.7): t 시작 ≥ <c>elite_chance.from</c> 인 일반 방에서 방 시계 <c>at</c> 에 한 번.</summary>
    private static void EliteCheck(World world, DirectorState st, RoomState room)
    {
        if (st.EliteChecked || room.Plan.Kind != RoomKind.Normal || world.Stage?.EliteChance is not { } chance) return;
        if (room.Plan.Def.From.Seconds < chance.From.Seconds) { st.EliteChecked = true; return; }
        if (room.ClockTicks < MathX.SecondsToTicks(chance.At.Seconds, World.Dt)) return;
        st.EliteChecked = true;
        if (!world.Rng.Spawn.Chance(chance.Chance)) return;
        if (DirectorSpawns.PickEliteKind(world) is { } def) DirectorSpawns.ElitePack(world, def, null, 1, null);
    }

    /// <summary>지금 t(난이도 분)의 생성 구간: [from, to) 에 드는 것, 보스가 나온 보스 방이면 <c>during: boss</c>.</summary>
    public static SpawnIntervalDef? CurrentInterval(World world)
    {
        if (world.Stage is not { } stage) return null;
        var boss = world.DirectorState.BossSpawned && (world.Room is null || world.Room.Plan.Kind == RoomKind.Boss);
        var seconds = world.DifficultyMinutes * 60f;
        SpawnIntervalDef? hit = null;
        foreach (var iv in stage.SpawnIntervals)
        {
            if (iv.During is not null)
            {
                if (boss && iv.During == "boss") return iv;
                continue;
            }
            var from = iv.From?.Seconds ?? 0f;
            var to = iv.To?.Seconds ?? float.PositiveInfinity;
            if (seconds >= from && seconds < to) hit = iv;
        }
        return hit;
    }

    private static void Intervals(World world, DirectorState st)
    {
        if (CurrentInterval(world) is not { } interval || interval.Weights.Count == 0) return;
        var rules = world.Defs.Rules.Spawn ?? new SpawnRulesDef();
        var maxAlive = world.Room is { } room ? (int)MathF.Round(interval.MaxAlive * room.AreaScale) : interval.MaxAlive;
        var alive = SpawnGate.Alive(world);
        st.Accumulator += interval.PerSecond * World.Dt;
        if (alive >= maxAlive)
        {
            st.Accumulator = MathF.Min(st.Accumulator, rules.AccumulatorHold > 0f ? rules.AccumulatorHold : 3f);
            return;
        }
        // 방의 첫 무리는 생성이 열린 틱(시작 유예 끝)에 바로 낸다. 누적기가 0 에서 무리 크기까지 차오르는 1~3초 동안 방이 비어 보였다
        // (첫 플레이 관찰 "방 2 의 1:12 에 적이 없음" — RealSpawnGapTests 가 그 틈이 유예 뒤 첫머리뿐임을 보였다). 그 뒤는 누적기대로.
        if (world.Room is not null && st.Groups == 0)
        {
            if (st.NextDef is null) PickGroup(world, st, interval);
            if (st.NextDef is not null) st.Accumulator = MathF.Max(st.Accumulator, st.NextSize);
        }
        for (var guard = 0; guard < 64; guard++)
        {
            if (st.NextDef is null) PickGroup(world, st, interval);
            if (st.NextDef is not { } def || st.Accumulator < st.NextSize || alive >= maxAlive) break;
            var size = Math.Min(st.NextSize, maxAlive - alive);
            var made = SpawnGroup(world, st, def, size);
            st.Accumulator -= st.NextSize;
            st.NextDef = null;
            alive += made;
        }
    }

    private static void PickGroup(World world, DirectorState st, SpawnIntervalDef interval)
    {
        var rng = world.Rng.Spawn;
        var sum = 0f;
        foreach (var w in interval.Weights) if (w.Ref.IsBound) sum += w.Weight;
        if (sum <= 0f) return;
        var x = rng.NextFloat() * sum;
        EnemyDef? pick = null;
        foreach (var w in interval.Weights)
        {
            if (!w.Ref.IsBound) continue;
            x -= w.Weight;
            if (x < 0f) { pick = w.Ref.Def; break; }
        }
        pick ??= interval.Weights[^1].Ref.Def;
        var group = pick.SpawnGroup ?? world.Defs.Rules.Spawn?.Group ?? [1, 3];
        st.NextDef = pick;
        st.NextSize = Math.Max(1, rng.RangeInclusive(group[0], group.Length > 1 ? group[1] : group[0]));
    }

    private static int SpawnGroup(World world, DirectorState st, EnemyDef def, int size)
    {
        size = SpawnGate.Reserve(world, SpawnSource.Interval, size);
        if (size <= 0) return 0;
        st.Groups++;
        var r = EnemyFactory.BodyRadius(world, def, EnemyRank.Normal);
        if (TryPressure(world, st, def, size, r, out var pressured)) return pressured;

        st.BandSpawns++;
        if (DirectorSpawns.TryBandPoint(world, r, out var center))
        {
            var groupRadius = world.Defs.Rules.Spawn?.GroupRadius ?? 40f;
            var made = 0;
            for (var i = 0; i < size; i++)
            {
                var wish = center + (i == 0 ? Vector2.Zero : MathX.FromAngle(MathF.Tau * i / size + 0.3f) * groupRadius);
                if (!EnemyPlacement.TryPlace(world.Nav, center, wish, r, null, out var pos)) pos = center;
                EnemyFactory.Create(world, def, EnemyRank.Normal, [], pos, new SpawnContext(SpawnSource.Interval, EntityId.None));
                made++;
            }
            st.IntervalSpawned += made;
            return made;
        }
        st.EntryFallbacks++;
        if (DirectorSpawns.FarEntry(world) is not { } entry) return 0;
        var list = DirectorSpawns.AtEntry(world, def, EnemyRank.Normal, [], entry, size, SpawnSource.Interval);
        st.IntervalSpawned += list.Count;
        return list.Count;
    }

    /// <summary>
    /// 방 체류 압박(6.12 v0.6.3): 주인공이 문 하나인 안쪽 방에 <c>after</c> 초 넘게 있으면 일반 생성 무리 <c>every_nth</c> 번째마다 하나를
    /// 그 방 바닥 칸 중 주인공에서 <c>min_distance</c> 이상인 걷기 칸에 등장 연출로. 다 실패하면 보통 자리(false).
    /// </summary>
    private static bool TryPressure(World world, DirectorState st, EnemyDef def, int size, float r, out int made)
    {
        made = 0;
        var pressure = world.Defs.Rules.Spawn?.RoomPressure;
        var presence = world.InnerRoomPresence;
        if (pressure is null || presence.Current < 0 || world.Arena is not { } arena) return false;
        var floor = world.InnerRooms.Rooms[presence.Current];
        if (!floor.OneDoor || (world.Tick - presence.EnteredTick) * World.Dt <= pressure.After) return false;
        if (pressure.EveryNth <= 0 || st.Groups % pressure.EveryNth != 0) return false;
        var tries = Math.Max(1, world.Defs.Rules.Spawn?.Tries ?? 12);
        for (var t = 0; t < tries; t++)
        {
            var cell = floor.Cells[world.Rng.Spawn.Index(floor.Cells.Count)];
            var p = arena.Iso.CellCenter(cell.X, cell.Y);
            if (Vector2.Distance(p, world.Hero.Pos) < pressure.MinDistance || world.Nav.Clearance(p) < r + 4f) continue;
            for (var i = 0; i < size; i++)
            {
                var e = EnemyFactory.Create(world, def, EnemyRank.Normal, [], p, new SpawnContext(SpawnSource.Interval, EntityId.None), LifeState.Emerging);
                e.WakeTick = world.Tick + MathX.SecondsToTicks(Enemies.Parts.Summon.EmergeSeconds, World.Dt);
                made++;
            }
            st.PressureSpawns++;
            st.IntervalSpawned += made;
            return true;
        }
        return false;
    }

    /// <summary>주인공이 선 안쪽 방(<c>Current</c>·<c>EnteredTick</c>)과 방마다 누적(체류 틱·앞 틱의 받은 피해·주운 보급).</summary>
    private static void UpdatePresence(World world)
    {
        var rooms = world.InnerRooms;
        if (rooms.Rooms.Count == 0) return;
        var presence = world.InnerRoomPresence;
        var index = rooms.IndexAt(world.Hero.Pos);
        if (index != presence.Current)
        {
            presence.Current = index;
            presence.EnteredTick = world.Tick;
        }
        if (index >= 0)
        {
            presence.TicksInside[index]++;
            foreach (ref readonly var d in world.Events.Previous.HeroDamaged.AsSpan()) presence.DamageInside[index] += d.Applied;
        }
        foreach (ref readonly var s in world.Events.Previous.SupplyCollected.AsSpan())
            for (var i = 0; i < rooms.Rooms.Count; i++)
                if (string.Equals(rooms.Rooms[i].Id, s.RoomId, StringComparison.Ordinal)) presence.SuppliesTaken[i]++;
    }
}
