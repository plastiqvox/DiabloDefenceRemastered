using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Sim.Combat;

namespace DiabloDefenceRemastered.Core.Sim.Heroes;

/// <summary>
/// 주인공 순간이동(보스 묶음 B — 설계서 6.20 ⑫~⑱, 기획 v0.9.5 3절·6절 V21, 소유 WP3). 규칙 = <c>rules.hero.blink</c>, 상태 = <c>Hero.Blink*</c>.
/// <list type="number">
/// <item>충전: 칸이 최대보다 적으면 판 틱마다 <c>BlinkRecharge += Dt</c>, 한 칸 시간이 차면 칸 +1(가득이면 0). 창·넘김으로 판 틱이 멈추면 같이 멈춘다.</item>
/// <item><c>Blink</c> 버튼: 판이 도는 중 ∧ 시전 중 아님 ∧ 칸 ≥ 1 ∧ 그 방향 <c>Throw</c> 거리 ≥ <c>min_distance</c> 면 칸 −1·방향을 굳히고 시전. 아니면 "못 함"(칸 그대로).</item>
/// <item>시전(누른 다음 틱부터 <c>CastTime</c> 동안): 제자리(<c>Hold</c>)·그 방향을 바라봄·맞음. 무기는 그대로 쏜다. 시전 0 이면 누른 틱에 바로 도착.</item>
/// <item>도착: <c>PlacementResolver.Throw</c>(4px 마다 clearance ≥ 몸 반경 — 벽·문 틈 끝·맵 끝 앞에서 멈춤, 적 몸은 막지 않음), 떠난 자리·도착 자리 이펙트,
/// <c>Pos = PrevPos = 도착</c>(그림이 끌리지 않게), 속도·넉백 0, 도착 무적, <c>CueKind.Blink</c>.</item>
/// </list>
/// 죽었거나 끝난 판의 <c>Blink</c> 는 신호 없이 버린다(<c>HeroControl</c> 이 죽은 주인공에서 일찍 끝나고, 물약 <c>Drink</c> 와 같음).
/// </summary>
public static class HeroBlink
{
    /// <summary>시간 비교 여유(초) — 6초 = 360틱이 부동소수 누적 오차로 361틱이 되지 않게.</summary>
    public const float Eps = 1e-4f;

    /// <summary>최대 칸 = <c>charges</c> + round(<c>blink_charges</c>), 1 이상. 규칙이 없으면 0.</summary>
    public static int MaxCharges(Hero hero, BlinkRulesDef? rules) =>
        rules is null ? 0 : Math.Max(1, rules.Charges + (int)MathF.Round(hero.Stats[StatKey.BlinkCharges], MidpointRounding.AwayFromZero));

    public static int MaxCharges(World world) => MaxCharges(world.Hero, world.Defs.Rules.Hero?.Blink);

    /// <summary>시전 = max(0, <c>cast</c> + <c>blink_cast</c>).</summary>
    public static float CastTime(Hero hero, BlinkRulesDef rules) => MathF.Max(0f, rules.Cast + hero.Stats[StatKey.BlinkCast]);

    /// <summary>한 칸 충전 = <c>recharge</c> × (1 + max(−0.5, <c>blink_recharge_pct</c>)).</summary>
    public static float RechargeTime(Hero hero, BlinkRulesDef rules) =>
        rules.Recharge * (1f + MathF.Max(-0.5f, hero.Stats[StatKey.BlinkRechargePct]));

    /// <summary>충전 중인 칸의 진행(0..1, HUD 부채꼴). 가득이면 0.</summary>
    public static float RechargeRatio(Hero hero, BlinkRulesDef rules)
    {
        var t = RechargeTime(hero, rules);
        return t > 0f ? Math.Clamp(hero.BlinkRecharge / t, 0f, 1f) : 0f;
    }

    /// <summary>방마다 가득(주인공 만들기 끝 — 짐 <c>Restore</c>·개발 옵션·능력치 계산 뒤). 짐·계단 저장에는 넣지 않는다(기획 3절).</summary>
    public static void Fill(World world)
    {
        var hero = world.Hero;
        hero.BlinkCharges = MaxCharges(world);
        hero.BlinkRecharge = 0f;
    }

    /// <summary>
    /// 최대 칸이 바뀜(<c>HeroStats.Recompute</c> 가 부름): 늘면 늘어난 만큼 바로 찬다(숙련 Lv2 "얻는 순간 바로 찬다" — 기획 6절), 줄면 최대에서 자른다.
    /// </summary>
    public static void OnMaxChanged(Hero hero, int oldMax, int newMax)
    {
        if (newMax > oldMax) hero.BlinkCharges += newMax - oldMax;
        if (hero.BlinkCharges >= newMax)
        {
            hero.BlinkCharges = newMax;
            hero.BlinkRecharge = 0f;
        }
    }

