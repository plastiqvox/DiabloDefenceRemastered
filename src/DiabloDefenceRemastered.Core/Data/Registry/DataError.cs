namespace DiabloDefenceRemastered.Core.Data.Registry;

public enum DataSeverity : byte { Error, Warning }

/// <summary>
/// 데이터 오류 하나 (설계서 6.2). 오류가 있어도 끝까지 돌아 모두 모은다. 예:
/// <c>enemies/skeleton_mage.json $.enemies[0].behaviors[1].projectile: 'pj_mage_boltt' 가 없다 (혹시 'pj_mage_bolt'?)</c>
/// </summary>
/// <param name="File">Data 폴더 기준 상대 경로(슬래시).</param>
/// <param name="JsonPath">JSON 경로(<c>$.enemies[0].hp</c>). 파일 전체 오류면 <c>$</c>.</param>
public sealed record DataError(string File, string JsonPath, string Message, DataSeverity Severity = DataSeverity.Error)
{
    public override string ToString() =>
        Severity == DataSeverity.Warning ? $"{File} {JsonPath}: (경고) {Message}" : $"{File} {JsonPath}: {Message}";
}

/// <summary>
/// 검사 중 오류를 모으는 곳. 부품 매개변수 검사(<see cref="IPartFactory.Validate"/>)는 로더가 <see cref="File"/>·<see cref="JsonPath"/> 를
/// 그 원소 위치로 맞춰 두고 부르므로 부품 쪽은 <see cref="Error(string)"/> 만 쓰면 된다.
/// </summary>
public sealed class ValidationContext
{
    private readonly List<DataError> _errors = [];

    /// <summary>검사 대상 정의. 연결 전 단계에서는 비어 있을 수 있다.</summary>
    public GameDefs Defs { get; set; } = GameDefs.Empty;

    public string File { get; set; } = "";
    public string JsonPath { get; set; } = "$";

    public IReadOnlyList<DataError> Errors => _errors;
    public bool HasErrors => _errors.Exists(e => e.Severity == DataSeverity.Error);

    public void Error(string message) => Add(File, JsonPath, message, DataSeverity.Error);
    public void Warning(string message) => Add(File, JsonPath, message, DataSeverity.Warning);

    /// <summary>현재 경로 아래 속성(<c>"radius"</c> → <c>$.enemies[0].behaviors[1].radius</c>).</summary>
    public void Error(string property, string message) => Add(File, $"{JsonPath}.{property}", message, DataSeverity.Error);

    public void Add(string file, string jsonPath, string message, DataSeverity severity = DataSeverity.Error)
        => _errors.Add(new DataError(file, jsonPath, message, severity));

    public void Add(DataError error) => _errors.Add(error);
}

/// <summary>등록식 데이터 검사기 (설계서 6.2 7단계). 기하 검사(WP2)·실제 PNG 크기(WP5)처럼 로더가 모르는 검사. 등록 순서대로 부른다.</summary>
public interface IDataValidator
{
    string Name { get; }
    void Validate(GameDefs defs, ValidationContext ctx);
}
