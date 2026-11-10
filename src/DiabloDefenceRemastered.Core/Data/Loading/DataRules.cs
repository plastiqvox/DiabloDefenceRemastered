namespace DiabloDefenceRemastered.Core.Data.Loading;

/// <summary>
/// 로딩 6단계 — 공통 의미 검사 (설계서 6.14 의 "Data (WP7)" 줄). 연결이 끝난 정의만 본다(연결 안 된 참조는 건너뜀 — 5단계가 이미 오류를 냈다).
/// <list type="bullet">
/// <item>13.5-1 적 <c>variant</c> ∈ 그림 묶음 <c>variants</c>, 13.5-3 생성 구간, 13.5-7(정의끼리) 그림·클립·이벤트 프레임, 13.5-8 진화, 13.5-14 크기(경고)</item>
/// <item>13.5-15 의 빛 반경 380·화면 반폭 쪽(<c>kite</c> 쪽은 WP4 검사기), 13.5-20 자원 묶음, 13.5-21 직업, 13.5-23 ①②④ 방 구조</item>
/// <item>추가: 스테이지가 있는데 <c>rules.sight</c>·<c>rules.rooms</c> 없음, 부품 슬롯 > 12, 반경 경고, 안 나오는 적 경고</item>
/// </list>
/// 13.5-2(레벨 표)·13.5-13(예고)·13.5-18·23 ⑤⑥(방 이벤트)은 부품을 아는 묶음의 검사기(WP3·WP4)가 본다.
/// </summary>
internal static class DataRules
{
    /// <summary>13.5-15 하한(기획 10.5-3)과 상한(화면 가로 반폭 = 960 / 2).</summary>
    public const float MinHeroLight = 380f;
    public const float MaxHeroLight = 480f;
    /// <summary>적 하나의 부품 슬롯 상한(행동 + 패턴, 6.6).</summary>
    public const int MaxPartSlots = 12;
    public const float MinScale = 0.65f, MaxScale = 1.6f;
    public const float FlowRadiusMax = 26f;

    public static void Check(GameDefs defs, Collected src, ValidationContext ctx)
    {
        new Checker(defs, src, ctx).Run();
    }

    private sealed class Checker(GameDefs defs, Collected src, ValidationContext ctx)
    {
        private void Err(string kind, string id, string sub, string message, DataSeverity sev = DataSeverity.Error)
        {
            var (file, path) = src.Where(kind, id);
            ctx.Add(file, path + sub, message, sev);
        }

        private void Warn(string kind, string id, string sub, string message) => Err(kind, id, sub, message, DataSeverity.Warning);

        private void RuleErr(string key, string sub, string message)
        {
            if (src.Rules.TryGetValue(key, out var el)) ctx.Add(el.File, el.Path + sub, message);
            else ctx.Add("", $"$.rules.{key}{sub}", message);
        }

        public void Run()
        {
            RequiredRules();
            Sprites();
            foreach (var e in defs.Enemies.All) Enemy(e);
            foreach (var ev in defs.Evolutions.All) Evolution(ev);
            foreach (var p in defs.Passives.All)
                if (p.Blessing && p.Trait) Err("passives", p.Id, ".blessing", $"축복 패시브 {p.Id} 가 trait 이기도 하다(13.5-23 ④)");
            foreach (var h in defs.HeroClasses.All) HeroClass(h);
            foreach (var s in defs.Stages.All) Stage(s);
            Bundles();
            Rewards();
            UnreachableEnemies();
        }

        // ── 규칙 ─────────────────────────────────────────

        private void RequiredRules()
        {
            if (defs.Stages.Count == 0) return;
            var first = defs.Stages[0].Id;
            if (defs.Rules.Sight is null) Err("stages", first, "", "스테이지가 있는데 rules.sight 가 없다(6.10 — rules/sight.json)");
            if (defs.Rules.Rooms is null) Err("stages", first, "", "스테이지가 있는데 rules.rooms 가 없다(6.10 — rules/rooms.json)");
            if (defs.Rules.Hero is null) Err("stages", first, "", "스테이지가 있는데 rules.hero 가 없다(6.10 — rules/hero.json)");
        }

