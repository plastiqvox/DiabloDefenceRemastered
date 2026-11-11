using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 액트 실행 시나리오 도우미 (설계서 10.1 <c>Sim</c> — <c>RunSession</c> + <c>AutoplayPolicy</c> + <c>--god</c>, v0.7.1 방 틱 상한).
/// 방마다 방 시계 상한(일반 방 = 생존 길이 + <c>cleanup_limit</c> + 60s)과 보상·문 단계 상한(<c>autoplay_door_skip</c> + 10s)을 두고, 넘으면 그 방·단계를 적고 실패한다.
/// 해시는 시험용 요약(WP7 <c>StateHasher</c> 전) — 틱·주인공·적(정의·위치·HP)·난수 상태.
/// </summary>
public static class ActRunner
{
    public sealed record Result(RunState Run, AutoplayPolicy Policy, int Ticks, IReadOnlyList<ulong> Hashes, int PeakAlive,
                                float KiteSeconds, int Spawned, int Fallbacks, int BandSpawns, RunOutcome Outcome);

    public static Result Run(GameDefs defs, string actId, ulong seed, RunOptions? options = null, AutoplayVisit visit = AutoplayVisit.None,
                             int startRoomIndex = 0, Func<ModuleContext>? modules = null, int hashEvery = 60, StageDef? act = null)
    {
        var rules = defs.Rules.Rooms!;
        var session = new RunSession(defs, (modules ?? (() => FakeArena.Modules()))(), new RunStart(seed, act ?? defs.Stages.Get(actId), null,
                                     options ?? new RunOptions { God = true }, startRoomIndex));
        var policy = new AutoplayPolicy(visit);
        var hashes = new List<ulong>();
        var ticks = 0;
        var peak = 0;
        var kiteTicks = 0;
        int spawned = 0, fallbacks = 0, band = 0;
        var phaseLimit = MathX.SecondsToTicks(rules.AutoplayDoorSkip + 10f, World.Dt);
        var cleanupLimit = MathX.SecondsToTicks(rules.CleanupLimit + 60f, World.Dt);
        while (true)
        {
            var w = session.World;
            var room = w.Room!;
            // 방 틱 상한(기획 15절 v0.8.1): 일반 = 생존 길이, 정예 = 생성 상한 2:30 + 1:30, 보스 = 6:00 — 그 위에 정리 안전장치 + 60s
            var survive = room.SurviveTicks > 0 ? room.SurviveTicks
                : room.Plan.Kind == RoomKind.Boss ? MathX.SecondsToTicks(360f, World.Dt)
                : MathX.SecondsToTicks(rules.EliteSpawnCap.Seconds + 90f, World.Dt);
            if (room.ClockTicks > survive + cleanupLimit)
                throw new Xunit.Sdk.XunitException($"방 {room.Plan.Act}:{room.Plan.Number} 방 시계 상한을 넘음(단계 {room.Phase}, 남은 적 {Alive(w)})");
            if (room.Phase is RoomPhase.Reward or RoomPhase.Doors && w.Tick - room.PhaseTick > phaseLimit)
                throw new Xunit.Sdk.XunitException($"방 {room.Plan.Act}:{room.Plan.Number} {room.Phase} 단계 상한을 넘음");

            w.Command = policy.Next(w);
            w.Simulation!.Step();
            ticks++;
            var alive = Alive(w);
            peak = Math.Max(peak, alive);
            foreach (var e in w.Enemies.Items)
                if (e.IsAlive && e.Kit.Positioning.Any(s => e.Kit.Part(0, s) is Kite)) kiteTicks++;
            if (hashEvery > 0 && w.Tick % hashEvery == 0) hashes.Add(Hash(w));

            if (w.Outcome is RunOutcome.Victory or RunOutcome.Defeat)
            {
                Add(w);
                session.Conclude();
                return new Result(session.Run, policy, ticks, hashes, peak, kiteTicks * World.Dt, spawned, fallbacks, band, w.Outcome);
            }
            if (room.Phase == RoomPhase.Exit && w.Outcome == RunOutcome.Running)
            {
                Add(w);
                session.Advance();
            }
        }

        void Add(World w)
        {
            spawned += w.DirectorState.IntervalSpawned;
            fallbacks += w.DirectorState.EntryFallbacks;
            band += w.DirectorState.BandSpawns;
        }
    }

    public static int Alive(World w)
    {
        var n = 0;
        foreach (var e in w.Enemies.Items) if (e.IsAlive) n++;
        return n;
    }

    /// <summary>시험용 상태 요약 해시(FNV-1a, 순서 = 저장소 밀집 순서).</summary>
    public static ulong Hash(World w)
    {
        var h = 14695981039346656037UL;
        void Mix(ulong v) { h ^= v; h *= 1099511628211UL; }
        void F(float f) => Mix(BitConverter.SingleToUInt32Bits(f));
        Mix((ulong)w.Tick);
        F(w.Hero.Pos.X); F(w.Hero.Pos.Y); F(w.Hero.Hp);
        foreach (var e in w.Enemies.Items)
        {
            if (e.Removed) continue;
            Mix((ulong)e.DefIndex); F(e.Pos.X); F(e.Pos.Y); F(e.Hp); Mix((ulong)e.Life);
        }
        var (s0, s1, s2, s3) = w.Rng.Spawn.GetState();
        Mix(s0); Mix(s1); Mix(s2); Mix(s3);
        var (a0, a1, a2, a3) = w.Rng.Ai.GetState();
        Mix(a0); Mix(a1); Mix(a2); Mix(a3);
        Mix((ulong)(w.Room?.Phase ?? 0));
        return h;
    }
}
