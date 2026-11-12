namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>
/// 신호·표시 이벤트 → 소리 요청 (설계서 7.9 표, 6.15 연결 표 — 순수 로직, 소유 WP8). 게임 <c>AudioSystem</c> 이 프레임 끝에 부르고,
/// 같은 규칙을 시험이 그래픽 없이 확인한다. 시뮬레이션은 이 형을 모른다(<c>Sim/</c> 은 <c>Core.Audio</c> 를 참조하지 않음).
/// <para>
/// 연결 키: 무기 <c>weapons.&lt;id&gt;.fire</c>, 적 <c>enemies.&lt;id&gt;</c> → 없으면 <c>sprite_sets.&lt;그림 묶음&gt;</c>, 적 투사체 <c>projectiles.&lt;id&gt;.fire</c>,
/// 주인공 <c>hero.&lt;World.HeroClass.Id&gt;</c>, 획득물 <c>pickups.&lt;id&gt;</c>, 문 보상 <c>rewards.&lt;id&gt;.collect</c>, 보스 대사 <c>enemies.&lt;id&gt;.voice</c>,
/// 정예 무리 <c>ui.elite_pack</c>, 문 열림 <c>ui.door_open</c>, 방 정리 <c>ui.room_clear</c>, 물약 마심 <c>ui.potion_drink</c>(v0.9.1 <see cref="CueKind.PotionDrink"/> — v0.8 의 <c>belt_potion</c> 을 바꿈),
/// v0.9 의 새 신호(wp4 — WP8 단계 2): 못 마심 <c>ui.potion_denied</c>, 회복 문 <c>ui.heal_on_enter</c>, 상인 인사 <c>rewards.&lt;그 상인 보상&gt;.open</c>, 사기 <c>ui.shop_buy</c> + 장비면 끼우기 <c>ui.equip_&lt;칸&gt;</c>,
/// 못 삼 <c>ui.shop_denied</c>, 상자 → 책 <c>ui.chest_drop</c>(그 책의 <c>PickupDropped</c> 는 내지 않음 — 둘이 겹치지 않게),
/// 이펙트 <c>effects.&lt;id&gt;</c>, 시체 → 그 적의 <c>death</c>. 연결이 없는 신호는 소리 없음(화살 맞음처럼 — 기획 12.7).
/// </para>
/// <para>
/// 보스·중간 보스(정의 <c>rank</c>)가 낸 몬스터 소리는 분류를 <c>boss</c> 로 올린다(화면 밖이어도 냄). 몬스터 맞음·죽음의 따로 센 제한은 일반 적에만 —
/// 보스 죽음이 몬스터 죽음 4자리에 밀려 사라지지 않게 한다(보스 맞음은 같은 소리 간격만).
/// </para>
/// </summary>
public sealed class SoundRouter(GameDefs defs)
{
    /// <summary>보스·중간 보스가 낸 몬스터 소리의 분류.</summary>
    public const string BossCategory = "boss";

    /// <summary>보스 대사 분류 — 고르면 음악을 줄인다(<c>voice_duck</c>).</summary>
    public const string BossVoiceCategory = "boss_voice";

    /// <summary><see cref="TryUi"/> 의 보상 소리 키 머리(<c>rewards.&lt;보상 ID&gt;.open</c> / <c>.collect</c>).</summary>
    public const string RewardKeyPrefix = "rewards.";

    private SoundBindingsDef Bindings => defs.SoundBindings;

    public GameDefs Defs => defs;

    /// <summary>
    /// 버퍼 하나의 소리를 요청으로 바꿔 <paramref name="mixer"/> 에 넣는다. 순서: 신호 → 이펙트 → 시체(같은 분류·같은 틱 안의 들어온 순서).
    /// <paramref name="world"/> = 이 버퍼를 만든 월드(직업·투사체 쏜 적의 등급을 본다). 버퍼는 비우지 않는다(부르는 쪽이 Clear).
    /// </summary>
    public void Route(PresentationBuffer buffer, World world, SoundMixer mixer)
    {
        foreach (ref readonly var cue in buffer.Cues.AsSpan())
        {
            if (TryCue(cue, world, out var r)) mixer.Request(r);
            if (TryEquip(cue, out var equip)) mixer.Request(equip);   // 상인에게 산 장비: 금화 소리에 더해 끼우는 소리(기획 12.8)
        }
        foreach (ref readonly var fx in buffer.Effects.AsSpan())
            if (TryEffect(fx, out var r)) mixer.Request(r);
        foreach (ref readonly var corpse in buffer.Corpses.AsSpan())
            if (TryCorpse(corpse, out var r)) mixer.Request(r);
    }

