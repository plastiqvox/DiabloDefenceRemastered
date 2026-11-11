namespace DiabloDefenceRemastered.Core.Sim.Progression;

/// <summary>
/// 금화 한 곳(v0.9 V3, 설계서 6.19 "금화" — WP3): 지갑 <c>Hero.Gold</c>(판 안 쓸 금화, 짐 <c>HeroCarry.Gold</c>)과 얻은 금화 <c>RunStats.Gold</c>(결과 화면·누적 — 늘기만).
/// 얻으면 둘 다 늘고, 쓰면 지갑만 줄고 쓴 금화 <c>RunStats.GoldSpent</c> 가 는다(기획 11.7 "상인에게 쓴 금화도 얻은 금화에는 센다").
/// </summary>
public static class Wallet
{
    /// <summary>금화를 얻는다(금화 줍기·보물 상자·특성·패시브 <c>on_acquire</c>). 0 이하면 아무것도 안 한다.</summary>
    public static void Earn(World world, int amount)
    {
        if (amount <= 0) return;
        world.Hero.Gold += amount;
        world.RunStats.Gold += amount;
    }

    /// <summary>지갑 ≥ <paramref name="amount"/> 이면 쓴다(참). 모자라면 아무 일 없이 거짓.</summary>
    public static bool TrySpend(World world, int amount)
    {
        var hero = world.Hero;
        if (amount < 0 || hero.Gold < amount) return false;
        hero.Gold -= amount;
        world.RunStats.GoldSpent += amount;
        return true;
    }
}
