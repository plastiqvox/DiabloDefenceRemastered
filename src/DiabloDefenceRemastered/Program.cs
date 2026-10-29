namespace DiabloDefenceRemastered;

/// <summary>진입점 (소유 WP7: 옵션 파싱·SDL 환경 변수·종료 코드). WP1 이 만든 최소 본문.</summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        using var app = new GameApp();
        app.Run();
        return 0;
    }
}