        // ── 그림 13.5-7(정의끼리) ─────────────────────────

        private void Sprites()
        {
            foreach (var s in defs.Sprites.All)
            {
                if (s.Directions <= 0) Err("sprites", s.Id, ".directions", $"방향 수가 0 이하다({s.Directions})");
                if (s.Frames <= 0) Err("sprites", s.Id, ".frames", $"프레임 수가 0 이하다({s.Frames})");
                if (s.OriginByDir is { } byDir && byDir.Count != s.Directions)
                    Err("sprites", s.Id, ".origin_by_dir", $"origin_by_dir 수 {byDir.Count} ≠ 방향 수 {s.Directions}");
                if (s.Frame.X > 0 && s.Frame.Y > 0 && (s.Origin.X < 0 || s.Origin.Y < 0 || s.Origin.X > s.Frame.X || s.Origin.Y > s.Frame.Y))
                    Err("sprites", s.Id, ".origin", $"발 위치 ({s.Origin.X},{s.Origin.Y}) 가 칸 {s.Frame.X}×{s.Frame.Y} 밖이다");
                if (s.Source != SpriteSource.Grid) continue;
                if (s.Frame.X <= 0 || s.Frame.Y <= 0) Err("sprites", s.Id, ".frame", "grid 그림에 frame [w,h] 가 없다");
                var slack = s.Slack ?? new Int2(0, 0);
                var w = s.Frame.X * s.Directions;
                var h = s.Frame.Y * s.Frames;
                if (s.Sheet.X < w || s.Sheet.X > w + slack.X || s.Sheet.Y < h || s.Sheet.Y > h + slack.Y)
                    Err("sprites", s.Id, ".sheet", $"시트 {s.Sheet.X}×{s.Sheet.Y} ≠ 칸 {s.Frame.X}×{s.Frame.Y} × (방향 {s.Directions}, 프레임 {s.Frames}) = {w}×{h} (+slack {slack.X},{slack.Y})");
            }
            foreach (var set in defs.SpriteSets.All)
                foreach (ClipKind kind in Enum.GetValues<ClipKind>())
                    if (set.Clips.Get(kind) is { } clip) ClipFrame(set, kind, clip);
        }

        private void ClipFrame(SpriteSetDef set, ClipKind kind, ClipDef clip)
        {
            if (clip.EventFrame is not { } ef || !clip.Sprite.IsBound) return;
            var frames = clip.Sprite.Def.Frames;
            if (ef < 1 || ef > frames)
                Err("sprite_sets", set.Id, $".clips.{Snake(kind)}.event_frame", $"이벤트 프레임 {ef} 가 1..{frames} 밖이다(프레임은 1부터)");
        }

        // ── 적 ───────────────────────────────────────────

