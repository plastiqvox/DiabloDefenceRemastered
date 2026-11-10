using System.Globalization;

namespace DiabloDefenceRemastered.Core.Dev;

/// <summary>
/// 상태 해시 (설계서 3.4, WP7) — 결정성 시험과 게임 <c>--log-hash</c> 가 같이 쓴다. 64비트 FNV-1a 를 값의 비트 그대로 섞는다(같은 빌드 구성끼리만 비교).
/// <list type="bullet">
/// <item><see cref="Room"/> = 방 해시: 그 <see cref="World"/> 의 상태만 — 직업 ID(첫 값)·틱·난수 흐름 여섯·주인공(위치·속도·HP·칸·허리띠·축복·열린 칸)·성장(레벨·경험치)·
/// 개체 저장소 다섯(수·ID·정의·위치·HP·보급 방·보상)·가 본 칸 집합(<see cref="VisibilityField.ExploredHash"/>)·안쪽 방 체류(<c>Current</c>·<c>EnteredTick</c>)·
/// 방 상태(<c>Phase</c>·<c>PhaseTick</c>·<c>ClockTicks</c>·출구 문·고른 문·무너짐)·결과. <c>RunStats</c>·판 흐름·<c>Room.Reward</c>(앞 방에서 고른 결과)는 넣지 않는다.</item>
/// <item><see cref="Run"/> = 판 해시: 방 해시 + <c>RunStats</c> + 판 흐름(액트·순번·깊이·들어온 문·지나온 방 기록의 문·보상·다음 방 보상·흐른 틱).</item>
/// </list>
/// 시야 안 칸(<c>InSight</c>)은 넣지 않는다 — 해시된 주인공 위치·빛 반경과 맵에서 다시 나오는 값이다(4.11).
/// </summary>
public static class StateHasher
{
    public static ulong Room(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var h = new Fnv();
        h.Str(world.HeroClass?.Id);
        h.Int(world.Tick);
        h.U64(world.Seed);
        for (var s = 0; s < SimRandom.StreamCount; s++)
        {
            var (a, b, c, d) = world.Rng[(RngStream)s].GetState();
            h.U32(a); h.U32(b); h.U32(c); h.U32(d);
        }

        var hero = world.Hero;
        h.V2(hero.Pos); h.V2(hero.Vel); h.F(hero.Hp); h.F(hero.MaxHp); h.Bool(hero.Dead);
        h.Int(hero.WeaponSlotsOpen); h.Int(hero.PassiveSlotsOpen); h.Int(hero.Potions);   // v0.9.1: 허리띠 → 물약 칸(장비·지갑·회복 중 열은 WP7)
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
        {
            var w = hero.Weapons[i];
            h.Str(w.Def?.Id); h.Int(w.Level); h.Bool(w.Evolved);
        }
        for (var i = 0; i < PassiveSlots.SlotCount; i++)
        {
            var p = hero.Passives[i];
            h.Str(p.Def?.Id); h.Int(p.Level);
        }
        for (var i = 0; i < BlessingSlots.SlotCount; i++)
        {
            var p = hero.Blessings[i];
            h.Str(p.Def?.Id); h.Int(p.Level);
        }
        if (world.Progression is { } prog)
        {
            h.Int(prog.Level); h.Int(prog.Xp); h.Int(prog.PendingLevelUps); h.Int(prog.PendingChests);
        }
        else h.Int(-1);

        h.Int(world.Enemies.LiveCount);
        foreach (var e in world.Enemies.Items)
        {
            if (e.Removed) continue;
            h.Int(e.Id.Slot); h.Int(e.Id.Gen); h.Int(e.DefIndex); h.V2(e.Pos); h.F(e.Hp); h.Bool(e.Dead); h.Int((int)e.Life);
        }
        h.Int(world.Projectiles.LiveCount);
        foreach (var p in world.Projectiles.Items)
        {
            if (p.Removed) continue;
            h.Int(p.Id.Slot); h.Str(p.Def.Id); h.V2(p.Pos);
        }
        h.Int(world.Hazards.LiveCount);
        foreach (var z in world.Hazards.Items)
        {
            if (z.Removed) continue;
            h.Int(z.Id.Slot); h.V2(z.Pos);
        }
        h.Int(world.Telegraphs.LiveCount);
        foreach (var t in world.Telegraphs.Items)
        {
            if (t.Removed) continue;
            h.Int(t.Id.Slot); h.V2(t.Pos);
        }
        h.Int(world.Pickups.LiveCount);
        foreach (var p in world.Pickups.Items)
        {
            if (p.Removed) continue;
            h.Int(p.Id.Slot); h.Str(p.Def.Id); h.V2(p.Pos); h.Int(p.Value); h.Str(p.SupplyRoom); h.Str(p.Reward?.Id); h.Bool(p.GatesExit);
        }

        h.U64(world.Visibility.ExploredHash);
        h.Int(world.InnerRoomPresence.Current);
        h.Int(world.InnerRoomPresence.EnteredTick);

        if (world.Room is { } room)
        {
            h.Int((int)room.Phase); h.Int(room.PhaseTick); h.Int(room.ClockTicks); h.Int(room.CleanupTicks); h.Bool(room.Collapsed); h.Int(room.ClearTick);
            h.Int(room.Exits.Count);
            foreach (var x in room.Exits) h.Str(x.GateId);
            h.Str(room.ChosenExit);
        }
        else h.Int(-1);
        h.Int((int)world.Outcome);
        return h.Value;
    }