    /// <summary>신호 하나 → 요청. 연결이 없거나 대상 형이 맞지 않으면 거짓.</summary>
    public bool TryCue(in CueEvent cue, World? world, out SoundRequest request)
    {
        request = default;
        var b = Bindings;
        SoundDef? sound = null;
        string? category = null;
        var limit = SoundLimit.None;
        Vector2? pos = cue.Pos;

        switch (cue.Kind)
        {
            case CueKind.WeaponFired:
                if (cue.Subject is WeaponDef w && b.Weapons.TryGetValue(w.Id, out var wf)) sound = Resolve(wf.Fire);
                break;
            case CueKind.EnemyAttack:
            case CueKind.EnemySpecial:
            case CueKind.EnemyHit:
                if (cue.Subject is EnemyDef e)
                {
                    var role = cue.Kind switch
                    {
                        CueKind.EnemyAttack => UnitRole.Attack,
                        CueKind.EnemySpecial => UnitRole.Special,
                        _ => UnitRole.Hit,
                    };
                    sound = EnemySound(e, role);
                    if (e.Rank != DefRank.Normal) category = BossCategory;
                    else if (role == UnitRole.Hit) limit = SoundLimit.MonsterHit;
                }
                break;
            case CueKind.EnemyShot:
                if (cue.Subject is ProjectileDef p && b.Projectiles.TryGetValue(p.Id, out var pf))
                {
                    sound = Resolve(pf.Fire);
                    if (world is not null && world.Enemies.Get(cue.Source) is { } shooter && shooter.Def is { Rank: not DefRank.Normal }) category = BossCategory;
                }
                break;
            case CueKind.HeroHurt:
            case CueKind.HeroDied:
                if (world?.HeroClass is { } hc && b.Hero.TryGetValue(hc.Id, out var hs))
                    sound = Resolve(cue.Kind == CueKind.HeroHurt ? hs.Hurt : hs.Death);
                break;
            case CueKind.PickupCollected:
            case CueKind.PickupDropped:
                // 보물 상자를 열면 책(보상 Drop 획득물)의 PickupDropped 와 ChestDrop 이 같은 틱에 둘 다 난다(WP3 단계 2) — 책 떨어짐은 ChestDrop(ui.chest_drop) 하나로 낸다
                if (cue.Kind == CueKind.PickupDropped && cue.Subject is PickupDef dropped && IsRewardDrop(dropped)) break;
                if (cue.Subject is PickupDef pk && b.Pickups.TryGetValue(pk.Id, out var ps))
                    sound = Resolve(cue.Kind == CueKind.PickupCollected ? ps.Collect : ps.Drop);
                break;
            case CueKind.BossArrived:
            case CueKind.BossPhase:
                if (cue.Subject is EnemyDef boss && b.Enemies.TryGetValue(boss.Id, out var bs)) sound = Resolve(bs.Voice);
                break;
            case CueKind.RewardCollected:
                if (cue.Subject is RewardDef rw && b.Rewards.TryGetValue(rw.Id, out var rs)) sound = Resolve(rs.Collect);
                break;
            case CueKind.ElitePack:
                sound = Ui(UiKeys.ElitePack);
                pos = null;
                break;
            case CueKind.RoomCleared:
                sound = Ui(UiKeys.RoomClear);
                pos = null;
                break;
            case CueKind.DoorsOpened:
                sound = Ui(UiKeys.DoorOpen);
                pos = null;
                break;
            case CueKind.PotionDrink:
                sound = Ui(UiKeys.PotionDrink);
                pos = null;
                break;
            // v0.9 새 신호(wp4 — WP8 단계 2): 못 함·회복 문·상인·상자 → 책. 대상 형은 WP3 약속(ShopOpen·ShopDenied = MerchantDef, ShopBuy = ItemBaseDef 또는 페핀 MerchantDef, ChestDrop = RewardDef)
            case CueKind.PotionDenied:
                sound = Ui(UiKeys.PotionDenied);
                pos = null;
                break;
            case CueKind.HealOnEnter:
                sound = Ui(UiKeys.HealOnEnter);
                pos = null;
                break;
            case CueKind.ShopOpen:
                // 인사 대사 = 그 상인을 가리키는 문 보상의 rewards.<보상 ID>.open(상인 창이 보상 창이다 — 기획 11.4 표의 "창 열림")
                if (cue.Subject is MerchantDef m && MerchantReward(m) is { } mr && b.Rewards.TryGetValue(mr.Id, out var ms)) sound = Resolve(ms.Open);
                pos = null;
                break;
            case CueKind.ShopBuy:
                if (cue.Subject is ItemBaseDef or MerchantDef) sound = Ui(UiKeys.ShopBuy);
                pos = null;
                break;
            case CueKind.ShopDenied:
                if (cue.Subject is MerchantDef) sound = Ui(UiKeys.ShopDenied);
                pos = null;
                break;
            case CueKind.ChestDrop:
                if (cue.Subject is RewardDef) sound = Ui(UiKeys.ChestDrop);
                break;
            // 보스 묶음 B(6.20 ⑲): 순간이동 도착 = ui.blink(원작 holybolt 빌림), 자리 없음(UI 소리 — 주인공 소리라 화면 어디서나 같은 크기). BlinkDenied 는 연결하지 않는다(무음 — HUD 만)
            case CueKind.Blink:
                sound = Ui(UiKeys.Blink);
                pos = null;
                break;
        }

        if (sound is null) return false;
        request = new SoundRequest(sound, cue.Tick, pos, category, limit);
        return true;
    }

