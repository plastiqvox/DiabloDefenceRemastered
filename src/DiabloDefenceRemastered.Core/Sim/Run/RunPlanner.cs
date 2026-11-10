namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>액트 하나의 방 계획(액트 시작 때 한 번 만들어 <c>RunState.Act</c> 에 둔다).</summary>
public sealed record ActPlan(StageDef Act, int ActNumber, IReadOnlyList<RoomPlan> Rooms);

/// <summary>
/// 방 하나의 계획(3.6 결정 2). 맵·들어온 문·출구 문·보상 후보 순서·방 씨앗.
/// <see cref="Index"/> 는 <c>rooms</c> 배열 자리(0부터, 코드 안), <see cref="Number"/> 는 보이는 방 번호(<c>RoomDef.Number</c>, v0.7.1 — 화면·로그·<c>--room</c>·<c>RunSeeds</c>·<c>RunDepth</c>).
/// v0.8: <see cref="Map"/> 은 거울판이면 <c>MapMirror</c> 가 만든 거울판 정의이고, <see cref="GateIn"/>·<see cref="ExitGates"/> 는 그 맵의 실제 문 이름(짝 이름)이다.
/// </summary>
/// <param name="Act">액트 번호(1부터).</param>
/// <param name="ExitGates">출구 문 순서(판의 마지막 방은 빈 목록). 문 열기(<c>RoomFlow</c>)가 이 순서로 보상을 단다.</param>
/// <param name="RewardOrder">액트 <c>rewards.weights</c> 의 가중 비복원 순열 — 상태 거르기는 문을 열 때(<c>RoomFlow</c>).</param>
/// <param name="RewardDraws">
/// v0.9: ④ 가 쓴 난수(가중표 원소 수만큼 — 순열의 k 번째 뽑기) + 상인 보장의 누구 고르기 하나. 문을 열 때 <c>RewardDraw.Pick</c> 이 바뀐 가중으로 같은 난수를 다시 쓴다
/// (난수 수 고정 — 문 단계의 상태가 판 흐름 난수를 밀지 않음). null(시험이 손으로 만든 계획)이면 <see cref="RewardOrder"/> 를 그대로 쓴다.
/// </param>
public sealed record RoomPlan(int Act, int Index, RoomDef Def, MapDef Map, string GateIn,
                              IReadOnlyList<string> ExitGates, IReadOnlyList<RewardDef> RewardOrder, ulong RoomSeed,
                              IReadOnlyList<float>? RewardDraws = null)
{
    public int Number => Def.Number;
    public RoomKind Kind => Def.Kind;
    /// <summary>거울판 방인가(v0.8 — 로그 <c>ROOM mirror</c>). 로그 <c>map=</c> 은 <see cref="MapId"/>.</summary>
    public bool Mirrored => Map.IsMirror;
    /// <summary>원판 맵 ID(v0.8 — 로그 <c>ROOM map=</c>·<c>RoomRecord.MapId</c>·넓이 배율).</summary>
    public string MapId => Map.MirrorOf ?? Map.Id;
}