        private void Enemy(EnemyDef e)
        {
            if (!e.Sprites.IsBound) return;
            var set = e.Sprites.Def;
            if (!set.Variants.Contains(e.Variant))
                Err("enemies", e.Id, ".variant", $"변종 '{e.Variant}' 이 그림 묶음 {set.Id} 의 variants 에 없다{IdSuggest.Hint(e.Variant, set.Variants)}");

            void PatternClip(PatternDef p, string sub)
            {
                if (p.Clip is not { } clip) return;
                var c = set.Clips.Get(clip);
                if (c is null) Err("enemies", e.Id, sub + ".clip", $"쓰는 클립 '{Snake(clip)}' 이 그림 묶음 {set.Id} 에 없다");
                else if (p.EventFrame is { } ef && c.Sprite.IsBound && (ef < 1 || ef > c.Sprite.Def.Frames))
                    Err("enemies", e.Id, sub + ".event_frame", $"이벤트 프레임 {ef} 가 1..{c.Sprite.Def.Frames} 밖이다");
            }
            for (var i = 0; i < e.Patterns.Count; i++) PatternClip(e.Patterns[i], $".patterns[{i}]");
            for (var k = 0; k < e.Phases.Count; k++)
                for (var i = 0; i < e.Phases[k].Patterns.Count; i++) PatternClip(e.Phases[k].Patterns[i], $".phases[{k}].patterns[{i}]");

            var slots = e.Behaviors.Count + e.Patterns.Count;
            foreach (var ph in e.Phases) slots = Math.Max(slots, e.Behaviors.Count + ph.Patterns.Count);
            if (slots > MaxPartSlots) Err("enemies", e.Id, ".behaviors", $"부품 슬롯 {slots} > {MaxPartSlots}");

            if (e.Scale < MinScale || e.Scale > MaxScale)
                Warn("enemies", e.Id, ".scale", $"크기 {e.Scale} 가 {MinScale}~{MaxScale} 밖이다(13.5-14)");
            else if (defs.Rules.Ranks is { } ranks && e.Rank == DefRank.Normal && e.EliteEligible
                     && e.Scale * Math.Max(ranks.Elite.Scale, ranks.Unique.Scale) > ranks.MaxScale + 1e-4f)
                Warn("enemies", e.Id, ".scale", $"크기 {e.Scale} × 등급 배율 {Math.Max(ranks.Elite.Scale, ranks.Unique.Scale)} > max_scale {ranks.MaxScale} — 생성 때 잘린다(13.5-14)");

            var computed = MathF.Round(set.BaseRadius * e.Scale, MidpointRounding.AwayFromZero);
            var radius = e.Radius ?? computed;
            if (e.Radius is { } r && MathF.Abs(r - computed) > 2f)
                Warn("enemies", e.Id, ".radius", $"radius {r} 가 round(base_radius {set.BaseRadius} × scale {e.Scale}) = {computed} 와 2 넘게 다르다");
            if (e.Movement == MovementKind.Flow && radius > FlowRadiusMax)
                Warn("enemies", e.Id, ".radius", $"flow 유닛 반경 {radius} > {FlowRadiusMax}");

            foreach (var b in e.Requires)
                if (b == ContentBundle.Hfvoice) Err("enemies", e.Id, ".requires", "requires 에 hfvoice 는 쓰지 않는다(원본 우선순위 전용, 13.5-20)");
        }

        private void Evolution(EvolutionDef ev)
        {
            if (ev.Weapon.IsBound && ev.Weapon.Def.MaxLevel < 1)
                Err("evolutions", ev.Id, ".weapon", $"진화 재료 무기 {ev.Weapon.Id} 의 max_level 이 없다(13.5-8)");
            if (ev.Passive.IsBound && ev.Passive.Def.Trait)
                Err("evolutions", ev.Id, ".passive", $"진화 재료 패시브 {ev.Passive.Id} 가 특성(trait)이다");
        }

        // ── 13.5-21 직업 ─────────────────────────────────

        private static readonly ClipKind[] HeroClips = [ClipKind.Idle, ClipKind.Walk, ClipKind.CastFire, ClipKind.CastLightning, ClipKind.CastMagic, ClipKind.Die];

