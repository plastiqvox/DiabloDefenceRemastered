using DiabloDefenceRemastered.Core.Nav.Validation;
using DiabloDefenceRemastered.Core.Sim.Movement;
using DiabloDefenceRemastered.Core.Sim.Movement.Invariants;
using DiabloDefenceRemastered.Core.Spatial;

namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 길찾기·충돌·공간 모듈 (소유 WP2): 월드마다 <see cref="NavWorld"/>·<see cref="SpatialHash"/> 를 넣고 Navigation·Locomotion·Collision·SpatialRebuild 시스템을 등록한다.
/// 검사기 = 길 검사 13.5-4~6·13.5-23 ③(<see cref="NavPathValidator"/>). 내비·해시·충돌 도형은 월드(방)마다 새로 만든다(3.6 결정 1).
/// </summary>
public sealed class NavModule : IGameModule
{
    public void Register(ModuleContext ctx)
    {
        ctx.AddValidator(new NavPathValidator());
        ctx.AddWorldInstaller(Install);
    }

    /// <summary>월드 설치(시험이 모듈 없이 같은 설치를 쓸 때도).</summary>
    public static void Install(World world, SimulationBuilder builder)
    {
        var nav = NavWorld.Build(world);
        var hash = new SpatialHash(world);
        world.Nav = nav;
        world.Spatial = hash;
        var navigator = new Navigator(world, nav);
        builder.Add(SimPhase.Navigation, new NavigationSystem(nav, navigator));
        builder.Add(SimPhase.Locomotion, new LocomotionSystem(hash, navigator));
        builder.Add(SimPhase.Collision, new CollisionSystem(nav, hash));
        builder.Add(SimPhase.SpatialRebuild, new SpatialRebuildSystem(hash, new InvariantChecker(world, nav)));
    }
}
