using DiabloDefenceRemastered.Core.Sim.Heroes;

namespace DiabloDefenceRemastered.Core.Sim.Autoplay;

/// <summary>자동 조작 이동 모드(v0.6.1 — 9절 <c>--autoplay-visit</c>, 게임 <c>AppOptions.AutoplayVisit</c>). Core 에 두어 <c>Scenario</c> 시험도 쓴다.</summary>
public enum AutoplayVisit : byte
{
    /// <summary>보통 자동 조작.</summary>
    None,
    /// <summary>이동 규칙에 더해 맵의 방 기준점(<c>room_*</c>)을 이름 순으로 차례로 들른다(기준점 쪽 가중 0.8, 반경 60 안이면 다음 방, 40초 넘으면 넘김).</summary>
    Rooms,
}

/// <summary>방 들르기 기록 하나(닿음 또는 넘김). WP6 이 새 기록마다 로그 <c>VISIT</c>·<c>VISIT_SKIP</c> 줄을 쓴다. <paramref name="MapId"/>(v0.7) = 그 방 맵(로그 <c>map=</c>).</summary>
public readonly record struct RoomVisit(string MapId, string RoomId, int StageTick, bool Skipped);

/// <summary>안쪽 방 하나의 누적(판 끝 <c>VISITS</c> 줄): 들른 수·넘긴 수·방 안에 있던 틱·그동안 받은 피해. v0.7: 키 = (맵 ID, 기준점 ID) — 판 전체에서 그 판이 쓴 맵마다.</summary>
public readonly record struct RoomVisitStats(string MapId, string RoomId, int Visits, int Skips, int TicksInside, float DamageInside);

/// <summary>자동 조작이 문 단계를 <c>rules.rooms.autoplay_door_skip</c> 안에 못 끝내 <c>CommandButtons.Exit</c> 로 나간 기록(v0.7 — 로그 <c>DOOR_SKIP</c>, 시험 기준 0).</summary>
public readonly record struct DoorSkip(int Act, int Room, int StageTick);

/// <summary>보상·문 단계가 <c>autoplay_door_skip</c> 안에 안 끝난 기록(v0.7.1 — 로그 <c>REWARD_STALL</c>, 상태는 바꾸지 않음, 시험 기준 0).</summary>
public readonly record struct RewardStall(int Act, int Room, RoomPhase Phase, int StageTick);

/// <summary>
/// 자동 조작 정책 (설계서 3.4·8.1·9절, 기획 15절, 소유 WP4). 시험과 게임(--autoplay)이 같은 정책을 써서 같은 씨앗·같은 옵션이면 같은 판이 된다.
/// 판마다 인스턴스 하나(WP6 이 <c>new AutoplayPolicy(visit)</c>). 방 번호·다시 구할 시각 같은 상태는 인스턴스에 둔다 — 시뮬레이션 상태가 아니다(해시에 들지 않음, 같은 입력이면 같은 명령).
/// 안개를 무시한다(월드의 시야 상태를 읽지 않는다 — 10.1 소스 검사). 메뉴 넘김은 WP6 AutoplayDriver.
/// <list type="bullet">
/// <item>이동: 반경 200 안 적에게서 멀어짐(거리 역수 가중 합) + 반경 300 안 가장 가까운 획득물 쪽 0.6 + 맵 가장자리에서 가운데 쪽.</item>
/// <item>방(v0.7·v0.8): 정리 단계 남은 적 ≤ 10 이면 가장 가까운 적 쪽 길 방향 0.8, 정예 방 생성이 멈추면 중간 보스 쪽 0.8, 보상 단계는 보상 물체·<c>GatesExit</c> 상자로,
/// 문 단계는 문 우선순위로 고른 문의 나가는 자리로. 문 단계가 <c>autoplay_door_skip</c> 을 넘으면 <c>Exit</c> 명령 + <see cref="DoorSkips"/>, 보상 단계가 넘으면 <see cref="RewardStalls"/>.</item>
/// <item>방마다 판단 상태는 월드가 바뀔 때 비운다(v0.8 — 계단 저장으로 끊고 이은 판이 같게).</item>
/// </list>
/// </summary>
public sealed class AutoplayPolicy(AutoplayVisit visit)
{
    /// <summary>정리 단계에서 남은 적을 쫓기 시작하는 수(v0.8: 정책 상수 — <c>remaining_marker</c> 키는 없앰).</summary>
    public const int ChaseRemaining = 10;
    public const float AvoidRadius = 200f, PickupRadius = 300f, VisitReach = 60f, VisitTimeout = 40f;
    /// <summary>길 다음 칸을 다시 구하는 간격(틱, 0.5s).</summary>
    public const int PathRefreshTicks = 30;
    /// <summary>상인 문을 보물 상자와 같은 순위로 올리는 지갑(기획 15절 v0.9 첫 값 — 자동 조작 판이 상인 창을 지나가게).</summary>
    public const int MerchantGold = 150;

