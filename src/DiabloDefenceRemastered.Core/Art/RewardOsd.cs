using System.Globalization;
using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Core.Sim.Progression;

namespace DiabloDefenceRemastered.Core.Art;

/// <summary>둘레 아이콘 종류(그리는 쪽이 모양을 고름). <see cref="OsdIcon.Icon"/> 이 있으면 그 그림, 없으면 코드 그림.</summary>
public enum OsdIconKind : byte
{
    /// <summary>올릴 무기(무기 아이콘 + <c>+n</c>).</summary>
    Weapon,
    /// <summary>올릴 무기가 없음 — <c>단련</c>(코드 그림).</summary>
    Temper,
    /// <summary>아직 정하지 않음(닫힌 상자의 무기·정예 상자) — <c>?</c>.</summary>
    Unknown,
    /// <summary>체력 물약(물약 칸이 가득이면 흐리게 + <c>가득</c>).</summary>
    Potion,
    /// <summary>금화 + 값.</summary>
    Gold,
    /// <summary>마법서 무기 카드 실루엣(주황 띠).</summary>
    Card,
    /// <summary>진화 결과 무기 아이콘(금색 테).</summary>
    Evolution,
}

/// <summary>
/// 보상 물체 둘레 아이콘 하나(기획 11.4 "보상 물체 그림과 둘레 아이콘" 표). <see cref="Icon"/> = 그릴 원작 그림(없으면 <see cref="Kind"/> 의 코드 그림),
/// <see cref="Label"/> = 주인공이 300px 안일 때 아이콘 아래 짧은 글자(빈 문자열이면 없음), <see cref="Dim"/> = 흐리게(물약 칸이 가득).
/// </summary>
public readonly record struct OsdIcon(OsdIconKind Kind, IconDef? Icon, string Label, bool Dim = false);

/// <summary>
/// 보상 물체 둘레 아이콘 (v0.9.1 V8, 설계서 6.19 "보상 물체 둘레 아이콘", 기획 11.4 — 표시 전용 순수 함수. 문서는 "WP3 본문, 그리기 WP5" 지만 단계 2 wp5 가 그리기와 함께 만듦,
/// 시험하려고 Core 에 둠 — 13장 WP5 단계 2 기록). 시뮬레이션 상태를 읽기만 하고 바꾸지 않는다(난수를 뽑지 않음).
/// <list type="bullet">
/// <item>보물 상자(<see cref="RewardKind.WeaponBundle"/>): 올릴 무기(<see cref="Pickup.RewardChosen"/> — 상자를 연 뒤 책에. −1 이면 <c>단련</c>, 닫힌 상자는 아직 안 정해 <c>?</c>)
/// + <c>+n</c>, 체력 물약(꾸러미에 있으면 — 칸이 가득이면 흐리게 + <c>가득</c>), 금화(연 뒤 = 정한 값, 닫힌 상자 = 범위 × 액트 <c>gold_scale</c>).
/// 두 단계 보상의 <b>열린 상자</b>는 아이콘이 없다(받을 것은 떨어진 책에).</item>
/// <item>마법서(<see cref="RewardKind.WeaponCards"/>): 카드 실루엣 셋(내용은 집는 순간 정함 — v0.8.1).</item>
/// <item>정예 상자(<see cref="PickupKind.Chest"/>): 진화할 수 있으면 진화 결과 아이콘(<c>Build.FindEvolution</c> — 상자가 실제로 고르는 것과 같은 함수), 아니면 <c>?</c> 셋.</item>
/// <item>그 밖(적이 떨군 물약·금화·보석, 성소 — 단계 3, 상인·회복 문 — 물체 없음): 없음.</item>
/// </list>
/// 최대 <see cref="MaxIcons"/> 개. 놓는 자리는 <see cref="Offset"/>(물체 위쪽 호 반지름 28, 위 180° 에 고르게), 흔들림 <see cref="Bob"/>(1초 주기 2px).
/// </summary>
public static class RewardOsd
{
    public const int MaxIcons = 4;
    /// <summary>호 반지름(px, 기획 11.4).</summary>
    public const float ArcRadius = 28f;
    /// <summary>호 가운데 = 발 자리에서 이만큼 위(px — 물체 그림 높이의 어림, 미리보기로 맞춤).</summary>
    public const float ArcLift = 30f;
    /// <summary>아이콘 한 변(px, 기획 11.4 "24px").</summary>
    public const float IconSize = 24f;
    /// <summary>글자를 보이는 주인공 거리(px).</summary>
    public const float LabelRange = 300f;

