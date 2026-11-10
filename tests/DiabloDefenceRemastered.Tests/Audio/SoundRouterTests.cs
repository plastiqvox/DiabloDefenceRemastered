using DiabloDefenceRemastered.Core.Audio;
using static DiabloDefenceRemastered.Tests.Audio.AudioTestDefs;

namespace DiabloDefenceRemastered.Tests.Audio;

/// <summary>신호·표시 이벤트 → 소리(7.9 표, 6.15 연결 키) — <c>SoundRouter</c>·<c>HazardLoops</c>·<c>SoundPipeline</c>·<c>AudioReach</c>.</summary>
public class SoundRouterTests
{
    private static readonly GameDefs D = Defs();

    private static TestWorld NewWorld() => new TestWorldBuilder().WithDefs(D).WithPresentation().Build();

    private static SoundRequest Cue(CueKind kind, IDef? subject = null, World? world = null, EntityId source = default)
    {
        var router = new SoundRouter(D);
        Assert.True(router.TryCue(new CueEvent(7, kind, new Vector2(10, 20), subject, source), world ?? NewWorld().World, out var r), $"{kind} 소리 없음");
        return r;
    }

    private static bool HasCue(CueKind kind, IDef? subject = null) =>
        new SoundRouter(D).TryCue(new CueEvent(0, kind, default, subject), NewWorld().World, out _);

    [Fact]
    public void WeaponFired_ByWeaponId()
    {
        var r = Cue(CueKind.WeaponFired, D.Weapons.Get(TestDefs.WeaponId));
        Assert.Equal("snd_fire", r.Def.Id);
        Assert.Equal(7, r.Tick);
        Assert.Equal(new Vector2(10, 20), r.Pos);
        Assert.Null(r.Category);
    }

    [Fact]
    public void Enemy_SpriteSetSounds_OverrideByEnemy_HitLimit()
    {
        var basic = D.Enemies.Get(TestDefs.EnemyId);
        Assert.Equal("snd_t_attack", Cue(CueKind.EnemyAttack, basic).Def.Id);
        Assert.Equal("snd_t_special", Cue(CueKind.EnemySpecial, basic).Def.Id);
        var hit = Cue(CueKind.EnemyHit, basic);
        Assert.Equal(("snd_t_hit", SoundLimit.MonsterHit), (hit.Def.Id, hit.Limit));
        Assert.Equal(SoundLimit.None, Cue(CueKind.EnemyAttack, basic).Limit);

        // 적 덮어쓰기는 그 칸만 — 나머지 칸은 그림 묶음 소리
        var own = D.Enemies.Get(OwnSoundEnemyId);
        Assert.Equal("snd_own_hit", Cue(CueKind.EnemyHit, own).Def.Id);
        Assert.Equal("snd_t_attack", Cue(CueKind.EnemyAttack, own).Def.Id);

        Assert.False(HasCue(CueKind.EnemyAttack, D.Enemies.Get(SilentEnemyId)));   // 소리 없는 그림 묶음
        Assert.False(HasCue(CueKind.EnemyHit, D.Weapons.Get(TestDefs.WeaponId)));   // 대상 형이 다르면 없음
    }

    [Fact]
    public void BossMonsterSounds_RaiseToBossCategory_NoMonsterLimits()
    {
        var boss = D.Enemies.Get(BossId);
        var attack = Cue(CueKind.EnemyAttack, boss);
        Assert.Equal(("snd_boss_attack", "boss"), (attack.Def.Id, attack.Category));
        var hit = Cue(CueKind.EnemyHit, boss);
        Assert.Equal(("snd_t_hit", "boss", SoundLimit.None), (hit.Def.Id, hit.Category, hit.Limit));

        var router = new SoundRouter(D);
        Assert.True(router.TryCorpse(Corpse(boss), out var death));
        Assert.Equal(("snd_t_death", "boss", SoundLimit.None), (death.Def.Id, death.Category, death.Limit));
        Assert.True(router.TryCorpse(Corpse(D.Enemies.Get(TestDefs.EnemyId)), out var normal));
        Assert.Equal((SoundLimit.MonsterDeath, TestDefs.EnemyId, (string?)null), (normal.Limit, normal.Group, normal.Category));
    }