    public static ulong Run(RunState run, World world)
    {
        ArgumentNullException.ThrowIfNull(run);
        var h = new Fnv();
        h.U64(Room(world));
        var s = run.Stats;
        h.Int(s.Kills); h.Int(s.EliteKills); h.Int(s.Gold); h.Int(s.XpCollected); h.F(s.DamageTaken);
        foreach (var d in s.DamageByWeapon) h.F(d);
        h.Str(run.Act.Act.Id); h.Int(run.Act.ActNumber); h.Int(run.RoomIndex); h.Int(run.Depth.Act); h.Int(run.Depth.Room); h.Int(run.ElapsedTicks);
        h.Str(world.Room?.Plan.GateIn);
        h.Str(run.NextReward?.Id);
        h.Int(run.Rooms.Count);
        foreach (var r in run.Rooms)
        {
            h.Int(r.Act); h.Int(r.Number); h.Str(r.MapId); h.Str(r.GateIn); h.Str(r.RewardId); h.Str(r.Door);
        }
        return h.Value;
    }

    /// <summary>로그용 16진 16자리.</summary>
    public static string Hex(ulong hash) => hash.ToString("x16", CultureInfo.InvariantCulture);

    /// <summary><c>--log-hash</c> 머리 줄(9절).</summary>
    public const string CsvHeader = "act,room,stage_tick,room_hash,run_hash";

    /// <summary><c>--log-hash</c> 한 행: <c>act, room, stage_tick, room_hash, run_hash</c>(앱 틱은 넣지 않음). 판이 없으면 act·room 0, run_hash 는 방 해시.</summary>
    public static string CsvRow(World world)
    {
        var room = Room(world);
        var run = world.Run is { } r ? Run(r, world) : room;
        var act = world.Room?.Plan.Act ?? 0;
        var number = world.Room?.Plan.Def.Number ?? 0;
        return string.Create(CultureInfo.InvariantCulture, $"{act},{number},{world.Tick},{Hex(room)},{Hex(run)}");
    }

    private struct Fnv
    {
        private const ulong Prime = 1099511628211ul;
        public ulong Value;

        public Fnv() => Value = 14695981039346656037ul;

        public void Byte(byte b) { Value ^= b; Value *= Prime; }
        public void U32(uint v) { for (var i = 0; i < 4; i++) Byte((byte)(v >> (8 * i))); }
        public void U64(ulong v) { U32((uint)v); U32((uint)(v >> 32)); }
        public void Int(int v) => U32(unchecked((uint)v));
        public void Bool(bool v) => Byte(v ? (byte)1 : (byte)0);
        public void F(float v) => U32(BitConverter.SingleToUInt32Bits(v));
        public void V2(Vector2 v) { F(v.X); F(v.Y); }

        public void Str(string? s)
        {
            if (s is null) { U32(0xFFFFFFFFu); return; }
            Int(s.Length);
            foreach (var c in s) { Byte((byte)c); Byte((byte)(c >> 8)); }
        }
    }
}
