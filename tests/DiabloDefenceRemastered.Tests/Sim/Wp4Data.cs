using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;
using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// WP4 실제 데이터 읽기(시험 전용 — WP7 로더 전). 실제 <c>Data/</c> 의 WP4 파일(<c>rules/enemies.json</c>·<c>rules/rooms.json</c>·<c>enemies/</c>·<c>affixes.json</c>·<c>buffs.json</c>·<c>stages/</c>)을
/// 실제 맵·타일셋·그림 묶음(WP0a)·<c>rules.sight</c> 와 합쳐 <see cref="GameDefs"/> 로 만든다. 부품은 <see cref="EnemyModule.RegisterParts"/> 레지스트리로 만든다(로더 4단계 흉내).
/// 다른 묶음 소유로 아직 없는 정의(투사체·이펙트·문 보상·획득물)는 <b>시험용 대역</b>을 만들어 연결한다 — 저장소 파일에는 임시 정의를 넣지 않는다(11.3).
/// <c>extends</c>·<c>*_patch</c> 는 하지 않는다(실제 데이터는 쓰지 않음). 페이즈는 로더와 같은 <c>JsonMerge.ResolvePhases</c> 로 푼다(단계 2 — 해골왕). 하위 폴더(<c>enemies/bosses/</c>)와 그 파일의 드롭 표도 읽는다.
/// </summary>
public static class Wp4Data
{
    private static readonly Lazy<Loaded> Cache = new(() => Load());

    public sealed record Loaded(GameDefs Defs, IReadOnlyList<string> StubIds);

    /// <summary>실제 WP4 데이터 + 실제 맵(한 번 읽어 둔다).</summary>
    public static GameDefs Defs => Cache.Value.Defs;

    /// <summary>대역으로 채운 다른 묶음 정의 ID(종류:ID).</summary>
    public static IReadOnlyList<string> Stubs => Cache.Value.StubIds;

    public static JsonSerializerOptions Options { get; } =
        DefJson.CreateOptions(new RefFactory(), new ClockConverter(), new WeightsFactory(), new EffectRefConverter());