    [Fact]
    public void EnemyShot_ByProjectile_BossShooterRaisesCategory()
    {
        var tw = NewWorld();
        var arrow = D.Projectiles.Get(ArrowId);
        var archer = tw.AddEnemy(tw.Hero.Pos + new Vector2(100, 0));
        var boss = tw.AddEnemy(D.Enemies.Get(BossId), tw.Hero.Pos + new Vector2(-100, 0));
        var fromArcher = Cue(CueKind.EnemyShot, arrow, tw.World, archer.Id);
        Assert.Equal(("snd_arrow", (string?)null), (fromArcher.Def.Id, fromArcher.Category));
        Assert.Equal("boss", Cue(CueKind.EnemyShot, arrow, tw.World, boss.Id).Category);
        Assert.Null(Cue(CueKind.EnemyShot, arrow, tw.World, new EntityId(9999, 1)).Category);   // 쏜 적이 이미 없음
    }

    [Fact]
    public void Hero_ByHeroClassId()
    {
        Assert.Equal("snd_hurt", Cue(CueKind.HeroHurt).Def.Id);
        Assert.Equal("snd_death", Cue(CueKind.HeroDied).Def.Id);

        // 줄이 없는 직업은 소리 없음(검사기가 경고)
        var other = TestDefs.HeroClass(D.SpriteSets[0], D.Weapons[0], id: "other_hero");
        var world = new TestWorldBuilder().WithDefs(D).WithHeroClass(other).Build().World;
        Assert.False(new SoundRouter(D).TryCue(new CueEvent(0, CueKind.HeroHurt, default, null), world, out _));
    }

    [Fact]
    public void Pickups_RewardsAndBossVoice()
    {
        var chest = D.Pickups.Get(TestDefs.EliteChestId);
        Assert.Equal("snd_chest", Cue(CueKind.PickupCollected, chest).Def.Id);
        Assert.Equal("snd_flip", Cue(CueKind.PickupDropped, chest).Def.Id);
        Assert.Equal("snd_invbook", Cue(CueKind.RewardCollected, D.Rewards.Get(TestDefs.RewardTomeId)).Def.Id);
        Assert.False(HasCue(CueKind.RewardCollected, D.Rewards.Get(TestDefs.RewardChestId)));

        var boss = D.Enemies.Get(BossId);
        Assert.Equal(("snd_boss_voice", "boss_voice"), (Cue(CueKind.BossArrived, boss).Def.Id, Cue(CueKind.BossArrived, boss).Def.Category));
        Assert.Equal("snd_boss_voice", Cue(CueKind.BossPhase, boss).Def.Id);
        Assert.False(HasCue(CueKind.BossArrived, D.Enemies.Get(TestDefs.EnemyId)));   // 대사 없는 적
    }

    [Fact]
    public void UiCues_BeltPotionDoorsElite_NoPosition()
    {
        var belt = Cue(CueKind.BeltPotion);
        Assert.Equal(("snd_invpot", (Vector2?)null), (belt.Def.Id, belt.Pos));
        Assert.Equal("snd_door", Cue(CueKind.DoorsOpened).Def.Id);
        Assert.Equal("snd_magic1", Cue(CueKind.ElitePack).Def.Id);
        Assert.False(HasCue(CueKind.RoomCleared));   // 연결이 없으면 소리 없음(기획 12.7 v0.8 — 방 정리는 소리 없이)
    }

