namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>적 생애 상태(죽으면 제거). 공포·행진·호위는 생애 상태가 아니라 이동 지시(WP4 MoveOrder).</summary>
public enum LifeState : byte { Entering, Emerging, Active, Stunned }

/// <summary>생성 때 붙는 등급. 정의 자체의 등급(<see cref="DefRank"/>)과 따로다.</summary>
public enum EnemyRank : byte { Normal, Elite, Unique }

/// <summary>면역. 정의의 <c>immunities</c> 문자열(버프 debuff 이름과 같은 말)을 생성 때 바꿔 둔다.</summary>
[Flags]
public enum Immunity : byte
{
    None = 0,
    Knockback = 1,
    Slow = 2,
}

public static class Immunities
{
    /// <summary>"knockback"·"slow" → 플래그. 모르는 이름은 <see cref="Immunity.None"/>.</summary>
    public static Immunity FromName(string name) => name switch
    {
        "knockback" => Immunity.Knockback,
        "slow" => Immunity.Slow,
        _ => Immunity.None,
    };

    public static Immunity FromNames(IReadOnlyList<string> names)
    {
        var flags = Immunity.None;
        for (var i = 0; i < names.Count; i++) flags |= FromName(names[i]);
        return flags;
    }
}

/// <summary>
/// 표시 상태 (설계서 6.6, 계약). EnemyThink 8번에서 정의·등급·접두사·버프·부품이 쓰고, 렌더(WP5)는 이것만 읽는다.
/// 맞을 때 흰 번쩍임은 따로(<c>HitFlashTime</c>, WP3).
/// </summary>
public struct EnemyVisual { public Rgb GlowColor; public float GlowAlpha; public Rgb FlashColor; public float FlashAlpha; public float AlphaMul; public Rgb FootRingColor; public float FootRingAlpha; }

/// <summary>생성 출처. 상한 정리 우선순위: 일반(Interval) → 이벤트 → 분열·소환. Stress 는 상한 검사를 건너뛴다.</summary>
public enum SpawnSource : byte { Interval, ElitePack, Ring, March, Escort, EscortMember, Miniboss, Boss, EventOther, Split, Summon, Stress }

/// <summary>생성 맥락 (설계서 6.6, 계약). 생성 훅이 출처를 본다. EscortCount -1 = 정의 기본값.</summary>
public readonly record struct SpawnContext(SpawnSource Source, EntityId Owner, int EscortCount = -1);
