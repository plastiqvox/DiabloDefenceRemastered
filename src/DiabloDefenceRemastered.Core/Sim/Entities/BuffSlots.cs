using System.Runtime.CompilerServices;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>붙은 버프 한 칸. Active 가 거짓이면 빈 칸.</summary>
public struct BuffSlot
{
    public bool Active;
    /// <summary><c>Defs.Buffs</c> 안 번호.</summary>
    public short BuffIndex;
    /// <summary>이 틱부터 없음(<c>World.Tick &lt; EndTick</c> 동안 유효).</summary>
    public int EndTick;
    /// <summary>값 배율(버프 stats × Mul).</summary>
    public float Mul;
}

/// <summary>
/// 적마다 고정 6칸 버프 (설계서 6.7, WP1 공통 도우미 — WP3 장판 둔화·WP4 오라·함성이 같이 쓴다).
/// 같은 버프는 큰 값 하나만 남고(끝 시각도 늦은 쪽), 다른 버프는 더한다. 칸이 모자라면 가장 먼저 끝나는 것을 바꾼다.
/// debuff 가 있는 버프는 그 이름의 면역이 있으면 붙지 않는다.
/// </summary>
[InlineArray(SlotCount)]
public struct BuffSlots
{
    public const int SlotCount = 6;

    private BuffSlot _e0;

    public void Clear()
    {
        for (var i = 0; i < SlotCount; i++) this[i] = default;
    }

    /// <summary>버프를 붙인다. 면역이면 false.</summary>
    /// <param name="buffIndex"><c>Defs.Buffs</c> 안 번호.</param>
    /// <param name="endTick">끝 틱(오라처럼 범위 안에서만이면 매 틱 지금 + 2).</param>
    public bool Apply(BuffDef def, int buffIndex, int endTick, float mul, Immunity immunities)
    {
        if (def.Debuff is { } debuff && (immunities & Immunities.FromName(debuff)) != 0) return false;

        var free = -1;
        var soonest = -1;
        for (var i = 0; i < SlotCount; i++)
        {
            ref var s = ref this[i];
            if (!s.Active) { if (free < 0) free = i; continue; }
            if (s.BuffIndex == buffIndex)
            {
                s.Mul = MathF.Max(s.Mul, mul);
                s.EndTick = Math.Max(s.EndTick, endTick);
                return true;
            }
            if (soonest < 0 || s.EndTick < this[soonest].EndTick) soonest = i;
        }
        var slot = free >= 0 ? free : soonest;
        this[slot] = new BuffSlot { Active = true, BuffIndex = (short)buffIndex, EndTick = endTick, Mul = mul };
        return true;
    }

    /// <summary>끝난 칸을 비운다(BeginTick).</summary>
    public void RemoveExpired(int tick)
    {
        for (var i = 0; i < SlotCount; i++)
        {
            ref var s = ref this[i];
            if (s.Active && tick >= s.EndTick) s = default;
        }
    }

    public readonly bool Has(int buffIndex)
    {
        for (var i = 0; i < SlotCount; i++)
            if (this[i].Active && this[i].BuffIndex == buffIndex) return true;
        return false;
    }

    /// <summary>붙은 버프들의 그 키 값 합(버프%는 더한다, 기획 4.2).</summary>
    public readonly float Sum(EnemyStatKey key, DefTable<BuffDef> buffs)
    {
        var sum = 0f;
        for (var i = 0; i < SlotCount; i++)
        {
            var s = this[i];
            if (!s.Active) continue;
            if (buffs[s.BuffIndex].Stats.TryGetValue(key, out var v)) sum += v * s.Mul;
        }
        return sum;
    }
}
