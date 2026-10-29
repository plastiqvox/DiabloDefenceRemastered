namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 시험 분류 (설계서 1.3). 기본(표시 없음)은 늘 돈다. 느린 분류는 환경 변수 <c>DDR_RUN</c>(예: <c>Scenario,Perf</c>)에 이름이 없으면
/// 건너뜀 표시한다 → 옵션 없는 <c>dotnet test</c> 가 늘 빠르다. 골라 돌릴 때는 <c>DDR_RUN=Perf</c> 와 <c>--filter Category=Perf</c> 를 같이 쓴다.
/// <code>
/// [Fact, Trait("Category", TestCategories.Perf)]
/// public void Tick500() { TestCategories.RequireEnabled(TestCategories.Perf); … }
/// </code>
/// </summary>
public static class TestCategories
{
    public const string Scenario = "Scenario";
    public const string Perf = "Perf";
    public const string Integration = "Integration";

    public static bool IsEnabled(string category)
    {
        var run = Environment.GetEnvironmentVariable("DDR_RUN");
        if (string.IsNullOrWhiteSpace(run)) return false;
        foreach (var part in run.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (string.Equals(part, category, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>분류가 켜져 있지 않으면 건너뜀(<see cref="Assert.Skip(string)"/>).</summary>
    public static void RequireEnabled(string category)
    {
        if (!IsEnabled(category)) Assert.Skip($"{category} 분류는 DDR_RUN 에 '{category}' 가 있을 때만 돈다");
    }
}