    [Fact]
    public void Effects_AndCorpses()
    {
        var router = new SoundRouter(D);
        Assert.True(router.TryEffect(new FxEvent(3, D.Effects.Get(ImpactId), new Vector2(5, 5), 1f, Rgb.White), out var r));
        Assert.Equal(("snd_imp", 3, (Vector2?)new Vector2(5, 5)), (r.Def.Id, r.Tick, r.Pos));
        Assert.False(router.TryEffect(new FxEvent(3, D.Effects.Get(MuteEffectId), default, 1f, Rgb.White), out _));   // 화살 맞음처럼 없음
        Assert.False(router.TryCorpse(Corpse(D.Enemies.Get(SilentEnemyId)), out _));
    }

    [Fact]
    public void TryUi_KeysAndRewardPaths()
    {
        var router = new SoundRouter(D);
        Assert.True(router.TryUi("level_up", out var lv));
        Assert.Equal(("snd_magic", (Vector2?)null), (lv.Def.Id, lv.Pos));
        Assert.True(router.TryUi($"rewards.{TestDefs.RewardTomeId}.open", out var open));
        Assert.Equal("snd_readbook", open.Def.Id);
        Assert.True(router.TryUi($"rewards.{TestDefs.RewardTomeId}.collect", out var collect));
        Assert.Equal("snd_invbook", collect.Def.Id);
        Assert.False(router.TryUi($"rewards.{TestDefs.RewardTomeId}.drop", out _));
        Assert.False(router.TryUi("rewards.nope.open", out _));
        Assert.False(router.TryUi("menu_move", out _));
    }

    [Fact]
    public void Music_KeyAndBundleFallback()
    {
        var router = new SoundRouter(D);
        Assert.Equal("mus_title", router.Music("title")!.Id);
        Assert.Equal("mus_stage", router.Music(TestDefs.StageId)!.Id);
        Assert.Null(router.Music("stage_none"));
        var bundled = D.Music.Get("mus_bundled");
        Assert.Same(bundled, router.Playable(bundled, _ => true));
        Assert.Equal("mus_stage", router.Playable(bundled, b => b != ContentBundle.Hfmusic)!.Id);
    }

    [Fact]
    public void MusicChoice_SameSongContinues_FallbackLogged()
    {
        var router = new SoundRouter(D);
        var noHf = new MusicChoice(router, b => b != ContentBundle.Hfmusic);
        Assert.True(noHf.TryChange("title", out var m, out var log, out _));
        Assert.Equal(("mus_title", "AUDIO music=mus_title"), (m!.Id, log));
        Assert.True(noHf.TryChange(TestDefs.StageId, out m, out log, out _));
        Assert.Equal("AUDIO music=mus_stage", log);
        // 같은 액트의 방 이동: 같은 키 → 바꾸지 않음, 로그 없음(액트마다 AUDIO music= 한 줄)
        Assert.False(noHf.TryChange(TestDefs.StageId, out m, out log, out var warn));
        Assert.Equal(((MusicDef?)null, (string?)null, (string?)null), (m, log, warn));
        Assert.False(noHf.TryChange("stage_none", out _, out _, out warn));
        Assert.StartsWith("AUDIO music 키 'stage_none'", warn);

        // 묶음이 없는 곡은 대체 곡 — 이미 그 곡이면 이어 틂
        var bindings = D.SoundBindings with
        {
            Music = new Dictionary<string, DefRef<MusicDef>>(D.SoundBindings.Music) { ["act_crypt"] = DefRef<MusicDef>.To(D.Music.Get("mus_bundled")) },
        };
        var defs = new GameDefs { Sounds = D.Sounds, Music = D.Music, SoundBindings = bindings };
        var crypt = new MusicChoice(new SoundRouter(defs), b => b != ContentBundle.Hfmusic);
        Assert.True(crypt.TryChange("act_crypt", out m, out log, out _));
        Assert.Equal(("mus_stage", "AUDIO music=mus_stage fallback=mus_bundled"), (m!.Id, log));
        Assert.False(crypt.TryChange(TestDefs.StageId, out _, out _, out _));
        var withHf = new MusicChoice(new SoundRouter(defs), _ => true);
        Assert.True(withHf.TryChange("act_crypt", out m, out log, out _));
        Assert.Equal(("mus_bundled", "AUDIO music=mus_bundled"), (m!.Id, log));
        Assert.Equal("mus_stage", crypt.Peek("act_crypt")!.Id);
    }

