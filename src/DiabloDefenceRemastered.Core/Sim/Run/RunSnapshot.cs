using System.Globalization;
using System.Text;
using System.Text.Json;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>
/// 계단 저장 한 칸(v0.8 → v0.8.1, 설계서 3.6 결정 4 "계단 저장"·8.3 — 계약·구현 WP1f, 파일 <c>run.json</c> 은 WP7). STJ 로 그대로 쓰고 읽는다(저장 파일 모양 = 이 record).
/// <see cref="RunSession.Suspend"/> 가 계단 문으로 나간 틱의 정산 뒤에 만들고, 이어 하기 생성자가 다음 액트 1번 방부터 잇는다 — 이어 한 판 = 끊지 않은 판.
/// <list type="bullet">
/// <item><see cref="Carry"/> 는 <see cref="IHeroCarry"/> 가 없는 판(WP3 없는 시험)에서만 null — 등록 여부와 null 여부가 다르면 불일치.</item>
/// <item><see cref="Format"/>: 짐·<see cref="RunStats"/>·<see cref="FairnessCounters"/>·<see cref="RoomRecord"/>·<see cref="RunDepth"/>·이 record 의 모양이 바뀌면 <see cref="CurrentFormat"/> 을 올린다
/// (모양 지문 시험이 지킴). 다른 <see cref="Format"/> 의 저장은 버린다(R41 — "없는 값 = 기본" 규칙을 두지 않음).</item>
/// </list>
/// </summary>
/// <param name="NextActRoomsHash">다음 액트 <c>rooms</c> 표 해시(<see cref="RoomsHash"/> — 8.3 깊이 기록과 같은 식).</param>
public sealed record RunSnapshot(int Format, ulong Seed, string StartActId, string? HeroClassId, string NextActId, int NextActNumber, string NextActRoomsHash,
                                 HeroCarry? Carry, RunStats Stats, FairnessCounters Fairness, int ElapsedTicks, RunDepth Depth, IReadOnlyList<RoomRecord> Rooms)
{
    /// <summary>지금 저장 모양의 판(v0.8.1). 모양 지문 시험의 기대 값(<see cref="ShapeFingerprint"/>)과 함께 올린다.</summary>
    public const int CurrentFormat = 1;

    /// <summary>
    /// <see cref="CurrentFormat"/> 의 모양 지문(v0.8.1, R41) — <see cref="ComputeShapeFingerprint"/> 가 이 값이어야 한다(시험). WP3·WP4 가 <c>RunStats</c> 조각에 멤버를 더하거나
    /// 짐 모양을 바꾸면 계산 값이 달라져 시험이 깨진다 → 고치는 사람이 <see cref="CurrentFormat"/> 을 올리고 이 값을 새 계산 값으로 바꾼다(옛 저장은 버려진다).
    /// </summary>
    public const string ShapeFingerprint = "7c149bd4";   // v0.8.2 검증 반영: 열거형 JSON 이름을 더해 다시 계산(CurrentFormat 1 그대로 — 아직 저장된 판 없음)

    /// <summary>모양 지문에 드는 형(<see cref="RunSnapshot"/> 에서 닿는 형 전부).</summary>
    public static IReadOnlyList<Type> ShapeTypes { get; } =
        [typeof(RunSnapshot), typeof(HeroCarry), typeof(CarriedSlot), typeof(CarriedStat), typeof(CarriedEffect),
         typeof(RunStats), typeof(FairnessCounters), typeof(RoomRecord), typeof(RunDepth)];

    /// <summary>
    /// 모양 지문 계산: <see cref="ShapeTypes"/> 마다 공개 인스턴스 필드·속성의 (형 이름, 멤버 이름, 멤버 형)과,
    /// v0.8.2 검증 반영: 그 멤버 형에서 닿는 열거형(배열·제네릭 인자·nullable 안 포함 — <c>StatKey</c> 등)마다 값의 JSON 이름 목록(<see cref="JsonOptions"/> 로 쓴 문자열)을
    /// 서수로 정렬해 이은 문자열의 <c>StableHash</c>(16진 8자리). 열거형 값을 없애거나 이름을 바꿔도 지문이 바뀐다(옛 저장이 <c>FromJson</c> 에서 깨지기 전에 시험이 잡음).
    /// </summary>
    public static string ComputeShapeFingerprint()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        var lines = new List<string>();
        var enums = new HashSet<Type>();
        foreach (var t in ShapeTypes)
        {
            foreach (var f in t.GetFields(flags))
            {
                lines.Add($"{t.Name}.{f.Name}:{TypeName(f.FieldType)}");
                CollectEnums(f.FieldType, enums);
            }
            foreach (var p in t.GetProperties(flags))
                if (p.GetIndexParameters().Length == 0 && p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), true).Length == 0)
                {
                    lines.Add($"{t.Name}.{p.Name}:{TypeName(p.PropertyType)}");
                    CollectEnums(p.PropertyType, enums);
                }
        }
        foreach (var e in enums)
        {
            var names = new List<string>();
            foreach (var v in Enum.GetValues(e)) names.Add(JsonSerializer.Serialize(v, e, JsonOptions));
            lines.Add($"enum {e.Name}:{string.Join(",", names)}");
        }
        lines.Sort(StringComparer.Ordinal);
        return StableHash.Of(string.Join("\n", lines)).ToString("x8", CultureInfo.InvariantCulture);

        static string TypeName(Type t) =>
            t.IsGenericType ? $"{t.Name}<{string.Join(",", t.GetGenericArguments().Select(TypeName))}>" : t.Name;

        static void CollectEnums(Type t, HashSet<Type> into)
        {
            if (t.IsEnum) into.Add(t);
            else if (t.IsArray) CollectEnums(t.GetElementType()!, into);
            else if (t.IsGenericType)
                foreach (var a in t.GetGenericArguments()) CollectEnums(a, into);
        }
    }

    /// <summary>
    /// 저장 파일 STJ 옵션: 공용 규칙(<see cref="DefJson"/> — snake_case·모르는 속성 오류·열거형 문자열) + <b>필드 포함</b>(<c>RunStats</c> 조각이 공개 필드를 더해도 왕복 — 3.6 "저장 멤버 규칙").
    /// </summary>
    public static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    /// <summary>저장 파일 본문(한 줄 JSON).</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>저장 파일 읽기. 깨진 JSON·<c>required</c> 가 빠진 JSON 은 <see cref="JsonException"/>(앱은 <c>run.json.bad</c> 로 옮김 — 8.3).</summary>
    public static RunSnapshot FromJson(string json) =>
        JsonSerializer.Deserialize<RunSnapshot>(json, JsonOptions) ?? throw new JsonException("계단 저장이 null 이다");

    /// <summary>
    /// 액트 <c>rooms</c> 표 해시(8.3 "깊이 모양" — 각 원소의 (<c>kind</c>, <c>number</c>, <c>from</c>, <c>to</c>)만, 맵·보상 가중을 바꿔도 같음). <c>StableHash</c> 16진 8자리.
    /// 저장 구현(WP7)의 <c>best_depth.rooms_hash</c> 도 이 함수를 쓴다.
    /// </summary>
    public static string RoomsHash(StageDef act)
    {
        ArgumentNullException.ThrowIfNull(act);
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder(act.Rooms.Count * 24);
        sb.Append("rooms/1|");
        foreach (var r in act.Rooms)
            sb.Append((int)r.Kind).Append(',').Append(r.Number.ToString(inv)).Append(',')
              .Append(r.From.Seconds.ToString("R", inv)).Append(',').Append(r.To?.Seconds.ToString("R", inv) ?? "-").Append(';');
        return StableHash.Of(sb.ToString()).ToString("x8", inv);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var o = DefJson.CreateOptions();
        o.IncludeFields = true;
        o.MakeReadOnly(populateMissingResolver: true);
        return o;
    }
}

/// <summary>
/// 이어 하기 정의 불일치(v0.8 → v0.8.1, 3.6 "정의가 안 맞으면"): <c>Format</c> 다름, 시작·다음 액트·직업 없음, <c>rooms_hash</c>·액트 번호 다름, 짐의 정의 ID 없음,
/// <c>DamageByWeapon</c> 길이 ≠ 6, <c>IHeroCarry</c> 등록 여부와 <c>Carry</c> null 여부가 다름, <c>IHeroCarry.Validate</c> 가 이유를 돌려줌. 모두 <c>Restore</c> 전에 본다.
/// 앱은 저장을 버리고(이미 <c>TakeRun</c> 이 지움) 로그 한 줄 + 타이틀 한 줄(8.3).
/// </summary>
public sealed class RunSnapshotMismatchException(string message) : Exception(message);
