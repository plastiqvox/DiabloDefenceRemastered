namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>앱 틱 하나에서 일어난 일.</summary>
public enum StageStep : byte
{
    /// <summary>스테이지 틱을 하나 돌았다.</summary>
    Stepped,
    /// <summary>선택 대기(레벨업·상자) — 오버레이가 받는다.</summary>
    AwaitingChoice,
    /// <summary>히트 스톱.</summary>
    Frozen,
    /// <summary>슬로모션 중 이번 앱 틱은 쌓기만 했다.</summary>
    Stretched,
    /// <summary>판이 끝났다(Victory·Defeat).</summary>
    Finished,
}

/// <summary>
/// 스테이지 틱을 돌릴지 정하는 규칙 한 곳 (설계서 3.3). PlayScene.FixedTick 은 앱 틱마다 <see cref="AppTick"/> 를 부르기만 한다.
/// - 선택 대기면 돌지 않는다(같은 앱 틱에 오버레이가 올라간다).
/// - 히트 스톱: <see cref="World.FreezeTicks"/> 동안 돌지 않는다.
/// - 슬로모션: 틱 길이는 그대로, 앱 틱마다 배율을 쌓아 1 이 될 때만 한 번 돈다 → 고정 Dt·결정성 유지.
/// 일시정지는 여기 규칙이 아니다(오버레이 PausesBelow 가 PlayScene 을 부르지 않는다).
/// </summary>
public static class StageStepper
{
    private const float StretchEpsilon = 1e-4f;

    public static StageStep AppTick(World world, PlayerCommand command)
    {
        var sim = world.Simulation ?? throw new InvalidOperationException("월드에 시뮬레이션이 없다(SimulationBuilder.Build 를 거치지 않음)");

        if (world.Outcome is RunOutcome.Victory or RunOutcome.Defeat) return StageStep.Finished;
        if (world.AwaitingChoice != ChoiceKind.None) return StageStep.AwaitingChoice;

        if (world.FreezeTicks > 0)
        {
            world.FreezeTicks--;
            return StageStep.Frozen;
        }

        if (world.SlowMoAppTicks > 0)
        {
            world.SlowMoAppTicks--;
            world.SlowMoStretch += world.SlowMoScale;
            if (world.SlowMoStretch < 1f - StretchEpsilon) return StageStep.Stretched;
            world.SlowMoStretch = MathF.Max(0f, world.SlowMoStretch - 1f);
        }
        else world.SlowMoStretch = 0f;

        world.Command = command;
        sim.Step();
        return StageStep.Stepped;
    }

    /// <summary>
    /// 보간 alpha (설계서 3.2). 슬로모션 중에는 늘임 누적값(앱 누적기 값을 쓰면 위치가 몇 프레임 멈췄다 뛴다), 아니면 누적기 값.
    /// 일시정지 중 마지막 값 고정·캡처 alpha = 1 은 부르는 쪽이 정한다.
    /// </summary>
    public static float Alpha(World world, float accumulatorAlpha) =>
        world.SlowMoAppTicks > 0 ? Math.Clamp(world.SlowMoStretch, 0f, 1f) : accumulatorAlpha;
}
