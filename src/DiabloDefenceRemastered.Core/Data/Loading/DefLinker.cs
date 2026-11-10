using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DiabloDefenceRemastered.Core.Data.Loading;

/// <summary>
/// 로딩 5단계 (설계서 6.2): 모든 <see cref="DefRef{T}"/> 를 한 번만 채운다. 없는 ID → "비슷한 ID" 제안과 함께 오류, <c>abstract</c> 정의를 가리키면 오류.
/// 같은 걸음에서 부품 매개변수 검사(<see cref="IPartFactory.Validate"/> — 그 원소 위치로 맞춘 <see cref="ValidationContext"/>)와
/// <c>[PartType]</c> 문자열 속성의 등록 여부(13.5-2 의 로더 쪽)를 본다.
/// <c>tilesets</c>·<c>maps</c> 는 <see cref="MapDataReader"/> 가 이미 연결했다.
/// </summary>
internal static class DefLinker
{
    public static void Link(GameDefs defs, Collected collected, PartRegistry registry, List<DataError> errors)
    {
        var index = DefKinds.Index(defs);
        var abstracts = AbstractIds(collected);
        var parts = new List<(PartDef Part, string File, string Path)>();

        void WalkTable<T>(string kind, DefTable<T> table) where T : class, IDef
        {
            foreach (var def in table.All)
            {
                var (file, path) = collected.Where(kind, def.Id);
                DefWalker.Walk(def, path, new Visitor(file, index, abstracts, registry, errors, parts));
            }
        }

        WalkTable("sprites", defs.Sprites);
        WalkTable("sprite_sets", defs.SpriteSets);
        WalkTable("effects", defs.Effects);
        WalkTable("projectiles", defs.Projectiles);
        WalkTable("enemies", defs.Enemies);
        WalkTable("affixes", defs.Affixes);
        WalkTable("buffs", defs.Buffs);
        WalkTable("weapons", defs.Weapons);
        WalkTable("passives", defs.Passives);
        WalkTable("evolutions", defs.Evolutions);
        WalkTable("pickups", defs.Pickups);
        WalkTable("drop_tables", defs.DropTables);
        WalkTable("stages", defs.Stages);
        WalkTable("sounds", defs.Sounds);
        WalkTable("music", defs.Music);
        WalkTable("hero_classes", defs.HeroClasses);
        WalkTable("rewards", defs.Rewards);
        WalkTable("items", defs.ItemBases);
        WalkTable("item_affixes", defs.ItemAffixes);
        WalkTable("merchants", defs.Merchants);

        WalkObject(defs.Rules, collected.Rules, index, abstracts, registry, errors, parts);
        WalkObject(defs.SoundBindings, collected.SoundBindings, index, abstracts, registry, errors, parts);

        // 부품 매개변수 검사는 모든 참조가 채워진 뒤(검사가 DefRef.Def 를 읽어도 되게)
        foreach (var (part, file, path) in parts)
        {
            if (!registry.TryGet(part.Kind, part.Type, out var factory)) continue;   // AllowUnknownParts 로 담은 것
            var ctx = new ValidationContext { Defs = defs, File = file, JsonPath = path };
            try
            {
                factory.Validate(part.Params, ctx);
            }
            catch (Exception ex)
            {
                ctx.Error($"부품 '{part.Type}' 검사가 예외로 멈췄다: {ex.GetType().Name}: {ex.Message}");
            }
            errors.AddRange(ctx.Errors);
        }
    }

    /// <summary><c>rules</c>·<c>sound_bindings</c>: 하위 키마다 그 파일·경로로.</summary>
    private static void WalkObject(object obj, Dictionary<string, RawElement> subkeys, Dictionary<Type, IReadOnlyDictionary<string, IDef>> index,
                                   Dictionary<Type, HashSet<string>> abstracts, PartRegistry registry, List<DataError> errors,
                                   List<(PartDef Part, string File, string Path)> parts)
    {
        var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => JsonNamingPolicy.SnakeCaseLower.ConvertName(p.Name), StringComparer.Ordinal);
        foreach (var (key, el) in subkeys)
            if (props.TryGetValue(key, out var p))
                DefWalker.Walk(p.GetValue(obj), el.Path, new Visitor(el.File, index, abstracts, registry, errors, parts));
    }

    private static Dictionary<Type, HashSet<string>> AbstractIds(Collected collected)
    {
        var result = new Dictionary<Type, HashSet<string>>();
        foreach (var (kind, type) in DefKinds.Types)
            foreach (var el in collected.Elements(kind))
                if (el.Node is JsonObject o && o["abstract"] is JsonValue v && v.TryGetValue<bool>(out var b) && b && JsonMerge.IdOf(o) is { } id)
                {
                    if (!result.TryGetValue(type, out var set)) result[type] = set = new HashSet<string>(StringComparer.Ordinal);
                    set.Add(id);
                }
        return result;
    }

    private sealed class Visitor(string file, Dictionary<Type, IReadOnlyDictionary<string, IDef>> index, Dictionary<Type, HashSet<string>> abstracts,
                                 PartRegistry registry, List<DataError> errors, List<(PartDef Part, string File, string Path)> parts) : DefWalker.IVisitor
    {
        public void DefRef(object defRef, Type target, string path)
        {
            var refType = defRef.GetType();
            if ((bool)refType.GetProperty(nameof(DefRef<IDef>.IsBound))!.GetValue(defRef)!) return;
            var id = (string)refType.GetProperty(nameof(DefRef<IDef>.Id))!.GetValue(defRef)!;
            if (index.TryGetValue(target, out var table) && table.TryGetValue(id, out var def))
            {
                refType.GetMethod(nameof(DefRef<IDef>.Bind))!.Invoke(defRef, [def]);
                return;
            }
            var kind = DefKinds.KindOf(target);
            if (abstracts.TryGetValue(target, out var abs) && abs.Contains(id))
            {
                errors.Add(new DataError(file, path, $"{kind} '{id}' 는 abstract 라 가리킬 수 없다(extends 로만 쓴다)"));
                return;
            }
            var ids = table?.Keys ?? [];
            errors.Add(new DataError(file, path, $"{kind} '{id}' 가 없다{IdSuggest.Hint(id, ids)}"));
        }

        public void Part(PartDef part, string path) => parts.Add((part, file, path));

        public void PartTypeName(PartKind kind, string type, string path)
        {
            if (registry.Contains(kind, type)) return;
            errors.Add(new DataError(file, path, $"{PartReader.PartKindName(kind)} '{type}' 가 등록되지 않았다{IdSuggest.Hint(type, registry.TypesOf(kind))}"));
        }
    }
}
