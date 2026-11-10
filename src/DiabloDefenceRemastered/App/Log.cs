using System.Globalization;
using System.Text;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// 앱 로그 (설계서 9절, WP7). 한 줄씩 파일에 바로 쓴다(감시 스레드·예외 처리에서도 남게 — 잠금 + 자동 비움).
/// 판정용 고정 문장(<c>SCENE</c>·<c>ASSETS</c>·<c>RESULT</c>·<c>ROOM</c>·<c>AUDIO</c>·<c>FOG</c>·<c>EXIT</c> …)은 앞머리 없이 그대로, 경고·오류는 <c>WARN </c>·<c>ERROR </c> 앞머리.
/// 파일 기본 위치: 개발 옵션 실행이면 현재 폴더 <c>logs\</c>, 아니면 저장 폴더 <c>logs\</c>(<see cref="DefaultPath"/>).
/// </summary>
public sealed class Log : IAppLog, IDisposable
{
    private readonly object _gate = new();
    private readonly StreamWriter? _writer;
    private bool _exitWritten;

    public Log(string? path)
    {
        Path = path;
        if (path is null) return;
        var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false)) { AutoFlush = true };
    }

    public string? Path { get; }

    /// <summary>로그 파일 기본 경로(<c>run-yyyyMMdd-HHmmss.log</c>).</summary>
    public static string DefaultPath(bool devRun, string saveDir)
    {
        var dir = devRun ? System.IO.Path.Combine(Environment.CurrentDirectory, "logs") : System.IO.Path.Combine(saveDir, "logs");
        return System.IO.Path.Combine(dir, $"run-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.log");
    }

    public void Info(string line) => Write(line);
    public void Warn(string line) => Write("WARN " + line);
    public void Error(string line) => Write("ERROR " + line);

    /// <summary>끝 줄 <c>EXIT code=…</c>(한 번만).</summary>
    public void Exit(int code)
    {
        lock (_gate)
        {
            if (_exitWritten) return;
            _exitWritten = true;
        }
        Write(string.Create(CultureInfo.InvariantCulture, $"EXIT code={code}"));
    }

    private void Write(string line)
    {
        lock (_gate)
        {
            _writer?.WriteLine(line);
            System.Diagnostics.Debug.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_gate) _writer?.Dispose();
    }
}
