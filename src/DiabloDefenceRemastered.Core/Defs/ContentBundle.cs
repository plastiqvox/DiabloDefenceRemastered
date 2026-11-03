namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 헬파이어 자원 묶음 (설계서 1.4·6.17, 사용자 결정 H, 계약 WP1d). MPQ 하나 = 묶음 하나. 기본 내용(diabdat)은 묶음이 아니다.
/// JSON 이름은 소문자(<c>"hellfire"</c> <c>"hfmonk"</c> <c>"hfmusic"</c> <c>"hfvoice"</c> — 공용 snake_case 열거형 변환기).
/// <see cref="Hfvoice"/> 는 원본 우선순위(가져오기 표 <c>prefer</c>)에만 쓰고 정의의 <c>requires</c> 에는 쓰지 않는다(13.5-20).
/// 묶음 MPQ 가 없는 PC 는 그 묶음을 <c>requires</c> 로 단 스테이지·직업만 꺼지고 나머지는 그대로 돈다(<c>Assets/ContentAvailability</c>).
/// </summary>
public enum ContentBundle : byte { Hellfire, Hfmonk, Hfmusic, Hfvoice }

public static class ContentBundles
{
    /// <summary>모든 묶음(열거 순서).</summary>
    public static IReadOnlyList<ContentBundle> All { get; } = [ContentBundle.Hellfire, ContentBundle.Hfmonk, ContentBundle.Hfmusic, ContentBundle.Hfvoice];

    /// <summary>묶음의 MPQ 파일 이름(<c>hellfire.mpq</c> …). 가져오기는 대소문자 무관으로 찾고, 카드 문구 <c>헬파이어 자원 필요(hellfire.mpq)</c> 가 이것을 쓴다.</summary>
    public static string MpqName(ContentBundle b) => Name(b) + ".mpq";

    /// <summary>JSON·로그 이름(<c>hellfire</c>).</summary>
    public static string Name(ContentBundle b) => b switch
    {
        ContentBundle.Hellfire => "hellfire",
        ContentBundle.Hfmonk => "hfmonk",
        ContentBundle.Hfmusic => "hfmusic",
        ContentBundle.Hfvoice => "hfvoice",
        _ => throw new ArgumentOutOfRangeException(nameof(b), b, null),
    };
}
