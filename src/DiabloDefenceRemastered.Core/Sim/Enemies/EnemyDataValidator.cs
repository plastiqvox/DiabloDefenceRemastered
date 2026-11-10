using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 적·스테이지 검사기 (설계서 6.14, WP4 몫). 정의끼리만 보므로 자원 없이 돈다.
/// <list type="bullet">
/// <item>13.5-13 예고: 큰 범위 즉발·돌진은 예고 ≥ <c>min_time</c>(자폭은 <c>fuse</c>), 무거운 근접 단일 대상은 타격 프레임까지 ≥ <c>heavy_melee_windup</c>(오류). 닿는 거리가 넘는 단일 근접은 경고.</item>
/// <item>13.5-14 크기(경고), 부품 슬롯 &gt; 12·여러 틱 동작이 마지막이 아님·쓰는 클립 없음(오류).</item>
/// <item>13.5-15 시야(오류): 스테이지마다 주인공 빛 반경(정의와 <c>light_change</c>) 380 ≤ R ≤ 480, 나오는 <c>kite</c> 적의 띠 최대 + edge + ranged_margin ≤ R.</item>
/// <item>13.5-17 <c>include_bosses: true</c>(경고), 13.5-18 정예 방 리듬(경고), 13.5-23 ⑥ 방 이벤트 자리(<c>RoomEvents.CheckPlacement</c>).</item>
/// </list>
/// 오류 위치의 파일은 비운다 — 로더(WP7)가 정의 → 파일 표로 채운다(<c>RoomEvents.CheckPlacement</c> 와 같은 방식).
/// </summary>
public sealed class EnemyDataValidator : IDataValidator
{
    public string Name => "enemies";

    /// <summary>13.5-15 의 빛 반경 범위(기획 10.5-3 하한, 화면 가로 반폭).</summary>
    public const float MinLight = 380f, MaxLight = 480f;

    public void Validate(GameDefs defs, ValidationContext ctx)
    {
        var rules = defs.Rules.Enemies?.TelegraphRequired ?? new TelegraphRequiredDef();
        var heroRadius = defs.Rules.Hero is { } h && h.Class.IsBound ? h.Class.Def.Radius : 14f;
        for (var i = 0; i < defs.Enemies.Count; i++)
            CheckEnemy(defs, defs.Enemies[i], i, rules, heroRadius, ctx);
        for (var i = 0; i < defs.Stages.Count; i++)
        {
            var stage = defs.Stages[i];
            CheckSight(defs, stage, ctx);
            if (defs.Rules.Rooms is { } rooms)
                foreach (var e in RoomEvents.CheckPlacement(stage, rooms)) ctx.Add(e);
            CheckRhythm(stage, ctx);
        }
    }

