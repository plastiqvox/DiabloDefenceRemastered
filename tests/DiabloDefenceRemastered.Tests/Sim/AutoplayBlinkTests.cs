using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Heroes;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 자동 조작 순간이동(보스 묶음 B — 설계서 6.20 ㉒, 기획 v0.9.5 15절): ⓐ 0.3초 안에 닿는 탄 → 탄 피하기 방향으로 <c>Blink</c>(이동 = 그 방향), 칸 0 이면 안 씀,
/// 그 방향이 벽(80px 못 감)이면 반대쪽, ⓒ 체력 &lt; 50% 이고 닿은 적 ≥ 3 이면 무리에서 멀어지는 쪽. 탄이 멀면 쓰지 않는다.
/// </summary>
public sealed class AutoplayBlinkTests
{
    private static readonly ProjectileDef Bullet = CombatKit.Projectile("t_pj_bot_bullet", 5f, 165f, range: 900f, blocked: false);

    private static void Shoot(TestWorld t, Vector2 offset, Vector2 dir) =>
        t.World.Spawner.SpawnProjectile(new EnemyProjectileSpawn(Bullet, t.Hero.Pos + offset, dir, 14, EntityId.None));

    [Fact]
    public void HeadOnBullet_Blinks()
    {
        var t = BlinkKit.World();
        Shoot(t, new Vector2(40f, 0f), new Vector2(-1f, 0f));            // 0.24s 뒤 정면으로 닿음
        var cmd = new AutoplayPolicy(AutoplayVisit.None).Next(t.World);
        Assert.True((cmd.Buttons & CommandButtons.Blink) != 0);
        Assert.Equal(1f, cmd.Move.Length(), 3);
        Assert.True(MathF.Abs(cmd.Move.X) < 1e-3f, $"탄 길에 수직이 아니다: {cmd.Move}");

        // 같은 명령으로 판을 돌리면 순간이동이 난다(사람 입력과 같은 길)
        t.AppTick(cmd);
        Assert.True(t.Hero.BlinkCast > 0f);
        Assert.Equal(1, t.Hero.BlinkCharges);

        // 탄이 멀면(1.2s) 쓰지 않는다 — 걸어서 피함
        var far = BlinkKit.World();
        Shoot(far, new Vector2(200f, 0f), new Vector2(-1f, 0f));
        Assert.False(AutoplayPolicy.ShouldBlink(far.World, out _));
    }

    [Fact]
    public void NoCharge_NoBlink()
    {
        var t = BlinkKit.World();
        t.Hero.BlinkCharges = 0;
        Shoot(t, new Vector2(40f, 0f), new Vector2(-1f, 0f));
        Assert.False(AutoplayPolicy.ShouldBlink(t.World, out _));
        var cmd = new AutoplayPolicy(AutoplayVisit.None).Next(t.World);
        Assert.Equal(CommandButtons.None, cmd.Buttons & CommandButtons.Blink);

        // 시전 중·도착 무적 중에도 쓰지 않는다
        var c = BlinkKit.World();
        Shoot(c, new Vector2(40f, 0f), new Vector2(-1f, 0f));
        c.Hero.BlinkCast = 0.1f;
        Assert.False(AutoplayPolicy.ShouldBlink(c.World, out _));
        c.Hero.BlinkCast = 0f;
        c.Hero.BlinkInvulnTime = 0.1f;
        Assert.False(AutoplayPolicy.ShouldBlink(c.World, out _));
    }

    [Fact]
    public void WallSide_BlinksOpposite()
    {
        // 벽이 주인공 오른쪽 20px. 탄이 주인공 왼쪽 8px 줄로 내려와 피하기 방향 = 오른쪽(벽) — 4px 밖에 못 가므로 반대쪽(왼쪽)으로
        var t = BlinkKit.World(nav: w => new WallXNav(w.Nav, w.MapSize.X * 0.5f + 20f));
        Shoot(t, new Vector2(-8f, -40f), new Vector2(0f, 1f));
        Assert.True(AutoplayPolicy.Dodge(t.World).X > 0f);
        Assert.True(AutoplayPolicy.ShouldBlink(t.World, out var dir));
        Assert.Equal(new Vector2(-1f, 0f), dir);

        // 양쪽 다 막히면 쓰지 않는다
        var boxed = BlinkKit.World(nav: w => new BoxNav(w.Nav, w.MapSize * 0.5f, 30f));
        Shoot(boxed, new Vector2(-8f, -40f), new Vector2(0f, 1f));
        Assert.False(AutoplayPolicy.ShouldBlink(boxed.World, out _));
    }

    [Fact]
    public void LowHpSurrounded_Blinks()
    {
        var t = BlinkKit.World();
        var hero = t.Hero.Pos;
        for (var i = 0; i < 3; i++) t.AddEnemy(hero + new Vector2(-24f, (i - 1) * 16f));
        t.Hero.TouchingCount = 3;
        t.Hero.Hp = 40f;
        Assert.True(AutoplayPolicy.ShouldBlink(t.World, out var dir));
        Assert.True(dir.X > 0.9f, $"무리에서 멀어지지 않음: {dir}");

        t.Hero.Hp = 60f;                                                   // 체력 50% 이상이면 쓰지 않음
        Assert.False(AutoplayPolicy.ShouldBlink(t.World, out _));
        t.Hero.Hp = 40f;
        t.Hero.TouchingCount = 2;                                         // 닿은 적 2 면 쓰지 않음
        Assert.False(AutoplayPolicy.ShouldBlink(t.World, out _));
    }

    /// <summary>중심에서 <paramref name="half"/> 안 정사각형만 걸을 수 있는 내비.</summary>
    private sealed class BoxNav(INavWorld inner, Vector2 center, float half) : INavWorld
    {
        public float Clearance(Vector2 p) => half - MathF.Max(MathF.Abs(p.X - center.X), MathF.Abs(p.Y - center.Y));
        public bool HasClearLine(Vector2 a, Vector2 b, float radius) => Clearance(a) >= radius && Clearance(b) >= radius;
        public bool ProjectileLineClear(Vector2 a, Vector2 b) => true;
        public bool IsInsideMap(Vector2 p) => inner.IsInsideMap(p);
        public float PathLength(Vector2 from, Vector2 to, float radius, float maxLength) => inner.PathLength(from, to, radius, maxLength);
        public float PathLengthToHero(Vector2 from, float radius, float maxLength) => inner.PathLengthToHero(from, radius, maxLength);
        public bool TryPathStep(Vector2 from, Vector2 to, float radius, out Vector2 next) => inner.TryPathStep(from, to, radius, out next);
    }
}