/// <summary>
/// 판 흐름을 액트 단위로 미리 정하는 순수 함수 (v0.7 → v0.8 WP1f, 설계서 3.6 결정 2 — 그 뒤 WP4). 공개 함수에 <c>World</c> 인자가 없다 — 플레이와 무관하다.
/// 방마다 <c>RunSeeds.Flow(씨앗, 액트, 방 번호)</c> 로 난수 하나를 만들어 <b>이 순서로</b> 뽑는다(순서를 바꾸면 계약 변경 — 11.3):
/// <list type="number">
/// <item><b>방 맵</b>(v0.8 — (맵, 방향) 쌍): 보스 방은 <c>room_maps.boss</c>, 아니면 그 종류 풀. 맵마다 원판, <c>mirror: true</c> 면 바로 뒤에 거울판(풀 순서 → 원판 → 거울판).
/// 후보가 둘 이상이면 바로 앞 방(배열 바로 앞 원소)과 같은 쌍(같은 맵·같은 방향)을 빼고 균등 — 풀 1장이어도 거울판이 있으면 원판·거울판이 번갈아 나온다.</item>
/// <item><b>들어온 문</b> — 보스 방 <c>gates.boss_entry</c>, 아니면 <c>gates.entry</c> 에서 균등.</item>
/// <item><b>출구 문</b> — 판의 마지막 방이면 없음. 이 방이 액트의 마지막 방(계단)이면 <c>gates.single_exit</c> 하나.
/// 아니면 <c>gates.exits</c> 둘, <c>exits.three_chance</c> 면 남은 앞 문을 더해 셋(순서 = exits 뒤에 남은 앞 문). v0.8: 다음이 정예·보스 방이어도 둘(셋) — 문마다 덤 보상이 다르다.
/// ②③ 은 <c>rules.rooms.gates</c> 이름으로 뽑고 ① 이 거울판이면 짝 이름(<c>GateZones.MirrorGate</c>)으로 바꿔 담는다.</item>
/// <item><b>보상 후보 순서</b> — 액트 <c>rewards.weights</c>(가중 &gt; 0) 전체의 가중 비복원 순열. v0.9: 쓴 난수를 <c>RoomPlan.RewardDraws</c> 에 두고, 끝에 하나(상인 보장)를 더 뽑는다 —
/// 마지막 단계라 앞 단계·다른 방에 영향 없음. 문 열기의 가중 바꿈·보장·거르기는 <c>RewardDraw</c>.</item>
/// </list>
/// 단계마다 난수를 <b>늘 정해진 개수만큼</b> 쓴다(①②③ 한 번씩, ④ 보상 수만큼) — 그래서 ②~④ 는 <c>Flow(n)</c> 만의 함수이고, 맵(①)만 앞 방 계획에 이어진다.
/// </summary>
public static class RunPlanner
{
    public static ActPlan PlanAct(StageDef act, int actNumber, ulong runSeed, RoomRulesDef rules)
    {
        ArgumentNullException.ThrowIfNull(act);
        ArgumentNullException.ThrowIfNull(rules);
        if (act.Rooms.Count == 0) throw new ArgumentException($"액트 '{act.Id}' 에 방이 없다(rooms)");
        var gates = rules.Gates;
        var weights = new List<WeightEntry<RewardDef>>(act.Rewards.Weights.Count);
        foreach (var w in act.Rewards.Weights)
            if (w.Weight > 0f) weights.Add(w);

        var plans = new List<RoomPlan>(act.Rooms.Count);
        var lastNumber = 0;
        for (var i = 0; i < act.Rooms.Count; i++)
        {
            var def = act.Rooms[i];
            if (def.Number <= lastNumber)
                throw new ArgumentException($"액트 '{act.Id}' 의 방 {i} 번호 {def.Number} — 1부터 커지기만 해야 한다(로더가 빈 번호를 배열 자리 + 1 로 채움, 13.5-23 ①)");
            lastNumber = def.Number;
            if (def.Kind == RoomKind.Elite && def.Miniboss is null)
                throw new ArgumentException($"액트 '{act.Id}' 의 정예 방 {def.Number} 에 중간 보스(miniboss)가 없다 — 생존 단계가 끝나지 않는다(13.5-23 ①)");
            if (def.Kind == RoomKind.Boss && def.Boss is null)
                throw new ArgumentException($"액트 '{act.Id}' 의 보스 방 {def.Number} 에 보스(boss)가 없다(13.5-23 ①)");
            var rng = new RandomStream(RunSeeds.Flow(runSeed, actNumber, def.Number));

            // ① 방 맵 — (맵, 방향) 쌍
            var pairs = new List<MapDef>();
            if (def.Kind == RoomKind.Boss)
                AddPair(act.RoomMaps.Boss?.Def ?? throw new ArgumentException($"액트 '{act.Id}' 에 보스 방 맵(room_maps.boss)이 없다"));
            else
            {
                var pool = def.Kind == RoomKind.Elite ? act.RoomMaps.Elite : act.RoomMaps.Normal;
                if (pool.Count == 0) throw new ArgumentException($"액트 '{act.Id}' 에 {Name(def.Kind)} 방 맵 풀이 없다(room_maps)");
                foreach (var r in pool) AddPair(r.Def);
            }
            var candidates = pairs;
            if (pairs.Count >= 2 && i > 0)
            {
                var previous = plans[i - 1].Map.Id;
                candidates = pairs.FindAll(m => !string.Equals(m.Id, previous, StringComparison.Ordinal));
                if (candidates.Count == 0) candidates = pairs;
            }
            var map = candidates[Pick(rng, candidates.Count)];
            var mirrored = map.IsMirror;

            // ② 들어온 문
            string gateIn;
            if (def.Kind == RoomKind.Boss)
            {
                rng.NextUInt();
                gateIn = gates.BossEntry;
            }
            else
            {
                if (gates.Entry.Count == 0) throw new ArgumentException("rules.rooms.gates.entry 가 비었다");
                gateIn = gates.Entry[Pick(rng, gates.Entry.Count)];
            }

            // ③ 출구 문 — 하나는 계단(액트의 마지막 방)뿐(v0.8: 다음이 정예·보스여도 둘·셋)
            var three = rng.NextFloat() < rules.Exits.ThreeChance;
            var lastOfAct = i == act.Rooms.Count - 1;
            List<string> exits;
            if (lastOfAct && act.NextAct is null) exits = [];
            else if (lastOfAct) exits = [gates.SingleExit];
            else
            {
                exits = new List<string>(gates.Exits);
                if (three)
                    foreach (var e in gates.Entry)
                        if (!string.Equals(e, gateIn, StringComparison.Ordinal) && !exits.Contains(e)) exits.Add(e);
            }

            // ②③ 거울판이면 짝 이름으로(분포는 그대로 — 들어오는 문 {sw, se}·출구 {nw, ne} 는 짝 바꾸기에 닫혀 있다)
            if (mirrored)
            {
                gateIn = GateZones.MirrorGate(gateIn);
                for (var k = 0; k < exits.Count; k++) exits[k] = GateZones.MirrorGate(exits[k]);
            }

            // ④ 보상 후보 순서(쓴 난수를 남김 — 문 열기의 RewardDraw 가 바뀐 가중으로 다시 씀) + 상인 보장 하나(v0.9)
            var order = new List<RewardDef>(weights.Count);
            var draws = new List<float>(weights.Count + 1);
            var left = new List<WeightEntry<RewardDef>>(weights);
            while (left.Count > 0)
            {
                var total = 0f;
                foreach (var w in left) total += w.Weight;
                var draw = rng.NextFloat();
                draws.Add(draw);
                var u = draw * total;
                var k = 0;
                for (; k < left.Count - 1; k++)
                {
                    u -= left[k].Weight;
                    if (u < 0f) break;
                }
                order.Add(left[k].Ref.Def);
                left.RemoveAt(k);
            }

            draws.Add(rng.NextFloat());

            plans.Add(new RoomPlan(actNumber, i, def, map, gateIn, exits, order, RunSeeds.Room(runSeed, actNumber, def.Number), draws));

            void AddPair(MapDef m)
            {
                pairs.Add(m);
                if (!m.Mirror) return;
                pairs.Add(m.Mirrored ?? throw new ArgumentException(
                    $"맵 '{m.Id}' 이 mirror: true 인데 거울판이 없다 — 읽기(MapDataReader)·시험 정의는 MapMirror.WithMirror 로 만든다"));
            }
        }
        return new ActPlan(act, actNumber, plans);
    }

