using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Core.Sim.Progression;

namespace DiabloDefenceRemastered.Core.Sim.Pickups;

/// <summary>
/// 단계 14 DeathRewards (WP3, 설계서 3.5·6.9): 이번 틱 <c>EnemyKilled</c> 중 <c>KillCause.Damage</c> 만 — 처치 수, 경험치 보석 하나(값 = 적 경험치 × 등급 배율, 색 = 값 범위),
/// 드롭 표(일반 = 적 정의, 정예·우두머리 = 등급 표가 있으면 그것). 보석이 상한(<c>rules.caps.gems</c>)이면 새 보석 대신 마지막 보석(더미)에 값을 더한다(기획 11.1).
/// </summary>
public sealed class DeathRewardsSystem : ISimSystem
{
    public const int DefaultGemCap = 300;
    /// <summary>드롭이 보석과 겹치지 않게 비키는 거리(px).</summary>
    public const float DropScatter = 12f;

    public void Tick(World world)
    {
        var kills = world.Events.Current.EnemyKilled;
        for (var i = 0; i < kills.Count; i++)
        {
            ref readonly var k = ref kills[i];
            if (k.Cause != KillCause.Damage || (uint)k.DefIndex >= (uint)world.Defs.Enemies.Count) continue;
            var def = world.Defs.Enemies[k.DefIndex];
            world.RunStats.Kills++;
            if (k.Rank != EnemyRank.Normal) world.RunStats.EliteKills++;
            var rank = k.Rank switch
            {
                EnemyRank.Elite => world.Defs.Rules.Ranks?.Elite,
                EnemyRank.Unique => world.Defs.Rules.Ranks?.Unique,
                _ => null,
            };
            var xp = (int)MathF.Round(def.Xp * (rank?.Xp ?? 1f), MidpointRounding.AwayFromZero);
            if (xp > 0) DropGem(world, k.Pos, xp);
            var table = rank?.DropTable?.Def ?? def.DropTable?.Def;
            if (table is not null) Roll(world, table, k.Pos);
        }
    }

    private static void DropGem(World world, Vector2 pos, int value)
    {
        var cap = world.Defs.Rules.Caps?.Gems ?? DefaultGemCap;
        var pickups = world.Pickups;
        var count = 0;
        Pickup? last = null;
        for (var i = 0; i < pickups.Count; i++)
        {
            var p = pickups[i];
            if (p.Removed || p.Def.Kind != PickupKind.Xp) continue;
            count++;
            last = p;
        }
        if (count >= cap && last is not null)
        {
            last.Value += value;
            var red = GemFor(world, int.MaxValue);
            if (red is not null && last.Def != red) last.Def = red;
            return;
        }
        if (GemFor(world, value) is { } gem) world.Spawner.SpawnPickup(new PickupSpawn(gem, pos, value));
    }

    /// <summary>값이 범위 안인 보석(없으면 범위 최솟값이 가장 큰 보석 중 값 이하인 것, 그래도 없으면 첫 보석).</summary>
    internal static PickupDef? GemFor(World world, int value)
    {
        PickupDef? best = null;
        PickupDef? first = null;
        foreach (var p in world.Defs.Pickups.All)
        {
            if (p.Kind != PickupKind.Xp) continue;
            first ??= p;
            if (p.ValueRange is not [var lo, var hi]) continue;
            if (value >= lo && value <= hi) return p;
            if (value >= lo && (best?.ValueRange is not [var blo, _] || lo > blo)) best = p;
        }
        return best ?? first;
    }

    private static void Roll(World world, DropTableDef table, Vector2 pos)
    {
        var rng = world.Rng.Drop;
        var n = 0;
        foreach (var entry in table.Entries)
        {
            if (!rng.Chance(entry.Chance)) continue;
            var value = entry.Value is [var lo, var hi] ? rng.RangeInclusive(lo, hi) : 0;
            var a = MathF.Tau * (n++ % 8) / 8f;
            world.Spawner.SpawnPickup(new PickupSpawn(entry.Pickup.Def, pos + new Vector2(MathF.Cos(a), MathF.Sin(a)) * DropScatter, value));
        }
    }
}

