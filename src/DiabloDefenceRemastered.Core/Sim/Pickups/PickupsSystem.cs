using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Core.Sim.Shops;

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
/// 획득 반경 안 보석·금화가 날아옴(처음 200px/s, 초당 +600) → 몸에 닿으면 획득(물약·룬·상자·보상 물체는 닿아야). 물약 칸이 가득이면 체력 물약은 남는다(보상 물체는 늘 집힘).
/// 보상 물체 = <see cref="ProgressionService.CollectReward"/>·신호 <c>RewardCollected</c>(v0.9.1 두 단계 보상은 상자에 닿으면 <see cref="ProgressionService.DropReward"/> — 책에 닿아야 받음),
/// 방 보급 = <c>SupplyCollected</c>, 금화 = 지갑과 얻은 금화(<see cref="Wallet"/>). 끝에 상인 열기 판정(v0.9 — <see cref="ShopService.TryOpen"/>, 이동이 끝난 자리로).
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
            // v0.9.1 V8(단계 2 wp5 이음새): 상자를 연 이 틱에 떨어진 책은 이 틱에 집지 않는다 — DropReward 가 저장소 끝에 붙여 이 반복이 곧 만나고,
            // 책 자리가 벽·맵 끝 때문에 상자 쪽으로 당겨지면 몸에 바로 닿아 책이 한 프레임도 안 보인 채 받아졌다(시험 Art/ChestBookSameTickTests)
            if (p.Reward is not null && p.RewardStage == RewardPickupStage.Dropped && p.SpawnTick == world.Tick) continue;
            if (Collect(world, p)) store.RemoveAt(i);
        }
        (world.ShopService as ShopService)?.TryOpen();
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

    /// <summary>획득. 집지 않고 남으면 false(물약 칸이 가득일 때의 체력 물약·두 단계 보상의 상자 — v0.9.1).</summary>
    private static bool Collect(World world, Pickup p)
    {
        var hero = world.Hero;
        var def = p.Def;
        var progression = world.Progression as ProgressionService;
        if (p.Reward is { } reward)
        {
            if (reward.Drop is { } drop && p.Def.Id != drop.Id && progression is not null)
            {
                // v0.9.1 V8 두 단계 보상: 닫힌 상자에 닿으면 열려 책이 떨어진다(열린 상자는 그대로 남고 다시 집히지 않음)
                if (p.RewardStage == RewardPickupStage.Closed) progression.DropReward(p);
                return false;
            }
            progression?.CollectReward(reward, p);
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
                Wallet.Earn(world, (int)MathF.Round(p.Value * (1f + hero.Stats[StatKey.GoldPct]), MidpointRounding.AwayFromZero));
                break;
            case PickupKind.PotionStack:
                // v0.9.1 V9: 물약 칸에 하나 쌓임(바로 마시지 않음). 칸이 가득이면 안 집힘(그대로 남음 — 6.19 "물약 칸")
                if (HeroPotions.Full(world)) return false;
                HeroPotions.Add(world, 1);
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