    /// <summary>
    /// 장비 끼우기(v0.9 — 기획 11.6·12.8): <c>ShopBuy</c> 의 대상이 <c>ItemBaseDef</c> 면 칸 종류의 <c>ui.equip_&lt;칸&gt;</c>(<see cref="UiKeys.Equip"/>), 그 키가 없으면 <c>ui.equip</c>.
    /// 페핀 카드(<c>MerchantDef</c>)는 끼우는 것이 없어 거짓.
    /// </summary>
    public bool TryEquip(in CueEvent cue, out SoundRequest request)
    {
        request = default;
        if (cue.Kind != CueKind.ShopBuy || cue.Subject is not ItemBaseDef item) return false;
        var sound = Ui(UiKeys.Equip(item.Kind, item.Weight)) ?? Ui(UiKeys.EquipOther);
        if (sound is null) return false;
        request = new SoundRequest(sound, cue.Tick, null);
        return true;
    }

    private HashSet<string>? _rewardDrops;

    /// <summary>보상의 <c>drop</c>(보물 상자 → 책)이 가리키는 획득물인가(정의 표에서 한 번 모음).</summary>
    private bool IsRewardDrop(PickupDef pickup)
    {
        _rewardDrops ??= [.. defs.Rewards.All.Where(r => r.Drop is not null).Select(r => r.Drop!.Id)];
        return _rewardDrops.Contains(pickup.Id);
    }

    /// <summary>그 상인을 가리키는 문 보상(표 순서의 첫 것). 없으면 null.</summary>
    private RewardDef? MerchantReward(MerchantDef merchant)
    {
        foreach (var r in defs.Rewards.All)
            if (r.Merchant is { } rm && string.Equals(rm.Id, merchant.Id, StringComparison.Ordinal)) return r;
        return null;
    }

    /// <summary>이펙트 → <c>effects.&lt;id&gt;</c>(무기 맞음·폭발·웅덩이 — 분류는 소리 정의).</summary>
    public bool TryEffect(in FxEvent fx, out SoundRequest request)
    {
        request = default;
        if (fx.Def is null || !Bindings.Effects.TryGetValue(fx.Def.Id, out var r) || Resolve(r) is not { } sound) return false;
        request = new SoundRequest(sound, fx.Tick, fx.Pos);
        return true;
    }

