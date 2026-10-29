namespace DiabloDefenceRemastered.Core.Common;

/// <summary>데이터에 적는 방위(<c>"south"</c>, <c>"south_west"</c> …). 시트 열 계산은 <see cref="FacingMath"/> 한 곳.</summary>
public enum Compass : byte { South, SouthWest, West, NorthWest, North, NorthEast, East, SouthEast }

/// <summary>
/// 방향 → 시트 열. angle = atan2(-dx, dy) 를 0..360 으로(남 0°, 서 90°, 북 180°, 동 270°),
/// 열 = round(angle / (360 / 방향 수)) mod 방향 수 (설계서 4.1). 그림이 틀리면 이 파일의 오프셋 하나만 고친다.
/// </summary>
public static class FacingMath
{
    /// <summary>열 0 이 가리키는 각도(도). --preview 로 맞출 때 고치는 값.</summary>
    public const float ColumnOffsetDegrees = 0f;

    public static int ToColumn(Vector2 dir, int dirs)
    {
        if (dirs <= 1) return 0;
        var deg = AngleDegrees(dir) - ColumnOffsetDegrees;
        var step = 360f / dirs;
        var col = (int)MathF.Round(deg / step, MidpointRounding.AwayFromZero) % dirs;
        return col < 0 ? col + dirs : col;
    }

    /// <summary>남 0°, 서 90°, 북 180°, 동 270° 기준 각도 [0, 360).</summary>
    public static float AngleDegrees(Vector2 dir)
    {
        var deg = MathF.Atan2(-dir.X, dir.Y) * MathX.RadToDeg;
        return deg < 0 ? deg + 360f : deg;
    }

    /// <summary>열 → 단위 방향(미리보기에서 방향을 돌릴 때). <see cref="ToColumn"/> 의 역.</summary>
    public static Vector2 FromColumn(int column, int dirs)
    {
        var deg = column * (360f / Math.Max(dirs, 1)) + ColumnOffsetDegrees;
        var rad = deg * MathX.DegToRad;
        return new Vector2(-MathF.Sin(rad), MathF.Cos(rad));   // atan2(-x, y) = rad
    }

    public static Vector2 ToVector(Compass c) => c switch
    {
        Compass.South => new Vector2(0, 1),
        Compass.North => new Vector2(0, -1),
        Compass.West => new Vector2(-1, 0),
        Compass.East => new Vector2(1, 0),
        Compass.SouthWest => Vector2.Normalize(new Vector2(-1, 1)),
        Compass.SouthEast => Vector2.Normalize(new Vector2(1, 1)),
        Compass.NorthWest => Vector2.Normalize(new Vector2(-1, -1)),
        Compass.NorthEast => Vector2.Normalize(new Vector2(1, -1)),
        _ => new Vector2(0, 1),
    };
}
