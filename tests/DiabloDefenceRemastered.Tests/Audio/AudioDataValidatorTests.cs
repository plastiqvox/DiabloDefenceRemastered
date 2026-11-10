using DiabloDefenceRemastered.Core.Audio;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Tests.Data;
using static DiabloDefenceRemastered.Tests.Audio.AudioTestDefs;

namespace DiabloDefenceRemastered.Tests.Audio;

/// <summary>소리 데이터 검사기(6.14 "소리" 행·13.5-21 소리 칸·소리 없는 적 경고) — 10.1 <c>Audio</c>.</summary>
public class AudioDataValidatorTests
{
    private static List<DataError> Validate(GameDefs defs)
    {
        var ctx = new ValidationContext { Defs = defs };
        new AudioDataValidator().Validate(defs, ctx);
        return [.. ctx.Errors];
    }

    private static GameDefs With(SoundBindingsDef? bindings = null, IEnumerable<SoundDef>? sounds = null, AudioRulesDef? rules = null,
                                 IEnumerable<MusicDef>? music = null, IEnumerable<HeroClassDef>? heroes = null)
    {
        var d = Defs();
        return new GameDefs
        {
            Rules = d.Rules with { Audio = rules ?? d.Rules.Audio },
            Sprites = d.Sprites, SpriteSets = d.SpriteSets, Weapons = d.Weapons, Passives = d.Passives,
            HeroClasses = heroes is null ? d.HeroClasses : new DefTable<HeroClassDef>(heroes),
            Tilesets = d.Tilesets, Maps = d.Maps, Stages = d.Stages, Enemies = d.Enemies, Rewards = d.Rewards, Pickups = d.Pickups,
            Projectiles = d.Projectiles, Effects = d.Effects,
            Sounds = sounds is null ? d.Sounds : new DefTable<SoundDef>(sounds),
            Music = music is null ? d.Music : new DefTable<MusicDef>(music),
            SoundBindings = bindings ?? d.SoundBindings,
        };
    }

    private static DefRef<SoundDef> Ref(string id) => new(id);

    [Fact]
    public void Synthetic_NoErrors_OnlySilentEnemyWarning()
    {
        var errors = Validate(Defs());
        Assert.DoesNotContain(errors, e => e.Severity == DataSeverity.Error);
        var w = Assert.Single(errors);
        Assert.Equal((AudioDataValidator.BindingsFile, "$.sound_bindings.sprite_sets"), (w.File, w.JsonPath));
        Assert.Contains(SilentEnemyId, w.Message);
        Assert.Contains("소리 없는 적", w.Message);
    }

    [Fact]
    public void Module_RegistersValidator()
    {
        Assert.Contains(GameModules.CreateContext().Validators, v => v is AudioDataValidator);
    }

