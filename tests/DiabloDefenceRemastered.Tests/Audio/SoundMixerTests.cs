using DiabloDefenceRemastered.Core.Audio;
using static DiabloDefenceRemastered.Tests.Audio.AudioTestDefs;

namespace DiabloDefenceRemastered.Tests.Audio;

/// <summary><c>SoundMixer</c>(7.9 고르기, 기획 12.7 표) — 10.1 <c>Audio</c> 행.</summary>
public class SoundMixerTests
{
    [Fact]
    public void TotalVoices24_NewSoundsDroppedWhenFull()
    {
        var mixer = Mixer();
        for (var i = 0; i < 30; i++) mixer.Request(new SoundRequest(Sound($"s{i}"), 0, View.Center));
        Assert.Equal(24, mixer.Mix(0, View).Count);
        Assert.Equal(new AudioCounts(30, 24, 6, 0, 0, 0), mixer.Counts);
        Assert.Equal(24, mixer.ActiveVoices(0));
    }

    [Fact]
    public void SameSound_AtMostThreeAtOnce()
    {
        var mixer = Mixer(seconds: 1.0);
        var s = Sound("cbolt");
        // 간격(0.05s = 3틱)을 지키며 1초짜리 소리를 4번 — 넷째는 같은 소리 3자리가 차서 버림
        Assert.Equal(1, mixer.Play(s, 0));
        Assert.Equal(1, mixer.Play(s, 3));
        Assert.Equal(1, mixer.Play(s, 6));
        Assert.Equal(0, mixer.Play(s, 9));
        Assert.Equal(1, mixer.Counts.DroppedCap);
        // 정의 max_instances 가 덮는다
        var one = Mixer();
        var solo = Sound("solo") with { MaxInstances = 1 };
        Assert.Equal(1, one.Play(solo, 0));
        Assert.Equal(0, one.Play(solo, 10));
    }

    [Fact]
    public void SameSound_MinInterval005()
    {
        var mixer = Mixer(seconds: 0.01);
        var s = Sound("hit");
        Assert.Equal(1, mixer.Play(s, 0));
        Assert.Equal(0, mixer.Play(s, 2));     // 0.033s < 0.05s
        Assert.Equal(1, mixer.Play(s, 3));     // 정확히 0.05s 는 통과
        Assert.Equal(1, mixer.Counts.DroppedInterval);

        // 한 프레임에 같은 소리 둘 → 둘째는 간격으로 버림(시간 기준은 앱 틱)
        mixer.Request(new SoundRequest(s, 0, View.Center));
        mixer.Request(new SoundRequest(s, 1, View.Center));
        Assert.Single(mixer.Mix(100, View));
    }

    [Fact]
    public void Gem_MinInterval006_FromDefinition()
    {
        var mixer = Mixer(seconds: 0.01);
        var gem = Sound("snd_gem", "pickup") with { Volume = 0.4f, MinInterval = 0.06f };
        Assert.Equal(1, mixer.Play(gem, 0));
        Assert.Equal(0, mixer.Play(gem, 3));   // 0.05s < 0.06s
        Assert.Equal(1, mixer.Play(gem, 4));   // 0.067s
    }

    [Fact]
    public void MonsterHit_OncePer008s_AcrossSounds_AtMost12PerSecond()
    {
        var mixer = Mixer(seconds: 0.01);
        var a = Sound("zombie_hit", "monster_hit");
        var b = Sound("skeleton_hit", "monster_hit");
        Assert.Equal(1, mixer.Play(a, 0, limit: SoundLimit.MonsterHit));
        Assert.Equal(0, mixer.Play(b, 4, limit: SoundLimit.MonsterHit));   // 다른 소리여도 전체 0.08s
        Assert.Equal(1, mixer.Play(b, 5, limit: SoundLimit.MonsterHit));

        // 1초 동안 틱마다 맞음 → ≤ 12.5/s (설계 완료 기준)
        var m = Mixer(seconds: 0.01);
        var played = 0;
        for (var t = 0; t < 60; t++) played += m.Play(t % 2 == 0 ? a : b, t, limit: SoundLimit.MonsterHit);
        Assert.Equal(12, played);
    }

