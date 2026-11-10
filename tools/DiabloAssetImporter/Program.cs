using System.Text;
using DiabloAssetImporter.Cli;
using DiabloAssetImporter.Commands;
using DiabloAssetImporter.Pipeline;

namespace DiabloAssetImporter;

/// <summary>
/// 진입점 (소유 WP0, 설계서 1.4): <c>import</c>·<c>verify</c>·<c>probe</c>·<c>contact</c>·<c>audit</c>·<c>upscaler-check</c>.
/// 종료 코드 0 성공, 1 처리 안 된 예외, 2 입력(원본·옵션), 3 원본에 없는 파일, 4 대조 실패, 6 확대를 못 해 원본 픽셀로(성공).
/// 창을 띄우지 않고 입력을 기다리지 않는다.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            return Run(args, Directory.GetCurrentDirectory(), Console.Out);
        }
        catch (UsageException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodes.Input;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"처리 안 된 예외: {ex}");
            return ExitCodes.Unhandled;
        }
    }

    /// <summary>시험이 현재 폴더·출력을 바꿔 부르는 진입점.</summary>
    internal static int Run(IReadOnlyList<string> args, string cwd, TextWriter output)
    {
        var o = Options.Parse(args);
        return o.Command switch
        {
            "import" => ImportCommand.Run(o, cwd, output),
            "verify" => VerifyCommand.Run(o, cwd, output),
            "probe" => ProbeCommand.Run(o, cwd, output),
            "contact" => ContactCommand.Run(o, cwd, output),
            "audit" => AuditCommand.Run(o, cwd, output),
            "upscaler-check" => UpscalerCheckCommand.Run(o, cwd, output),
            "" => throw new UsageException("명령을 준다: import | verify | probe | contact | audit | upscaler-check (설계서 1.4)"),
            _ => throw new UsageException($"모르는 명령 '{o.Command}' — import | verify | probe | contact | audit | upscaler-check"),
        };
    }
}