/// <summary>
/// 단계 15 Pickups (WP3, 기획 11.1·11.4, 설계서 6.9·6.18): 앞 틱 <c>RoomCleared</c> → 보석·금화 끌어당김(보스 방이면 체력 전부) →
/// 획득 반경 안 보석·금화가 날아옴(처음 200px/s, 초당 +600) → 몸에 닿으면 획득(물약·룬·상자·보상 물체는 닿아야). 체력이 가득이면 체력 물약은 남는다(보상 물체는 늘 집힘).
/// 보상 물체 = <see cref="ProgressionService.CollectReward"/>·신호 <c>RewardCollected</c>, 방 보급 = <c>SupplyCollected</c>.
/// </summary>
public sealed class PickupsSystem : ISimSystem
{
    public const float MagnetStartSpeed = 200f;
    public const float MagnetAccel = 600f;

    public void Tick(World world)
    {
        var hero = world.Hero;
        var cleared = world.Events.Previous.RoomCleared;
        for (var i = 0; i < cleared.Count; i++)
        {
            MagnetizeAll(world, includeGold: true);
            if (cleared[i].Kind == RoomKind.Boss && !hero.Dead) hero.Hp = hero.MaxHp;
        }
        if (hero.Dead) return;

        var radius = HeroStats.PickupRadius(hero);
        var store = world.Pickups;
        for (var i = 0; i < store.Count; i++)
        {
            var p = store[i];
            if (p.Removed) continue;
            p.Age += World.Dt;
            var d = hero.Pos - p.Pos;
            var dist = d.Length();
            if (!p.Magnetized && p.Def.Magnet && p.Reward is null && dist <= radius) Magnetize(p);
            if (p.Magnetized)
            {
                p.MagnetSpeed += MagnetAccel * World.Dt;
                var step = p.MagnetSpeed * World.Dt;
                p.Pos = step >= dist ? hero.Pos : p.Pos + d / dist * step;
                dist = Vector2.Distance(hero.Pos, p.Pos);
            }
            if (dist > hero.Radius + p.Radius) continue;
            if (Collect(world, p)) store.RemoveAt(i);
        }
    }

    private static void Magnetize(Pickup p)
    {
        p.Magnetized = true;
        p.MagnetSpeed = MagnetStartSpeed;
    }

    /// <summary>맵의 보석(과 금화)을 모두 끌어당김(끌어당김 룬·방 정리).</summary>
    internal static void MagnetizeAll(World world, bool includeGold)
    {
        var store = world.Pickups;
        for (var i = 0; i < store.Count; i++)
        {
            var p = store[i];
            if (p.Removed || p.Magnetized || p.Reward is not null) continue;
            if (p.Def.Kind == PickupKind.Xp || includeGold && p.Def.Kind == PickupKind.Gold) Magnetize(p);
        }
    }

    /// <summary>획득. 집지 않고 남으면 false(체력이 가득일 때의 체력 물약).</summary>
    private static bool Collect(World world, Pickup p)
    {
        var hero = world.Hero;
        var def = p.Def;
        var progression = world.Progression as ProgressionService;
        if (p.Reward is { } reward)
        {
            progression?.CollectReward(reward);
            world.Presentation.Cue(new CueEvent(world.Tick, CueKind.RewardCollected, p.Pos, reward));
            if (p.SupplyRoom is { } room) world.Events.Current.SupplyCollected.Add(new SupplyCollected(room, def, p.Pos));
            return true;
        }
        switch (def.Kind)
        {
            case PickupKind.Xp:
                progression?.AddXp(p.Value);
                break;
            case PickupKind.Gold:
                world.RunStats.Gold += (int)MathF.Round(p.Value * (1f + hero.Stats[StatKey.GoldPct]), MidpointRounding.AwayFromZero);
                break;
            case PickupKind.Heal:
                if (def.SkipWhenFull && hero.Hp >= hero.MaxHp) return false;
                hero.Hp = MathF.Min(hero.MaxHp, hero.Hp + (def.Amount ?? 0f));
                break;
            case PickupKind.Surge:
                world.Combat.SurgeTicks = Math.Max(1, MathX.SecondsToTicks(def.Duration ?? 0f, World.Dt));
                world.Combat.SurgePct = def.Amount ?? 0f;
                for (var i = 0; i < WeaponSlots.SlotCount; i++)
                    if (!hero.Weapons[i].IsEmpty && !hero.Weapons[i].Pattern.Active) hero.Weapons[i].Cooldown = 0f;
                break;
            case PickupKind.Magnet:
                MagnetizeAll(world, includeGold: false);
                break;
            case PickupKind.Chest:
                world.Combat.Chests.Add(Math.Max(1, p.Value));
                break;
        }
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.PickupCollected, p.Pos, def));
        if (p.SupplyRoom is { } supply) world.Events.Current.SupplyCollected.Add(new SupplyCollected(supply, def, p.Pos));
        return true;
    }
}