    [Fact]
    public void MonsterDeath_FourAtOnce_SameKindOncePer01s()
    {
        var mixer = Mixer(seconds: 2.0);
        for (var i = 0; i < 4; i++)
            Assert.Equal(1, mixer.Play(Sound($"death{i}", "monster"), 0, limit: SoundLimit.MonsterDeath, group: $"enemy{i}"));
        Assert.Equal(0, mixer.Play(Sound("death4", "monster"), 10, limit: SoundLimit.MonsterDeath, group: "enemy4"));
        Assert.Equal(1, mixer.Counts.DroppedCap);
        // 한계 밖 일반 소리는 그대로 자리를 얻음
        Assert.Equal(1, mixer.Play(Sound("other", "monster"), 10));

        var kind = Mixer(seconds: 0.01);
        Assert.Equal(1, kind.Play(Sound("d_a", "monster"), 0, limit: SoundLimit.MonsterDeath, group: "zombie"));
        Assert.Equal(0, kind.Play(Sound("d_b", "monster"), 5, limit: SoundLimit.MonsterDeath, group: "zombie"));   // 같은 종 0.083s
        Assert.Equal(1, kind.Play(Sound("d_c", "monster"), 5, limit: SoundLimit.MonsterDeath, group: "ghoul"));    // 다른 종
        Assert.Equal(1, kind.Play(Sound("d_b", "monster"), 6, limit: SoundLimit.MonsterDeath, group: "zombie"));   // 0.1s
    }

    [Fact]
    public void Priority_HigherCategoryGetsLastSlot()
    {
        var mixer = Mixer(seconds: 5.0);
        for (var i = 0; i < 23; i++) mixer.Request(new SoundRequest(Sound($"fill{i}", "monster"), 0, View.Center));
        Assert.Equal(23, mixer.Mix(0, View).Count);

        // 같은 프레임에 줍기(낮음)가 먼저, 주인공 맞음(높음)이 나중에 들어와도 자리는 주인공이
        mixer.Request(new SoundRequest(Sound("gold", "pickup"), 0, View.Center));
        mixer.Request(new SoundRequest(Sound("hurt", "hero"), 5, View.Center));
        var played = mixer.Mix(10, View);
        Assert.Equal("hurt", Assert.Single(played).Def.Id);
        Assert.Equal(1, mixer.Counts.DroppedCap);
    }

    [Fact]
    public void Order_ByPriorityThenTickThenArrival()
    {
        var mixer = Mixer();
        mixer.Request(new SoundRequest(Sound("m1", "monster"), 2, View.Center));
        mixer.Request(new SoundRequest(Sound("w2", "weapon_fire"), 3, View.Center));
        mixer.Request(new SoundRequest(Sound("w1", "weapon_fire"), 1, View.Center));
        mixer.Request(new SoundRequest(Sound("m0", "monster"), 2, View.Center));
        mixer.Request(new SoundRequest(Sound("u", "unknown_category"), 0, View.Center));   // 모르는 분류 = 가장 낮음
        Assert.Equal(["w1", "w2", "m1", "m0", "u"], mixer.Mix(0, View).Select(p => p.Def.Id));
    }

    [Fact]
    public void SlotFreesAtLengthEndTick_WithPitch()
    {
        var durations = new FakeDurations(1.0);
        var mixer = new SoundMixer(Rules(), durations, seed: 7);
        var s = Sound("jitter", "monster_hit") with { PitchJitter = 0.05f };
        mixer.Request(new SoundRequest(s, 0, View.Center));
        var p = Assert.Single(mixer.Mix(100, View));
        Assert.InRange(p.Pitch, 0.95f, 1.05f);
        Assert.NotEqual(1f, p.Pitch);
        Assert.Equal(100 + durations.Ticks(p.File, p.Pitch), p.EndTick);
        Assert.Equal((long)Math.Ceiling(60.0 / p.Pitch - 1e-9), p.EndTick - 100);

        Assert.Equal(1, mixer.ActiveVoices(p.EndTick - 1));
        Assert.Equal(0, mixer.ActiveVoices(p.EndTick));
    }

    [Fact]
    public void SlotFree_RealSoundIndexLength()
    {
        // 소리 색인의 길이(22050Hz 표본 11025 = 0.5s → 30틱)
        var index = Core.Assets.SoundIndex.Parse("""{ "format": 1, "files": [ { "path": "d1/sound/sfx/misc/half.wav", "samples": 11025, "rate": 22050, "channels": 1 } ] }""");
        var mixer = new SoundMixer(Rules(), new SoundIndexDurations(index));
        mixer.Request(new SoundRequest(Sound("half", "ui", "d1/sound/sfx/misc/half.wav"), 0));
        Assert.Equal(30, Assert.Single(mixer.Mix(0, View)).EndTick);
        // 색인에 없는 파일은 1틱으로 세고 따로 센다
        mixer.Request(new SoundRequest(Sound("missing", "ui"), 0));
        Assert.Equal(31, Assert.Single(mixer.Mix(30, View)).EndTick);
        Assert.Equal(1, mixer.Counts.MissingLength);
    }