    /// <summary>시체 → 그 적의 <c>death</c>(일반 적은 몬스터 죽음 제한 — 같은 종 = 적 ID).</summary>
    public bool TryCorpse(in CorpseSpawned corpse, out SoundRequest request)
    {
        request = default;
        if (corpse.Enemy is not { } e || EnemySound(e, UnitRole.Death) is not { } sound) return false;
        request = e.Rank != DefRank.Normal
            ? new SoundRequest(sound, corpse.Tick, corpse.Pos, BossCategory)
            : new SoundRequest(sound, corpse.Tick, corpse.Pos, null, SoundLimit.MonsterDeath, e.Id);
        return true;
    }

    /// <summary>
    /// 씬이 부르는 UI 소리(<c>IAudioServices.PlayUi</c>): <c>ui.&lt;키&gt;</c>. 문 보상 창 소리는 연결 표 경로 그대로
    /// <c>rewards.&lt;보상 ID&gt;.open</c>(또는 <c>.collect</c>) — 계약 <c>PlayUi(string)</c> 하나로 받는다(WP8 구현 기록).
    /// </summary>
    public bool TryUi(string key, out SoundRequest request)
    {
        request = default;
        SoundDef? sound;
        if (key.StartsWith(RewardKeyPrefix, StringComparison.Ordinal))
        {
            var rest = key.AsSpan(RewardKeyPrefix.Length);
            var dot = rest.LastIndexOf('.');
            if (dot <= 0) return false;
            var id = rest[..dot].ToString();
            var which = rest[(dot + 1)..];
            if (!Bindings.Rewards.TryGetValue(id, out var rs)) return false;
            sound = which.SequenceEqual("open") ? Resolve(rs.Open) : which.SequenceEqual("collect") ? Resolve(rs.Collect) : null;
        }
        else sound = Ui(key);

        if (sound is null) return false;
        request = new SoundRequest(sound, 0);
        return true;
    }

    /// <summary>음악 키(<c>title</c> 또는 스테이지 ID) → 곡. 없으면 null.</summary>
    public MusicDef? Music(string key) =>
        Bindings.Music.TryGetValue(key, out var r) ? r.IsBound ? r.Def : defs.Music.TryGet(r.Id, out var m) ? m : null : null;

    /// <summary>
    /// 묶음을 보고 실제로 틀 곡(6.15·6.17): <c>requires</c> 가 모두 있으면 그 곡, 아니면 <c>fallback</c>(없으면 null — 13.5-20 이 막음).
    /// <paramref name="has"/> = 묶음이 있나(게임은 <c>ContentAvailability.Has</c>).
    /// </summary>
    public MusicDef? Playable(MusicDef music, Func<ContentBundle, bool> has)
    {
        foreach (var b in music.Requires)
            if (!has(b))
                return music.Fallback is { } f ? f.IsBound ? f.Def : defs.Music.TryGet(f.Id, out var m) ? m : null : null;
        return music;
    }

    /// <summary>적 소리: <c>enemies.&lt;id&gt;</c> 의 그 칸 → 없으면 그 적 그림 묶음의 <c>sprite_sets.&lt;묶음&gt;</c>.</summary>
    public SoundDef? EnemySound(EnemyDef enemy, UnitRole role)
    {
        var b = Bindings;
        if (b.Enemies.TryGetValue(enemy.Id, out var es) && Resolve(Pick(es, role)) is { } own) return own;
        return b.SpriteSets.TryGetValue(enemy.Sprites.Id, out var ss) ? Resolve(Pick(ss, role)) : null;
    }

    public SoundDef? Ui(string key) => Bindings.Ui.TryGetValue(key, out var r) ? Resolve(r) : null;

    /// <summary>연결된 참조는 그대로, 아직 연결 안 된 참조(시험이 형으로만 읽은 JSON)는 ID 로 찾는다.</summary>
    public SoundDef? Resolve(DefRef<SoundDef>? r) =>
        r is null ? null : r.IsBound ? r.Def : defs.Sounds.TryGet(r.Id, out var s) ? s : null;