    [Fact]
    public void MissingTargets_AreErrorsWithPath()
    {
        var b = Defs().SoundBindings;
        var bad = b with
        {
            Weapons = new Dictionary<string, FireSoundsDef>(b.Weapons) { ["magic_misile"] = new() { Fire = Ref("snd_fire") } },
            Projectiles = new Dictionary<string, FireSoundsDef> { ["pj_nope"] = new() },
            Effects = new Dictionary<string, DefRef<SoundDef>> { ["fx_nope"] = Ref("snd_imp") },
            HazardLoops = new Dictionary<string, DefRef<SoundDef>> { ["fx_nope_loop"] = Ref("snd_loop") },
            SpriteSets = new Dictionary<string, UnitSoundsDef>(b.SpriteSets) { ["set_nope"] = new() },
            Enemies = new Dictionary<string, EnemySoundsDef> { ["enemy_nope"] = new() },
            Pickups = new Dictionary<string, PickupSoundsDef> { ["pickup_nope"] = new() },
            Hero = new Dictionary<string, HeroSoundsDef>(b.Hero) { ["hero_nope"] = new() },
            Rewards = new Dictionary<string, RewardSoundsDef> { ["reward_nope"] = new() },
            Music = new Dictionary<string, DefRef<MusicDef>> { ["title"] = new("mus_title"), ["stage_nope"] = new("mus_stage") },
        };
        var errors = Validate(With(bindings: bad)).Where(e => e.Severity == DataSeverity.Error).ToList();
        string[] expected =
        [
            "$.sound_bindings.weapons.magic_misile", "$.sound_bindings.projectiles.pj_nope", "$.sound_bindings.effects.fx_nope",
            "$.sound_bindings.hazard_loops.fx_nope_loop", "$.sound_bindings.sprite_sets.set_nope", "$.sound_bindings.enemies.enemy_nope",
            "$.sound_bindings.pickups.pickup_nope", "$.sound_bindings.hero.hero_nope", "$.sound_bindings.rewards.reward_nope", "$.sound_bindings.music.stage_nope",
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), errors.Select(e => e.JsonPath).Order(StringComparer.Ordinal));
        Assert.All(errors, e => Assert.Equal(AudioDataValidator.BindingsFile, e.File));
        Assert.Contains(errors, e => e.Message == "무기 'magic_misile' 가 없다");
    }

    [Fact]
    public void MissingSoundOrMusic_IsError_WithSuggestion()
    {
        var b = Defs().SoundBindings;
        var bad = b with
        {
            Ui = new Dictionary<string, DefRef<SoundDef>>(b.Ui) { ["menu_move"] = Ref("snd_magik") },
            Music = new Dictionary<string, DefRef<MusicDef>> { ["title"] = new("mus_titel") },
        };
        var errors = Validate(With(bindings: bad)).Where(e => e.Severity == DataSeverity.Error).ToList();
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.JsonPath == "$.sound_bindings.ui.menu_move" && e.Message == "소리 'snd_magik' 가 없다 (혹시 'snd_magic'?)");
        Assert.Contains(errors, e => e.JsonPath == "$.sound_bindings.music.title" && e.Message.StartsWith("음악 'mus_titel' 가 없다"));
    }

    [Fact]
    public void Sounds_CategoryFilesRanges()
    {
        var sounds = Defs().Sounds.All.ToList();
        sounds.Add(Sound("snd_badcat", "voices"));
        sounds.Add(Sound("snd_nofile") with { Files = [] });
        sounds.Add(Sound("snd_badpath", "ui", "sound/sfx/a.wav", "d1/sound/Upper.wav", "d1/sound/x.mp3", "d1/sound/../x.wav"));
        sounds.Add(Sound("snd_ranges") with { Volume = -1f, MaxInstances = 0, MinInterval = -0.1f, PitchJitter = 0.5f });
        var errors = Validate(With(sounds: sounds)).Where(e => e.Severity == DataSeverity.Error).ToList();
        var paths = errors.Select(e => e.JsonPath).ToList();
        var n = sounds.Count;
        Assert.Contains($"$.sounds[{n - 4}].category", paths);
        Assert.Contains($"$.sounds[{n - 3}].files", paths);
        for (var f = 0; f < 4; f++) Assert.Contains($"$.sounds[{n - 2}].files[{f}]", paths);
        foreach (var p in new[] { "volume", "max_instances", "min_interval", "pitch_jitter" }) Assert.Contains($"$.sounds[{n - 1}].{p}", paths);
        Assert.Equal(10, errors.Count);
        Assert.All(errors, e => Assert.Equal(AudioDataValidator.SoundsFile, e.File));
    }

    [Fact]
    public void Rules_MissingOrBroken()
    {
        var d = Defs();
        var noRules = new GameDefs { Rules = d.Rules with { Audio = null }, Sounds = d.Sounds };
        var e1 = Assert.Single(Validate(noRules));
        Assert.Equal((AudioDataValidator.RulesFile, "$.rules"), (e1.File, e1.JsonPath));

        var broken = Rules() with
        {
            Voices = 0,
            Pan = 2f,
            Categories = [Category("ui", 0.7f, true), Category("ui", -1f, true)],
        };
        var errors = Validate(new GameDefs { Rules = d.Rules with { Audio = broken } });
        Assert.Equal(["$.rules.audio.categories[1].id", "$.rules.audio.categories[1].volume", "$.rules.audio.pan", "$.rules.audio.voices"],
            errors.Select(e => e.JsonPath).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Music_FileAndFallback()
    {
        MusicDef[] music =
        [
            new() { Id = "mus_title", File = "d1/sound/music/dintro.wav" },
            new() { Id = "mus_stage", File = "music/dlvla.wav" },
            new() { Id = "mus_lost", File = "d1/sound/music/dlvlf.wav", Fallback = new DefRef<MusicDef>("mus_nope") },
        ];
        var errors = Validate(With(music: music)).Where(e => e.Severity == DataSeverity.Error).ToList();
        Assert.Equal(["$.music[1].file", "$.music[2].fallback"], errors.Select(e => e.JsonPath));
    }

    [Fact]
    public void Warnings_HeroClassUiKeyAndLoopMismatch()
    {
        var d = Defs();
        var second = TestDefs.HeroClass(d.SpriteSets[0], d.Weapons[0], id: "warrior");
        var b = d.SoundBindings with
        {
            Ui = new Dictionary<string, DefRef<SoundDef>>(d.SoundBindings.Ui) { ["menu_bounce"] = Ref("snd_magic"), ["chest_open"] = Ref("snd_loop") },
            HazardLoops = new Dictionary<string, DefRef<SoundDef>> { [PoolId] = Ref("snd_imp") },
        };
        var warnings = Validate(With(bindings: b, heroes: [.. d.HeroClasses.All, second]))
            .Where(e => e.Severity == DataSeverity.Warning && !e.Message.Contains("소리 없는 적")).ToList();
        Assert.Equal(4, warnings.Count);
        Assert.Contains(warnings, w => w.JsonPath == "$.sound_bindings.hero" && w.Message.Contains("hero.warrior"));
        Assert.Contains(warnings, w => w.JsonPath == "$.sound_bindings.ui.menu_bounce");
        Assert.Contains(warnings, w => w.JsonPath == "$.sound_bindings.ui.chest_open" && w.Message.Contains("loop"));
        Assert.Contains(warnings, w => w.JsonPath == $"$.sound_bindings.hazard_loops.{PoolId}" && w.Message.Contains("loop 가 아니다"));
    }

    [Fact]
    public void IsSoundPath_Rules()
    {
        Assert.True(AudioDataValidator.IsSoundPath("d1/sound/sfx/misc/cbolt.wav"));
        Assert.True(AudioDataValidator.IsSoundPath("d1/sound/music/dlvla.wav"));
        Assert.False(AudioDataValidator.IsSoundPath("d1/sound/sfx\\misc/cbolt.wav"));
        Assert.False(AudioDataValidator.IsSoundPath("d1/sound/SFX/cbolt.wav"));
        Assert.False(AudioDataValidator.IsSoundPath("d1/gfx/cbolt.wav"));
        Assert.False(AudioDataValidator.IsSoundPath("d1/sound/cbolt.ogg"));
    }

    /// <summary>10.1 <c>Audio</c>: <c>Sim/</c> 소스 파일이 <c>Core.Audio</c> 를 참조하지 않는다(소리를 켜든 끄든 같은 결과 — 7.9, R4).</summary>
    [Fact]
    public void SimSources_DoNotReferenceCoreAudio()
    {
        var sim = Path.Combine(RealData.RepoRoot(), "src", "DiabloDefenceRemastered.Core", "Sim");
        var files = Directory.GetFiles(sim, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        string[] banned = ["Core.Audio", "SoundMixer", "SoundRouter", "SoundPipeline", "HazardLoops", "AudioReach", "AudioDataValidator"];
        var hits = files.SelectMany(f => banned.Where(b => File.ReadAllText(f).Contains(b, StringComparison.Ordinal))
                                                .Select(b => $"{Path.GetRelativePath(sim, f)}: {b}")).ToList();
        Assert.Empty(hits);
    }
}