    public static Loaded Load()
    {
        var registry = new PartRegistry();
        EnemyModule.RegisterParts(registry);

        // rules: 파일마다 하위 키 단위로 합침
        var rules = new RulesDef();
        foreach (var rel in new[] { "rules/enemies.json", "rules/rooms.json", "rules/sight.json" })
        {
            var r = ((JsonObject)RealData.Read(rel)["rules"]!).Deserialize<RulesDef>(Options)!;
            rules = rules with
            {
                Ranks = r.Ranks ?? rules.Ranks, Enemies = r.Enemies ?? rules.Enemies, Caps = r.Caps ?? rules.Caps, Spawn = r.Spawn ?? rules.Spawn,
                Rooms = r.Rooms ?? rules.Rooms, Sight = r.Sight ?? rules.Sight,
            };
        }

        var sprites = new List<SpriteDef>();
        var sets = new List<SpriteSetDef>();
        foreach (var rel in new[] { "sprites/monsters.json", "sprites/hero.json" })
        {
            var o = RealData.Read(rel);
            if (o["sprites"] is JsonArray s) sprites.AddRange(s.Select(n => n!.Deserialize<SpriteDef>(Options)!));
            if (o["sprite_sets"] is JsonArray ss) sets.AddRange(ss.Select(n => n!.Deserialize<SpriteSetDef>(Options)!));
        }

        var enemies = new List<EnemyDef>();
        var raw = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        var dropTables = new List<DropTableDef>();
        foreach (var file in Directory.GetFiles(Path.Combine(RealData.Dir, "enemies"), "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var root = RealData.Read(Path.GetRelativePath(RealData.Dir, file));
            // 그 적만 쓰는 드롭 표(6.1 — 액트 보스 dt_boss)는 같은 파일에
            if (root["drop_tables"] is JsonArray dts) dropTables.AddRange(dts.Select(n => n!.Deserialize<DropTableDef>(Options)!));
            foreach (var n in (JsonArray)root["enemies"]!)
            {
                raw[(string)n!["id"]!] = (JsonObject)n;
                enemies.Add(ReadEnemy((JsonObject)n, registry));
            }
        }
        // 코드 없는 변형 픽스처(6.13 — Fixtures/variants/): extends 를 시험 안에서 최소로 풀어 같은 표에 넣는다
        foreach (var file in Directory.GetFiles(VariantsDir, "*.json").Order(StringComparer.Ordinal))
            foreach (var n in (JsonArray)((JsonObject)DefJson.ParseNode(File.ReadAllText(file))!)["enemies"]!)
                enemies.Add(ReadEnemy(ResolveVariant((JsonObject)n!, raw), registry));

        var affixes = ((JsonArray)RealData.Read("affixes.json")["affixes"]!).Select(n => ReadAffix((JsonObject)n!, registry)).ToList();
        var buffs = ((JsonArray)RealData.Read("buffs.json")["buffs"]!).Select(n => n!.Deserialize<BuffDef>(Options)!).ToList();
        var stages = new List<StageDef>();
        foreach (var file in Directory.GetFiles(Path.Combine(RealData.Dir, "stages"), "*.json").Order(StringComparer.Ordinal))
            foreach (var n in (JsonArray)RealData.Read(Path.GetRelativePath(RealData.Dir, file))["stages"]!)
                stages.Add(ReadStage((JsonObject)n!, registry));

        var maps = RealData.Maps;
        var linker = new Linker();
        linker.Add(sprites); linker.Add(sets); linker.Add(enemies); linker.Add(affixes); linker.Add(buffs); linker.Add(stages); linker.Add(dropTables);
        linker.Add(maps.Maps); linker.Add(maps.Tilesets);
        foreach (var root in new object[] { rules, sprites, sets, enemies, affixes, buffs, stages, dropTables }) linker.Walk(root);

        var defs = new GameDefs
        {
            Rules = rules,
            Sprites = new DefTable<SpriteDef>(sprites),
            SpriteSets = new DefTable<SpriteSetDef>(sets),
            Tilesets = new DefTable<TilesetDef>(maps.Tilesets),
            Maps = new DefTable<MapDef>(maps.Maps),
            Enemies = new DefTable<EnemyDef>(enemies),
            Affixes = new DefTable<AffixDef>(affixes),
            Buffs = new DefTable<BuffDef>(buffs),
            Stages = new DefTable<StageDef>(stages),
            Projectiles = new DefTable<ProjectileDef>(linker.Stubs<ProjectileDef>()),
            Effects = new DefTable<EffectDef>(linker.Stubs<EffectDef>()),
            Rewards = new DefTable<RewardDef>(linker.Stubs<RewardDef>()),
            Pickups = new DefTable<PickupDef>([.. linker.Stubs<PickupDef>(), .. linker.Stubs<RewardDef>().Where(r => r.Object is not null).Select(r => r.Object!.Def)]),
        };
        return new Loaded(defs, linker.StubNames);
    }

    /// <summary>변형 픽스처 폴더(<c>tests/…/Fixtures/variants/</c>).</summary>
    public static string VariantsDir { get; } = Path.Combine(RealData.RepoRoot(), "tests", "DiabloDefenceRemastered.Tests", "Fixtures", "variants");

    /// <summary>변형 픽스처 ID(시험이 실제 적과 나눠 볼 때).</summary>
    public static IReadOnlyList<string> VariantIds =>
        [.. Directory.GetFiles(VariantsDir, "*.json").Order(StringComparer.Ordinal)
                     .SelectMany(f => ((JsonArray)((JsonObject)DefJson.ParseNode(File.ReadAllText(f))!)["enemies"]!).Select(n => (string)n!["id"]!))];

    /// <summary>
    /// 시험용 최소 <c>extends</c> 풀기(WP7 로더가 정본 — 6.13): 부모 원소를 복사해 자식 속성으로 덮고, <c>behaviors_add</c> 는 뒤에 붙이고,
    /// <c>behaviors_patch</c> 는 같은 <c>id</c> 부품에 속성을 덮는다(자리 지킴). 부모는 실제 적 하나(한 단계).
    /// </summary>
    public static JsonObject ResolveVariant(JsonObject child, IReadOnlyDictionary<string, JsonObject> parents)
    {
        if (child["extends"] is not { } ext) return child;
        var result = (JsonObject)parents[(string)ext!]!.DeepClone();
        var behaviors = result["behaviors"] as JsonArray ?? [];
        result.Remove("behaviors");
        foreach (var (key, value) in child)
        {
            switch (key)
            {
                case "extends": break;
                case "behaviors_add":
                    foreach (var b in (JsonArray)value!) behaviors.Add(b!.DeepClone());
                    break;
                case "behaviors_patch":
                    foreach (var patch in (JsonArray)value!)
                    {
                        var id = (string)patch!["id"]!;
                        var target = behaviors.OfType<JsonObject>().Single(b => (string?)b["id"] == id);
                        foreach (var (pk, pv) in (JsonObject)patch) target[pk] = pv?.DeepClone();
                    }
                    break;
                default:
                    result[key] = value?.DeepClone();
                    break;
            }
        }
        result["behaviors"] = behaviors;
        return result;
    }

    // ── 정의 읽기 ──────────────────────────────────────────────────

    private static readonly string[] PartKeys = ["type", "id", "trigger", "at"];

    public static EnemyDef ReadEnemy(JsonObject n, PartRegistry registry)
    {
        var copy = (JsonObject)n.DeepClone();
        var behaviors = Take(copy, "behaviors");
        var patterns = Take(copy, "patterns");
        var phases = Take(copy, "phases");
        var def = copy.Deserialize<EnemyDef>(Options)!;
        // 페이즈(단계 2 — 해골왕): 로더(WP7)와 같은 풀기(JsonMerge.ResolvePhases)로 페이즈마다 완성 패턴 목록
        var phaseDefs = new List<PhaseDef>();
        if (phases is { Count: > 0 })
        {
            var resolved = JsonMerge.ResolvePhases(patterns, phases, ".phases");
            for (var k = 0; k < phases.Count; k++)
            {
                var ph = (JsonObject)phases[k]!.DeepClone();
                foreach (var key in new[] { "patch", "add", "remove" }) ph.Remove(key);
                phaseDefs.Add(ph.Deserialize<PhaseDef>(Options)! with
                {
                    Patterns = [.. resolved[k].Select(p => ReadPattern((JsonObject)p!, registry))],
                });
            }
        }
        return def with
        {
            Behaviors = behaviors?.Select(b => ReadPart((JsonObject)b!, PartKind.EnemyBehavior, registry)).ToList() ?? [],
            Patterns = patterns?.Select(p => ReadPattern((JsonObject)p!, registry)).ToList() ?? [],
            Phases = phaseDefs,
        };
    }

    public static PatternDef ReadPattern(JsonObject n, PartRegistry registry)
    {
        var copy = (JsonObject)n.DeepClone();
        var actions = Take(copy, "actions");
        return copy.Deserialize<PatternDef>(Options)! with
        {
            Actions = actions?.Select(a => ReadPart((JsonObject)a!, PartKind.EnemyAction, registry)).ToList() ?? [],
        };
    }

    public static AffixDef ReadAffix(JsonObject n, PartRegistry registry)
    {
        var copy = (JsonObject)n.DeepClone();
        var behaviors = Take(copy, "behaviors_add");
        return copy.Deserialize<AffixDef>(Options)! with
        {
            Behaviors = behaviors?.Select(b => ReadPart((JsonObject)b!, PartKind.EnemyBehavior, registry)).ToList() ?? [],
        };
    }

    public static StageDef ReadStage(JsonObject n, PartRegistry registry)
    {
        var copy = (JsonObject)n.DeepClone();
        var events = Take(copy, "events");
        return copy.Deserialize<StageDef>(Options)! with
        {
            Events = events?.Select(e => ReadPart((JsonObject)e!, PartKind.StageEvent, registry)).ToList() ?? [],
        };
    }

    /// <summary>부품 원소 하나(로더 4단계 흉내): type·id·trigger·at 을 빼고 매개변수 형으로, 등록된 생성 함수로 객체. 매개변수 안 DefRef&lt;EnemyDef&gt; 를 모은다.</summary>
    public static PartDef ReadPart(JsonObject n, PartKind kind, PartRegistry registry)
    {
        var type = (string)n["type"]!;
        var id = (string?)n["id"];
        var trigger = (string?)n["trigger"];
        ClockTime? at = n["at"] is { } atNode ? ClockTime.Parse((string)atNode!) : null;
        if (trigger is not null && kind == PartKind.EnemyBehavior) kind = PartKind.EnemyAction;
        if (!registry.TryGet(kind, type, out var factory)) throw new JsonException($"부품 ({kind}, '{type}') 가 등록되지 않았다");
        var body = (JsonObject)n.DeepClone();
        foreach (var k in PartKeys) body.Remove(k);
        var parameters = body.Deserialize(factory.ParamsType, Options) ?? throw new JsonException($"{type}: 매개변수 null");
        var ctx = new ValidationContext();
        factory.Validate(parameters, ctx);
        if (ctx.HasErrors) throw new JsonException($"{type}: {string.Join("; ", ctx.Errors)}");
        var enemies = new List<DefRef<EnemyDef>>();
        Linker.CollectEnemyRefs(parameters, enemies, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return new PartDef(kind, type, id, trigger, parameters, factory.Create(parameters)) { At = at, ReferencedEnemies = enemies };
    }

    private static JsonArray? Take(JsonObject o, string key)
    {
        if (o[key] is not JsonArray a) return null;
        o.Remove(key);
        return a;
    }

    // ── 연결(로더 5단계 흉내) ─────────────────────────────────────

    /// <summary>DefRef 를 반사로 찾아 표에서 연결한다. 표에 없는 다른 묶음 종류는 대역을 만든다(투사체·이펙트·보상·획득물).</summary>
    private sealed class Linker
    {
        private readonly Dictionary<Type, Dictionary<string, IDef>> _tables = [];
        private readonly Dictionary<Type, Dictionary<string, IDef>> _stubs = [];
        private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);

        public List<string> StubNames { get; } = [];

        public void Add<T>(IEnumerable<T> defs) where T : class, IDef
        {
            var t = Table(typeof(T));
            foreach (var d in defs) t[d.Id] = d;
        }

        public IEnumerable<T> Stubs<T>() where T : class, IDef =>
            _stubs.TryGetValue(typeof(T), out var s) ? s.Values.Cast<T>().OrderBy(d => d.Id, StringComparer.Ordinal) : [];

        private Dictionary<string, IDef> Table(Type t) => _tables.TryGetValue(t, out var d) ? d : _tables[t] = new(StringComparer.Ordinal);

        public void Walk(object? o)
        {
            if (o is null || o is string || o.GetType().IsPrimitive || o.GetType().IsEnum || !_seen.Add(o)) return;
            var type = o.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefRef<>))
            {
                Bind(o, type.GetGenericArguments()[0]);
                return;
            }
            if (o is IEnumerable list)
            {
                foreach (var item in list) Walk(item);
                return;
            }
            if (type.Assembly != typeof(EnemyDef).Assembly) return;
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length > 0 || p.Name is "Instance") continue;
                object? v;
                try { v = p.GetValue(o); } catch (TargetInvocationException) { continue; }
                Walk(v);
            }
        }

        private void Bind(object reference, Type t)
        {
            var isBound = (bool)reference.GetType().GetProperty("IsBound")!.GetValue(reference)!;
            if (isBound) return;
            var id = (string)reference.GetType().GetProperty("Id")!.GetValue(reference)!;
            if (!Table(t).TryGetValue(id, out var def)) def = Stub(t, id);
            reference.GetType().GetMethod("Bind")!.Invoke(reference, [def]);
            Walk(def);
        }

        private IDef Stub(Type t, string id)
        {
            if (!_stubs.TryGetValue(t, out var s)) _stubs[t] = s = new(StringComparer.Ordinal);
            if (s.TryGetValue(id, out var existing)) return existing;
            IDef def;
            if (t == typeof(ProjectileDef)) def = new ProjectileDef { Id = id, Sprite = DefRef<SpriteDef>.To(TestDefs.Sprite("stub_" + id)), Radius = 4f, Speed = 260f, Range = 350f };
            else if (t == typeof(EffectDef)) def = new EffectDef { Id = id, Procedural = "stub" };
            else if (t == typeof(PickupDef)) def = TestDefs.RewardObject(id) with { Kind = PickupKind.PotionStack };
            else if (t == typeof(RewardDef))
            {
                var kind = id.Contains("tome", StringComparison.Ordinal) ? RewardKind.WeaponCards : RewardKind.WeaponBundle;
                def = new RewardDef { Id = id, Name = id, Kind = kind, Object = DefRef<PickupDef>.To(TestDefs.RewardObject("obj_" + id)) };
            }
            else if (t == typeof(DropTableDef)) def = new DropTableDef { Id = id };   // 드롭 표(WP3)는 빈 표 — 드롭 없이 돈다
            else throw new InvalidOperationException($"{t.Name} '{id}' 가 없다(대역을 만들지 않는 종류)");
            s[id] = def;
            StubNames.Add($"{t.Name}:{id}");
            return def;
        }

        public static void CollectEnemyRefs(object? o, List<DefRef<EnemyDef>> into, HashSet<object> seen)
        {
            if (o is null || o is string || o.GetType().IsPrimitive || o.GetType().IsEnum || !seen.Add(o)) return;
            if (o is DefRef<EnemyDef> r) { into.Add(r); return; }
            if (o.GetType().IsGenericType && o.GetType().GetGenericTypeDefinition() == typeof(DefRef<>)) return;
            if (o is IEnumerable list) { foreach (var item in list) CollectEnemyRefs(item, into, seen); return; }
            if (o.GetType().Assembly != typeof(EnemyDef).Assembly) return;
            foreach (var p in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (p.GetIndexParameters().Length == 0) CollectEnemyRefs(p.GetValue(o), into, seen);
        }
    }

    // ── 변환기(시험용 — 실제 변환기는 WP7) ─────────────────────────

    private sealed class RefFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(DefRef<>);

        public override JsonConverter CreateConverter(Type t, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(RefConverter<>).MakeGenericType(t.GetGenericArguments()[0]))!;
    }

    private sealed class RefConverter<T> : JsonConverter<DefRef<T>> where T : class, IDef
    {
        public override DefRef<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.String ? new DefRef<T>(reader.GetString()!) : throw new JsonException($"{typeof(T).Name} ID 문자열이 와야 한다");

        public override void Write(Utf8JsonWriter writer, DefRef<T> value, JsonSerializerOptions options) => writer.WriteStringValue(value.Id);
    }

    private sealed class ClockConverter : JsonConverter<ClockTime>
    {
        public override ClockTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.String && ClockTime.TryParse(reader.GetString()!, out var t) ? t : throw new JsonException("시각은 \"mm:ss\"");

        public override void Write(Utf8JsonWriter writer, ClockTime value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }

    private sealed class WeightsFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type t) =>
            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
            && t.GetGenericArguments()[0] is { IsGenericType: true } e && e.GetGenericTypeDefinition() == typeof(WeightEntry<>);

        public override JsonConverter CreateConverter(Type t, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(WeightsConverter<>).MakeGenericType(t.GetGenericArguments()[0].GetGenericArguments()[0]))!;
    }

    private sealed class WeightsConverter<T> : JsonConverter<IReadOnlyList<WeightEntry<T>>> where T : class, IDef
    {
        public override IReadOnlyList<WeightEntry<T>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("가중치는 객체");
            var list = new List<WeightEntry<T>>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var id = reader.GetString()!;
                reader.Read();
                list.Add(new WeightEntry<T>(new DefRef<T>(id), reader.GetSingle()));
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<WeightEntry<T>> value, JsonSerializerOptions options) => throw new NotSupportedException();
    }

    private sealed class EffectRefConverter : JsonConverter<EffectRef>
    {
        private sealed record Body(DefRef<EffectDef> Effect, Rgb? Tint, float? Scale);

        public override EffectRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String) return new EffectRef { Effect = new DefRef<EffectDef>(reader.GetString()!) };
            var b = JsonSerializer.Deserialize<Body>(ref reader, options) ?? throw new JsonException("이펙트 참조 null");
            return new EffectRef { Effect = b.Effect, Tint = b.Tint, Scale = b.Scale };
        }

        public override void Write(Utf8JsonWriter writer, EffectRef value, JsonSerializerOptions options) => throw new NotSupportedException();
    }
}