    private static void CheckEnemy(GameDefs defs, EnemyDef def, int index, TelegraphRequiredDef rules, float heroRadius, ValidationContext ctx)
    {
        var path = $"$.enemies[{index}]";
        void Err(string msg) => ctx.Add("", path, $"적 '{def.Id}': {msg}");
        void Warn(string msg) => ctx.Add("", path, $"적 '{def.Id}': {msg}", DataSeverity.Warning);

        // 13.5-14 크기
        var maxScale = defs.Rules.Ranks?.MaxScale ?? 1.6f;
        if (def.Scale is < 0.65f or > 1.6f) Warn($"scale {def.Scale} 이 0.65~1.6 밖이다(13.5-14)");
        var rankMul = def.Rank == DefRank.Normal && def.EliteEligible ? defs.Rules.Ranks?.Unique.Scale ?? 1f : 1f;
        if (def.Scale * rankMul > maxScale + 1e-4f) Warn($"scale {def.Scale} × 등급 {rankMul} > max_scale {maxScale} — 생성 때 잘린다(13.5-14)");

        // 슬롯 수(접두사 최대 2개 — 부품을 가진 접두사 중 큰 둘)
        var affixParts = defs.Affixes.All.Select(a => a.Behaviors.Count).OrderByDescending(n => n).Take(2).Sum();
        EnemyKit? kit = null;
        try { kit = EnemyKit.Build(def, index, []); }
        catch (InvalidOperationException ex) { Err(ex.Message); }
        if (kit is not null && kit.Count + (def.Rank == DefRank.Normal ? affixParts : 0) > PartSlots.SlotCount)
            Err($"부품 슬롯 {kit.Count} + 접두사 {affixParts} > {PartSlots.SlotCount}(6.6)");

        var set = def.Sprites.IsBound ? def.Sprites.Def : null;
        var contact = (def.Radius ?? MathF.Round((set?.BaseRadius ?? 12f) * def.Scale)) + heroRadius;
        var animSpeed = def.AnimSpeed > 0f ? def.AnimSpeed : 1f;

        foreach (var pd in def.Behaviors)
        {
            if (pd.Trigger is not null && pd.Instance is IEnemyAction ta) CheckAction(ta, null, def, rules, Err);
            switch (pd.Instance)
            {
                case SelfDestruct sd when Base(sd.Params.Damage, def) >= rules.MinDamage && sd.Params.Fuse < rules.MinTime:
                    Err($"self_destruct 피해 {Base(sd.Params.Damage, def)} — fuse {sd.Params.Fuse}s < {rules.MinTime}s(13.5-13)");
                    break;
                case Charge ch when Base(ch.Params.Damage, def) >= rules.MinDamage && (ch.Params.Telegraph?.Time ?? 0f) < rules.MinTime:
                    Err($"charge 피해 {Base(ch.Params.Damage, def)} — 예고 {ch.Params.Telegraph?.Time ?? 0f}s < {rules.MinTime}s(13.5-13)");
                    break;
                case ChaseMelee cm:
                {
                    var reach = cm.Params.Reach + cm.Params.HitSlack;
                    if (reach > rules.MeleeReachOverContact) Warn($"chase_melee 닿는 거리 여분 {reach} > {rules.MeleeReachOverContact}(6.5)");
                    var dmg = Base(cm.Params.Damage, def);
                    if (dmg >= rules.HeavyMeleeDamage)
                    {
                        var windup = Windup(set, ClipKind.Attack, cm.Params.EventFrame) / animSpeed;
                        if (windup < rules.HeavyMeleeWindup) Err($"chase_melee 피해 {dmg} — 타격까지 {windup:0.00}s < {rules.HeavyMeleeWindup}s(13.5-13)");
                    }
                    break;
                }
                case CommandAura { Params.IncludeBosses: true }:
                case WarCry { Params.IncludeBosses: true }:
                    Warn($"{pd.Type} 의 include_bosses: true — 보스·중간 보스에게도 걸린다(13.5-17)");
                    break;
            }
            if (pd.Instance is ShootProjectile or WarCry && set?.Clips.Get(pd.Instance is ShootProjectile sp ? sp.Params.Clip : ((WarCry)pd.Instance).Params.Clip) is null)
                Err($"{pd.Type} 가 쓰는 클립이 그림 묶음 '{def.Sprites.Id}' 에 없다");
        }

        var patternLists = new List<IReadOnlyList<PatternDef>> { def.Patterns };
        foreach (var ph in def.Phases) patternLists.Add(ph.Patterns);
        foreach (var list in patternLists)
            foreach (var pattern in list)
            {
                if (set is not null && set.Clips.Get(pattern.Clip ?? ClipKind.Attack) is null)
                    Err($"패턴 '{pattern.Id}' 의 클립 {pattern.Clip ?? ClipKind.Attack} 이 그림 묶음 '{def.Sprites.Id}' 에 없다");
                for (var a = 0; a < pattern.Actions.Count; a++)
                {
                    if (pattern.Actions[a].Instance is not IEnemyAction act) continue;
                    if (act.MultiTick && a != pattern.Actions.Count - 1) Err($"패턴 '{pattern.Id}': 여러 틱 동작 '{pattern.Actions[a].Type}' 은 actions 의 마지막에만");
                    CheckAction(act, pattern, def, rules, Err);
                    if (act is MeleeSlam { Params.Radius: null } ms && Base(ms.Params.Damage, def) >= rules.HeavyMeleeDamage)
                    {
                        var windup = Windup(set, pattern.Clip ?? ClipKind.Attack, pattern.EventFrame) / animSpeed;
                        if (windup < rules.HeavyMeleeWindup)
                            Err($"패턴 '{pattern.Id}' 단일 근접 피해 {Base(ms.Params.Damage, def)} — 타격까지 {windup:0.00}s < {rules.HeavyMeleeWindup}s(13.5-13)");
                        if (ms.Params.Reach > rules.MeleeReachOverContact)
                            Warn($"패턴 '{pattern.Id}' 단일 근접 닿는 거리 여분 {ms.Params.Reach} > {rules.MeleeReachOverContact}(6.5)");
                    }
                }
            }
        _ = contact;
    }