    [Fact]
    public void HazardLoops_OneLoopPerSound_StopsWhenGone()
    {
        var tw = NewWorld();
        var loops = new HazardLoops(new SoundRouter(D));
        var started = new List<SoundDef>();
        var stopped = new List<SoundDef>();

        var pools = Enumerable.Range(0, 3).Select(_ => AddHazard(tw.World, PoolId)).ToList();
        AddHazard(tw.World, ImpactId);   // 반복 연결이 없는 장판
        loops.Update(tw.World, started, stopped);
        Assert.Equal(["snd_loop"], started.Select(s => s.Id));
        Assert.Empty(stopped);

        started.Clear();
        tw.World.Hazards.Remove(pools[0].Id);
        tw.World.Hazards.Remove(pools[1].Id);
        loops.Update(tw.World, started, stopped);
        Assert.Empty(started);   // 하나라도 남아 있으면 그대로
        Assert.Empty(stopped);

        tw.World.Hazards.Remove(pools[2].Id);
        loops.Update(tw.World, started, stopped);
        Assert.Equal(["snd_loop"], stopped.Select(s => s.Id));
        Assert.Equal(0, loops.Count);
    }

    [Fact]
    public void Pipeline_MutedAndSoundSameCounts_SinkOnlyWhenNotMuted()
    {
        static (AudioCounts Counts, RecordingSink Sink) Run(bool muted)
        {
            var sink = new RecordingSink();
            var pipeline = new SoundPipeline(D, Rules(), new FakeDurations(0.4), seed: 3, muted, sink);
            var tw = NewWorld();
            var w = tw.World;
            var hero = w.Hero.Pos;
            var basic = D.Enemies.Get(TestDefs.EnemyId);
            for (long frame = 0; frame < 300; frame++)
            {
                var buf = w.Presentation;
                for (var k = 0; k < 6; k++)
                {
                    buf.Cue(new CueEvent((int)frame, CueKind.EnemyHit, hero + new Vector2(k * 30, 0), basic));
                    buf.Effect(new FxEvent((int)frame, D.Effects.Get(ImpactId), hero, 1f, Rgb.White));
                }
                if (frame % 7 == 0) buf.Corpse(Corpse(basic, (int)frame));
                if (frame % 50 == 0) buf.Cue(new CueEvent((int)frame, CueKind.BeltPotion, hero, null));
                if (frame % 3 == 0) buf.Cue(new CueEvent((int)frame, CueKind.WeaponFired, hero, D.Weapons[0]));
                if (frame == 100) AddHazard(w, PoolId);
                pipeline.Consume(buf, w, frame);
                buf.Clear();
            }
            Assert.True(pipeline.PlayUi("level_up", 300, w.ViewRect));
            return (pipeline.Counts, sink);
        }

        var muted = Run(true);
        var sound = Run(false);
        Assert.Equal(muted.Counts, sound.Counts);
        Assert.True(sound.Counts.Played > 100 && sound.Counts.DroppedInterval > 0, sound.Counts.ToString());
        Assert.Empty(muted.Sink.Plays);
        Assert.Empty(muted.Sink.Loops);
        Assert.Equal(sound.Counts.Played, sound.Sink.Plays.Count);
        Assert.Equal(["+snd_loop"], sound.Sink.Loops);
        Assert.Contains(sound.Sink.Plays, p => p.Def.Id == "snd_invpot");
        Assert.Equal(
            $"AUDIO muted=true requested={muted.Counts.Requested} played={muted.Counts.Played} dropped_cap={muted.Counts.DroppedCap} dropped_interval={muted.Counts.DroppedInterval} dropped_offscreen={muted.Counts.DroppedOffscreen}",
            SoundPipeline.LogLine(true, muted.Counts));
    }

