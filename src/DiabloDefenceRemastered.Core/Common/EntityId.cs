namespace DiabloDefenceRemastered.Core.Common;

/// <summary>
/// 개체 핸들. Slot = 저장소의 칸 번호, Gen = 그 칸을 몇 번째로 썼는지(1부터).
/// 칸이 다시 쓰이면 Gen 이 바뀌므로 지운 개체의 낡은 핸들은 무효가 된다.
/// 저장소마다 칸 번호가 따로이므로 어느 저장소의 핸들인지는 쓰는 자리가 안다(적·투사체 …).
/// </summary>
public readonly record struct EntityId(int Slot, int Gen)
{
    /// <summary>없음. default 와 같다.</summary>
    public static readonly EntityId None = default;

    /// <summary>주인공. 저장소에 없는 특수 핸들.</summary>
    public static readonly EntityId Hero = new(-1, 1);

    public bool IsNone => Gen == 0;
    public bool IsHero => this == Hero;

    /// <summary>해시·정렬용 정수 하나. 같은 저장소 안에서 유일하다.</summary>
    public uint Key => unchecked((uint)Slot * 0x9E3779B1u ^ (uint)Gen);

    public override string ToString() => IsNone ? "None" : IsHero ? "Hero" : $"{Slot}:{Gen}";
}