        private void HeroClass(HeroClassDef h)
        {
            if (h.StartWeapons.Count == 0) Err("hero_classes", h.Id, ".start_weapons", "시작 무기가 없다(13.5-21)");
            for (var i = 0; i < h.StartWeapons.Count; i++)
            {
                var w = h.StartWeapons[i];
                if (!w.IsBound) continue;
                if (!AllowsClass(w.Def.Classes, h.Id))
                    Err("hero_classes", h.Id, $".start_weapons[{i}]", $"시작 무기 {w.Id} 를 이 직업이 고를 수 없다(classes)");
                if (!Covers(h.Requires, w.Def.Requires))
                    Err("hero_classes", h.Id, $".start_weapons[{i}]", $"시작 무기 {w.Id} 의 requires 가 직업 requires 밖이다(13.5-20)");
            }
            var evoPassives = new HashSet<string>(defs.Evolutions.All.Select(e => e.Passive.Id), StringComparer.Ordinal);
            for (var i = 0; i < h.Traits.Count; i++)
            {
                var t = h.Traits[i];
                if (!t.IsBound) continue;
                var p = t.Def;
                if (!p.Trait) Err("hero_classes", h.Id, $".traits[{i}]", $"특성 {p.Id} 가 trait: true 가 아니다");
                if (p.MaxLevel != 1) Err("hero_classes", h.Id, $".traits[{i}]", $"특성 {p.Id} 의 max_level 이 1 이 아니다({p.MaxLevel})");
                if (evoPassives.Contains(p.Id)) Err("hero_classes", h.Id, $".traits[{i}]", $"특성 {p.Id} 가 진화의 짝 패시브다");
                if (!AllowsClass(p.Classes, h.Id)) Err("hero_classes", h.Id, $".traits[{i}]", $"특성 {p.Id} 를 이 직업이 가질 수 없다(classes)");
            }
            if (h.Radius > 14f) Err("hero_classes", h.Id, ".radius", $"직업 radius {h.Radius} > 14(13.5-5 길 검사 등급)");
            foreach (var b in h.Requires)
                if (b == ContentBundle.Hfvoice) Err("hero_classes", h.Id, ".requires", "requires 에 hfvoice 는 쓰지 않는다(13.5-20)");
            if (!h.Sprites.IsBound) return;
            var set = h.Sprites.Def;
            foreach (var clip in HeroClips)
                if (set.Clips.Get(clip) is null) Err("hero_classes", h.Id, ".sprites", $"그림 묶음 {set.Id} 에 '{Snake(clip)}' 클립이 없다(13.5-21)");
            if (h.StartWeapons.Any(w => w.IsBound && w.Def.CastClip == ClipKind.Attack) && set.Clips.Get(ClipKind.Attack) is null)
                Err("hero_classes", h.Id, ".sprites", $"시작 무기가 attack 클립을 쓰는데 그림 묶음 {set.Id} 에 attack 이 없다(13.5-21)");
        }

        private static bool AllowsClass(IReadOnlyList<DefRef<HeroClassDef>> classes, string id) =>
            classes.Count == 0 || classes.Any(c => c.Id == id);

        // ── 스테이지(액트) ───────────────────────────────

        private void Stage(StageDef s)
        {
            Light(s);
            Rooms(s);
            Intervals(s);
            foreach (var b in s.Requires)
                if (b == ContentBundle.Hfvoice) Err("stages", s.Id, ".requires", "requires 에 hfvoice 는 쓰지 않는다(13.5-20)");
        }

        /// <summary>13.5-15 의 380·화면 반폭 쪽: 정의 값과 <c>light_change</c> 가 넣는 <c>hero_light.radius</c> 값 모두.</summary>
        private void Light(StageDef s)
        {
            void Check(float r, string sub)
            {
                if (r < MinHeroLight || r > MaxHeroLight)
                    Err("stages", s.Id, sub, $"{s.Id}: 주인공 빛 반경 {r} 가 {MinHeroLight}~{MaxHeroLight}(화면 반폭) 밖이다(13.5-15)");
            }
            Check(s.Lighting.HeroLight.Radius, ".lighting.hero_light.radius");
            for (var i = 0; i < s.Events.Count; i++)
                if (s.Events[i].Params is LightChangeParams lc)
                    for (var j = 0; j < lc.Set.Count; j++)
                        if (lc.Set[j].Path == "hero_light.radius" && lc.Set[j].Value.Length > 0)
                            Check(lc.Set[j].Value[0], $".events[{i}].set[{j}]");
        }