    private readonly List<RoomVisit> _events = [];
    private readonly Dictionary<(string Map, string Room), (int Visits, int Skips, int Ticks, float Damage)> _stats = [];
    private readonly List<DoorSkip> _doorSkips = [];
    private readonly List<RewardStall> _rewardStalls = [];

    // 방(월드)마다 판단 상태
    private World? _world;
    private int _visitIndex;
    private int _visitStartTick;
    private bool _stallLogged;
    private bool _skipLogged;
    private bool _shopBought;
    private Vector2 _pathTarget;
    private Vector2 _pathNext;
    private int _pathTick = int.MinValue;

    public AutoplayVisit Visit { get; } = visit;

    /// <summary>방 들르기 기록(닿음·넘김, 일어난 순서). <see cref="AutoplayVisit.None"/> 이면 늘 빈 목록.</summary>
    public IReadOnlyList<RoomVisit> Events => _events;

    /// <summary>안쪽 방마다 누적((맵 ID, 방 ID) 서수 순).</summary>
    public IReadOnlyList<RoomVisitStats> RoomStats =>
        [.. _stats.OrderBy(kv => kv.Key.Map, StringComparer.Ordinal).ThenBy(kv => kv.Key.Room, StringComparer.Ordinal)
                  .Select(kv => new RoomVisitStats(kv.Key.Map, kv.Key.Room, kv.Value.Visits, kv.Value.Skips, kv.Value.Ticks, kv.Value.Damage))];

    /// <summary>문 단계 넘김 기록(v0.7, 일어난 순서). WP6 이 새 기록마다 로그 <c>DOOR_SKIP</c>.</summary>
    public IReadOnlyList<DoorSkip> DoorSkips => _doorSkips;

    /// <summary>보상 단계 멈춤 기록(v0.7.1, 일어난 순서 — 방마다 한 번). WP6 이 새 기록마다 로그 <c>REWARD_STALL</c>.</summary>
    public IReadOnlyList<RewardStall> RewardStalls => _rewardStalls;

    /// <summary>
    /// 이번 스테이지 틱의 명령(v0.7 — v0.6 의 <c>Move</c> 를 대신함): 이동(길이 ≤ 1) + 방 규칙(9절 — 정리 추적·보상 집기·문 우선순위),
    /// 문 단계가 <c>autoplay_door_skip</c> 안에 안 끝나면 <see cref="CommandButtons.Exit"/> + <c>ExitIndex</c>. 정책 인스턴스는 판마다 하나(월드가 바뀌어도 그대로).
    /// </summary>
    public PlayerCommand Next(World world)
    {
        if (!ReferenceEquals(world, _world)) BeginWorld(world);
        if (world.Room?.Shop is { Open: true } open) return ShopCommand(world, open);   // 상인 창 앱 틱(v0.9 — 판 틱이 돌지 않음)
        var command = Decide(world);
        return ShouldDrink(world) ? command with { Buttons = command.Buttons | CommandButtons.Drink } : command;
    }

