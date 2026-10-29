namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>ID 를 가진 정의. 같은 종류 안에서 ID 는 유일하다(13.5-1).</summary>
public interface IDef
{
    string Id { get; }
}

/// <summary>
/// 다른 정의를 가리키는 참조 (설계서 6.2). JSON 에서는 ID 문자열이다.
/// 로딩 5단계(연결)가 <see cref="Bind"/> 로 한 번만 채운다. 두 번째 채우기·채우기 전 읽기는 예외.
/// 정적 사전이 없으므로 월드·시험이 여러 벌이어도 부딪히지 않는다.
/// </summary>
public sealed class DefRef<T>(string id) where T : class, IDef
{
    private T? _def;

    public string Id { get; } = id;

    public T Def => _def ?? throw new InvalidOperationException($"DefRef<{typeof(T).Name}>('{Id}') 를 연결 전에 읽었다");

    public bool IsBound => _def is not null;

    public void Bind(T def)
    {
        ArgumentNullException.ThrowIfNull(def);
        if (_def is not null) throw new InvalidOperationException($"DefRef<{typeof(T).Name}>('{Id}') 는 이미 연결됐다");
        if (!string.Equals(def.Id, Id, StringComparison.Ordinal))
            throw new ArgumentException($"DefRef<{typeof(T).Name}>('{Id}') 에 다른 정의 '{def.Id}' 를 연결하려 했다", nameof(def));
        _def = def;
    }

    /// <summary>이미 연결된 참조(코드로 정의를 만드는 시험용).</summary>
    public static DefRef<T> To(T def)
    {
        var r = new DefRef<T>(def.Id);
        r.Bind(def);
        return r;
    }

    public override string ToString() => Id;
}
