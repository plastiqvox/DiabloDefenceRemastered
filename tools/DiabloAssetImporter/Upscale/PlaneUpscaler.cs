using System.Collections.Concurrent;
using System.Security.Cryptography;
using DiabloAssetImporter.Imaging;

namespace DiabloAssetImporter.Upscale;

/// <summary>
/// 굽기 코드가 부르는 판 확대(④). 세 단계(1.4 "작업 나누기")를 같은 굽기 코드로 돌리려고 둘로 나눈다:
/// (1) <see cref="CollectingPlanes"/> 는 판을 모으고 최근접 4배를 돌려준다(그 결과는 버림), (3) <see cref="ResolvedPlanes"/> 는 (2) 의 결과를 판 해시로 찾는다.
/// </summary>
internal interface IPlaneUpscaler
{
    /// <summary>판 하나(불투명 RGB)를 모델 배율(4)로. <paramref name="name"/> 은 같은 작업 안 판 이름(묶음 순서).</summary>
    RgbaImage Up4(string name, RgbaImage plane);
}

/// <summary>판 = (이름, 그림, 해시). 해시 = 크기 + 픽셀 바이트의 SHA-256 — 같은 그림 판은 한 번만 확대한다.</summary>
internal sealed record UpscalePlane(string Job, string Name, RgbaImage Image, string Hash)
{
    public static string HashOf(RgbaImage img)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> head = stackalloc byte[8];
        BitConverter.TryWriteBytes(head, img.Width);
        BitConverter.TryWriteBytes(head[4..], img.Height);
        sha.AppendData(head);
        sha.AppendData(img.Pixels);
        return Convert.ToHexStringLower(sha.GetHashAndReset());
    }
}

internal sealed class CollectingPlanes(string job) : IPlaneUpscaler
{
    public List<UpscalePlane> Planes { get; } = [];

    public RgbaImage Up4(string name, RgbaImage plane)
    {
        Planes.Add(new UpscalePlane(job, name, plane, UpscalePlane.HashOf(plane)));
        return UpscaleMath.Nearest(plane, UpscaleMath.ModelScale);
    }
}

internal sealed class ResolvedPlanes(IReadOnlyDictionary<string, RgbaImage> byHash) : IPlaneUpscaler
{
    public RgbaImage Up4(string name, RgbaImage plane)
    {
        var h = UpscalePlane.HashOf(plane);
        return byHash.TryGetValue(h, out var r) ? r : throw new InvalidOperationException($"확대 결과가 없는 판 {name}({h[..12]})");
    }
}

/// <summary>시험용: 픽셀을 4배로 늘리기만 하는 판 확대(<c>IUpscaler</c> 가짜와 같은 결과).</summary>
internal sealed class NearestPlanes : IPlaneUpscaler
{
    public ConcurrentBag<string> Calls { get; } = [];

    public RgbaImage Up4(string name, RgbaImage plane)
    {
        Calls.Add(name);
        return UpscaleMath.Nearest(plane, UpscaleMath.ModelScale);
    }
}
