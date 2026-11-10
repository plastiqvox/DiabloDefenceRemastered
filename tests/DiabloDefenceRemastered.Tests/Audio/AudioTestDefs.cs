using DiabloDefenceRemastered.Core.Audio;

namespace DiabloDefenceRemastered.Tests.Audio;

/// <summary>소리 시험용 합성 정의(WP8). 값은 6.15 의 <c>rules.audio</c> 첫 판과 같다.</summary>
public static class AudioTestDefs
{
    public static readonly RectF View = new(0f, 0f, 960f, 540f);

    public static AudioRulesDef Rules() => new()
    {
        Voices = 24,
        SameSound = 3,
        MinInterval = 0.05f,
        MonsterHitInterval = 0.08f,
        MonsterDeath = new MonsterDeathAudioDef { Voices = 4, SameKindInterval = 0.1f },
        OffscreenMargin = 100f,
        Pan = 0.5f,
        Categories =
        [
            Category("boss_voice", 1.0f, true),
            Category("hero", 1.0f, true),
            Category("ui", 0.7f, true),
            Category("boss", 0.8f, true),
            Category("weapon_fire", 0.6f, true),
            Category("weapon_hit", 0.5f, false),
            Category("monster", 0.5f, false),
            Category("monster_hit", 0.5f, false),
            Category("pickup", 0.4f, true),
            Category("ambient", 0.6f, true),
        ],
    };

    public static SoundCategoryDef Category(string id, float volume, bool offscreen) => new() { Id = id, Volume = volume, Offscreen = offscreen };

    public static SoundDef Sound(string id, string category = "weapon_fire", params string[] files) => new()
    {
        Id = id,
        Category = category,
        Files = files.Length > 0 ? files : [$"d1/sound/sfx/misc/{id}.wav"],
    };

    /// <summary>파일마다 길이(초). 없으면 <paramref name="defaultSeconds"/>. 틱 = ceil(초 / 높낮이 × 60) — <c>SoundInfo.DurationTicks</c> 와 같은 식.</summary>
    public sealed class FakeDurations(double defaultSeconds = 1.0, IReadOnlyDictionary<string, double>? seconds = null) : ISoundDurations
    {
        public int Ticks(string file, float pitch)
        {
            var s = seconds is not null && seconds.TryGetValue(file, out var v) ? v : defaultSeconds;
            return s < 0 ? 0 : (int)Math.Ceiling(s / pitch * SoundMixer.TicksPerSecond - 1e-9);
        }
    }

    public static SoundMixer Mixer(double seconds = 1.0, ulong seed = 1, AudioRulesDef? rules = null) =>
        new(rules ?? Rules(), new FakeDurations(seconds), seed);

    /// <summary>요청 하나를 넣고 바로 고른다(그 프레임의 고른 수).</summary>
    public static int Play(this SoundMixer mixer, SoundDef sound, long now, Vector2? pos = null, SoundLimit limit = SoundLimit.None, string? group = null, string? category = null)
    {
        mixer.Request(new SoundRequest(sound, 0, pos ?? View.Center, category, limit, group));
        return mixer.Mix(now, View).Count;
    }

    public const string BossId = "test_boss";
    public const string OwnSoundEnemyId = "test_own_sound";
    public const string SilentEnemyId = "test_silent";
    public const string ArrowId = "pj_test_arrow";
    public const string ImpactId = "fx_test_impact";
    public const string PoolId = "fx_test_pool";
    public const string MuteEffectId = "fx_test_quiet";