    [Fact]
    public void Pipeline_AttachStopsLoopsAndClearsCooldowns_KeepsVoices()
    {
        var sink = new RecordingSink();
        var pipeline = new SoundPipeline(D, Rules(), new FakeDurations(2.0), seed: 1, muted: false, sink);
        var old = NewWorld().World;
        AddHazard(old, PoolId);
        old.Presentation.Cue(new CueEvent(0, CueKind.WeaponFired, old.Hero.Pos, D.Weapons[0]));
        pipeline.Consume(old, 0);

        var next = NewWorld().World;   // 방 이동: 새 월드, 틱 0 부터
        pipeline.Attach();
        Assert.Equal(["+snd_loop", "-snd_loop"], sink.Loops);
        next.Presentation.Cue(new CueEvent(0, CueKind.WeaponFired, next.Hero.Pos, D.Weapons[0]));
        pipeline.Consume(next, 1);   // 간격(0.05s) 안이지만 쿨다운을 비워 남
        Assert.Equal(2, sink.Plays.Count(p => p.Def.Id == "snd_fire"));
        Assert.Equal(2, pipeline.Mixer.ActiveVoices(1));   // 옛 방 소리는 끊지 않음
        Assert.Equal(0, pipeline.ActiveLoops);
    }

    [Fact]
    public void Pipeline_BossVoiceFlag()
    {
        var pipeline = new SoundPipeline(D, Rules(), new FakeDurations(1.0), seed: 1, muted: true, sink: null);
        var w = NewWorld().World;
        w.Presentation.Cue(new CueEvent(0, CueKind.BossArrived, w.Hero.Pos, D.Enemies.Get(BossId)));
        pipeline.Consume(w, 0);
        Assert.True(pipeline.BossVoicePlayed);
        w.Presentation.Clear();
        pipeline.Consume(w, 1);
        Assert.False(pipeline.BossVoicePlayed);
    }

    [Fact]
    public void AudioReach_StageEnemiesOnly_PlusGlobalRows()
    {
        var all = AudioReach.Sounds(D, null).Select(s => s.Id).ToList();
        Assert.Equal(D.Sounds.Count, all.Count);
        Assert.Equal(all.OrderBy(x => x, StringComparer.Ordinal), all);

        // 합성 스테이지는 적을 내지 않는다 → 적 소리 없음, 나머지 줄(무기·이펙트·반복·획득물·UI·보상·주인공)은 전부
        var stage = AudioReach.Sounds(D, D.Stages[0]).Select(s => s.Id).ToList();
        Assert.DoesNotContain("snd_t_hit", stage);
        Assert.DoesNotContain("snd_boss_voice", stage);
        Assert.Contains("snd_fire", stage);
        Assert.Contains("snd_loop", stage);
        Assert.Contains("snd_invpot", stage);
        Assert.Contains("snd_readbook", stage);
        Assert.Contains("snd_hurt", stage);

        Assert.Equal(["d1/sound/sfx/misc/a.wav", "d1/sound/sfx/misc/b.wav"],
            AudioReach.Files([Sound("b"), Sound("a"), Sound("a2", "ui", "d1/sound/sfx/misc/a.wav")]));
    }

    internal static CorpseSpawned Corpse(EnemyDef e, int tick = 0) =>
        new(tick, e, e.Sprites.Def, "", Vector2.Zero, new Vector2(0, 1), Rgb.White, 1f, 1f, Rgb.White, 0f, DeathStyle.Anim);

    private static Hazard AddHazard(World w, string effectId)
    {
        var h = w.Hazards.Add();
        h.Effect = D.Effects.Get(effectId);
        h.Duration = h.TimeLeft = 5f;
        return h;
    }
}

file static class PipelineExtensions
{
    public static void Consume(this SoundPipeline p, World w, long now)
    {
        p.Consume(w.Presentation, w, now);
        w.Presentation.Clear();
    }
}