    /// <summary>방향 = 명령의 이동(길이 &gt; 1e-3 이면 정규화), 없으면 바라보는 방향.</summary>
    public static Vector2 Direction(Hero hero, Vector2 move)
    {
        var len = move.Length();
        if (len > 1e-3f && float.IsFinite(len)) return move / len;
        return MathX.NormalizeOr(hero.Facing, new Vector2(0, 1));
    }

    /// <summary><paramref name="from"/> 에서 <paramref name="dir"/> 로 갈 수 있는 자리(내비가 없으면 제자리).</summary>
    public static Vector2 Destination(World world, Vector2 from, Vector2 dir, BlinkRulesDef rules) =>
        world.Nav is { } nav ? PlacementResolver.Throw(nav, from, dir, rules.Distance, world.Hero.Radius) : from;

    /// <summary>
    /// <c>HeroControl</c> 차례(물약 다음, 이동 의도 앞). 참이면 이번 틱은 순간이동이 이동을 쥔다(시전 중·도착 틱 — 이동 의도 <c>Hold</c>, 방향 = <c>BlinkDir</c>).
    /// </summary>
    public static bool Step(World world)
    {
        var hero = world.Hero;
        var rules = world.Defs.Rules.Hero?.Blink;
        var pressed = (world.Command.Buttons & CommandButtons.Blink) != 0 && world.Outcome == RunOutcome.Running;
        if (rules is null)
        {
            if (pressed) Denied(world);
            return false;
        }

        // 1. 충전
        var max = MaxCharges(hero, rules);
        if (hero.BlinkCharges < max)
        {
            hero.BlinkRecharge += World.Dt;
            var full = RechargeTime(hero, rules);
            if (hero.BlinkRecharge >= full - Eps)
            {
                hero.BlinkCharges++;
                hero.BlinkRecharge = MathF.Max(0f, hero.BlinkRecharge - full);
            }
        }
        if (hero.BlinkCharges >= max)
        {
            hero.BlinkCharges = max;
            hero.BlinkRecharge = 0f;
        }

        // 2. 버튼 — 누른 틱은 시전을 줄이지 않는다(도착 = 누른 틱 + 시전 시간, 0.15s = 9틱 뒤)
        if (pressed)
        {
            if (hero.BlinkCast > 0f || hero.BlinkCharges < 1) Denied(world);
            else
            {
                var dir = Direction(hero, world.Command.Move);
                var to = Destination(world, hero.Pos, dir, rules);
                if (Vector2.Distance(hero.Pos, to) < rules.MinDistance) Denied(world);
                else
                {
                    hero.BlinkCharges--;
                    hero.BlinkDir = dir;
                    hero.Facing = dir;
                    world.RunStats.Blinks++;
                    var cast = CastTime(hero, rules);
                    if (cast <= Eps)
                    {
                        Arrive(world, rules);
                        return true;
                    }
                    hero.BlinkCast = cast;
                    hero.Intent = MoveIntent.Hold;
                    return true;
                }
            }
        }

        // 3. 시전
        if (hero.BlinkCast <= 0f) return false;
        hero.BlinkCast -= World.Dt;
        hero.Facing = hero.BlinkDir;
        hero.Intent = MoveIntent.Hold;
        if (hero.BlinkCast <= Eps) Arrive(world, rules);
        return true;
    }

    /// <summary>4. 도착(시전이 끝난 틱 — 시전 동안 밀려났으면 지금 자리에서 다시 찾는다).</summary>
    private static void Arrive(World world, BlinkRulesDef rules)
    {
        var hero = world.Hero;
        var from = hero.Pos;
        var to = Destination(world, from, hero.BlinkDir, rules);
        if (rules.Effect is { } fx)
        {
            CombatFx.Effect(world, fx, from, 0f, hero.BlinkDir);
            CombatFx.Effect(world, fx, to, 0f, hero.BlinkDir);
        }
        hero.Pos = hero.PrevPos = to;
        hero.Vel = Vector2.Zero;
        hero.KnockbackVel = Vector2.Zero;
        hero.KnockbackTime = 0f;
        hero.BlinkInvulnTime = rules.Invuln;
        hero.BlinkCast = 0f;
        hero.Facing = hero.BlinkDir;
        hero.Intent = MoveIntent.Hold;
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.Blink, to, null));
    }

    /// <summary>"못 함"(칸 그대로): HUD 붉은 테 틱 + 신호(소리 연결 없음).</summary>
    private static void Denied(World world)
    {
        world.Hero.BlinkDeniedTick = world.Tick;
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.BlinkDenied, world.Hero.Pos, null));
    }
}