    /// <summary>
    /// 합성 정의(<see cref="TestDefs.Basic"/>) + 소리 정의·연결 표·<c>rules.audio</c>. 적: 기본 적(그림 묶음 <c>set_test</c> 소리),
    /// 보스(<see cref="BossId"/> — 대사·공격 덮어쓰기), 맞음만 덮어쓴 적(<see cref="OwnSoundEnemyId"/>), 소리 없는 그림 묶음의 적(<see cref="SilentEnemyId"/>).
    /// </summary>
    public static GameDefs Defs()
    {
        var basic = TestDefs.Basic();
        var set = basic.SpriteSets[0];
        var silentSprite = TestDefs.Sprite("SilentSprite");
        var silentSet = TestDefs.SpriteSet(silentSprite, "set_silent");
        var boss = TestDefs.Enemy(set, BossId) with { Rank = DefRank.Boss };
        var own = TestDefs.Enemy(set, OwnSoundEnemyId);
        var silent = TestDefs.Enemy(silentSet, SilentEnemyId);

        SoundDef[] sounds =
        [
            Sound("snd_fire", "weapon_fire"), Sound("snd_arrow", "monster"), Sound("snd_imp", "weapon_hit") with { PitchJitter = 0.05f },
            Sound("snd_loop", "ambient") with { Loop = true },
            Sound("snd_t_attack", "monster"), Sound("snd_t_hit", "monster_hit"), Sound("snd_t_death", "monster"), Sound("snd_t_special", "monster"),
            Sound("snd_boss_voice", "boss_voice"), Sound("snd_boss_attack", "monster"), Sound("snd_own_hit", "monster_hit"),
            Sound("snd_chest", "pickup"), Sound("snd_flip", "pickup"), Sound("snd_hurt", "hero"), Sound("snd_death", "hero"),
            Sound("snd_magic", "ui"), Sound("snd_magic1", "ui") with { Volume = 0.5f }, Sound("snd_door", "ui"), Sound("snd_invpot", "pickup"),
            Sound("snd_invbook", "pickup"), Sound("snd_readbook", "ui"),
        ];
        var byId = sounds.ToDictionary(s => s.Id);
        DefRef<SoundDef> S(string id) => DefRef<SoundDef>.To(byId[id]);

        MusicDef title = new() { Id = "mus_title", File = "d1/sound/music/dintro.wav" };
        MusicDef stage = new() { Id = "mus_stage", File = "d1/sound/music/dlvla.wav" };
        MusicDef bundled = new() { Id = "mus_bundled", File = "d1/sound/music/dlvlf.wav", Requires = [ContentBundle.Hfmusic], Fallback = DefRef<MusicDef>.To(stage) };

        var arrow = new ProjectileDef { Id = ArrowId, Sprite = DefRef<SpriteDef>.To(basic.Sprites[0]) };
        EffectDef[] effects = [new() { Id = ImpactId }, new() { Id = PoolId }, new() { Id = MuteEffectId }];

        return new GameDefs
        {
            Rules = basic.Rules with { Audio = Rules() },
            Sprites = new DefTable<SpriteDef>([.. basic.Sprites.All, silentSprite]),
            SpriteSets = new DefTable<SpriteSetDef>([set, silentSet]),
            Weapons = basic.Weapons,
            Passives = basic.Passives,
            HeroClasses = basic.HeroClasses,
            Tilesets = basic.Tilesets,
            Maps = basic.Maps,
            Stages = basic.Stages,
            Enemies = new DefTable<EnemyDef>([.. basic.Enemies.All, boss, own, silent]),
            Rewards = basic.Rewards,
            Pickups = basic.Pickups,
            Projectiles = new DefTable<ProjectileDef>([arrow]),
            Effects = new DefTable<EffectDef>(effects),
            Sounds = new DefTable<SoundDef>(sounds),
            Music = new DefTable<MusicDef>([title, stage, bundled]),
            SoundBindings = new SoundBindingsDef
            {
                Weapons = new Dictionary<string, FireSoundsDef> { [TestDefs.WeaponId] = new() { Fire = S("snd_fire") } },
                Projectiles = new Dictionary<string, FireSoundsDef> { [ArrowId] = new() { Fire = S("snd_arrow") } },
                Effects = new Dictionary<string, DefRef<SoundDef>> { [ImpactId] = S("snd_imp") },
                HazardLoops = new Dictionary<string, DefRef<SoundDef>> { [PoolId] = S("snd_loop") },
                SpriteSets = new Dictionary<string, UnitSoundsDef>
                {
                    [TestDefs.SpriteSetId] = new() { Attack = S("snd_t_attack"), Hit = S("snd_t_hit"), Death = S("snd_t_death"), Special = S("snd_t_special") },
                },
                Enemies = new Dictionary<string, EnemySoundsDef>
                {
                    [BossId] = new() { Voice = S("snd_boss_voice"), Attack = S("snd_boss_attack") },
                    [OwnSoundEnemyId] = new() { Hit = S("snd_own_hit") },
                },
                Pickups = new Dictionary<string, PickupSoundsDef> { [TestDefs.EliteChestId] = new() { Collect = S("snd_chest"), Drop = S("snd_flip") } },
                Hero = new Dictionary<string, HeroSoundsDef> { [TestDefs.HeroClassId] = new() { Hurt = S("snd_hurt"), Death = S("snd_death") } },
                Ui = new Dictionary<string, DefRef<SoundDef>>
                {
                    ["level_up"] = S("snd_magic"), ["elite_pack"] = S("snd_magic1"), ["door_open"] = S("snd_door"), ["belt_potion"] = S("snd_invpot"),
                },
                Rewards = new Dictionary<string, RewardSoundsDef> { [TestDefs.RewardTomeId] = new() { Collect = S("snd_invbook"), Open = S("snd_readbook") } },
                Music = new Dictionary<string, DefRef<MusicDef>>
                {
                    ["title"] = DefRef<MusicDef>.To(title), [TestDefs.StageId] = DefRef<MusicDef>.To(stage),
                },
            },
        };
    }

    /// <summary>재생 기록용 가짜 재생 쪽.</summary>
    public sealed class RecordingSink : ISoundSink
    {
        public List<SoundPlay> Plays { get; } = [];
        public List<string> Loops { get; } = [];

        public void Play(in SoundPlay play) => Plays.Add(play);
        public void StartLoop(SoundDef sound) => Loops.Add("+" + sound.Id);
        public void StopLoop(SoundDef sound) => Loops.Add("-" + sound.Id);
    }
}
