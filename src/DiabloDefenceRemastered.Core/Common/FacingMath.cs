namespace DiabloDefenceRemastered.Core.Common;

/// <summary>데이터에 적는 방위(<c>"south"</c>, <c>"south_west"</c> …) — 땅 위 방위다. 시트 열 계산은 <see cref="FacingMath"/> 한 곳.</summary>
public enum Compass : byte { South, SouthWest, West, NorthWest, North, NorthEast, East, SouthEast }

/// <summary>
/// 방향 → 시트 열 (설계서 4.1). 원작 시트의 8·16방향은 땅 위 방향을 등각으로 그린 것이라(남서 그림 = 화면 (−32, +16) 쪽)
/// 열을 고르기 전에 화면 방향의 y 를 <see cref="IsoYScale"/> 배 한다: angle = atan2(−dx, IsoYScale·dy) 를 0..360 으로(남 0°, 서 90°, 북 180°, 동 270°),
/// 열 = round(angle / (360 / 방향 수)) mod 방향 수. 역(<see cref="FromColumn"/>)과 방위(<see cref="ToVector"/>)도 같은 보정을 쓴다.
/// 그림이 틀리면 <c>--preview sprites</c> 로 보고 이 파일의 두 상수만 고친다.
/// </summary>
public static class FacingMath
{
    /// <summary>열 0 이 가리키는 각도(도). --preview 로 맞출 때 고치는 값.</summary>
    public const float ColumnOffsetDegrees = 0f;

    /// <summary>등각 보정: 열을 고르기 전 화면 y 배율(1 = 보정 없음).</summary>
    public const float IsoYScale = 2f;

    public static int ToColumn(Vector2 dir, int dirs) => ToColumn(dir, dirs, IsoYScale);

    /// <summary><paramref name="yScale"/> 로 보정한 열(1 = 화면 각도 그대로 — 시험·비교용).</summary>
    public static int ToColumn(Vector2 dir, int dirs, float yScale)
    {
        if (dirs <= 1) return 0;
        var deg = AngleDegrees(new Vector2(dir.X, dir.Y * yScale)) - ColumnOffsetDegrees;
        var step = 360f / dirs;
        var col = (int)MathF.Round(deg / step, MidpointRounding.AwayFromZero) % dirs;
        return col < 0 ? col + dirs : col;
    }

    /// <summary>
    /// 땅 기준 속도 <c>|(vx, IsoYScale·vy)|</c>(px/s, 4.1). 원작 걷기 그림은 땅 위 걸음이라 화면 세로 v 는 땅에서 2v 다.
    /// 걷기 재생 배율은 이것 한 곳으로 센다(<see cref="AnimClock.WalkRate"/>) — 화면 속도(<c>vel.Length()</c>)를 쓰면 세로 걷기에서 발이 미끄러진다.
    /// </summary>
    public static float GroundSpeed(Vector2 vel) => new Vector2(vel.X, vel.Y * IsoYScale).Length();

    /// <summary>남 0°, 서 90°, 북 180°, 동 270° 기준 각도 [0, 360)(보정 없는 화면 각도).</summary>
    public static float AngleDegrees(Vector2 dir)
    {
        var deg = MathF.Atan2(-dir.X, dir.Y) * MathX.RadToDeg;
        return deg < 0 ? deg + 360f : deg;
    }

    /// <summary>
    /// 열 → 화면 단위 방향(미리보기에서 방향을 돌릴 때). <see cref="ToColumn(Vector2, int)"/> 의 역:
    /// 각도 θ 에서 normalize(−sin θ, cos θ / IsoYScale) → 8·12·16방향 왕복이 선다.
    /// </summary>
    public static Vector2 FromColumn(int column, int dirs)
    {
        var deg = column * (360f / Math.Max(dirs, 1)) + ColumnOffsetDegrees;
        var rad = deg * MathX.DegToRad;
        return Vector2.Normalize(new Vector2(-MathF.Sin(rad), MathF.Cos(rad) / IsoYScale));
    }

    /// <summary>
    /// 땅 위 방위 → 화면 단위 방향(<c>hero_start.facing</c>, 행진 <c>heading</c>). 대각선은 <b>등각 대각선</b> normalize(±2, ±1)
    /// (남서 = (−2, 1)) — 원작 그림 방향과 같다. 화면 45° (−1, 1) 로 두면 16방향 열로는 남남서가 된다.
    /// </summary>
    public static Vector2 ToVector(Compass c) => c switch
    {
        Compass.South => new Vector2(0, 1),
        Compass.North => new Vector2(0, -1),
        Compass.West => new Vector2(-1, 0),
        Compass.East => new Vector2(1, 0),
        Compass.SouthWest => Vector2.Normalize(new Vector2(-2, 1)),
        Compass.SouthEast => Vector2.Normalize(new Vector2(2, 1)),
        Compass.NorthWest => Vector2.Normalize(new Vector2(-2, -1)),
        Compass.NorthEast => Vector2.Normalize(new Vector2(2, -1)),
        _ => new Vector2(0, 1),
    };
}
