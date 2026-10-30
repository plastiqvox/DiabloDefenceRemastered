namespace DiabloDefenceRemastered.Core.Map;

/// <summary>
/// 하위 타일 좌표(실수) — 정의 안의 맵 위치(입구·기준점·주인공 시작·점 빛) (설계서 4.1, 계약 WP1b). JSON <c>[tx,ty]</c>.
/// <b>칸 중심 = 정수 좌표</b>: 칸 (tx, ty) 는 <c>[tx−0.5, tx+0.5) × [ty−0.5, ty+0.5)</c>.
/// 월드 px(<see cref="Vector2"/>)와 형이 달라 섞이면 컴파일러가 잡는다. 월드로 바꾸는 곳은 <see cref="IsoMath"/> 한 곳이고,
/// 다른 묶음은 <c>World.Resolved</c>(<see cref="ResolvedMap"/>)만 읽는다.
/// </summary>
public readonly record struct TileCoord(float Tx, float Ty)
{
    /// <summary>이 좌표가 든 칸 번호(칸 중심 = 정수, 반올림 — 0.5 는 큰 쪽).</summary>
    public Int2 Cell => new(IsoMath.RoundToCell(Tx), IsoMath.RoundToCell(Ty));

    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"[{Tx},{Ty}]");
}
