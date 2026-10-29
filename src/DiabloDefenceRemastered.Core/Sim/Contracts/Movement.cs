namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>
/// 이동 의도 종류 (설계서 4.4, 계약).
/// Hold: 멈춤. SeekHero: 정의 movement(flow/astar/direct)대로 주인공에게. SeekPoint: astar 면 A*, 아니면 직진 + 미끄러짐.
/// Away: 기준점 반대로 + 미끄러짐. Heading: 방향 고정 + 미끄러짐. Straight: 미끄러짐 없는 직선(입장·돌진. 막히면 충돌 단계가 멈춘다)
/// </summary>
public enum MoveKind : byte { Hold, SeekHero, SeekPoint, Away, Heading, Straight }

/// <summary>
/// 적 부품(WP4)이 틱마다 쓰는 이동 의도. 실제 희망 속도로 바꾸는 일은 WP2 Navigator 가 Locomotion 단계에서 한다.
/// </summary>
public struct MoveIntent
{
    public MoveKind Kind;
    public Vector2 Point;       // SeekPoint 의 목표, Away 의 도망칠 기준점
    public Vector2 Dir;         // Heading(행진)·Straight(입장·돌진) 방향
    public float SpeedMul;      // 1 = Enemy.MoveSpeed 그대로. 돌진처럼 자기 속도가 있으면 그 비율

    public static MoveIntent Hold => new() { Kind = MoveKind.Hold, SpeedMul = 0f };
    public static MoveIntent SeekHero(float speedMul = 1f) => new() { Kind = MoveKind.SeekHero, SpeedMul = speedMul };
    public static MoveIntent SeekPoint(Vector2 point, float speedMul = 1f) => new() { Kind = MoveKind.SeekPoint, Point = point, SpeedMul = speedMul };
    public static MoveIntent Away(Vector2 from, float speedMul = 1f) => new() { Kind = MoveKind.Away, Point = from, SpeedMul = speedMul };
    public static MoveIntent Heading(Vector2 dir, float speedMul = 1f) => new() { Kind = MoveKind.Heading, Dir = dir, SpeedMul = speedMul };
    public static MoveIntent Straight(Vector2 dir, float speedMul = 1f) => new() { Kind = MoveKind.Straight, Dir = dir, SpeedMul = speedMul };
}