    public static IReadOnlyList<OsdIcon> Icons(Pickup pickup, World world)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        ArgumentNullException.ThrowIfNull(world);
        if (pickup.Reward is { } reward)
        {
            // 두 단계 보상의 열린 상자: 받을 것은 책에 — 상자는 비어 있다
            if (reward.Drop is { } drop && pickup.Def.Id != drop.Id && pickup.RewardStage == RewardPickupStage.Dropped) return [];
            return Icons(reward, world, pickup.RewardStage == RewardPickupStage.Dropped, pickup.RewardChosen, pickup.Value);
        }
        return pickup.Def.Kind == PickupKind.Chest ? EliteChest(world) : [];
    }

    /// <summary>
    /// 보상 하나의 아이콘. <paramref name="decided"/> = 올릴 무기·금화를 정했음(두 단계 보상의 책), <paramref name="chosen"/>·<paramref name="gold"/> = 그 값.
    /// </summary>
    public static IReadOnlyList<OsdIcon> Icons(RewardDef reward, World world, bool decided, int chosen, int gold)
    {
        ArgumentNullException.ThrowIfNull(reward);
        ArgumentNullException.ThrowIfNull(world);
        var list = new List<OsdIcon>(MaxIcons);
        switch (reward.Kind)
        {
            case RewardKind.WeaponBundle:
            {
                var bundle = reward.Bundle ?? new RewardBundleDef();
                var levels = bundle.WeaponLevels;
                if (!decided) list.Add(new OsdIcon(OsdIconKind.Unknown, null, $"무기 +{levels}"));
                else if (chosen >= 0 && chosen < WeaponSlots.SlotCount && world.Hero.Weapons[chosen].Def is { } w)
                    list.Add(new OsdIcon(OsdIconKind.Weapon, w.Icon, $"{w.Name} +{levels}"));
                else list.Add(new OsdIcon(OsdIconKind.Temper, null, TemperName(world)));
                if (bundle.Potions > 0)
                {
                    var full = HeroPotions.Full(world);
                    var label = full ? "가득" : bundle.Potions > 1 ? $"물약 {bundle.Potions}" : "물약";
                    list.Add(new OsdIcon(OsdIconKind.Potion, PotionIcon(world), label, full));
                }
                if (decided ? gold > 0 : bundle.Gold is [_, _])
                    list.Add(new OsdIcon(OsdIconKind.Gold, GoldIcon(world), decided ? $"금화 {gold.ToString(CultureInfo.InvariantCulture)}" : GoldRange(world, bundle)));
                break;
            }
            case RewardKind.WeaponCards:
                list.Add(new OsdIcon(OsdIconKind.Card, null, ""));
                list.Add(new OsdIcon(OsdIconKind.Card, null, "무기 카드"));
                list.Add(new OsdIcon(OsdIconKind.Card, null, ""));
                break;
        }
        if (list.Count > MaxIcons) list.RemoveRange(MaxIcons, list.Count - MaxIcons);
        return list;
    }

    /// <summary>정예 상자: 진화할 수 있으면 진화 결과 아이콘, 아니면 <c>?</c> 셋.</summary>
    private static IReadOnlyList<OsdIcon> EliteChest(World world)
    {
        if (Build.FindEvolution(world) is { } evo)
            return [new OsdIcon(OsdIconKind.Evolution, evo.Evo.Result.Def.Icon, evo.Evo.Result.Def.Name)];
        return [new OsdIcon(OsdIconKind.Unknown, null, ""), new OsdIcon(OsdIconKind.Unknown, null, "강화"), new OsdIcon(OsdIconKind.Unknown, null, "")];
    }

    /// <summary>
    /// <paramref name="count"/> 개 중 <paramref name="index"/> 번째 아이콘 가운데(호 가운데 기준, 월드 px — y 아래가 +). 위 반원(180°)의 양 끝을 포함해 고르게, 왼쪽부터 —
    /// 하나면 바로 위, 둘이면 왼쪽·오른쪽, 셋이면 180°·90°·0°, 넷이면 60° 간격. 양 끝을 빼고 나누면(45° 간격) 24px 아이콘이 반지름 28 호에서 겹쳤다(미리보기 — 13장 WP5 단계 2 기록).
    /// </summary>
    public static Vector2 Offset(int count, int index)
    {
        if (count <= 0) return Vector2.Zero;
        var a = count == 1 ? MathF.PI * 0.5f : MathF.PI * (count - 1 - index) / (count - 1);   // 왼쪽(π)부터
        return new Vector2(MathF.Cos(a) * ArcRadius, -MathF.Sin(a) * ArcRadius);
    }

    /// <summary>흔들림(px, 1초 주기 ±2 — 스테이지 틱으로, 3.4 표시 시간 규칙).</summary>
    public static float Bob(int stageTick) => 2f * MathF.Sin(MathF.Tau * (stageTick * World.Dt));

    private static string TemperName(World world) =>
        world.Defs.Rules.LevelUp?.Temper is { } id && world.Defs.Rules.LevelUp.Fillers.FirstOrDefault(f => f.Id == id) is { Name.Length: > 0 } f ? f.Name : "단련";

    /// <summary>체력 물약 아이콘: 물약 칸 획득물(<c>potion_stack</c>)의 바닥 그림 마지막 프레임, 없으면 그 draw 그림.</summary>
    private static IconDef? PotionIcon(World world) => FloorIcon(world, PickupKind.PotionStack);

    private static IconDef? GoldIcon(World world) => FloorIcon(world, PickupKind.Gold);

    private static IconDef? FloorIcon(World world, PickupKind kind)
    {
        foreach (var p in world.Defs.Pickups.All)
        {
            if (p.Kind != kind) continue;
            if (p.Floor is { } f) return f.Frame is null && f.Sprite.IsBound ? f with { Frame = f.Sprite.Def.Frames } : f;
            if (p.Draw.Sprite is { } s) return new IconDef { Sprite = s, Frame = p.Draw.Frame };
            return null;
        }
        return null;
    }

    private static string GoldRange(World world, RewardBundleDef bundle)
    {
        var scale = world.Run?.Act.Act.Rewards.GoldScale ?? world.Stage?.Rewards.GoldScale ?? 1f;
        var lo = (int)MathF.Round(bundle.Gold![0] * scale, MidpointRounding.AwayFromZero);
        var hi = (int)MathF.Round(bundle.Gold[1] * scale, MidpointRounding.AwayFromZero);
        return string.Create(CultureInfo.InvariantCulture, $"금화 {lo}~{hi}");
    }
}
