namespace DiabloDefenceRemastered.Core.Art;

/// <summary>
/// 획득물 바닥 그림 고르기 (설계서 6.9 "보석 그림"·6.19 "보상 물체 둘레 아이콘", 기획 11.1·11.4 — 소유 WP5, 단계 2 wp5). 게임 <c>WorldRenderer</c> 가 부른다.
/// <list type="bullet">
/// <item><see cref="PickupDef.Floor"/> 가 있으면 원작 그림: <c>frame</c> 이 없으면 떨어짐 애니 전부(<see cref="FrameTime"/>)를 한 번 돌리고 마지막 프레임에서 멈춤,
/// 있으면 그 프레임 하나만(보석 — 떨어짐 애니 없음). 두 단계 보상의 열린 상자는 그 프레임 + <see cref="OpenedStep"/>(원작 물체 CEL 의 닫힘 1 → 열림(빔) 3).</item>
/// <item>그림을 못 쓰면(가져오기에서 빠짐·자원 없는 실행) <see cref="PickupDef.Draw"/> 대체 — 그리는 쪽이 정한다.</item>
/// <item>빛: 경험치 보석만, <c>draw.color</c> 로 가산. 원작 돌을 그리면 마름모 빛의 <see cref="FloorGlow"/> 배(은은하게 — 기획 v0.9.3 첫 값).</item>
/// </list>
/// </summary>
public static class PickupArt
{
    /// <summary>바닥 아이템 떨어짐 애니 프레임 시간(초, 기획 11.1 V8 "0.05s").</summary>
    public const float FrameTime = 0.05f;
    /// <summary>원작 돌 보석의 빛 배율(마름모 빛 대비, 기획 v0.9.3 "0.6배").</summary>
    public const float FloorGlow = 0.6f;
    /// <summary>열린 상자 = 닫힌 상자 프레임 + 이 값(원작 <c>objects\chest*</c>: 1 닫힘, 2 열림(내용), 3 열림(빔) — 책이 떨어져 나가 빈 상자).</summary>
    public const int OpenedStep = 2;

    /// <summary>두 단계 보상(<c>RewardDef.Drop</c>)의 열린 상자인가(떨어진 책은 아님).</summary>
    public static bool IsOpenedObject(Pickup p) =>
        p.Reward is { Drop: { } drop } && p.RewardStage == RewardPickupStage.Dropped && !string.Equals(p.Def.Id, drop.Id, StringComparison.Ordinal);

    /// <summary>바닥 그림 프레임(1부터). <paramref name="frames"/> = 그림 정의의 프레임 수, <paramref name="age"/> = 획득물 나이(초).</summary>
    public static int FloorFrame(IconDef floor, int frames, float age, bool opened)
    {
        ArgumentNullException.ThrowIfNull(floor);
        frames = Math.Max(1, frames);
        if (floor.Frame is { } f) return Math.Clamp(opened ? f + OpenedStep : f, 1, frames);
        return Math.Min(frames, 1 + (int)(Math.Max(0f, age) / FrameTime));
    }

    /// <summary>가산 빛 배율: 경험치 보석이 아니면 0, 원작 그림을 그리면 <see cref="FloorGlow"/>, 마름모면 1.</summary>
    public static float GlowScale(PickupDef def, bool floorDrawn)
    {
        ArgumentNullException.ThrowIfNull(def);
        if (def.Kind != PickupKind.Xp) return 0f;
        return floorDrawn ? FloorGlow : 1f;
    }
}
