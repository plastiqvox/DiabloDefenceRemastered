using System.Text.Json;
using DiabloDefenceRemastered.Core.Audio;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Audio;

/// <summary>
/// 실제 <c>Data/audio/*.json</c>·<c>Data/rules/audio.json</c>(WP8). WP7 로더 전이라 형으로 엄격하게 읽고(모르는 속성 오류) 실제 그림 묶음과 함께
/// 검사기를 돌린다. 아직 이 저장소에 없는 종류(무기·투사체·이펙트·획득물·직업·보상·스테이지 — 다른 묶음이 같은 때 쓴다)를 가리키는 줄은
/// "대상 없음" 오류로 나오므로 그 목록을 따로 굳혀 둔다 — 합친 뒤에는 실제 데이터 엄격 시험(10.1 <c>Data</c> 실제, WP6)이 오류 0 을 본다.
/// </summary>
public class RealAudioDataTests
{
    private static GameDefs RealAudioDefs()
    {
        var bindings = RealData.Read("audio/bindings.json")["sound_bindings"]!.Deserialize<SoundBindingsDef>(TestJson.Options)!;
        var rules = RealData.Read("rules/audio.json")["rules"]!["audio"]!.Deserialize<AudioRulesDef>(TestJson.Options)!;
        var sprites = new List<SpriteDef>();
        var sets = new List<SpriteSetDef>();
        foreach (var f in new[] { "sprites/monsters.json", "sprites/hero.json" })
        {
            sprites.AddRange(RealData.Kind<SpriteDef>(f, "sprites"));
            sets.AddRange(RealData.Kind<SpriteSetDef>(f, "sprite_sets"));
        }
        return new GameDefs
        {
            Rules = new RulesDef { Audio = rules },
            Sprites = new DefTable<SpriteDef>(sprites),
            SpriteSets = new DefTable<SpriteSetDef>(sets),
            Sounds = new DefTable<SoundDef>(RealData.Kind<SoundDef>("audio/sounds.json", "sounds")),
            Music = new DefTable<MusicDef>(RealData.Kind<MusicDef>("audio/sounds.json", "music")),
            SoundBindings = bindings,
        };
    }

    [Fact]
    public void Rules_AreSection615Values()
    {
        Assert.Equal(["audio"], RealData.Read("rules/audio.json")["rules"]!.AsObject().Select(p => p.Key));
        var real = RealAudioDefs().Rules.Audio!;
        var expected = AudioTestDefs.Rules();
        Assert.Equal(expected with { Categories = [] }, real with { Categories = [], Defaults = expected.Defaults, VoiceDuck = expected.VoiceDuck, MonsterDeath = expected.MonsterDeath });
        Assert.Equal(expected.Categories, real.Categories);
        Assert.Equal(expected.MonsterDeath, real.MonsterDeath);
        Assert.Equal(new VolumeDefaultsDef { Master = 0.8f, Sfx = 0.8f, Music = 0.6f }, real.Defaults);
        Assert.Equal(new VoiceDuckDef { To = 0.5f, Time = 2f }, real.VoiceDuck);
        Assert.Equal((0.5f, 0.5f), (real.MusicFade, real.PauseMusic));
        // 우선순위 = 기획 12.7 순서
        Assert.Equal(["boss_voice", "hero", "ui", "boss", "weapon_fire", "weapon_hit", "monster", "monster_hit", "pickup", "ambient"],
            real.Categories.Select(c => c.Id));
    }

