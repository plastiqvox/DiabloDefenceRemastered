using System.Diagnostics.CodeAnalysis;

namespace DiabloDefenceRemastered.Core.Data.Registry;

/// <summary>레지스트리 종류. 같은 type 이 종류마다 따로 있을 수 있다(escort, summon, ground_pool …, 설계서 6.2).</summary>
public enum PartKind : byte { EnemyBehavior, EnemyAction, FirePattern, Targeting, StageEvent, WeaponEffect }

/// <summary>정의 속성이 담는 부품 종류. 로더가 이것을 보고 원소를 어느 종류로 찾을지 정한다.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PartKindAttribute(PartKind kind) : Attribute
{
    public PartKind Kind { get; } = kind;
}

/// <summary>
/// 문자열 속성이 부품 <b>type 이름</b>(원소가 아닌 키)임을 표시한다(<c>WeaponDef.Pattern</c>).
/// 로더는 (Kind, 값) 이 등록돼 있는지만 검사한다. 매개변수가 없으므로 그 키에 딸린 값 검사(무기 레벨 표 키·단조성 13.5-2)는
/// 등록한 묶음이 <see cref="IDataValidator"/> 로 한다(WP3 CombatModule).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PartTypeAttribute(PartKind kind) : Attribute
{
    public PartKind Kind { get; } = kind;
}

/// <summary>(종류, type) 하나의 매개변수 형·생성·검사.</summary>
public interface IPartFactory
{
    PartKind Kind { get; }
    string Type { get; }
    /// <summary>"type"·"id"·"trigger" 를 뺀 나머지를 역직렬화할 형.</summary>
    Type ParamsType { get; }
    object Create(object parameters);
    void Validate(object parameters, ValidationContext ctx);
}

/// <summary>
/// 부품 레지스트리 (설계서 2.1·6.2). 키 = (PartKind, type). 부품·동작·발사 패턴·조준·이벤트·무기 효과를 각 묶음 모듈이 등록한다.
/// 로더는 구체 부품을 모른다. 모르는 (종류, type) 은 로딩 오류로 모인다.
/// </summary>
public sealed class PartRegistry
{
    private readonly Dictionary<(PartKind, string), IPartFactory> _map = [];
    private readonly List<IPartFactory> _ordered = [];

    public IReadOnlyList<IPartFactory> All => _ordered;

    public void Register<TParams>(PartKind kind, string type, Func<TParams, object> create,
                                  Action<TParams, ValidationContext>? validate = null)
        => Register(new PartFactory<TParams>(kind, type, create, validate));

    public void Register(IPartFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (string.IsNullOrWhiteSpace(factory.Type)) throw new ArgumentException("부품 type 이 비었다", nameof(factory));
        if (!_map.TryAdd((factory.Kind, factory.Type), factory))
            throw new InvalidOperationException($"부품 ({factory.Kind}, '{factory.Type}') 를 두 번 등록했다");
        _ordered.Add(factory);
    }

    public bool TryGet(PartKind kind, string type, [NotNullWhen(true)] out IPartFactory? factory)
        => _map.TryGetValue((kind, type), out factory);

    public bool Contains(PartKind kind, string type) => _map.ContainsKey((kind, type));

    /// <summary>그 종류에 등록된 type(등록 순서). "비슷한 이름" 제안에 쓴다.</summary>
    public IEnumerable<string> TypesOf(PartKind kind)
    {
        foreach (var f in _ordered)
            if (f.Kind == kind) yield return f.Type;
    }

    private sealed class PartFactory<TParams>(PartKind kind, string type, Func<TParams, object> create,
                                             Action<TParams, ValidationContext>? validate) : IPartFactory
    {
        public PartKind Kind => kind;
        public string Type => type;
        public Type ParamsType => typeof(TParams);
        public object Create(object parameters) => create((TParams)parameters);
        public void Validate(object parameters, ValidationContext ctx) => validate?.Invoke((TParams)parameters, ctx);
    }
}
