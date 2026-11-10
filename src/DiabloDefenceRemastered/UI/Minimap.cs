using DiabloDefenceRemastered.Rendering;

namespace DiabloDefenceRemastered.UI;

/// <summary>미니맵 점 하나(월드 px, 색, 지름 px).</summary>
public readonly record struct MinimapDot(Vector2 World, Rgb Color, float Size);

/// <summary>
/// 미니맵 (설계서 8.2, 기획 12.1). 월드 ÷ 16 축척, 오른쪽 위 HUD 아래. <see cref="IWorldBound"/> — 방마다 그 방 맵으로 다시 만든다(7.11).
/// <para>점 규칙(<see cref="CollectDots"/>, 순수 함수): 주인공 흰색, 중간 보스·보스 큰 빨강·정예 상자·보상 물체 금색은 늘,
/// 정예·우두머리 파랑은 <c>FogMath.EnemyAlpha &gt; 0</c> 일 때만(<c>--reveal</c> 이면 늘), 일반 적은 그리지 않음,
/// 방 보급(<c>Pickup.SupplyRoom</c>)은 흰 점으로 늘(v0.6.3), 정리 단계에서는 남은 적 모두 붉은 작은 점(v0.8 — 정리 시작부터),
/// 문 단계에서는 열린 출구 문 가운데에 보상 색 점(v0.7).</para>
/// <para>지형 그림(<c>minimap.png</c>, 가 본 칸만 — 기획 14.1 단계 2)은 아직 없다: 지금은 맵 마름모 자리를 어두운 판으로 그린다.</para>
/// </summary>
public sealed class Minimap(AppServices app) : IWorldBound
{
    /// <summary>월드 px → 미니맵 px.</summary>
    public const float Scale = 1f / 16f;
    public const float Alpha = 0.75f;

    private readonly List<MinimapDot> _dots = [];
    private World? _world;

    public bool Visible { get; set; } = true;

    public void Attach(World world)
    {
        _world = world;
        _dots.Clear();
    }

    /// <summary>미니맵 크기(가상 px) = 맵 외곽 상자 ÷ 16.</summary>
    public static Vector2 SizeOf(World world) => world.MapSize * Scale;

    /// <summary>
    /// 이번 프레임 점(그리는 순서: 문·보급·상자 → 적 → 주인공 — 주인공이 맨 위). <paramref name="reveal"/> = <c>--reveal</c>.
    /// </summary>
    public static void CollectDots(World world, bool reveal, List<MinimapDot> into)
    {
        into.Clear();
        var room = world.Room;

        // 열린 출구 문(문 단계 — 보상 색, 계단은 흰색)
        if (room is { Phase: RoomPhase.Doors } && world.Arena is { } arena)
            foreach (var exit in room.Exits)
                if (world.Gates.TryGetValue(exit.GateId, out var zone) && zone.Cells.Count > 0)
                {
                    var sum = Vector2.Zero;
                    foreach (var cell in zone.Cells) sum += arena.Iso.CellCenter(cell.X, cell.Y);
                    into.Add(new MinimapDot(sum / zone.Cells.Count, exit.Stairs ? Theme.White : RewardColor(exit.Reward), 4f));
                }

        foreach (var p in world.Pickups.Items)
        {
            if (p.Removed) continue;
            if (p.SupplyRoom is not null) into.Add(new MinimapDot(p.Pos, Theme.White, 3f));
            else if (p.Reward is not null || p.GatesExit || p.Def.Kind == PickupKind.Chest) into.Add(new MinimapDot(p.Pos, Theme.Gold, 3f));
        }

        var cleanup = room is { Phase: RoomPhase.Cleanup };
        foreach (var e in world.Enemies.Items)
        {
            if (e.Removed || e.Dead) continue;
            var boss = IsBoss(world, e);
            if (boss) into.Add(new MinimapDot(e.Pos, Theme.Red, 4f));
            else if (e.Rank != EnemyRank.Normal && (reveal || !world.Visibility.Enabled || FogMath.EnemyAlpha(world, e, world.Tick) > 0f))
                into.Add(new MinimapDot(e.Pos, Theme.Blue, 3f));
            else if (cleanup) into.Add(new MinimapDot(e.Pos, Theme.Red, 2f));
        }

        into.Add(new MinimapDot(world.Hero.Pos, Theme.White, 3f));
    }

    /// <summary>보스 막대가 있는 적(중간 보스·보스).</summary>
    public static bool IsBoss(World world, Enemy e)
    {
        if (e.Def.ShowBossBar) return true;
        foreach (var b in world.BossBars)
            if (b.Id == e.Id) return true;
        return false;
    }

    /// <summary>보상 종류 색(문 점·방 줄).</summary>
    public static Rgb RewardColor(RewardDef? reward) => reward?.Kind switch
    {
        RewardKind.WeaponCards => Theme.WeaponBand,
        RewardKind.WeaponBundle => Theme.Gold,
        RewardKind.BeltPotions => Theme.Red,
        RewardKind.Shrine => Theme.PassiveBand,
        _ => Theme.Grey,
    };

    /// <summary>오른쪽 위(<paramref name="topRight"/> = 미니맵 오른쪽 위 모서리, 가상 좌표).</summary>
    public void Draw(UiCanvas c, Vector2 topRight)
    {
        if (!Visible || _world is not { } world) return;
        var size = SizeOf(world);
        if (size.X <= 0f || size.Y <= 0f) return;
        var origin = new Vector2(topRight.X - size.X, topRight.Y);
        c.Fill(new RectF(origin.X, origin.Y, origin.X + size.X, origin.Y + size.Y), Theme.Panel, 0.5f * Alpha);
        CollectDots(world, app.Options.Reveal, _dots);
        foreach (var d in _dots)
        {
            var p = origin + d.World * Scale;
            var h = d.Size / 2;
            c.Fill(new RectF(p.X - h, p.Y - h, p.X + h, p.Y + h), d.Color, Alpha);
        }
    }
}
