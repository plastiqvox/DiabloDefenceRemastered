using DiabloAssetImporter.Bake;
using DiabloAssetImporter.Upscale;

namespace DiabloAssetImporter.Pipeline;

/// <summary>만든 파일 하나(Assets 뿌리 기준 경로, 바이트, 그림 배율).</summary>
internal sealed record OutFile(string Path, byte[] Bytes, int Scale = 1);

/// <summary>작업 하나의 결과: 파일들 + (소리면) 색인 줄.</summary>
internal sealed record JobOutput(IReadOnlyList<OutFile> Files, IReadOnlyList<SoundInfo> Sounds)
{
    public static JobOutput Of(IEnumerable<OutFile> files) => new([.. files], []);
}

/// <summary>
/// 가져오기 작업 하나(1.4 "결정론" — 작업 목록은 이름 서수 정렬, 작업 하나는 한 스레드). 원본 읽기는 주 스레드가 <see cref="Sources"/> 를 차례로 읽어 넘긴다.
/// <see cref="Spec"/> 는 캐시 키의 작업 명세 JSON.
/// </summary>
internal sealed record ImportJob(
    string Name,
    string Kind,
    ContentBundle? Bundle,
    IReadOnlyList<ContentBundle> Prefer,
    IReadOnlyList<string> Sources,
    string Spec,
    bool UpscaleTarget,
    Func<SourceBag, IPlaneUpscaler?, JobOutput> Bake);