        /// <summary>13.5-23 ①②.</summary>
        private void Rooms(StageDef s)
        {
            var rooms = s.Rooms;
            if (rooms.Count == 0)
            {
                Err("stages", s.Id, ".rooms", "방 순번 표(rooms)가 비었다(13.5-23 ①)");
                return;
            }
            var normals = rooms.Count(r => r.Kind == RoomKind.Normal);
            if (!s.Slice && (normals < 6 || normals > 8)) Err("stages", s.Id, ".rooms", $"일반 방이 {normals}개다(6~8, 13.5-23 ①)");
            if (rooms[0].Kind != RoomKind.Normal) Err("stages", s.Id, ".rooms[0].kind", "1번 방은 일반 방이어야 한다(13.5-23 ①)");
            for (var i = 0; i < rooms.Count; i++)
                if (rooms[i].Kind == RoomKind.Boss && i != rooms.Count - 1)
                    Err("stages", s.Id, $".rooms[{i}].kind", "보스 방은 마지막 하나뿐이어야 한다(13.5-23 ①)");
            if (!s.Slice && rooms[^1].Kind != RoomKind.Boss) Err("stages", s.Id, ".rooms", "보스 방이 없다(13.5-23 ①, slice 가 아니면)");

            var expect = 0f;
            var prevNumber = 0;
            for (var i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                var at = $".rooms[{i}]";
                if (r.Number <= prevNumber) Err("stages", s.Id, at + ".number", $"방 번호 {r.Number} 가 앞 번호 {prevNumber} 보다 크지 않다(13.5-23 ①)");
                else if (!s.Slice && r.Number != i + 1) Err("stages", s.Id, at + ".number", $"방 번호 {r.Number} ≠ 배열 자리 + 1 = {i + 1}(slice 가 아니면)");
                prevNumber = r.Number;

                if (MathF.Abs(r.From.Seconds - expect) > 1e-3f)
                {
                    if (r.From.Seconds < expect) Err("stages", s.Id, at + ".from", $"t 범위가 겹친다(from {r.From} < 앞 방 끝 {new ClockTime(expect)})");
                    else if (!s.Slice) Err("stages", s.Id, at + ".from", $"t 범위에 빈틈이 있다(앞 방 끝 {new ClockTime(expect)} → from {r.From})");
                }
                if (r.Kind == RoomKind.Boss)
                {
                    if (r.To is not null) Err("stages", s.Id, at + ".to", "보스 방은 from 만 적는다");
                    if (r.Boss is null) Err("stages", s.Id, at + ".boss", "보스 방에 boss 가 없다");
                    else if (r.Boss.IsBound && r.Boss.Def.Rank != DefRank.Boss) Err("stages", s.Id, at + ".boss", $"boss {r.Boss.Id} 가 rank: boss 가 아니다");
                    if (r.Boss is { } boss && !s.Events.Any(e => e.Type == "boss" && e.ReferencedEnemies.Any(x => x.Id == boss.Id)))
                        Err("stages", s.Id, at + ".boss", $"보스 방의 boss {boss.Id} 를 내는 boss 이벤트가 없다(13.5-23 ①)");
                    expect = r.From.Seconds;
                    continue;
                }
                if (r.To is not { } to)
                {
                    Err("stages", s.Id, at + ".to", "일반·정예 방은 to 가 있어야 한다");
                    expect = r.From.Seconds;
                    continue;
                }
                if (to.Seconds <= r.From.Seconds) Err("stages", s.Id, at + ".to", $"to {to} 가 from {r.From} 보다 크지 않다");
                if (r.Kind == RoomKind.Normal)
                {
                    var len = to.Seconds - r.From.Seconds;
                    if (len < 60f - 1e-3f || len > 120f + 1e-3f) Err("stages", s.Id, at, $"일반 방 생존 {new ClockTime(len)} 가 1:00~2:00 밖이다(13.5-23 ①)");
                }
                else if (r.Kind == RoomKind.Elite)
                {
                    if (r.Miniboss is null) Err("stages", s.Id, at + ".miniboss", "정예 방에 miniboss 가 없다(13.5-23 ①)");
                    else if (r.Miniboss.IsBound && r.Miniboss.Def.Rank != DefRank.Miniboss) Err("stages", s.Id, at + ".miniboss", $"miniboss {r.Miniboss.Id} 가 rank: miniboss 가 아니다");
                }
                expect = to.Seconds;
            }

            // floor: 모두 적거나 모두 안 적음, ≥ 1, 줄지 않음(v0.8)
            var floors = rooms.Count(r => r.Floor != 0);
            if (floors != 0 && floors != rooms.Count) Err("stages", s.Id, ".rooms", "floor 는 모든 방에 적거나 모두 안 적는다(13.5-23 ①)");
            else if (floors == rooms.Count)
                for (var i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].Floor < 1) Err("stages", s.Id, $".rooms[{i}].floor", "floor 는 1 이상이다");
                    else if (i > 0 && rooms[i].Floor < rooms[i - 1].Floor) Err("stages", s.Id, $".rooms[{i}].floor", "floor 가 줄었다");
                }

