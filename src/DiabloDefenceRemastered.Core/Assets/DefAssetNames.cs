namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>정의가 가리키는 파일 하나와 그 주인 정의 D(적·스테이지·직업·무기·음악, 또는 기본 내용이면 null — 6.14 13.5-10 귀속 규칙).</summary>
/// <param name="Owner">
/// 그 파일에 닿는 <c>requires</c> 를 가진 정의. 그림 묶음·소리·이펙트처럼 <c>requires</c> 가 없는 정의의 파일은 그것을 가리키는 D 들마다 한 줄씩,
/// 맵 그림(<c>art</c> 폴더)은 그 맵을 쓰는 스테이지, 소리 연결 표 줄은 귀속 규칙대로(<c>hero.&lt;id&gt;</c> → 직업, <c>ui</c>·<c>pickups</c>·<c>music.title</c> → null).
/// </param>
/// <param name="File">Assets 뿌리 기준 '/' 상대 경로(<c>d1/monsters/zombie.png</c>).</param>
public readonly record struct DefAsset(IDef? Owner, string File);

/// <summary>
/// 정의 → 파일 이름(주인 포함) 추출 (설계서 6.14 13.5-10 ②·1.4 끝 시작 검사 ③ 교차 확인, 계약 WP1d v0.6.1, 본문 WP7).
/// 13.5-10 ② 시험(WP0)과 시작 검사(WP7)가 같은 추출을 쓴다 — 두 벌이면 "꺼짐" 판정과 이름 대조가 어긋난다.
/// WP1d 가 시그니처만 만든 빈 연결 파일.
/// </summary>
public static class DefAssetNames
{
    /// <summary>
    /// 정의가 가리키는 모든 파일(atlas 페이지 × 변종·메타·grid PNG·맵 그림 — <see cref="ArenaArt.OutputNames"/>·소리·음악·소리 색인)과 주인.
    /// 순서는 (파일 서수, 주인 ID 서수) — 결정적.
    /// </summary>
    /// <param name="arenas">맵 그림 이름 계산용 펼친 격자(<c>ArenaLayout.Expand</c>).</param>
    public static IReadOnlyList<DefAsset> For(GameDefs defs, IEnumerable<ArenaGrid> arenas)
        => throw new NotImplementedException("WP7 DefAssetNames.For");
}
