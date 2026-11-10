using System.Runtime.CompilerServices;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>주인공 능력치 합(<see cref="StatKey"/> 마다 값 하나). 패시브·영구 강화가 더한 결과(WP3 가 쓴다).</summary>
public sealed class StatBlock
{
    private readonly float[] _values = new float[StatKeys.Count];

    public float this[StatKey key]
    {
        get => _values[(int)key];
        set => _values[(int)key] = value;
    }

    public void Clear() => Array.Clear(_values);

    public void CopyFrom(StatBlock other) => Array.Copy(other._values, _values, _values.Length);
}

/// <summary>
/// 무기 패턴이 칸에 들고 다니는 상태(간격 발사 남은 발 수, 궤도 각도 …). 뜻은 패턴(WP3)이 정한다.
/// partial 이다: WP3 은 자기 폴더 조각(<c>Sim/Weapons/WeaponPatternState.Combat.cs</c>, namespace <c>…Core.Sim</c>)에 필드를 더한다.
/// 빈 칸·새 무기는 <c>default</c> 로 지워지므로 더한 필드도 기본값이 "상태 없음" 이어야 한다.
/// </summary>
public partial struct WeaponPatternState
{
    public int BurstLeft;
    public float BurstTimer;
    public float Timer;
    public float Angle;
    public int Counter;
    public bool Active;
}

/// <summary>무기 칸 하나 (설계서 5.3). Def 가 null 이면 빈 칸.</summary>
public struct WeaponSlot
{
    public WeaponDef? Def;
    public int Level;
    /// <summary>쿨다운 남은 시간(초).</summary>
    public float Cooldown;
    public WeaponPatternState Pattern;
    public bool Evolved;
    /// <summary>얻은 스테이지 틱(결과 화면 초당 피해의 분모).</summary>
    public int AcquiredTick;
    /// <summary>최대 레벨이 된 틱. -1 = 아직("먼저 만렙이 된 무기를 진화").</summary>
    public int MaxedAtTick;

    public readonly bool IsEmpty => Def is null;
}

/// <summary>
/// 무기 칸 그릇(v0.8 — 4 → 6, 설계서 6.10). 열린 칸 수는 <see cref="Hero.WeaponSlotsOpen"/>(판 시작 4, 계단마다 +1, 상한 6 — <c>RunSlots.OpenAt</c>).
/// 열린 칸 밖 칸은 비어 있다(레벨업·상자·<c>--weapons</c> 가 열린 칸만 채움 — WP3).
/// </summary>
[InlineArray(SlotCount)]
public struct WeaponSlots
{
    public const int SlotCount = 6;
    private WeaponSlot _e0;
}

public struct PassiveSlot
{
    public PassiveDef? Def;
    public int Level;
    public int AcquiredTick;

    public readonly bool IsEmpty => Def is null;
}

/// <summary>패시브 칸 그릇(v0.8 — 4 → 6). 열린 칸 수는 <see cref="Hero.PassiveSlotsOpen"/>(<see cref="WeaponSlots"/> 와 같은 규칙).</summary>
[InlineArray(SlotCount)]
public struct PassiveSlots
{
    public const int SlotCount = 6;
    private PassiveSlot _e0;
}

/// <summary>축복 칸 8(v0.7, 6.9 — <c>blessing</c> 패시브, 패시브 칸 밖). 쓰기 WP3(성소 창), 읽기 WP6(일시정지 빌드 목록)·WP7(해시).</summary>
[InlineArray(SlotCount)]
public struct BlessingSlots
{
    public const int SlotCount = 8;
    private PassiveSlot _e0;
}

/// <summary>
/// 주인공 — 공통 조각 (WP1, 설계서 11.2). 저장소에 없고 핸들은 <see cref="EntityId.Hero"/>.
/// 재생·무기 내부 상태는 <c>Sim/Hero/Hero.Combat.cs</c>(WP3, <see cref="ResetCombat"/>).
/// </summary>
public sealed partial class Hero
{
    public EntityId Id => EntityId.Hero;