            // ② 방 맵 풀
            void Pool(RoomKind kind, IReadOnlyList<DefRef<MapDef>> pool, string sub)
            {
                if (!rooms.Any(r => r.Kind == kind)) return;
                if (pool.Count == 0) Err("stages", s.Id, ".room_maps" + sub, $"순번 표에 {Snake(kind)} 방이 있는데 맵 풀이 비었다(13.5-23 ②)");
                for (var i = 0; i < pool.Count; i++)
                    if (pool[i].IsBound && !pool[i].Def.RoomKinds.Contains(kind))
                        Err("stages", s.Id, $".room_maps{sub}[{i}]", $"맵 {pool[i].Id} 의 room_kinds 에 {Snake(kind)} 이 없다(13.5-23 ②)");
            }
            Pool(RoomKind.Normal, s.RoomMaps.Normal, ".normal");
            Pool(RoomKind.Elite, s.RoomMaps.Elite, ".elite");
            if (rooms.Any(r => r.Kind == RoomKind.Boss))
            {
                if (s.RoomMaps.Boss is not { } bm) Err("stages", s.Id, ".room_maps.boss", "보스 방이 있는데 보스 맵이 없다(13.5-23 ②)");
                else if (bm.IsBound && !bm.Def.RoomKinds.Contains(RoomKind.Boss)) Err("stages", s.Id, ".room_maps.boss", $"맵 {bm.Id} 의 room_kinds 에 boss 가 없다(13.5-23 ②)");
            }

