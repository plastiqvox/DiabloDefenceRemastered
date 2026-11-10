namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>
/// 액트를 시작할 때 미리 읽을 소리 (설계서 7.9 <c>SoundBank</c> — 순수 로직, 소유 WP8). <c>TextureStore</c> 와 같은 "나올 수 있는 것" 계산:
/// 적 소리는 <c>StageReach.Enemies</c>(구간·이벤트·방 순번 표·소환·분열 — 6.14)가 내는 적의 <c>enemies.&lt;id&gt;</c> 와 그 그림 묶음의 <c>sprite_sets</c> 줄만,
/// 나머지(무기·투사체·이펙트·장판 반복·획득물·UI·문 보상·주인공)는 줄 전부다 — 몇 개 안 되고, 레벨업 후보·직업에 따라 달라 미리 알 수 없다.
/// 주인공은 직업 줄 전부(<c>PreloadStage</c> 가 직업을 받지 않는다 — 직업마다 두 소리라 작다).
/// </summary>
public static class AudioReach
{
    /// <summary>그 액트에서 날 수 있는 소리(ID 서수 순, 중복 없음). <paramref name="stage"/> null = 연결 표 전부.</summary>
    public static IReadOnlyList<SoundDef> Sounds(GameDefs defs, StageDef? stage)
    {
        var router = new SoundRouter(defs);
        var b = defs.SoundBindings;
        var found = new SortedDictionary<string, SoundDef>(StringComparer.Ordinal);

        void Add(DefRef<SoundDef>? r)
        {
            if (router.Resolve(r) is { } s) found.TryAdd(s.Id, s);
        }

        void AddUnit(UnitSoundsDef u) { Add(u.Attack); Add(u.Hit); Add(u.Death); Add(u.Special); }
        void AddEnemy(EnemySoundsDef e) { Add(e.Attack); Add(e.Hit); Add(e.Death); Add(e.Special); Add(e.Voice); }

        foreach (var w in b.Weapons.Values) Add(w.Fire);
        foreach (var p in b.Projectiles.Values) Add(p.Fire);
        foreach (var e in b.Effects.Values) Add(e);
        foreach (var h in b.HazardLoops.Values) Add(h);
        foreach (var p in b.Pickups.Values) { Add(p.Collect); Add(p.Drop); }
        foreach (var u in b.Ui.Values) Add(u);
        foreach (var r in b.Rewards.Values) { Add(r.Collect); Add(r.Open); }
        foreach (var h in b.Hero.Values) { Add(h.Hurt); Add(h.Death); }

        if (stage is null)
        {
            foreach (var u in b.SpriteSets.Values) AddUnit(u);
            foreach (var e in b.Enemies.Values) AddEnemy(e);
        }
        else
        {
            foreach (var enemy in StageReach.Enemies(stage, defs))
            {
                if (b.Enemies.TryGetValue(enemy.Id, out var own)) AddEnemy(own);
                if (b.SpriteSets.TryGetValue(enemy.Sprites.Id, out var set)) AddUnit(set);
            }
        }

        return [.. found.Values];
    }

    /// <summary>소리들이 쓰는 파일(서수 순, 중복 없음) — 소리 켬에서 <c>SoundBank</c> 가 읽을 목록.</summary>
    public static IReadOnlyList<string> Files(IEnumerable<SoundDef> sounds)
    {
        var files = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var s in sounds)
            foreach (var f in s.Files) files.Add(f);
        return [.. files];
    }
}
