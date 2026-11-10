using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Core.Dev;
using DiabloDefenceRemastered.Core.Sim.Autoplay;

namespace DiabloDefenceRemastered.Tests.Dev;

/// <summary>개발 옵션 파싱·판정(설계서 9절, 10.1 <c>Dev</c> — WP7).</summary>
public sealed class DevOptionsTests
{
    private static DevOptions Ok(params string[] args)
    {
        var (o, errors) = DevOptions.Parse(args);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        return o;
    }

    private static string Err(params string[] args)
    {
        var (_, errors) = DevOptions.Parse(args);
        Assert.NotEmpty(errors);
        return string.Join("\n", errors);
    }

    [Fact]
    public void Parses_Values()
    {
        var o = Ok("--seed", "7", "--autoplay", "--room", "1:2", "--window", "1280x720", "--capture", "out/s1.png", "--capture-at", "120,30",
                   "--quit-after", "125", "--save-dir", "sv", "--log", "run.log", "--log-hash", "h.csv", "--check", "--start-at", "00:45",
                   "--weapons", "firebolt:3,nova:1", "--passives", "armor:2", "--level", "5", "--stress", "300,50", "--time-scale", "4", "--vsync", "off");
        Assert.Equal(7ul, o.Seed);
        Assert.True(o.Autoplay);
        Assert.True(o.EffectiveBackground);
        Assert.Equal(new RoomRef(1, 2), o.Room);
        Assert.Equal(new WindowSize(1280, 720), o.Window);
        Assert.Equal([30d, 120d], o.CaptureAt);   // 정렬
        Assert.Equal(Path.Combine("out", "s1_30.png").Replace('\\', '/'), o.CapturePath(30).Replace('\\', '/'));
        Assert.Equal(125d, o.QuitAfter);
        Assert.Equal(45f, o.StartAt!.Value.Seconds);
        Assert.Equal([new LoadoutEntry("firebolt", 3), new LoadoutEntry("nova", 1)], o.Weapons);
        Assert.Equal(new StressSpec(300, 50), o.Stress);
        Assert.Equal(4d, o.TimeScale);
        Assert.False(o.Vsync);
        var run = o.ToRunOptions();
        Assert.True(run.God);   // --stress 가 --god 를 켬
        Assert.Equal(5, run.Level);
        Assert.True(run.Check);
        Assert.Equal(300, run.StressEnemies);
    }

    [Fact]
    public void SingleCapture_KeepsName()
    {
        var o = Ok("--capture", "x.png", "--capture-at", "1", "--quit-after", "2");
        Assert.Equal("x.png", o.CapturePath(1));
        Assert.True(o.CaptureOnlyRun);
        Assert.False(Ok("--autoplay", "--capture", "x.png", "--capture-at", "1").CaptureOnlyRun);
    }

    [Theory]
    [InlineData("--bogus")]
    [InlineData("--seed", "-1")]
    [InlineData("--seed")]
    [InlineData("--room", "1")]
    [InlineData("--room", "0:3")]
    [InlineData("--window", "100")]
    [InlineData("--capture-at", "1,x")]
    [InlineData("--weapons", "firebolt")]
    [InlineData("--weapons", "a:1,a:2")]
    [InlineData("--level", "0")]
    [InlineData("--start-at", "1:75")]
    [InlineData("--preview", "obstacles")]
    [InlineData("--capture-on", "never")]
    [InlineData("--vsync", "maybe")]
    [InlineData("--sound-device", "speakers")]
    [InlineData("--autoplay-visit", "rooms")]                       // --autoplay 없음
    [InlineData("--room", "1:2", "--stage", "stage_cathedral")]       // 같이 주면 오류
    [InlineData("--map", "cathedral_arena")]                         // 미리보기 밖
    [InlineData("--mute", "--sound")]
    [InlineData("--capture-at", "3")]                                // --capture 없음
    [InlineData("--seed", "1", "--seed", "2")]
    [InlineData("stray")]
    public void Rejects(params string[] args) => Err(args);