    /// <summary>
    /// 뿌리 액트부터 <c>next_act</c> 사슬의 번호(1부터). 사슬에 없으면(헬파이어 곁가지 등) 1.
    /// <c>--stage stage_catacombs</c> 로 시작해도 액트 2 다 — 깊이 기록·<c>--room 2:n</c> 이 같은 번호를 쓴다.
    /// </summary>
    public static int ActNumber(GameDefs defs, StageDef act)
    {
        var n = 1;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var s = Root(defs); s is not null && seen.Add(s.Id); s = Next(defs, s), n++)
            if (string.Equals(s.Id, act.Id, StringComparison.Ordinal)) return n;
        return 1;
    }

    /// <summary><c>--room</c> 의 액트: 뿌리부터 <c>next_act</c> 를 <paramref name="number"/> − 1 번 따라간 액트. 없으면 null.</summary>
    public static StageDef? ActAt(GameDefs defs, int number)
    {
        if (number < 1) return null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var s = Root(defs);
        for (var n = 1; s is not null && seen.Add(s.Id); n++)
        {
            if (n == number) return s;
            s = Next(defs, s);
        }
        return null;
    }

    /// <summary>
    /// 뿌리 액트 = 다른 액트의 <c>next_act</c> 가 아니고 <c>requires</c> 가 빈 액트(표 순서의 첫 번째 — 13.5-20 이 하나만 허용). 없으면 null.
    /// </summary>
    public static StageDef? Root(GameDefs defs)
    {
        var pointed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in defs.Stages.All)
            if (s.NextAct is { } next) pointed.Add(next.Id);
        foreach (var s in defs.Stages.All)
            if (s.Requires.Count == 0 && !pointed.Contains(s.Id)) return s;
        return null;
    }

    /// <summary>다음 액트(연결 안 된 참조는 ID 로 찾음). 없으면 null.</summary>
    public static StageDef? Next(GameDefs defs, StageDef act)
    {
        if (act.NextAct is not { } r) return null;
        if (r.IsBound) return r.Def;
        return defs.Stages.TryGet(r.Id, out var d) ? d : null;
    }

    /// <summary>판의 마지막 방인가: 액트의 마지막 방이고 다음 액트가 없음.</summary>
    public static bool IsLastInRun(ActPlan act, int index) => index == act.Rooms.Count - 1 && act.Act.NextAct is null;

    /// <summary>늘 난수 하나를 쓴다(<see cref="RandomStream.Index"/> 는 1개일 때 뽑지 않아 뒤 단계의 난수 열이 밀린다).</summary>
    private static int Pick(RandomStream rng, int count)
    {
        var u = rng.NextUInt();
        return count <= 1 ? 0 : (int)((ulong)u * (uint)count >> 32);
    }

    private static string Name(RoomKind kind) => kind switch
    {
        RoomKind.Normal => "normal",
        RoomKind.Elite => "elite",
        _ => "boss",
    };
}
