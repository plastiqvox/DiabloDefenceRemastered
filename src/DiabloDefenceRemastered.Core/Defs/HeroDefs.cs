namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 직업 <c>hero_classes[]</c> (설계서 6.16, 사용자 결정 K, 계약 WP1d). 직업마다 파일 하나(<c>Data/heroes/&lt;id&gt;.json</c>).
/// 주인공을 만드는 코드는 <c>World.HeroClass</c> 만 읽는다 — 직업을 더할 때 늘어나는 것은 정의 파일·그림 묶음·가져오기 표 줄·소리 연결 줄뿐이다.
/// 능력치 = <c>rules.hero.stats</c> 를 <see cref="Stats"/> 가 키별로 덮어쓰고, 그 위에 특성(<see cref="Traits"/>)·고른 패시브를 더한다(기획 4.1).
/// 소리는 여기 두지 않는다 — <c>sound_bindings.hero.&lt;직업 ID&gt;</c>(6.15).
/// </summary>
public sealed record HeroClassDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    /// <summary>직업 고르기 카드(직업 단계). 없으면 서기 그림 첫 프레임.</summary>
    public IconDef? Icon { get; init; }
    /// <summary>그림 묶음: idle·walk·cast_*·die(+ 근접 시작 무기가 있으면 attack) — 13.5-21.</summary>
    public required DefRef<SpriteSetDef> Sprites { get; init; }
    /// <summary>몸 반경(px). 13.5-21: 14 이하(13.5-5 길 검사의 주인공 등급).</summary>
    public float Radius { get; init; } = 14f;
    /// <summary><c>rules.hero.stats</c> 덮어쓰기(키별).</summary>
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();
    public required IReadOnlyList<DefRef<WeaponDef>> StartWeapons { get; init; }
    /// <summary>특성 = 레벨 없고 슬롯을 안 쓰는 패시브(<c>trait: true</c>, v0.6.1 — 직업 형을 고치지 않고 특성 둘·<c>on_acquire</c>·직업 전용 패시브를 쓴다).</summary>
    public IReadOnlyList<DefRef<PassiveDef>> Traits { get; init; } = [];
    /// <summary>
    /// 겉모습 아홉 벌(v0.9.1 V10, 6.19 "주인공 겉모습"): 키 = 무게 글자(<c>l</c>·<c>m</c>·<c>h</c>) + 든 것 글자(<c>n</c>·<c>u</c>·<c>t</c>) → 그림 묶음.
    /// 고르기는 <c>HeroLook.Key</c>(순수 함수 — 시뮬레이션은 키만 안다), 그리기(WP5)가 프레임마다 키로 묶음을 고른다. 비면 <see cref="Sprites"/> 하나.
    /// 키는 아홉 글자 조합만(다른 키는 로딩 오류).
    /// </summary>
    public IReadOnlyDictionary<string, DefRef<SpriteSetDef>> Looks { get; init; } = new Dictionary<string, DefRef<SpriteSetDef>>();
    /// <summary>
    /// 고른 키의 묶음이 <see cref="Looks"/> 에 없거나 꺼졌을 때(<c>ContentAvailability</c>·가져오기에서 빠짐) 대신 쓰는 키(v0.9.1 — 기본 <c>lt</c>, 기획 34절 "못 가져온 벌은 <c>slt</c>").
    /// </summary>
    public string Look { get; init; } = "lt";
    /// <summary>이 직업이 쓰는 자원 묶음(6.17). 묶음이 없는 PC 에서는 직업이 꺼진다. 기본 직업(<c>rules.hero.class</c>)은 비어 있어야 한다(13.5-20).</summary>
    public IReadOnlyList<ContentBundle> Requires { get; init; } = [];
}