    /// <summary>예고 필수 동작(범위 즉발·돌진)의 예고 시간. 패턴 예고가 있으면 그것, 없으면 동작 자체의 예고.</summary>
    private static void CheckAction(IEnemyAction act, PatternDef? pattern, EnemyDef def, TelegraphRequiredDef rules, Action<string> err)
    {
        var where = pattern is null ? "trigger 동작" : $"패턴 '{pattern.Id}'";
        switch (act)
        {
            case MeleeSlam { Params.Radius: not null } ms when Base(ms.Params.Damage, def) >= rules.MinDamage:
                if ((pattern?.Telegraph?.Time ?? 0f) < rules.MinTime)
                    err($"{where} 범위 내려치기 피해 {Base(ms.Params.Damage, def)} — 예고 {pattern?.Telegraph?.Time ?? 0f}s < {rules.MinTime}s(13.5-13)");
                break;
            case TelegraphedBlast tb when Base(tb.Params.Damage, def) >= rules.MinDamage:
                var t = MathF.Max(tb.Params.Telegraph?.Time ?? 0f, pattern?.Telegraph?.Time ?? 0f);
                if (t < rules.MinTime) err($"{where} telegraphed_blast 피해 {Base(tb.Params.Damage, def)} — 예고 {t}s < {rules.MinTime}s(13.5-13)");
                break;
            case Dash d when Base(d.Params.Damage, def) >= rules.MinDamage:
                var dt = MathF.Max(d.Params.Telegraph?.Time ?? 0f, pattern?.Telegraph?.Time ?? 0f);
                if (dt < rules.MinTime) err($"{where} dash 피해 {Base(d.Params.Damage, def)} — 예고 {dt}s < {rules.MinTime}s(13.5-13)");
                break;
        }
    }

    private static float Base(float? damage, EnemyDef def) => damage ?? def.Damage;

    /// <summary>공격 시작부터 이벤트 프레임까지(초, 배율 전) = (event_frame − 1) × 프레임 시간.</summary>
    private static float Windup(SpriteSetDef? set, ClipKind clip, int? eventFrame)
    {
        var c = set?.Clips.Get(clip);
        if (c is null) return 0f;
        var f = eventFrame ?? c.EventFrame ?? 0;
        return f >= 1 ? (f - 1) * (c.FrameTime ?? set!.FrameTime) : 0f;
    }

