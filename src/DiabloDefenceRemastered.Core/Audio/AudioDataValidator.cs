using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>
/// 소리 데이터 검사기 (설계서 6.14 "소리" 행·13.5-21 의 소리 칸·"소리 없는 적" 경고, 소유 WP8). 로딩 7단계에 <see cref="AudioModule"/> 이 등록한다.
/// <list type="bullet">
/// <item>오류: <c>rules.audio</c> 없음(소리가 있는데)·분류 ID 중복·음량 음수, 소리 파일 0개·파일 이름이 <c>d1/sound/…</c> 가 아님·분류가 <c>rules.audio</c> 에 없음·
/// 값 범위(<c>volume</c>·<c>max_instances</c>·<c>min_interval</c>·<c>pitch_jitter</c>), 음악 파일 이름, 연결 표의 대상(무기·투사체·이펙트·그림 묶음·적·획득물·직업·보상·음악 키)과
/// 가리키는 소리·음악이 없음.</item>
/// <item>경고: 직업 ID 마다 <c>hero.&lt;id&gt;</c> 가 없음(13.5-21), 소리 연결이 없는 그림 묶음을 쓰는 적(소리 없는 적), 모르는 <c>ui</c> 키,
/// <c>hazard_loops</c> 소리가 <c>loop</c> 가 아님 / 한 번 소리 자리에 <c>loop</c> 소리.</item>
/// </list>
/// 파일 이름 ⊂ 가져오기 표가 만드는 이름(13.5-10)은 자원 시험(WP0)이, 묶음 덮기(음악 <c>fallback</c>, 13.5-20)는 로더(WP7)가 본다.
/// </summary>
public sealed class AudioDataValidator : IDataValidator
{
    public const string SoundsFile = "audio/sounds.json";
    public const string BindingsFile = "audio/bindings.json";
    public const string RulesFile = "rules/audio.json";

    /// <summary>소리 파일 이름 머리(가져오기 출력, 1.4).</summary>
    public const string SoundRoot = "d1/sound/";

    /// <summary>음악 키 중 스테이지 ID 가 아닌 것.</summary>
    public const string TitleMusicKey = "title";

    public string Name => "audio";

    public void Validate(GameDefs defs, ValidationContext ctx)
    {
        var rules = defs.Rules.Audio;
        var categories = new HashSet<string>(StringComparer.Ordinal);
        if (rules is null)
        {
            if (defs.Sounds.Count > 0 || defs.Music.Count > 0)
                ctx.Add(RulesFile, "$.rules", "rules.audio 가 없다(소리 정의가 있으면 필요하다)");
        }
        else ValidateRules(rules, categories, ctx);

        ValidateSounds(defs, rules is null ? null : categories, ctx);
        ValidateMusic(defs, ctx);
        ValidateBindings(defs, ctx);
    }

    private static void ValidateRules(AudioRulesDef rules, HashSet<string> categories, ValidationContext ctx)
    {
        const string root = "$.rules.audio";
        if (rules.Voices < 1) ctx.Add(RulesFile, $"{root}.voices", $"voices {rules.Voices} < 1");
        if (rules.SameSound < 1) ctx.Add(RulesFile, $"{root}.same_sound", $"same_sound {rules.SameSound} < 1");
        if (rules.MonsterDeath.Voices < 1) ctx.Add(RulesFile, $"{root}.monster_death.voices", $"monster_death.voices {rules.MonsterDeath.Voices} < 1");
        if (rules.MinInterval < 0f || rules.MonsterHitInterval < 0f || rules.MonsterDeath.SameKindInterval < 0f)
            ctx.Add(RulesFile, root, "간격(min_interval·monster_hit_interval·same_kind_interval)은 0 이상이어야 한다");
        if (rules.OffscreenMargin < 0f) ctx.Add(RulesFile, $"{root}.offscreen_margin", "offscreen_margin 은 0 이상이어야 한다");
        if (rules.Pan is < 0f or > 1f) ctx.Add(RulesFile, $"{root}.pan", $"pan {rules.Pan} 은 0..1 이어야 한다");
        var d = rules.Defaults;
        if (!Unit(d.Master) || !Unit(d.Sfx) || !Unit(d.Music)) ctx.Add(RulesFile, $"{root}.defaults", "기본 음량은 0..1 이어야 한다");
        if (!Unit(rules.PauseMusic) || !Unit(rules.VoiceDuck.To)) ctx.Add(RulesFile, root, "pause_music·voice_duck.to 는 0..1 이어야 한다");
        if (rules.MusicFade < 0f || rules.VoiceDuck.Time < 0f) ctx.Add(RulesFile, root, "music_fade·voice_duck.time 은 0 이상이어야 한다");
        if (rules.Categories.Count == 0) ctx.Add(RulesFile, $"{root}.categories", "분류가 하나도 없다");
        for (var i = 0; i < rules.Categories.Count; i++)
        {
            var c = rules.Categories[i];
            if (!categories.Add(c.Id)) ctx.Add(RulesFile, $"{root}.categories[{i}].id", $"분류 '{c.Id}' 가 두 번 있다");
            if (c.Volume < 0f) ctx.Add(RulesFile, $"{root}.categories[{i}].volume", $"분류 '{c.Id}' 음량 {c.Volume} < 0");
        }
    }