            // ④ 문 보상(액트 쪽)
            if (s.Rewards.Weights.Count(w => w.Weight > 0) < 2) Err("stages", s.Id, ".rewards.weights", "가중 > 0 인 보상이 둘 이상이어야 한다(출구 문 둘, 13.5-23 ④)");
            if (s.Rewards.FirstRoom is null) Err("stages", s.Id, ".rewards.first_room", "first_room 이 없다(13.5-23 ④)");
        }

        /// <summary>13.5-3: 구간이 0:00 부터 보스 방 t 시작(보스 방이 없으면 마지막 방 t 끝)까지 빈틈·겹침 없이, 가중치 합 > 0, during: boss 는 보스 방이 있을 때만.</summary>
        private void Intervals(StageDef s)
        {
            var hasBoss = s.Rooms.Any(r => r.Kind == RoomKind.Boss);
            float? end = hasBoss ? s.Rooms.First(r => r.Kind == RoomKind.Boss).From.Seconds
                : s.Rooms.Count > 0 && s.Rooms[^1].To is { } last ? last.Seconds : null;
            var timed = new List<(int Index, float From, float To)>();
            for (var i = 0; i < s.SpawnIntervals.Count; i++)
            {
                var iv = s.SpawnIntervals[i];
                var at = $".spawn_intervals[{i}]";
                if (iv.Weights.Sum(w => w.Weight) <= 0f) Err("stages", s.Id, at + ".weights", "가중치 합이 0 이다(13.5-3)");
                if (iv.During is { } during)
                {
                    if (during != "boss") Err("stages", s.Id, at + ".during", $"during 은 \"boss\" 만 된다('{during}')");
                    else if (!hasBoss) Err("stages", s.Id, at + ".during", "during: boss 는 보스 방이 있을 때만 쓴다(13.5-3)");
                    if (iv.From is not null || iv.To is not null) Err("stages", s.Id, at, "during 구간에는 from·to 를 적지 않는다");
                    continue;
                }
                if (iv.From is not { } f || iv.To is not { } t)
                {
                    Err("stages", s.Id, at, "구간에 from·to(또는 during)가 있어야 한다");
                    continue;
                }
                if (t.Seconds <= f.Seconds) Err("stages", s.Id, at + ".to", $"to {t} 가 from {f} 보다 크지 않다");
                timed.Add((i, f.Seconds, t.Seconds));
            }
            if (timed.Count == 0)
            {
                if (s.Rooms.Count > 0) Err("stages", s.Id, ".spawn_intervals", "생성 구간이 없다(13.5-3)");
                return;
            }
            timed.Sort((a, b) => a.From.CompareTo(b.From));
            var expect = 0f;
            foreach (var (index, from, to) in timed)
            {
                if (MathF.Abs(from - expect) > 1e-3f)
                    Err("stages", s.Id, $".spawn_intervals[{index}].from",
                        from < expect ? $"구간이 겹친다(from {new ClockTime(from)} < 앞 끝 {new ClockTime(expect)})" : $"구간에 빈틈이 있다({new ClockTime(expect)} → {new ClockTime(from)})");
                expect = Math.Max(expect, to);
            }
            if (end is { } e && expect + 1e-3f < e)
                Err("stages", s.Id, ".spawn_intervals", $"구간이 {new ClockTime(expect)} 에서 끝난다 — {(hasBoss ? "보스 방 t 시작" : "마지막 방 t 끝")} {new ClockTime(e)} 까지 덮어야 한다(13.5-3)");
        }

        // ── 13.5-20 자원 묶음 ─────────────────────────────

        private void Bundles()
        {
            foreach (var s in defs.Stages.All)
            {
                foreach (var e in StageReach.Enemies(s, defs))
                    if (!Covers(s.Requires, e.Requires))
                        Err("stages", s.Id, ".requires", $"스테이지 {s.Id} 가 내는 적 {e.Id} 의 requires [{Names(e.Requires)}] 가 스테이지 requires [{Names(s.Requires)}] 밖이다(13.5-20)");
                if (s.NextAct is { IsBound: true } next && s.Requires.Count == 0 && next.Def.Requires.Count > 0)
                    Err("stages", s.Id, ".next_act", $"requires 가 빈 액트의 next_act {next.Id} 가 묶음 액트다(13.5-20)");
                if (defs.SoundBindings.Music.TryGetValue(s.Id, out var m) && m.IsBound && !Covers(s.Requires, m.Def.Requires) && m.Def.Fallback is null)
                    Err("stages", s.Id, "", $"스테이지 음악 {m.Id} 의 requires 가 스테이지 밖인데 fallback 이 없다(13.5-20)");
            }
            NextActCycles();
            if (defs.Rules.Hero?.Class is { IsBound: true } hc && hc.Def.Requires.Count > 0)
                RuleErr("hero", ".class", $"rules.hero.class {hc.Id} 는 기본 직업이어야 한다(requires 가 빔, 13.5-20)");
            foreach (var m in defs.Music.All)
            {
                if (m.Fallback is not { IsBound: true } fb) continue;
                if (fb.Def.Requires.Count > 0) Err("music", m.Id, ".fallback", $"fallback {fb.Id} 의 requires 가 비지 않았다(13.5-20)");
                if (fb.Def.Fallback is not null) Err("music", m.Id, ".fallback", $"fallback {fb.Id} 이 다시 fallback 을 가진다(13.5-20)");
            }
            foreach (var w in defs.Weapons.All)
                if (w.Requires.Contains(ContentBundle.Hfvoice)) Err("weapons", w.Id, ".requires", "requires 에 hfvoice 는 쓰지 않는다(13.5-20)");
            foreach (var m in defs.Music.All)
                if (m.Requires.Contains(ContentBundle.Hfvoice)) Err("music", m.Id, ".requires", "requires 에 hfvoice 는 쓰지 않는다(13.5-20)");
        }

        private void NextActCycles()
        {
            foreach (var s in defs.Stages.All)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal) { s.Id };
                for (var cur = s; cur.NextAct is { IsBound: true } n; cur = n.Def)
                    if (!seen.Add(n.Id))
                    {
                        Err("stages", s.Id, ".next_act", $"next_act 사슬이 고리를 이룬다({string.Join(" → ", seen)} → {n.Id})");
                        break;
                    }
            }
        }

        // ── 13.5-23 ④ 보상 쪽 ─────────────────────────────

        private void Rewards()
        {
            foreach (var r in defs.Rewards.All)
            {
                if (r.Object.IsBound && r.Object.Def.Kind != PickupKind.Reward)
                    Err("rewards", r.Id, ".object", $"보상 물체 {r.Object.Id} 의 kind 가 reward 가 아니다(13.5-23 ④)");
                if (r.Shrine is { PickWeapon: > 0, PickMaxLevel: < 1 })
                    Err("rewards", r.Id, ".shrine.pick_max_level", "성소의 pick_max_level 은 1 이상이다(13.5-23 ④)");
                if (r.Requires.Contains(ContentBundle.Hfvoice)) Err("rewards", r.Id, ".requires", "requires 에 hfvoice 는 쓰지 않는다(13.5-20)");
            }
            if (defs.Rules.Rooms is { } rooms)
            {
                var gates = new HashSet<string>(RoomMapRules.Gates.Select(g => g.Id), StringComparer.Ordinal);
                void Gate(string name, string sub)
                {
                    if (!gates.Contains(name)) RuleErr("rooms", sub, $"문 이름 '{name}' 이 문 넷({string.Join("·", gates)}) 안에 없다(13.5-23 ④)");
                }
                for (var i = 0; i < rooms.Gates.Entry.Count; i++) Gate(rooms.Gates.Entry[i], $".gates.entry[{i}]");
                for (var i = 0; i < rooms.Gates.Exits.Count; i++) Gate(rooms.Gates.Exits[i], $".gates.exits[{i}]");
                Gate(rooms.Gates.BossEntry, ".gates.boss_entry");
                Gate(rooms.Gates.SingleExit, ".gates.single_exit");

                // 시작 칸 수 + stairs × (액트 수 − 1) ≤ 6
                var acts = Math.Max(1, ActCount());
                var lv = defs.Rules.LevelUp ?? new LevelUpRulesDef();
                if (lv.WeaponSlots + rooms.Stairs.WeaponSlots * (acts - 1) > WeaponSlots.SlotCount)
                    RuleErr("rooms", ".stairs.weapon_slots", $"무기 칸 {lv.WeaponSlots} + {rooms.Stairs.WeaponSlots} × (액트 {acts} − 1) > {WeaponSlots.SlotCount}(13.5-23 ④)");
                if (lv.PassiveSlots + rooms.Stairs.PassiveSlots * (acts - 1) > PassiveSlots.SlotCount)
                    RuleErr("rooms", ".stairs.passive_slots", $"패시브 칸 {lv.PassiveSlots} + {rooms.Stairs.PassiveSlots} × (액트 {acts} − 1) > {PassiveSlots.SlotCount}(13.5-23 ④)");
            }
            if (defs.Rules.LevelUp is { Temper: { } temper } lu && !lu.Fillers.Any(f => f.Id == temper))
                RuleErr("level_up", ".temper", $"temper '{temper}' 가 fillers 에 없다{IdSuggest.Hint(temper, lu.Fillers.Select(f => f.Id))}(13.5-23 ④)");
        }

        /// <summary>뿌리 액트(아무도 next_act 로 가리키지 않는 액트)부터 사슬 길이의 최댓값.</summary>
        private int ActCount()
        {
            var pointed = new HashSet<string>(defs.Stages.All.Where(s => s.NextAct is not null).Select(s => s.NextAct!.Id), StringComparer.Ordinal);
            var best = 0;
            foreach (var root in defs.Stages.All.Where(s => !pointed.Contains(s.Id)))
            {
                var n = 0;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (StageDef? cur = root; cur is not null && seen.Add(cur.Id); cur = cur.NextAct is { IsBound: true } nx ? nx.Def : null) n++;
                best = Math.Max(best, n);
            }
            return best;
        }

        /// <summary>안 나오는 적(경고): 어느 스테이지에서도 나오지 않는 일반 적. 스테이지가 하나도 없으면 보지 않는다.</summary>
        private void UnreachableEnemies()
        {
            if (defs.Stages.Count == 0) return;
            var reach = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in defs.Stages.All)
                foreach (var e in StageReach.Enemies(s, defs)) reach.Add(e.Id);
            foreach (var e in defs.Enemies.All)
                if (!reach.Contains(e.Id)) Warn("enemies", e.Id, "", $"적 {e.Id} 가 어느 스테이지에서도 나오지 않는다");
        }

        private static bool Covers(IReadOnlyList<ContentBundle> outer, IReadOnlyList<ContentBundle> inner)
        {
            foreach (var b in inner)
                if (!outer.Contains(b)) return false;
            return true;
        }

        private static string Names(IReadOnlyList<ContentBundle> list) => string.Join(", ", list.Select(ContentBundles.Name));

        private static string Snake<T>(T value) where T : struct, Enum => System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
    }
}