    /// <summary>13.5-15: 빛 반경 범위와 나오는 <c>kite</c> 적의 띠.</summary>
    private static void CheckSight(GameDefs defs, StageDef stage, ValidationContext ctx)
    {
        var radii = new List<float> { stage.Lighting.HeroLight.Radius };
        foreach (var e in stage.Events)
            if (e.Params is LightChangeParams lc)
                foreach (var s in lc.Set)
                    if (s.Path == "hero_light.radius" && s.Value.Length == 1) radii.Add(s.Value[0]);
        foreach (var r in radii)
            if (r < MinLight || r > MaxLight)
                ctx.Add("", "$.lighting", $"스테이지 '{stage.Id}': 주인공 빛 반경 {r} 이 {MinLight}~{MaxLight} 밖이다(13.5-15)");
        var sight = defs.Rules.Sight;
        if (sight is null) return;
        var need = 0f;
        string? who = null;
        foreach (var enemy in StageReach.Enemies(stage, defs))
            foreach (var pd in enemy.Behaviors)
                if (pd.Instance is Kite k && k.Max + sight.Edge + sight.RangedMargin > need)
                {
                    need = k.Max + sight.Edge + sight.RangedMargin;
                    who = enemy.Id;
                }
        foreach (var r in radii)
            if (who is not null && r < need)
                ctx.Add("", "$.lighting", $"스테이지 '{stage.Id}': 빛 반경 {r} < '{who}' kite 띠 최대 + edge + ranged_margin = {need}(13.5-15)");
    }

    /// <summary>13.5-18(경고, 방 판): 정예 방끼리·마지막 정예 방과 보스 방 사이 일반 방 1개 미만, 정예 방 t 시작끼리 2:00 미만, 정예 방 구간이 바로 앞 구간보다 큼.</summary>
    private static void CheckRhythm(StageDef stage, ValidationContext ctx)
    {
        var rooms = stage.Rooms;
        var lastElite = -1;
        for (var i = 0; i < rooms.Count; i++)
        {
            var r = rooms[i];
            if (r.Kind == RoomKind.Normal) continue;
            if (lastElite >= 0)
            {
                var normals = 0;
                for (var j = lastElite + 1; j < i; j++) if (rooms[j].Kind == RoomKind.Normal) normals++;
                if (normals < 1) ctx.Add("", $"$.rooms[{i}]", $"스테이지 '{stage.Id}': 방 {r.Number} 앞 정예 방과의 사이에 일반 방이 없다(13.5-18)", DataSeverity.Warning);
                if (r.Kind == RoomKind.Elite && r.From.Seconds - rooms[lastElite].From.Seconds < 120f)
                    ctx.Add("", $"$.rooms[{i}]", $"스테이지 '{stage.Id}': 정예 방 t 시작 간격 < 2:00(13.5-18)", DataSeverity.Warning);
            }
            if (r.Kind == RoomKind.Elite)
            {
                lastElite = i;
                CheckEliteIntervals(stage, i, ctx);
            }
        }
    }

    private static void CheckEliteIntervals(StageDef stage, int roomIndex, ValidationContext ctx)
    {
        var room = stage.Rooms[roomIndex];
        var from = room.From.Seconds;
        var to = room.To?.Seconds ?? from;
        SpawnIntervalDef? before = null;
        foreach (var iv in stage.SpawnIntervals)
        {
            if (iv.During is not null) continue;
            var a = iv.From?.Seconds ?? 0f;
            var b = iv.To?.Seconds ?? float.PositiveInfinity;
            if (b <= from && (before is null || (before.To?.Seconds ?? 0f) < b)) before = iv;
        }
        if (before is null) return;
        foreach (var iv in stage.SpawnIntervals)
        {
            if (iv.During is not null) continue;
            var a = iv.From?.Seconds ?? 0f;
            var b = iv.To?.Seconds ?? float.PositiveInfinity;
            if (b <= from || a >= to) continue;
            if (iv.MaxAlive > before.MaxAlive || iv.PerSecond > before.PerSecond)
                ctx.Add("", "$.spawn_intervals", $"스테이지 '{stage.Id}': 정예 방 {room.Number} 의 구간({iv.From}~{iv.To})이 바로 앞 구간보다 크다(13.5-18)", DataSeverity.Warning);
        }
    }
}
