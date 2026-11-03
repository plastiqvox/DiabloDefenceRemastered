namespace DiabloDefenceRemastered.Core.Data.Registry;

/// <summary>
/// "스테이지에서 나올 수 있는 적" (설계서 6.14 v0.6.1, 계약·구현 WP1d, 그 뒤 소유 WP4). 13.5-15·13.5-20·"안 나오는 적" 경고·
/// <c>TextureStore</c>·<c>SoundBank</c>·시작 검사 ③ 교차 확인이 모두 이것을 부른다 — 같은 닫힘 계산을 여러 벌 짜다 엇갈리지 않게
/// (소환으로만 나오는 헬파이어 적을 놓치면 기본 스테이지가 묶음 그림을 쓰는 것을 못 잡는다).
/// <list type="number">
/// <item>시작 집합 = 생성 구간 <c>weights</c> 의 적 + 이벤트 부품(<see cref="PartKind.StageEvent"/>)의 <see cref="PartDef.ReferencedEnemies"/>(보스·중간 보스·행진·고리·호위 대장 등)
/// + 방 순번 표의 <c>rooms[].miniboss</c>·<c>boss</c>(v0.7 — 정예 방 중간 보스가 이벤트 없이도 "나오는 적" 에 듦).</item>
/// <item>고정점까지: 집합의 각 적이 가진 모든 부품(행동·훅·죽음 훅 = <c>behaviors</c>, 패턴 동작, 페이즈 패턴 동작)의 <see cref="PartDef.ReferencedEnemies"/>(summon·split_on_death·호위 등)를 더한다.</item>
/// </list>
/// 접두사(<c>affixes</c>) 부품은 보지 않는다 — 접두사는 적을 부르지 않는다(6.7). 접두사가 적을 부르게 되면 여기에 더한다.
/// </summary>
public static class StageReach
{
    /// <summary>나올 수 있는 적 전부(ID 서수 순, 결정적). 고리(서로 부르는 적)가 있어도 끝난다. 연결 안 된 참조는 <paramref name="defs"/> 에서 ID 로 찾고, 없으면 건너뛴다(13.5-1 이 따로 오류).</summary>
    public static IReadOnlyList<EnemyDef> Enemies(StageDef stage, GameDefs defs)
    {
        var found = new Dictionary<string, EnemyDef>(StringComparer.Ordinal);
        var queue = new Queue<EnemyDef>();

        void Add(DefRef<EnemyDef> r)
        {
            var def = r.IsBound ? r.Def : defs.Enemies.TryGet(r.Id, out var d) ? d : null;
            if (def is null || !found.TryAdd(def.Id, def)) return;
            queue.Enqueue(def);
        }

        void AddParts(IReadOnlyList<PartDef> parts)
        {
            foreach (var p in parts)
                foreach (var r in p.ReferencedEnemies) Add(r);
        }

        void AddPatterns(IReadOnlyList<PatternDef> patterns)
        {
            foreach (var pattern in patterns) AddParts(pattern.Actions);
        }

        foreach (var interval in stage.SpawnIntervals)
            foreach (var w in interval.Weights) Add(w.Ref);
        AddParts(stage.Events);
        foreach (var room in stage.Rooms)
        {
            if (room.Miniboss is { } mb) Add(mb);
            if (room.Boss is { } boss) Add(boss);
        }

        while (queue.Count > 0)
        {
            var e = queue.Dequeue();
            AddParts(e.Behaviors);
            AddPatterns(e.Patterns);
            foreach (var phase in e.Phases)
            {
                // 로더가 푼 완성 목록(Patterns)과 원본 add 둘 다 본다 — 합집합이라 겹쳐도 결과가 같고, 풀기 전 정의(시험·검사 순서)도 놓치지 않는다
                AddPatterns(phase.Patterns);
                AddPatterns(phase.Add);
            }
        }

        return [.. found.Values.OrderBy(d => d.Id, StringComparer.Ordinal)];
    }
}