    private static DefRef<SoundDef>? Pick(EnemySoundsDef s, UnitRole role) => role switch
    {
        UnitRole.Attack => s.Attack,
        UnitRole.Hit => s.Hit,
        UnitRole.Death => s.Death,
        _ => s.Special,
    };

    private static DefRef<SoundDef>? Pick(UnitSoundsDef s, UnitRole role) => role switch
    {
        UnitRole.Attack => s.Attack,
        UnitRole.Hit => s.Hit,
        UnitRole.Death => s.Death,
        _ => s.Special,
    };
}

/// <summary>몬스터 소리 칸(원작 <c>a</c>·<c>h</c>·<c>d</c>·<c>s</c>).</summary>
public enum UnitRole : byte { Attack, Hit, Death, Special }

/// <summary>연결 표 <c>ui</c> 키(6.15). 씬이 부르는 것과 신호가 부르는 것.</summary>
public static class UiKeys
{
    public const string MenuMove = "menu_move";
    public const string MenuSelect = "menu_select";
    public const string LevelUp = "level_up";
    public const string ChestOpen = "chest_open";
    public const string ElitePack = "elite_pack";
    public const string DoorOpen = "door_open";
    public const string RoomClear = "room_clear";
    /// <summary>물약 하나를 마심(v0.9.1 — v0.8 의 <c>belt_potion</c>, 저절로 마심을 바꿈).</summary>
    public const string PotionDrink = "potion_drink";

    /// <summary>물약을 못 마심(v0.9.1 <c>PotionDenied</c> — 창·회복 중·물약 0·체력 가득, 기획 3절 "못 함" 소리).</summary>
    public const string PotionDenied = "potion_denied";
    /// <summary>회복 문으로 들어온 방의 첫 틱(v0.9 <c>HealOnEnter</c>).</summary>
    public const string HealOnEnter = "heal_on_enter";
    /// <summary>상인 카드를 삼(v0.9 <c>ShopBuy</c> — 원작 금화 소리, 기획 12.8).</summary>
    public const string ShopBuy = "shop_buy";
    /// <summary>금화가 모자라거나 살 수 없음(v0.9 <c>ShopDenied</c>).</summary>
    public const string ShopDenied = "shop_denied";
    /// <summary>보물 상자가 열려 책이 떨어짐(v0.9.1 <c>ChestDrop</c>).</summary>
    public const string ChestDrop = "chest_drop";
    /// <summary>순간이동 도착(보스 묶음 B <c>CueKind.Blink</c> — 원작 순간이동 소리는 다음 가져오기 때, 지금은 <c>holybolt</c> 빌림).</summary>
    public const string Blink = "blink";
    /// <summary>장비 끼우기(칸 종류를 모르거나 그 칸 키가 없을 때).</summary>
    public const string EquipOther = "equip";
    public const string EquipHead = "equip_head";
    public const string EquipBody = "equip_body";
    /// <summary>판금 갑옷(무게 heavy — 원작 갑옷 소리가 경·판금 둘이다).</summary>
    public const string EquipBodyHeavy = "equip_body_heavy";
    public const string EquipShield = "equip_shield";
    public const string EquipStaff = "equip_staff";
    public const string EquipRing = "equip_ring";

    /// <summary>장비 칸 종류(·무게)의 끼우기 키.</summary>
    public static string Equip(EquipKind kind, ArmorWeight weight) => kind switch
    {
        EquipKind.Head => EquipHead,
        EquipKind.Body => weight == ArmorWeight.Heavy ? EquipBodyHeavy : EquipBody,
        EquipKind.Shield => EquipShield,
        EquipKind.Staff => EquipStaff,
        EquipKind.Ring => EquipRing,
        _ => EquipOther,
    };

    /// <summary>알려진 키(검사기가 모르는 키를 경고).</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        MenuMove, MenuSelect, LevelUp, ChestOpen, ElitePack, DoorOpen, RoomClear, PotionDrink,
        PotionDenied, HealOnEnter, ShopBuy, ShopDenied, ChestDrop, Blink, EquipOther, EquipHead, EquipBody, EquipBodyHeavy, EquipShield, EquipStaff, EquipRing,
    ];
}
