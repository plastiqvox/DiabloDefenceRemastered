namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 획득물만 놓는 가짜 <see cref="ICombatSpawner"/>(v0.7.1 #23-형). WP3 없이 <c>RoomFlow</c> 를 시험한다: <see cref="SpawnPickup"/> 이 획득물을 만들고
/// <see cref="PickupSpawn.Reward"/> 가 있으면 <c>Pickup.Reward</c>·<c>GatesExit = true</c>(6.18 — 실제 구현 WP3 과 같은 규칙). 투사체·장판·예고는 만들지 않는다(예외).
/// </summary>
public sealed class FakeSpawner(World world) : ICombatSpawner
{
    public EntityId SpawnProjectile(in EnemyProjectileSpawn spawn) => throw new NotSupportedException("FakeSpawner: 투사체는 만들지 않는다");
    public EntityId SpawnHazard(in HazardSpawn spawn) => throw new NotSupportedException("FakeSpawner: 장판은 만들지 않는다");
    public EntityId SpawnTelegraph(in TelegraphSpawn spawn) => throw new NotSupportedException("FakeSpawner: 예고는 만들지 않는다");

    public EntityId SpawnPickup(in PickupSpawn spawn)
    {
        var p = world.Pickups.Add();
        p.Def = spawn.Def;
        p.Pos = p.PrevPos = spawn.Pos;
        p.Radius = 12f;
        p.Value = spawn.Value;
        p.SpawnTick = world.Tick;
        p.SupplyRoom = spawn.SupplyRoom;
        p.Reward = spawn.Reward;
        p.GatesExit = spawn.Reward is not null;
        return p.Id;
    }

    /// <summary>시험이 "주웠다" 를 흉내: 그 획득물을 지운다(WP3 Pickups 대신).</summary>
    public static void Collect(World world, Pickup pickup) => world.Pickups.Remove(pickup.Id);
}
