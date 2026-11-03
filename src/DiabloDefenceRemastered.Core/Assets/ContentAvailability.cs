namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>
/// 이 PC 에 있는 자원 묶음 → 꺼진 스테이지·직업 (설계서 6.17, 사용자 결정 H, 계약·구현 WP1d).
/// 꺼짐 단위는 스테이지·직업이다: 적·무기·음악은 스테이지·직업을 거쳐서만 쓰이고 13.5-20 이 <c>requires</c> 를 위로 덮게 하므로
/// 스테이지·직업 자신의 <c>requires</c> 만 본다. 음악은 꺼지지 않고 <c>fallback</c> 으로 바뀐다.
/// 헬파이어 MPQ 가 없는 PC 는 그 묶음을 단 스테이지·직업만 빠지고 나머지(기본 내용)는 그대로 돈다 — 안내 화면을 띄우지 않는다.
/// <b>시뮬레이션(<c>Sim/</c>)은 이것을 쓰지 않는다</b>(10.1 소스 문자열 시험) — 같은 씨앗·스테이지·직업이면 묶음 유무와 상관없이 결과가 같다.
/// 쓰는 곳: 시작 검사 ③ "꺼짐"(WP7), 타이틀 한 줄(WP6 — v0.7: 스테이지 선택 화면 없음), 직업 고르기(WP9), 음악 대체(WP8), <c>--hero</c>·<c>--stage</c>·<c>--room</c> 판정(WP7).
/// 맵 그림의 묶음 귀속(13.5-10 묶음판)은 v0.7 부터 그 맵을 <c>room_maps</c> 에 둔 스테이지들이다(본문 WP7 — <c>DefAssetNames</c>).
/// </summary>
public sealed class ContentAvailability
{
    private readonly bool[] _has;
    private readonly ManifestSkippedBundle?[] _skipped;

    private ContentAvailability(bool[] has, ManifestSkippedBundle?[] skipped)
    {
        _has = has;
        _skipped = skipped;
    }

    /// <summary>모든 묶음이 있음(시험·<c>--validate-data</c>·자원 없이 도는 경로).</summary>
    public static ContentAvailability All { get; } = new(Filled(true), new ManifestSkippedBundle?[ContentBundles.All.Count]);

    /// <summary>manifest 의 <c>skipped_bundles</c> 에 없는 묶음 = 있음. 같은 묶음이 두 번 있으면 앞의 것을 이유로 쓴다.</summary>
    public static ContentAvailability From(AssetManifest manifest)
    {
        var has = Filled(true);
        var skipped = new ManifestSkippedBundle?[ContentBundles.All.Count];
        foreach (var s in manifest.SkippedBundles)
        {
            var i = (int)s.Bundle;
            if ((uint)i >= (uint)has.Length) continue;
            has[i] = false;
            skipped[i] ??= s;
        }
        return new ContentAvailability(has, skipped);
    }

    public bool Has(ContentBundle b) => (uint)b < (uint)_has.Length && _has[(int)b];

    /// <summary><paramref name="requires"/> 중 없는 묶음(중복 없이, 열거 순서). 빈 목록 = 켜짐.</summary>
    public IReadOnlyList<ContentBundle> Missing(IReadOnlyList<ContentBundle> requires)
    {
        List<ContentBundle>? missing = null;
        foreach (var b in ContentBundles.All)
            if (!Has(b) && requires.Contains(b)) (missing ??= []).Add(b);
        return missing is null ? [] : missing;
    }

    /// <summary>= <see cref="Missing"/>(s.Requires). 13.5-20 이 스테이지가 내는 적·무기의 묶음을 스테이지 <c>requires</c> 로 덮어 준다.</summary>
    public IReadOnlyList<ContentBundle> MissingFor(StageDef s) => Missing(s.Requires);

    public IReadOnlyList<ContentBundle> MissingFor(HeroClassDef c) => Missing(c.Requires);

    /// <summary>꺼진 스테이지·직업(정의 순서)과 그 묶음(꺼진 정의들이 요구하는데 없는 묶음, 열거 순서).</summary>
    public DisabledContent Disabled(GameDefs defs)
    {
        var stages = new List<StageDef>();
        var heroes = new List<HeroClassDef>();
        var bundles = new SortedSet<ContentBundle>();
        foreach (var s in defs.Stages.All)
        {
            var m = MissingFor(s);
            if (m.Count == 0) continue;
            stages.Add(s);
            bundles.UnionWith(m);
        }
        foreach (var c in defs.HeroClasses.All)
        {
            var m = MissingFor(c);
            if (m.Count == 0) continue;
            heroes.Add(c);
            bundles.UnionWith(m);
        }
        return new DisabledContent(stages, heroes, [.. bundles]);
    }

    /// <summary>건너뛴 이유(타이틀 한 줄의 앞말 — 없음 / 읽지 못함). 있는 묶음이거나 <see cref="All"/> 이면 null.</summary>
    public ManifestSkippedBundle? Skipped(ContentBundle b) => (uint)b < (uint)_skipped.Length ? _skipped[(int)b] : null;

    private static bool[] Filled(bool value)
    {
        var a = new bool[ContentBundles.All.Count];
        Array.Fill(a, value);
        return a;
    }
}

/// <summary>꺼진 스테이지·직업과 그 묶음(v0.6.1 — v0.6 의 <c>DisabledInDefs</c> 를 대신함).</summary>
public sealed record DisabledContent(IReadOnlyList<StageDef> Stages, IReadOnlyList<HeroClassDef> Heroes, IReadOnlyList<ContentBundle> Bundles)
{
    /// <summary>타이틀 한 줄·<c>ASSETS disabled=</c> 는 이것이 참일 때만(음악만 대체되면 거짓 — 로그 <c>AUDIO … fallback=</c> 만).</summary>
    public bool Any => Stages.Count > 0 || Heroes.Count > 0;
}