    /// <summary>
    /// 물약(v0.9.1 V9, 기획 15절·설계 6.19): 체력 &lt; 최대 × <c>rules.hero.potions.bot_threshold</c> ∧ 마시는 중이 아님 ∧ 물약 ≥ 1 이면 그 틱에 <c>Drink</c>.
    /// 끝난 판·죽은 주인공·물약 칸 규칙이 없는 정의는 마시지 않는다.
    /// </summary>
    public static bool ShouldDrink(World world)
    {
        var hero = world.Hero;
        if (world.Defs.Rules.Hero?.Potions is not { } potions || hero.Dead || world.Outcome != RunOutcome.Running) return false;
        return hero.MaxHp > 0f && hero.Hp < hero.MaxHp * potions.BotThreshold && !hero.Heal.Active && hero.Potions > 0;
    }

    private PlayerCommand Decide(World world)
    {
        var hero = world.Hero;
        var room = world.Room;
        var skip = world.Defs.Rules.Rooms?.AutoplayDoorSkip ?? 20f;

        if (room is { Phase: RoomPhase.Reward })
        {
            if (!_stallLogged && (world.Tick - room.PhaseTick) * World.Dt > skip)
            {
                _stallLogged = true;
                _rewardStalls.Add(new RewardStall(room.Plan.Act, room.Plan.Number, room.Phase, world.Tick));
            }
            if (NearestGatingPickup(world) is { } target) return new PlayerCommand(PathDir(world, target));
            // v0.9 상인 방: 창을 한 번 닫아야 보상을 받은 것 — 상인 자리(보상 자리)로 걸어가 연다(WP3 — 주인공 반경 + 20 안이면 열림)
            if (room.Shop is { Visited: false }) return new PlayerCommand(PathDir(world, room.RewardSpot));
            return new PlayerCommand(Avoid(world));
        }
        if (room is { Phase: RoomPhase.Doors } && room.Exits.Count > 0)
        {
            var index = ChooseDoor(world, room);
            if ((world.Tick - room.PhaseTick) * World.Dt > skip)
            {
                if (!_skipLogged)
                {
                    _skipLogged = true;
                    _doorSkips.Add(new DoorSkip(room.Plan.Act, room.Plan.Number, world.Tick));
                }
                return new PlayerCommand(Vector2.Zero, CommandButtons.Exit, (byte)index);
            }
            if (DoorPoint(world, room.Exits[index].GateId) is { } door) return new PlayerCommand(PathDir(world, door));
            return new PlayerCommand(Vector2.Zero, CommandButtons.Exit, (byte)index);
        }

        var move = Avoid(world);
        if (NearestPickup(world, PickupRadius) is { } gem) move += MathX.NormalizeOr(gem - hero.Pos, Vector2.Zero) * 0.6f;
        if (room is { Phase: RoomPhase.Cleanup })
        {
            var (count, nearest) = Remaining(world);
            if (count <= ChaseRemaining && nearest is { } n) move += PathDir(world, n) * 0.8f;
        }
        else if (room is { Phase: RoomPhase.Survive } && room.Plan.Kind == RoomKind.Elite && !room.SpawningOpen && Miniboss(world, room) is { } mb)
            move += PathDir(world, mb) * 0.8f;
        if (Visit == AutoplayVisit.Rooms && (room is null || room.Phase <= RoomPhase.Cleanup) && VisitTarget(world) is { } anchor)
            move += PathDir(world, anchor) * 0.8f;
        move += EdgePull(world);
        TrackInside(world);
        var len = move.Length();
        return new PlayerCommand(len > 1f ? move / len : move);
    }

