namespace DiabloDefenceRemastered.Core.Sim.Contracts;

// 표시 이벤트(시뮬레이션 → 화면, 설계서 3.4·3.5·5.6). 모두 생긴 스테이지 틱(Tick)을 싣고,
// 그릴 때 나이 = 현재 스테이지 틱 − Tick 으로 계산한다(진행량을 누적하지 않는다).

/// <summary>
/// 규칙에 영향 없는 이펙트 한 번(폭발·맞음 불꽃·빛 기둥 …).
/// 크기: <paramref name="Radius"/> &gt; 0 이고 정의가 fit_radius 를 가지면 배율 = Radius / fit_radius, 아니면 정의 scale. 거기에 <paramref name="Scale"/> 을 곱한다.
/// </summary>
public readonly record struct FxEvent(int Tick, EffectDef Def, Vector2 Pos, float Scale, Rgb Tint, float Radius = 0f, Vector2 Dir = default);

/// <summary>피해 숫자. 같은 대상 0.1s 합치기는 게임 쪽.</summary>
public readonly record struct DamageNumber(int Tick, EntityId Target, Vector2 Pos, int Value, bool Crit, bool ToHero);

/// <summary>
/// 적 시체(죽음 애니메이션). 그 순간의 색조·알파·발광을 복사해 간다. Culled 는 내지 않는다.
/// <paramref name="Enemy"/> 는 죽음 소리 연결(<c>enemies.&lt;id&gt;</c> → 그림 묶음, 7.9), <paramref name="Variant"/> 는 시체 그림의 TRN 변종 페이지.
/// </summary>
public readonly record struct CorpseSpawned(int Tick, EnemyDef Enemy, SpriteSetDef Sprites, string Variant, Vector2 Pos, Vector2 Facing, Rgb Tint,
                                            float Scale, float Alpha, Rgb GlowColor, float GlowAlpha, DeathStyle Death);

/// <summary>화면 흔들림. 지금 남은 것 중 가장 센 것을 쓴다(7.2).</summary>
public readonly record struct ShakeEvent(int Tick, float Px, float Duration);

/// <summary>소리 신호 종류 (설계서 7.9). 내는 곳·연결 키는 7.9 표.</summary>
public enum CueKind : byte
{
    /// <summary>WP3 Weapons — 발동 한 번에 하나(여러 발이어도). Subject = WeaponDef.</summary>
    WeaponFired,
    /// <summary>WP4 EnemyThink — attack 클립을 시작할 때. Subject = EnemyDef.</summary>
    EnemyAttack,
    /// <summary>WP4 EnemyThink — special 클립을 시작할 때. Subject = EnemyDef.</summary>
    EnemySpecial,
    /// <summary>WP3 DamageResolve — 적에게 실제로 들어간 피해. Subject = EnemyDef.</summary>
    EnemyHit,
    /// <summary>WP4 shoot_projectile·projectile_spread. Subject = ProjectileDef.</summary>
    EnemyShot,
    /// <summary>WP3 DamageResolve — 피격 무적이 아닐 때 한 번. Subject 없음.</summary>
    HeroHurt,
    /// <summary>WP3 죽음 처리. Subject 없음.</summary>
    HeroDied,
    /// <summary>WP3 Pickups. Subject = PickupDef.</summary>
    PickupCollected,
    /// <summary>WP3 드롭 생성. Subject = PickupDef.</summary>
    PickupDropped,
    /// <summary>WP4 boss·miniboss 이벤트. Subject = EnemyDef.</summary>
    BossArrived,
    /// <summary>WP4 PhaseController(페이즈 voice: true). Subject = EnemyDef.</summary>
    BossPhase,
    /// <summary>WP4 정예 무리 등장. Subject 없음.</summary>
    ElitePack,
}

/// <summary>
/// 소리 신호 — "무엇이 일어났나" 만 싣는다(7.9). 시뮬레이션은 소리를 모르고, 게임 AudioSystem(WP8)이 연결 표로 소리를 고른다.
/// </summary>
/// <param name="Subject">신호의 대상 정의(무기·적·투사체·획득물). 없는 신호는 null.</param>
/// <param name="Source">낸 개체(보스·적 ID 등, 없으면 default).</param>
public readonly record struct CueEvent(int Tick, CueKind Kind, Vector2 Pos, IDef? Subject, EntityId Source = default);

// 알림 문구는 표시 이벤트가 아니다: HUD 문구의 출처는 World.Banner(BannerState) 하나다(WP4 가 쓰고 WP6 이 읽는다).

/// <summary>
/// 표시 이벤트 버퍼. <b>틱마다 비우지 않는다</b> — 한 프레임에 여러 틱이 돌아도 앞 틱의 이벤트가 남고,
/// 프레임 끝에 게임이 소비(FxSystem.Consume 등)한 뒤 <see cref="Clear"/> 한다.
/// 그래픽 없는 실행(시험·시나리오)은 <see cref="Enabled"/> = false 로 쌓지 않는다.
/// </summary>
public sealed class PresentationBuffer
{
    public bool Enabled { get; set; } = true;

    public EventList<FxEvent> Effects { get; } = new(64);
    public EventList<DamageNumber> DamageNumbers { get; } = new(64);
    public EventList<CorpseSpawned> Corpses { get; } = new(32);
    public EventList<ShakeEvent> Shakes { get; } = new(8);
    /// <summary>소리 신호(7.9). 프레임 끝 PlayScene: FxSystem.Consume → Audio.Consume → Clear.</summary>
    public EventList<CueEvent> Cues { get; } = new(64);

    public void Effect(in FxEvent e) { if (Enabled) Effects.Add(e); }
    public void Damage(in DamageNumber e) { if (Enabled) DamageNumbers.Add(e); }
    public void Corpse(in CorpseSpawned e) { if (Enabled) Corpses.Add(e); }
    public void Shake(in ShakeEvent e) { if (Enabled) Shakes.Add(e); }
    public void Cue(in CueEvent e) { if (Enabled) Cues.Add(e); }

    public void Clear()
    {
        Effects.Clear();
        DamageNumbers.Clear();
        Corpses.Clear();
        Shakes.Clear();
        Cues.Clear();
    }
}