    [Fact]
    public void Validator_OnlyMissingTargetsOfKindsNotYetInRepo()
    {
        var defs = RealAudioDefs();
        var ctx = new ValidationContext { Defs = defs };
        new AudioDataValidator().Validate(defs, ctx);

        // 다른 묶음이 정의를 넣으면 사라질 오류(합친 뒤 실제 데이터 시험이 0 을 본다). 이 목록 = 다른 묶음에 넘길 ID
        string[] pending =
        [
            "$.sound_bindings.effects.fx_bolt_hit", "$.sound_bindings.effects.fx_fire_explosion",
            "$.sound_bindings.hero.sorcerer",
            "$.sound_bindings.music.stage_cathedral",
            "$.sound_bindings.pickups.gem_blue", "$.sound_bindings.pickups.gold", "$.sound_bindings.pickups.healing_potion",
            "$.sound_bindings.projectiles.pj_enemy_arrow",
            "$.sound_bindings.rewards.reward_chest", "$.sound_bindings.rewards.reward_tome",
            "$.sound_bindings.weapons.arcane_orbit", "$.sound_bindings.weapons.fireball", "$.sound_bindings.weapons.magic_missile",
        ];
        Assert.All(ctx.Errors, e => Assert.Equal(DataSeverity.Error, e.Severity));
        Assert.All(ctx.Errors, e => Assert.EndsWith("가 없다", e.Message));
        Assert.Equal(pending, ctx.Errors.Select(e => e.JsonPath).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Sounds_AllUsed_EveryMonsterSetBound_FilesFollowImportNames()
    {
        var defs = RealAudioDefs();
        var b = defs.SoundBindings;
        var used = new HashSet<string>(StringComparer.Ordinal);
        void Use(DefRef<SoundDef>? r) { if (r is not null) used.Add(r.Id); }
        foreach (var v in b.Weapons.Values.Concat(b.Projectiles.Values)) Use(v.Fire);
        foreach (var v in b.Effects.Values.Concat(b.HazardLoops.Values).Concat(b.Ui.Values)) Use(v);
        foreach (var v in b.SpriteSets.Values) { Use(v.Attack); Use(v.Hit); Use(v.Death); Use(v.Special); }
        foreach (var v in b.Enemies.Values) { Use(v.Attack); Use(v.Hit); Use(v.Death); Use(v.Special); Use(v.Voice); }
        foreach (var v in b.Pickups.Values) { Use(v.Collect); Use(v.Drop); }
        foreach (var v in b.Hero.Values) { Use(v.Hurt); Use(v.Death); }
        foreach (var v in b.Rewards.Values) { Use(v.Collect); Use(v.Open); }
        Assert.DoesNotContain(defs.Sounds.All, s => !used.Contains(s.Id));
        Assert.DoesNotContain(defs.Music.All, m => !b.Music.Values.Any(r => r.Id == m.Id));

        // 주인공 그림 묶음(set_sorcerer)은 hero.<직업> 이 맡는다 — 몬스터 묶음은 모두 소리 연결이 있다(소리 없는 적 경고 0)
        foreach (var set in defs.SpriteSets.All.Where(s => s.Id != "set_sorcerer"))
        {
            Assert.True(b.SpriteSets.TryGetValue(set.Id, out var u), set.Id);
            Assert.NotNull(u.Attack); Assert.NotNull(u.Hit); Assert.NotNull(u.Death);
        }

        // 몬스터 소리 파일 = d1/sound/monsters/<그림 묶음 폴더>/… — 그림 파일(d1/monsters/<폴더>.png)과 같은 원작 폴더
        foreach (var (setId, u) in b.SpriteSets)
        {
            var folder = setId["set_".Length..];
            foreach (var r in new[] { u.Attack, u.Hit, u.Death })
                Assert.All(defs.Sounds.Get(r!.Id).Files, f => Assert.StartsWith($"d1/sound/monsters/{folder}/", f));
        }

        // 맞음 소리만 높낮이 흔들림(기획 12.7 "몬스터·무기 맞음 소리는 ±5%")
        foreach (var s in defs.Sounds.All)
            Assert.Equal(s.Category is "monster_hit" or "weapon_hit" ? 0.05f : 0f, s.PitchJitter);
    }

    [Fact]
    public void Gem_And_HeroSounds_MatchDesign()
    {
        var defs = RealAudioDefs();
        var gem = defs.Sounds.Get("snd_gem");
        Assert.Equal((0.4f, (float?)0.06f, "pickup"), (gem.Volume, gem.MinInterval, gem.Category));
        var hurt = defs.Sounds.Get(defs.SoundBindings.Hero["sorcerer"].Hurt!.Id);
        Assert.Equal(["d1/sound/sfx/sorceror/mage69.wav", "d1/sound/sfx/sorceror/mage69b.wav"], hurt.Files);
        Assert.Equal(["d1/sound/sfx/sorceror/mage71.wav"], defs.Sounds.Get(defs.SoundBindings.Hero["sorcerer"].Death!.Id).Files);
        Assert.Equal("d1/sound/sfx/items/dooropen.wav", Assert.Single(defs.Sounds.Get(defs.SoundBindings.Ui["door_open"].Id).Files));
        Assert.False(defs.SoundBindings.Ui.ContainsKey("room_clear"));   // 기획 12.7 v0.8: 방 정리는 소리 없이
        Assert.Equal("snd_invpot", defs.SoundBindings.Ui["belt_potion"].Id);   // v0.8 허리띠 저절로 마심 = 물약 소리
    }
}