    /// <summary>
    /// 고를 카드 번호(0부터): 진화 재료 무기 강화 > 가진 무기 강화 > 새 무기 > 가진 패시브 > 새 패시브 > 채움. 같은 순위면 왼쪽부터(기획 15절).
    /// 마법서·성소 창도 같은 무기 순위(v0.8).
    /// 진화 재료 = 주인공이 그 진화의 재료 패시브를 가진 진화 전 무기(마지막으로 <see cref="Next"/> 를 받은 월드로 판단 — 아직 없으면 보통 무기 강화와 같음).
    /// </summary>
    public int PickCard(LevelUpOffer offer)
    {
        var best = 0;
        var bestRank = int.MaxValue;
        for (var i = 0; i < offer.Cards.Count; i++)
        {
            var rank = offer.Cards[i].Kind switch
            {
                CardKind.WeaponUpgrade => IsEvolutionMaterial(offer.Cards[i].Id) ? 0 : 1,
                CardKind.NewWeapon => 2,
                CardKind.PassiveUpgrade => 3,
                CardKind.NewPassive => 4,
                _ => 5,
            };
            if (rank < bestRank) { bestRank = rank; best = i; }
        }
        return best;
    }

    private bool IsEvolutionMaterial(string weaponId)
    {
        if (_world is not { } world) return false;
        foreach (var evo in world.Defs.Evolutions.All)
        {
            if (!string.Equals(evo.Weapon.Id, weaponId, StringComparison.Ordinal)) continue;
            for (var i = 0; i < PassiveSlots.SlotCount; i++)
                if (world.Hero.Passives[i].Def is { } p && string.Equals(p.Id, evo.Passive.Id, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private void BeginWorld(World world)
    {
        _world = world;
        _visitIndex = 0;
        _visitStartTick = world.Tick;
        _stallLogged = false;
        _skipLogged = false;
        _shopBought = false;
        _pathTick = int.MinValue;
    }

    /// <summary>
    /// 상인 창(기획 12.8·15절 v0.9.1, 설계 6.19 — 열린 동안의 앱 틱마다): 입력 잠금(<c>World.Combat.ShopLockTicks</c>)이 남았으면 아무것도 누르지 않는다.
    /// 반지 바꾸기 중이면 견준 값이 작은 반지 자리(같으면 첫째)로 <c>Choice1/2</c>. 한 번 샀으면(또는 살 것이 없으면) <c>Skip</c> = 떠나기. 다시 굴리기는 하지 않는다.
    /// 장비 상인 = 살 수 있는 카드(산 것·값 &gt; 지갑 제외) 중 지금 칸 대비 <see cref="EquipValue"/> 차이가 가장 큰 것(같으면 왼쪽).
    /// 페핀 = 물약 칸이 비었으면 채우기 → 주머니 → 빠른 물약 중 살 수 있는 첫 것.
    /// </summary>
    private PlayerCommand ShopCommand(World world, ShopState shop)
    {
        if (world.Combat.ShopLockTicks > 0) return PlayerCommand.None;
        if (shop.RingPending >= 0)
        {
            _shopBought = true;
            return new PlayerCommand(Vector2.Zero, WeakerRing(world) == 0 ? CommandButtons.Choice1 : CommandButtons.Choice2);
        }
        if (_shopBought) return new PlayerCommand(Vector2.Zero, CommandButtons.Skip);
        var pick = PickShopCard(world, shop);
        if (pick < 0) return new PlayerCommand(Vector2.Zero, CommandButtons.Skip);
        _shopBought = true;
        return new PlayerCommand(Vector2.Zero, pick switch { 0 => CommandButtons.Choice1, 1 => CommandButtons.Choice2, _ => CommandButtons.Choice3 });
    }

    /// <summary>살 카드 번호(0부터, 없으면 −1) — <see cref="ShopCommand"/> 의 규칙. 시험이 합성 <c>ShopState</c> 로 부른다.</summary>
    public static int PickShopCard(World world, ShopState shop)
    {
        var hero = world.Hero;
        bool Affordable(ShopCard c) => !c.Bought && c.Price <= hero.Gold;
        if (shop.Cards.Count > 0 && shop.Cards.TrueForAll(c => c.Kind == ShopCardKind.Potion))
        {
            PotionUpgrade[] order = hero.Potions == 0 ? [PotionUpgrade.Refill, PotionUpgrade.Slots, PotionUpgrade.Speed] : [PotionUpgrade.Slots, PotionUpgrade.Speed];
            foreach (var u in order)
            {
                var k = shop.Cards.FindIndex(c => c.Upgrade == u && Affordable(c));
                if (k >= 0) return k;
            }
            return -1;
        }
        var best = -1;
        var bestGain = float.NegativeInfinity;
        for (var i = 0; i < shop.Cards.Count; i++)
        {
            var c = shop.Cards[i];
            if (c.Kind != ShopCardKind.Equip || c.Item is null || !Affordable(c)) continue;
            var gain = EquipGain(world, c.Item);
            if (gain > bestGain) { bestGain = gain; best = i; }
        }
        return best;
    }

    /// <summary>
    /// 견주는 값(기획 15절 v0.9 "최대 체력 + 10 × 방어"): 장비만의 능력치(<c>EquipStats.Sum</c>)의 <c>max_hp</c> + 10 × <c>armor</c>.
    /// <c>rules.equipment</c> 가 없는 정의는 0.
    /// </summary>
    public static float EquipValue(World world, IReadOnlyList<EquipItem?> equipment)
    {
        if (world.Defs.Rules.Equipment is not { } rules) return 0f;
        var s = EquipStats.Sum(equipment, world.Defs, rules);
        return s[StatKey.MaxHp] + 10f * s[StatKey.Armor];
    }

    /// <summary>이 장비를 끼웠을 때 견주는 값의 차이 — 그 칸(반지는 빈 칸 먼저, 둘 다 차면 값이 작은 반지 자리)을 바꾼 장비 칸으로 견준다.</summary>
    public static float EquipGain(World world, EquipItem item)
    {
        if (!world.Defs.ItemBases.TryGet(item.BaseId, out var b)) return float.NegativeInfinity;
        var now = world.Hero.Equipment;
        var slot = (int)HeroEquipment.SlotOf(b.Kind);
        if (b.Kind == EquipKind.Ring)
            slot = now[(int)EquipSlot.Ring1] is null ? (int)EquipSlot.Ring1
                 : now[(int)EquipSlot.Ring2] is null ? (int)EquipSlot.Ring2
                 : WeakerRing(world) == 0 ? (int)EquipSlot.Ring1 : (int)EquipSlot.Ring2;
        var next = (EquipItem?[])now.Clone();
        next[slot] = item;
        return EquipValue(world, next) - EquipValue(world, now);
    }

    /// <summary>두 반지 중 견주는 값이 작은 쪽(0 = 첫째, 1 = 둘째, 같으면 첫째).</summary>
    public static int WeakerRing(World world)
    {
        var now = world.Hero.Equipment;
        var a = EquipValue(world, [now[(int)EquipSlot.Ring1]]);
        var b = EquipValue(world, [now[(int)EquipSlot.Ring2]]);
        return b < a ? 1 : 0;
    }

    /// <summary>반경 200 안 적에게서 멀어지는 방향(거리 역수 가중 합, 길이 ≤ 1).</summary>
    private static Vector2 Avoid(World world)
    {
        var hero = world.Hero.Pos;
        var sum = Vector2.Zero;
        foreach (var e in world.Enemies.Items)
        {
            if (!e.IsAlive) continue;
            var d = hero - e.Pos;
            var len = d.Length();
            if (len > AvoidRadius || len < 1e-3f) continue;
            sum += d / (len * len) * 40f;
        }
        var l = sum.Length();
        return l > 1f ? sum / l : sum;
    }

    /// <summary>맵 가장자리(경계까지 80px 안)에서 가운데 쪽으로.</summary>
    private static Vector2 EdgePull(World world)
    {
        if (world.Nav is null) return Vector2.Zero;
        var c = world.Nav.Clearance(world.Hero.Pos);
        if (c > 80f) return Vector2.Zero;
        var center = world.Resolved is { } r && r.TryGetAnchor(RoomMapRules.CenterAnchor, out var a) ? a : world.MapSize * 0.5f;
        return MathX.NormalizeOr(center - world.Hero.Pos, Vector2.Zero) * (1f - MathF.Max(0f, c) / 80f) * 1.5f;
    }

    /// <summary>목표 쪽 길 방향(<c>TryPathStep</c> 의 다음 꺾임점, 0.5초마다 다시 구함 — 길이 없으면 곧장).</summary>
    private Vector2 PathDir(World world, Vector2 target)
    {
        var hero = world.Hero.Pos;
        if (world.Tick - _pathTick >= PathRefreshTicks || Vector2.DistanceSquared(target, _pathTarget) > 32f * 32f
            || Vector2.DistanceSquared(hero, _pathNext) < 12f * 12f)
        {
            _pathTarget = target;
            _pathTick = world.Tick;
            _pathNext = world.Nav is not null && world.Nav.TryPathStep(hero, target, world.Hero.Radius, out var next) ? next : target;
        }
        return MathX.NormalizeOr(_pathNext - hero, Vector2.Zero);
    }

    private static Vector2? NearestGatingPickup(World world)
    {
        Vector2? best = null;
        var bestD = float.MaxValue;
        foreach (var p in world.Pickups.Items)
        {
            if (p.Removed || !p.GatesExit) continue;
            var d = Vector2.DistanceSquared(p.Pos, world.Hero.Pos);
            if (d < bestD) { bestD = d; best = p.Pos; }
        }
        return best;
    }

    private static Vector2? NearestPickup(World world, float radius)
    {
        Vector2? best = null;
        var bestD = radius * radius;
        foreach (var p in world.Pickups.Items)
        {
            if (p.Removed) continue;
            var d = Vector2.DistanceSquared(p.Pos, world.Hero.Pos);
            if (d <= bestD) { bestD = d; best = p.Pos; }
        }
        return best;
    }

    private static (int Count, Vector2? Nearest) Remaining(World world)
    {
        var n = 0;
        Vector2? best = null;
        var bestD = float.MaxValue;
        foreach (var e in world.Enemies.Items)
        {
            if (!e.IsAlive) continue;
            n++;
            var d = Vector2.DistanceSquared(e.Pos, world.Hero.Pos);
            if (d < bestD) { bestD = d; best = e.Pos; }
        }
        return (n, best);
    }

    private static Vector2? Miniboss(World world, RoomState room)
    {
        if (room.Plan.Def.Miniboss is not { } mb) return null;
        foreach (var e in world.Enemies.Items)
            if (e.IsAlive && string.Equals(e.Def.Id, mb.Id, StringComparison.Ordinal)) return e.Pos;
        return null;
    }

    /// <summary>
    /// 문 우선순위(기획 15절 v0.9.1): 체력 &lt; 50% 이고 물약 칸이 비었으면 회복 문 > 마법서(무기 칸이 남았거나 Lv8 아닌 무기) > 보물 상자 = 상인(금화 ≥ 150) > 성소 > 나머지.
    /// 같으면 문 틈 가운데의 화면 x 가 작은 문, 그다음 화면 y 가 작은 문, 그다음 <c>Exits</c> 순서.
    /// </summary>
    private static int ChooseDoor(World world, RoomState room)
    {
        var best = 0;
        (int Rank, float X, float Y) bestKey = (int.MaxValue, float.MaxValue, float.MaxValue);
        for (var i = 0; i < room.Exits.Count; i++)
        {
            var exit = room.Exits[i];
            var p = DoorPoint(world, exit.GateId) ?? new Vector2(float.MaxValue, float.MaxValue);
            var key = (RewardRank(world, exit.Reward), p.X, p.Y);
            if (key.CompareTo(bestKey) < 0) { bestKey = key; best = i; }
        }
        return best;
    }

    private static int RewardRank(World world, RewardDef? reward)
    {
        if (reward is null) return 9;
        var hero = world.Hero;
        return reward.Kind switch
        {
            // v0.9.1(기획 15절): 체력 < 50% 이고 물약 칸이 비었으면 회복 문이 맨 앞
            RewardKind.HealNextRoom when hero.MaxHp > 0f && hero.Hp < hero.MaxHp * 0.5f && hero.Potions == 0 => 0,
            RewardKind.WeaponCards when TomeUseful(hero) => 1,
            RewardKind.WeaponBundle => 2,
            RewardKind.Merchant when hero.Gold >= MerchantGold => 2,   // v0.9(기획 15절): 금화 ≥ 150 이면 보물 상자와 같은 순위(첫 값)
            RewardKind.Shrine => 3 + ShrineOrder(reward.Id),
            RewardKind.WeaponCards => 6,
            _ => 7,
        };
    }

    private static int ShrineOrder(string id) =>
        id.Contains("enchanted", StringComparison.Ordinal) ? 0 : id.Contains("fascinating", StringComparison.Ordinal) ? 1 : 2;

    private static bool TomeUseful(Hero hero)
    {
        var owned = 0;
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
        {
            var w = hero.Weapons[i];
            if (w.IsEmpty) continue;
            owned++;
            if (w.Level < 8) return true;
        }
        return owned < Math.Max(1, hero.WeaponSlotsOpen) || owned == 0;
    }

    /// <summary>문 틈 칸 가운데(월드 px).</summary>
    private static Vector2? DoorPoint(World world, string gateId)
    {
        if (world.Arena is not { } arena || !world.Gates.TryGetValue(gateId, out var zone) || zone.Cells.Count == 0) return null;
        var sum = Vector2.Zero;
        foreach (var c in zone.Cells) sum += arena.Iso.CellCenter(c.X, c.Y);
        return sum / zone.Cells.Count;
    }

    /// <summary>방 들르기: 기준점(<c>room_*</c>, 이름 순)을 차례로 — 반경 60 안이면 닿음, 40초 넘으면 넘김.</summary>
    private Vector2? VisitTarget(World world)
    {
        if (world.Resolved is not { } map || world.Map is not { } mapDef) return null;
        var anchors = RoomAnchors(map);
        if (anchors.Count == 0) return null;
        if (_visitIndex >= anchors.Count) _visitIndex = 0;
        var (id, pos) = anchors[_visitIndex];
        var mapId = mapDef.MirrorOf ?? mapDef.Id;
        if (Vector2.Distance(world.Hero.Pos, pos) <= VisitReach)
        {
            _events.Add(new RoomVisit(mapId, id, world.Tick, false));
            Bump(mapId, id, visit: 1, skip: 0);
            _visitIndex = (_visitIndex + 1) % anchors.Count;
            _visitStartTick = world.Tick;
        }
        else if ((world.Tick - _visitStartTick) * World.Dt > VisitTimeout)
        {
            _events.Add(new RoomVisit(mapId, id, world.Tick, true));
            Bump(mapId, id, visit: 0, skip: 1);
            _visitIndex = (_visitIndex + 1) % anchors.Count;
            _visitStartTick = world.Tick;
        }
        return anchors[_visitIndex].Pos;
    }

    private static List<(string Id, Vector2 Pos)> RoomAnchors(ResolvedMap map)
    {
        var list = new List<(string, Vector2)>();
        foreach (var kv in map.Anchors)
            if (kv.Key.StartsWith("room_", StringComparison.Ordinal)) list.Add((kv.Key, kv.Value));
        list.Sort(static (a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return list;
    }

    private void Bump(string map, string room, int visit, int skip, int ticks = 0, float damage = 0f)
    {
        _stats.TryGetValue((map, room), out var s);
        _stats[(map, room)] = (s.Visits + visit, s.Skips + skip, s.Ticks + ticks, s.Damage + damage);
    }

    /// <summary>방 들르기 모드의 "방 안"(= <c>World.InnerRooms</c> 의 바닥 칸) 누적.</summary>
    private void TrackInside(World world)
    {
        if (Visit != AutoplayVisit.Rooms || world.Map is not { } mapDef) return;
        var index = world.InnerRooms.IndexAt(world.Hero.Pos);
        if (index < 0) return;
        var damage = 0f;
        foreach (ref readonly var d in world.Events.Previous.HeroDamaged.AsSpan()) damage += d.Applied;
        Bump(mapDef.MirrorOf ?? mapDef.Id, world.InnerRooms.Rooms[index].Id, 0, 0, 1, damage);
    }
}
