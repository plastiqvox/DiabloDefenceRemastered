using System.Diagnostics;

namespace DiabloAssetImporter.Cli;

/// <summary>
/// 저장소 뿌리 찾기·출력 경로 검사 (1.4 "실행"·"출력 경로 검사", R24). <c>--out</c> 기본값은 현재 폴더가 아니라 저장소 뿌리 기준이다.
/// </summary>
internal static class RepoPaths
{
    public const string SolutionFile = "DiabloDefenceRemastered.sln";

    /// <summary>현재 폴더에서 위로 올라가며 <c>DiabloDefenceRemastered.sln</c> 이 있는 폴더. 없으면 null.</summary>
    public static string? FindRoot(string start)
    {
        for (var d = new DirectoryInfo(Path.GetFullPath(start)); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, SolutionFile))) return d.FullName;
        return null;
    }

    public static string AssetsDir(string root) => Path.Combine(root, "src", "DiabloDefenceRemastered", "Assets");
    public static string TableDir(string root) => Path.Combine(root, "tools", "DiabloAssetImporter", "Table");
    public static string DataDir(string root) => Path.Combine(root, "src", "DiabloDefenceRemastered", "Data");

    /// <summary>
    /// 출력 폴더가 git 작업 트리 안이면 <c>git check-ignore -q &lt;폴더&gt;/x.png</c> 로 무시되는 곳인지 본다. 무시되지 않으면 거절 문구(종료 코드 2).
    /// git 을 찾을 수 없으면 작업 트리(sln 이 있는 뿌리) 안은 <c>src\DiabloDefenceRemastered\Assets\</c> 아래만 허용한다. 문제가 없으면 null.
    /// </summary>
    public static string? CheckOutput(string outDir)
    {
        var full = Path.GetFullPath(outDir);
        var root = FindRoot(Directory.Exists(full) ? full : Path.GetDirectoryName(full) ?? full);
        var git = TryGitCheckIgnore(full, out var inTree);
        if (git is { } ignored)
        {
            if (!inTree) return null;
            return ignored ? null : $"출력 폴더 {full} 는 git 이 추적하는 곳이다 — 원작 자원을 저장소에 섞지 않는다(R24). 저장소 밖이나 src\\DiabloDefenceRemastered\\Assets 를 준다";
        }
        if (root is null) return null;
        var assets = Path.GetFullPath(AssetsDir(root)) + Path.DirectorySeparatorChar;
        return (full + Path.DirectorySeparatorChar).StartsWith(assets, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"git 을 찾을 수 없어 저장소 안 출력은 src\\DiabloDefenceRemastered\\Assets 아래만 허용한다({full})";
    }

    /// <summary>null = git 을 못 씀. <paramref name="inTree"/> = 작업 트리 안인가.</summary>
    private static bool? TryGitCheckIgnore(string dir, out bool inTree)
    {
        inTree = false;
        var probeDir = dir;
        while (!Directory.Exists(probeDir))
        {
            var parent = Path.GetDirectoryName(probeDir);
            if (parent is null || parent == probeDir) return null;
            probeDir = parent;
        }
        try
        {
            var top = Run(probeDir, "rev-parse", "--show-toplevel");
            if (top.Code != 0) { inTree = false; return false; }   // 작업 트리 밖
            inTree = true;
            var probe = Path.Combine(dir, "x.png");
            var r = Run(probeDir, "check-ignore", "-q", probe);
            return r.Code == 0;
        }
        catch (System.ComponentModel.Win32Exception) { return null; }
    }

    private static (int Code, string Out) Run(string cwd, params string[] args)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = cwd, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi) ?? throw new System.ComponentModel.Win32Exception("git");
        var o = p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, o.Trim());
    }
}