    private static void ValidateSounds(GameDefs defs, HashSet<string>? categories, ValidationContext ctx)
    {
        for (var i = 0; i < defs.Sounds.Count; i++)
        {
            var s = defs.Sounds[i];
            var at = $"$.sounds[{i}]";
            if (s.Files.Count == 0) ctx.Add(SoundsFile, $"{at}.files", $"소리 '{s.Id}' 의 파일이 없다");
            for (var f = 0; f < s.Files.Count; f++)
                if (!IsSoundPath(s.Files[f]))
                    ctx.Add(SoundsFile, $"{at}.files[{f}]", $"소리 '{s.Id}' 파일 '{s.Files[f]}' 는 {SoundRoot}… 아래 소문자 .wav 여야 한다");
            if (categories is not null && !categories.Contains(s.Category))
                ctx.Add(SoundsFile, $"{at}.category", $"소리 '{s.Id}' 의 분류 '{s.Category}' 가 rules.audio.categories 에 없다");
            if (s.Volume < 0f) ctx.Add(SoundsFile, $"{at}.volume", $"소리 '{s.Id}' 음량 {s.Volume} < 0");
            if (s.MaxInstances is < 1) ctx.Add(SoundsFile, $"{at}.max_instances", $"소리 '{s.Id}' max_instances {s.MaxInstances} < 1");
            if (s.MinInterval is < 0f) ctx.Add(SoundsFile, $"{at}.min_interval", $"소리 '{s.Id}' min_interval < 0");
            if (s.PitchJitter is < 0f or >= 0.5f) ctx.Add(SoundsFile, $"{at}.pitch_jitter", $"소리 '{s.Id}' pitch_jitter {s.PitchJitter} 는 0 이상 0.5 미만이어야 한다");
        }
    }

    private static void ValidateMusic(GameDefs defs, ValidationContext ctx)
    {
        for (var i = 0; i < defs.Music.Count; i++)
        {
            var m = defs.Music[i];
            if (!IsSoundPath(m.File)) ctx.Add(SoundsFile, $"$.music[{i}].file", $"음악 '{m.Id}' 파일 '{m.File}' 는 {SoundRoot}… 아래 소문자 .wav 여야 한다");
            if (m.Fallback is { } f && !defs.Music.TryGet(f.Id, out _))
                ctx.Add(SoundsFile, $"$.music[{i}].fallback", $"음악 '{m.Id}' 의 대체 곡 '{f.Id}' 가 없다");
        }
    }

