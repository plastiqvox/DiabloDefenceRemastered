namespace DiabloDefenceRemastered.Core.Common;

/// <summary><see cref="EntityStore{T}"/> 에 들어가는 개체. 풀에서 다시 쓰므로 <see cref="Reset"/> 이 모든 조각을 지운다.</summary>
public interface IEntity
{
    EntityId Id { get; set; }

    /// <summary>지우기 표시. 실제로 빠지는 것은 <see cref="EntityStore{T}.Compact"/> 때(EndTick).</summary>
    bool Removed { get; set; }

    void Reset();
}