    [Fact]
    public void UnknownOption_Suggests()
    {
        Assert.Contains("혹시 --autoplay", Err("--autoplya"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--reveal")]
    [InlineData("--god")]
    [InlineData("--level", "3")]
    [InlineData("--weapons", "a:1")]
    [InlineData("--passives", "b:1")]
    [InlineData("--hero", "sorcerer")]
    [InlineData("--room", "1:9")]
    [InlineData("--preview", "fog")]
    [InlineData("--autoplay")]
    [InlineData("--stage", "x")]
    [InlineData("--capture", "x.png")]
    [InlineData("--validate-data")]
    [InlineData("--stress", "100")]
    public void AnyOneOfThese_IsDevRun_ProtectsSave_AndMutes(params string[] args)
    {
        var o = Ok(args);
        Assert.True(o.DevRun);
        Assert.True(o.ProtectUserSave);
        Assert.True(o.Muted);
        Assert.True(o.NullAudioDevice);
        Assert.Equal(DevOptions.DefaultDevTimeout, o.EffectiveTimeout);
        Assert.Equal(DevOptions.DefaultDevWindow, o.EffectiveWindow);
    }

    [Fact]
    public void PlainRun_IsNotDev()
    {
        var o = Ok("--seed", "3", "--log", "x.log", "--window", "1600x900", "--time-scale", "2");
        Assert.False(o.DevRun);
        Assert.False(o.ProtectUserSave);
        Assert.False(o.Muted);
        Assert.Null(o.EffectiveTimeout);
        Assert.Equal(new WindowSize(1600, 900), o.EffectiveWindow);
    }

    [Fact]
    public void SaveDir_AllowsSaveInDevRun_AndSoundFlags()
    {
        Assert.False(Ok("--autoplay", "--save-dir", "s").ProtectUserSave);
        var sound = Ok("--autoplay", "--sound");
        Assert.False(sound.Muted);
        Assert.True(sound.NullAudioDevice);   // --sound 여도 장치는 출력 없음
        var dev = Ok("--autoplay", "--sound-device", "default");
        Assert.False(dev.Muted);
        Assert.False(dev.NullAudioDevice);
        Assert.True(Ok("--mute").Muted);
    }

    [Fact]
    public void Preview_Fog_WithMap_AndMirror()
    {
        var o = Ok("--preview", "fog", "--map", "test_room_a@m");
        Assert.Equal("fog", o.Preview);
        Assert.Equal(new MapChoice("test_room_a", true), o.Map);
        Assert.Equal("test_room_a@m", o.Map!.Value.ToString());
        var v = Ok("--autoplay", "--autoplay-visit", "rooms", "--debug-nav", "sight,grid", "--debug-nav");
        Assert.Equal(AutoplayVisit.Rooms, v.AutoplayVisit);
        Assert.Equal(["sight", "grid", .. DevOptions.DebugNavLayers], v.DebugNav);
    }

    // ── 데이터를 읽은 뒤(합성 액트: 액트 1 = 일반 1·2·3 → 정예 4 → 일반 5 → 보스 6, 액트 2 = 일반 1·2 → 보스 3) ──

    private static IReadOnlyList<OptionError> Check(GameDefs defs, params string[] args) => Ok(args).CheckData(defs);

    [Fact]
    public void Room_InRange_AndBossStartAtPasses()
    {
        var defs = RoomActs.Defs();
        Assert.Empty(Check(defs, "--room", "1:6"));
        Assert.Empty(Check(defs, "--room", "1:6", "--start-at", "03:10"));   // 보스 방 = 상한 없음
        Assert.Empty(Check(defs, "--room", "2:3"));
    }

    [Theory]
    [InlineData("--room", "1:7")]
    [InlineData("--room", "3:1")]
    [InlineData("--room", "1:4", "--start-at", "02:40")]   // 정예 방 2:30 넘음
    [InlineData("--room", "1:1", "--start-at", "01:00")]   // 일반 방 생존 길이와 같음
    [InlineData("--start-at", "01:00")]                     // 뿌리 액트 1번 방
    [InlineData("--stage", "nope")]
    [InlineData("--hero", "nope")]
    [InlineData("--weapons", "nope:1")]
    [InlineData("--passives", "nope:1")]
    [InlineData("--preview", "tiles", "--map", "nope")]
    [InlineData("--preview", "tiles", "--map", "test_room_a@m")]   // mirror: true 가 아님
    public void DataChecks_Invalid64(params string[] args)
    {
        var errors = Check(RoomActs.Defs(), args);
        var e = Assert.Single(errors);
        Assert.Equal(OptionProblem.Invalid, e.Problem);
        Assert.Equal(ExitCodes.BadOptions, e.ExitCode);
    }

    [Fact]
    public void ElitesBelowCap_AndNormalBelowLength_Pass()
    {
        var defs = RoomActs.Defs();
        Assert.Empty(Check(defs, "--room", "1:4", "--start-at", "02:29"));
        Assert.Empty(Check(defs, "--room", "1:1", "--start-at", "00:59"));
    }

    [Fact]
    public void SliceAct_MissingNumber_IsInvalid()
    {
        var defs = RoomActs.Defs();
        var act = defs.Stages[0];
        var slice = act with { Slice = true, Rooms = [act.Rooms[0], act.Rooms[1] with { Number = 3 }] };
        var d2 = new GameDefs { Rules = defs.Rules, Stages = new DefTable<StageDef>([slice]), Maps = defs.Maps, Tilesets = defs.Tilesets, HeroClasses = defs.HeroClasses };
        Assert.Empty(Check(d2, "--room", "1:3"));
        Assert.Single(Check(d2, "--room", "1:2"));
    }

    [Fact]
    public void Weapons_MoreThanOpenSlots_IsInvalid()
    {
        var basic = TestDefs.Basic();
        var weapons = Enumerable.Range(0, 6).Select(i => TestDefs.Weapon($"w{i}")).ToList();
        var defs = new GameDefs { Rules = basic.Rules, Stages = basic.Stages, Weapons = new DefTable<WeaponDef>(weapons), HeroClasses = basic.HeroClasses };
        Assert.Empty(Check(defs, "--weapons", "w0:1,w1:1,w2:1,w3:1"));
        var e = Assert.Single(Check(defs, "--weapons", "w0:1,w1:1,w2:1,w3:1,w4:1"));
        Assert.Contains("열린 무기 칸 4", e.Message, StringComparison.Ordinal);
        // 액트 2 부터면 칸 하나 더(RunSlots — 계단 +1)
        var acts = RoomActs.Defs();
        var withWeapons = new GameDefs { Rules = acts.Rules, Stages = acts.Stages, Weapons = new DefTable<WeaponDef>(weapons), HeroClasses = acts.HeroClasses };
        Assert.Empty(Check(withWeapons, "--room", "2:1", "--weapons", "w0:1,w1:1,w2:1,w3:1,w4:1"));
    }

    [Fact]
    public void Weapon_ClassOutside_AndLevelOverMax()
    {
        var basic = TestDefs.Basic();
        var other = new HeroClassDef { Id = "other", Sprites = basic.HeroClasses[0].Sprites, StartWeapons = basic.HeroClasses[0].StartWeapons };
        var w = TestDefs.Weapon("only_other") with { Classes = [DefRef<HeroClassDef>.To(other)], MaxLevel = 3 };
        var defs = new GameDefs { Rules = basic.Rules, Stages = basic.Stages, Weapons = new DefTable<WeaponDef>([w]), HeroClasses = new DefTable<HeroClassDef>([basic.HeroClasses[0], other]) };
        Assert.Contains("고를 수 없다", Assert.Single(Check(defs, "--weapons", "only_other:1")).Message, StringComparison.Ordinal);
        Assert.Empty(Check(defs, "--hero", "other", "--weapons", "only_other:3"));
        Assert.Contains("최대 레벨", Assert.Single(Check(defs, "--hero", "other", "--weapons", "only_other:4")).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DisabledHeroOrAct_IsExit5()
    {
        var acts = RoomActs.Defs();
        var hf = acts.Stages[1] with { Requires = [ContentBundle.Hellfire] };
        var hero = acts.HeroClasses[0];
        var monk = hero with { Id = "monk", Requires = [ContentBundle.Hfmonk] };
        var defs = new GameDefs
        {
            Rules = acts.Rules, Maps = acts.Maps, Tilesets = acts.Tilesets,
            Stages = new DefTable<StageDef>([acts.Stages[0] with { NextAct = DefRef<StageDef>.To(hf) }, hf]),
            HeroClasses = new DefTable<HeroClassDef>([hero, monk]),
        };
        var avail = ContentAvailability.From(new AssetManifest
        {
            SkippedBundles = [new ManifestSkippedBundle(ContentBundle.Hellfire, "hellfire.mpq", ManifestSkippedBundle.ReasonMissing),
                              new ManifestSkippedBundle(ContentBundle.Hfmonk, "hfmonk.mpq", ManifestSkippedBundle.ReasonMissing)],
        });
        var room = Assert.Single(Ok("--room", "2:1").CheckData(defs, avail));
        Assert.Equal(OptionProblem.Disabled, room.Problem);
        Assert.Equal(ExitCodes.MissingAssets, room.ExitCode);
        Assert.Equal(OptionProblem.Disabled, Assert.Single(Ok("--hero", "monk").CheckData(defs, avail)).Problem);
        Assert.Empty(Ok("--room", "1:2").CheckData(defs, avail));
    }
}