    [Fact]
    public void Offscreen_ByCategory_MarginAndOverrides()
    {
        var mixer = Mixer(seconds: 0.01);
        var far = new Vector2(View.X1 + 150f, View.Center.Y);
        var near = new Vector2(View.X1 + 50f, View.Center.Y);
        Assert.Equal(0, mixer.Play(Sound("mon_far", "monster"), 0, far));
        Assert.Equal(1, mixer.Play(Sound("mon_near", "monster"), 0, near));          // 100px 여유 안
        Assert.Equal(0, mixer.Play(Sound("hit_far", "weapon_hit"), 0, far));
        Assert.Equal(1, mixer.Play(Sound("fire_far", "weapon_fire"), 0, far));       // 분류 offscreen: true
        Assert.Equal(1, mixer.Play(Sound("boss_far", "monster"), 0, far, category: "boss"));   // 보스 몬스터 소리 → boss 분류
        Assert.Equal(1, mixer.Play(Sound("def_far", "monster") with { Offscreen = true }, 0, far));   // 정의 덮어쓰기
        Assert.Equal(2, mixer.Counts.DroppedOffscreen);
    }

    [Fact]
    public void Offscreen_CountsAndNoPositionNeverDropped()
    {
        var mixer = Mixer(seconds: 0.01);
        var far = new Vector2(-500f, -500f);
        mixer.Play(Sound("a", "monster"), 0, far);
        mixer.Play(Sound("b", "weapon_hit"), 0, far);
        mixer.Request(new SoundRequest(Sound("c", "monster"), 0, null));   // 위치 없음
        Assert.Single(mixer.Mix(0, View));
        Assert.Equal(2, mixer.Counts.DroppedOffscreen);
    }

    [Fact]
    public void Pan_CenterEdgesClamp()
    {
        var mixer = Mixer();
        var y = View.Center.Y;
        Assert.Equal(0f, mixer.Pan(View.Center, View));
        Assert.Equal(0.5f, mixer.Pan(new Vector2(View.X1, y), View), 4);
        Assert.Equal(-0.5f, mixer.Pan(new Vector2(View.X0, y), View), 4);
        Assert.Equal(0.25f, mixer.Pan(new Vector2(View.Center.X + 240f, y), View), 4);
        Assert.Equal(0.5f, mixer.Pan(new Vector2(View.X1 + 400f, y), View), 4);   // 화면 밖도 ±pan 으로 자름
        Assert.Equal(0f, mixer.Pan(null, View));
    }

    [Fact]
    public void Volume_CategoryTimesDefinition()
    {
        var mixer = Mixer();
        mixer.Request(new SoundRequest(Sound("gem", "pickup") with { Volume = 0.4f }, 0));
        Assert.Equal(0.4f * 0.4f, Assert.Single(mixer.Mix(0, View)).Volume, 5);
    }

    [Fact]
    public void SameInput_SameResult_SeedChangesPick()
    {
        static List<(string Id, string File, float Pitch, long Start, long End)> Run(ulong seed)
        {
            var mixer = Mixer(seconds: 0.3, seed: seed);
            var multi = Sound("multi", "monster_hit", "d1/sound/a.wav", "d1/sound/b.wav", "d1/sound/c.wav") with { PitchJitter = 0.05f };
            var result = new List<(string, string, float, long, long)>();
            for (var t = 0; t < 600; t += 3)
            {
                mixer.Request(new SoundRequest(multi, t, View.Center));
                mixer.Request(new SoundRequest(Sound($"x{t % 7}", "monster"), t, View.Center));
                result.AddRange(mixer.Mix(t, View).Select(p => (p.Def.Id, p.File, p.Pitch, p.StartTick, p.EndTick)));
            }
            return result;
        }

        var a = Run(5);
        Assert.Equal(a, Run(5));
        Assert.NotEqual(a.Select(p => (p.File, p.Pitch)), Run(6).Select(p => (p.File, p.Pitch)));
        Assert.Equal(3, a.Where(p => p.Id == "multi").Select(p => p.File).Distinct().Count());
    }

    [Fact]
    public void ResetCooldowns_KeepsPlayingVoices()
    {
        var mixer = Mixer(seconds: 1.0);
        var s = Sound("loopish");
        Assert.Equal(1, mixer.Play(s, 0));
        Assert.Equal(0, mixer.Play(s, 1));   // 간격
        mixer.ResetCooldowns();
        Assert.Equal(1, mixer.Play(s, 1));   // 간격 기록만 비움
        Assert.Equal(2, mixer.ActiveVoices(1));   // 나는 소리는 그대로
    }
}
