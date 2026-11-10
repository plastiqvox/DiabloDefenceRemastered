using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using DiabloDefenceRemastered.Tests.Sim;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 코드 없는 변형 증거 (설계서 6.13, 11.1 WP4 완료 기준 "변형 픽스처"): <c>Fixtures/variants/ash_skeleton.json</c> 이
/// <c>extends: skeleton</c> + 색조 + <c>behaviors_add</c>(죽으면 장판) + <c>behaviors_patch</c>(근접 쿨)만으로 실제 적 표에 들어가 돌고 죽음 훅이 돈다.
/// <c>extends</c> 풀기는 시험 안 최소 구현(<see cref="Wp4Data.ResolveVariant"/>) — 로더(WP7)가 생기면 그것으로 읽는다.
/// </summary>
public sealed class VariantFixtureTests
{
    [Fact]
    public void Ash_skeleton_is_skeleton_with_tint_patch_and_added_hook()
    {
        var defs = Wp4Data.Defs;
        Assert.Contains("ash_skeleton", Wp4Data.VariantIds);
        var ash = defs.Enemies.Get("ash_skeleton");
        var skel = defs.Enemies.Get("skeleton");
        Assert.Equal(skel.Sprites.Id, ash.Sprites.Id);
        Assert.Equal(skel.Hp, ash.Hp);
        Assert.Equal(new Rgb(150, 150, 150), ash.Tint);
        Assert.Equal("잿빛 해골", ash.Name);

        Assert.Equal(2, ash.Behaviors.Count);
        var melee = Assert.IsType<ChaseMelee>(ash.Behaviors[0].Instance);   // 같은 id 자리 지킴
        Assert.Equal(0.8f, melee.Params.Cooldown);
        Assert.Equal(1.0f, Assert.IsType<ChaseMelee>(skel.Behaviors[0].Instance).Params.Cooldown);
        Assert.Equal("on_death", ash.Behaviors[1].Trigger);
        Assert.IsType<GroundHazard>(ash.Behaviors[1].Instance);
    }

    [Fact]
    public void Ash_skeleton_leaves_hazard_on_damage_death()
    {
        var defs = Wp4Data.Defs;
        var t = new TestWorldBuilder().WithDefs(defs).WithModules(FakeArena.Modules(weapons: false)).Build();
        var e = EnemyFactory.Create(t.World, defs.Enemies.Get("ash_skeleton"), EnemyRank.Normal, [], t.Hero.Pos + new Vector2(150f, 0f),
                                    new SpawnContext(SpawnSource.Interval, EntityId.None));
        t.Step(30);
        Assert.True(e.IsAlive);
        e.Dead = true;
        e.KillCause = KillCause.Damage;
        t.Step();
        Assert.Single(((RecordingSpawner)t.World.Spawner).Hazards);
    }
}