    private static void ValidateBindings(GameDefs defs, ValidationContext ctx)
    {
        var b = defs.SoundBindings;
        const string root = "$.sound_bindings";

        void Sound(DefRef<SoundDef>? r, string path, bool loop = false)
        {
            if (r is null) return;
            if (!defs.Sounds.TryGet(r.Id, out var s))
            {
                ctx.Add(BindingsFile, path, $"소리 '{r.Id}' 가 없다{Suggest(r.Id, defs.Sounds)}");
                return;
            }
            if (loop && !s.Loop) ctx.Add(BindingsFile, path, $"장판 반복 소리 '{s.Id}' 가 loop 가 아니다", DataSeverity.Warning);
            if (!loop && s.Loop) ctx.Add(BindingsFile, path, $"한 번 나는 자리에 반복 소리 '{s.Id}'(loop) 를 걸었다", DataSeverity.Warning);
        }

        void Target<T>(string key, DefTable<T> table, string kind, string path) where T : class, IDef
        {
            if (!table.TryGet(key, out _)) ctx.Add(BindingsFile, path, $"{kind} '{key}' 가 없다{Suggest(key, table)}");
        }

        foreach (var (k, v) in Sorted(b.Weapons)) { var p = $"{root}.weapons.{k}"; Target(k, defs.Weapons, "무기", p); Sound(v.Fire, $"{p}.fire"); }
        foreach (var (k, v) in Sorted(b.Projectiles)) { var p = $"{root}.projectiles.{k}"; Target(k, defs.Projectiles, "투사체", p); Sound(v.Fire, $"{p}.fire"); }
        foreach (var (k, v) in Sorted(b.Effects)) { var p = $"{root}.effects.{k}"; Target(k, defs.Effects, "이펙트", p); Sound(v, p); }
        foreach (var (k, v) in Sorted(b.HazardLoops)) { var p = $"{root}.hazard_loops.{k}"; Target(k, defs.Effects, "이펙트", p); Sound(v, p, loop: true); }
        foreach (var (k, v) in Sorted(b.SpriteSets))
        {
            var p = $"{root}.sprite_sets.{k}";
            Target(k, defs.SpriteSets, "그림 묶음", p);
            Sound(v.Attack, $"{p}.attack"); Sound(v.Hit, $"{p}.hit"); Sound(v.Death, $"{p}.death"); Sound(v.Special, $"{p}.special");
        }
        foreach (var (k, v) in Sorted(b.Enemies))
        {
            var p = $"{root}.enemies.{k}";
            Target(k, defs.Enemies, "적", p);
            Sound(v.Attack, $"{p}.attack"); Sound(v.Hit, $"{p}.hit"); Sound(v.Death, $"{p}.death"); Sound(v.Special, $"{p}.special"); Sound(v.Voice, $"{p}.voice");
        }
        foreach (var (k, v) in Sorted(b.Pickups)) { var p = $"{root}.pickups.{k}"; Target(k, defs.Pickups, "획득물", p); Sound(v.Collect, $"{p}.collect"); Sound(v.Drop, $"{p}.drop"); }
        foreach (var (k, v) in Sorted(b.Hero)) { var p = $"{root}.hero.{k}"; Target(k, defs.HeroClasses, "직업", p); Sound(v.Hurt, $"{p}.hurt"); Sound(v.Death, $"{p}.death"); }
        foreach (var (k, v) in Sorted(b.Rewards)) { var p = $"{root}.rewards.{k}"; Target(k, defs.Rewards, "보상", p); Sound(v.Collect, $"{p}.collect"); Sound(v.Open, $"{p}.open"); }
        foreach (var (k, v) in Sorted(b.Ui))
        {
            var p = $"{root}.ui.{k}";
            if (!UiKeys.All.Contains(k)) ctx.Add(BindingsFile, p, $"모르는 UI 키 '{k}'(알려진 키: {string.Join(", ", UiKeys.All)})", DataSeverity.Warning);
            Sound(v, p);
        }
        foreach (var (k, v) in Sorted(b.Music))
        {
            var p = $"{root}.music.{k}";
            if (k != TitleMusicKey) Target(k, defs.Stages, "스테이지", p);
            if (!defs.Music.TryGet(v.Id, out _)) ctx.Add(BindingsFile, p, $"음악 '{v.Id}' 가 없다{Suggest(v.Id, defs.Music)}");
        }

        // 13.5-21: 직업마다 주인공 소리 줄
        foreach (var hc in defs.HeroClasses.All)
            if (!b.Hero.ContainsKey(hc.Id))
                ctx.Add(BindingsFile, $"{root}.hero", $"직업 '{hc.Id}' 의 주인공 소리 줄 hero.{hc.Id} 가 없다", DataSeverity.Warning);

        // 소리 없는 적: 그림 묶음 줄도 적 덮어쓰기(attack·hit·death·special)도 없음
        foreach (var e in defs.Enemies.All)
        {
            var own = b.Enemies.TryGetValue(e.Id, out var es) && (es.Attack ?? es.Hit ?? es.Death ?? es.Special) is not null;
            if (!own && !b.SpriteSets.ContainsKey(e.Sprites.Id))
                ctx.Add(BindingsFile, $"{root}.sprite_sets", $"적 '{e.Id}' 의 그림 묶음 '{e.Sprites.Id}' 에 소리 연결이 없다(소리 없는 적)", DataSeverity.Warning);
        }
    }

    /// <summary>소리·음악 파일 이름 규칙: <c>d1/sound/</c> 아래, 상대 경로 규칙(1.4), 소문자, <c>.wav</c>.</summary>
    public static bool IsSoundPath(string path) =>
        path.StartsWith(SoundRoot, StringComparison.Ordinal) && AssetFormat.IsValidRelativePath(path)
        && path.EndsWith(".wav", StringComparison.Ordinal) && !path.Any(char.IsUpper);

    private static bool Unit(float v) => v is >= 0f and <= 1f;

    // 사전 순회 순서가 넣은 순서라 오류 순서가 로더 구현에 따라 달라지지 않게 키 서수 순으로 본다
    private static IEnumerable<KeyValuePair<string, T>> Sorted<T>(IReadOnlyDictionary<string, T> d) =>
        d.OrderBy(p => p.Key, StringComparer.Ordinal);

    private static string Suggest<T>(string id, DefTable<T> table) where T : class, IDef
    {
        string? best = null;
        var bestDist = int.MaxValue;
        foreach (var d in table.All)
        {
            var dist = Distance(id, d.Id);
            if (dist < bestDist) { bestDist = dist; best = d.Id; }
        }
        return best is not null && bestDist <= Math.Max(2, id.Length / 4) ? $" (혹시 '{best}'?)" : "";
    }

    private static int Distance(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
                cur[j] = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }
}
