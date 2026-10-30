namespace DiabloAssetImporter;

/// <summary>
/// 진입점 (소유 WP0, 설계서 1.4: import·verify·probe·contact·audit, 종료 코드 0~4). WP1b 가 만든 빈 연결 파일 — 아직 아무 명령도 없다.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.Error.WriteLine("가져오기 도구는 아직 없다(WP0). 지금은 tools/import-assets.ps1 로 원작 exe PNG 만 채울 수 있다.");
        return 1;
    }
}