    // 쓰기 WP2(이동)·WP3(시작 위치)
    public Vector2 Pos;
    public Vector2 PrevPos;
    public Vector2 Vel;
    public float Radius = 14f;

    // 쓰기 WP3 / 읽기 모두
    /// <summary>바라보는 방향 = 마지막 이동 방향(단위 벡터, 기획 3).</summary>
    public Vector2 Facing = new(0, 1);
    public AnimClock Anim;
    public float Hp;
    public float MaxHp;
    public bool Dead;
    public float InvulnTime;
    public float HitFlashTime;
    public MoveIntent Intent;
    public float MoveSpeed;
    /// <summary>받은 넉백(WP3 DamageResolve 가 쓰고 WP2 Locomotion 이 적분).</summary>
    public Vector2 KnockbackVel;
    public float KnockbackTime;

    // 쓰기 WP3 / 읽기 WP4(자동 조작)·WP6(HUD)
    public StatBlock Stats { get; } = new();
    public WeaponSlots Weapons;
    public PassiveSlots Passives;
    /// <summary>축복(v0.7 — <c>blessing</c> 패시브, 슬롯 밖 8칸). 방 사이에는 <c>HeroCarry.Blessings</c> 로 넘긴다.</summary>
    public BlessingSlots Blessings;
    /// <summary>
    /// 열린 무기 칸 수(v0.8 — 짐으로 넘김, 3.6). <see cref="Reset"/> 은 0 — 주인공 만들기(WP3)가 <c>RunSlots.OpenAt(World.Room?.Plan.Act ?? 1, …)</c> 로 정하고 짐 <c>Restore</c> 가 덮는다.
    /// 읽기 WP4(자동 조작 문 우선순위)·WP6(HUD 칸)·WP7(해시).
    /// </summary>
    public int WeaponSlotsOpen;
    /// <summary>열린 패시브 칸 수(v0.8 — <see cref="WeaponSlotsOpen"/> 과 같은 규칙).</summary>
    public int PassiveSlotsOpen;
    /// <summary>허리띠 물약 수(v0.8, 0..<c>rules.hero.belt.slots</c> — 쓰기 WP3, 읽기 WP6 HUD·WP4 문 우선순위·WP7 해시). 짐 <c>Belt</c> 로 방·액트 사이에 이어진다.</summary>
    public int Belt;

    // 쓰기 WP2 / 읽기 WP3
    /// <summary>몸이 닿은 적 수(3 이상이면 다음 틱 이동 ×0.6).</summary>
    public int TouchingCount;

    public void Reset()
    {
        Pos = PrevPos = Vel = Vector2.Zero;
        Radius = 14f;
        Facing = new Vector2(0, 1);
        Anim = default;
        Hp = MaxHp = 0f;
        Dead = false;
        InvulnTime = HitFlashTime = 0f;
        Intent = MoveIntent.Hold;
        MoveSpeed = 0f;
        KnockbackVel = Vector2.Zero;
        KnockbackTime = 0f;
        Stats.Clear();
        for (var i = 0; i < WeaponSlots.SlotCount; i++) Weapons[i] = new WeaponSlot { MaxedAtTick = -1 };
        for (var i = 0; i < PassiveSlots.SlotCount; i++) Passives[i] = default;
        for (var i = 0; i < BlessingSlots.SlotCount; i++) Blessings[i] = default;
        WeaponSlotsOpen = PassiveSlotsOpen = 0;   // 규칙을 받지 않는다 — 주인공 만들기(WP3)가 RunSlots.OpenAt 로, 짐 Restore 가 덮는다
        Belt = 0;
        TouchingCount = 0;
        ResetCombat();
    }

    /// <summary>WP3 조각(Hero.Combat.cs)의 상태 지우기.</summary>
    partial void ResetCombat();
}
