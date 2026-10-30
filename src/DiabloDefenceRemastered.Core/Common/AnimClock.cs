namespace DiabloDefenceRemastered.Core.Common;

/// <summary>
/// 그림 묶음의 클립 이름(<c>sprite_sets[].clips</c> 의 키, 원작 클립 이름 — 설계서 6.3).
/// <c>Special</c> = 몬스터의 시전·함성·소환(v0.2 의 cast), <c>Cast*</c> = 주인공 시전(무기 <c>cast_clip</c>).
/// </summary>
public enum ClipKind : byte { Idle, Walk, Attack, Special, Die, CastFire, CastLightning, CastMagic }

[Flags]
public enum AnimStep : byte
{
    None = 0,
    /// <summary>이번 진행에서 이벤트 프레임(타격·발사)에 들어섰다.</summary>
    EventFrame = 1,
    /// <summary>반복하지 않는 클립이 끝났다.</summary>
    Finished = 2,
    /// <summary>반복 클립이 처음으로 돌아갔다.</summary>
    Looped = 4,
}

/// <summary>
/// 애니메이션 시계(시뮬레이션 상태, 설계서 1.2·6.3). 어느 클립의 어디쯤인지만 가진다.
/// 프레임 시간·프레임 수는 정의에서 받아 <see cref="Advance"/> 에 넘긴다 → 주인공·적·미리보기·시체가 같은 계산을 쓴다.
/// 프레임 번호 규칙 하나: <b>정의(Defs)·부품 매개변수·이 시계의 이벤트 프레임은 모두 데이터 그대로 1부터</b>
/// (<c>ClipDef.EventFrame</c>, <c>PatternDef.EventFrame</c>, 부품의 <c>event_frame</c>, <c>EffectDef.Frames</c>, <c>IconDef.Frame</c>, <c>PickupDrawDef.Frame</c>).
/// 로더도 부르는 쪽도 1을 빼지 않고 그대로 넘긴다. 0부터인 것은 <see cref="Frame"/> 의 결과(시트 열 안 칸 번호)뿐이다.
/// </summary>
public struct AnimClock
{
    public ClipKind Clip;
    /// <summary>클립 시작부터 흐른 시간(재생 배율 반영, 초).</summary>
    public float Time;
    /// <summary>재생 배율(걷기 = <see cref="WalkRate"/>, anim_speed …).</summary>
    public float Rate;
    public bool Loop;
    public bool Done;

    /// <summary>
    /// 걷기 재생 배율 = 땅 기준 속도 / base_speed (4.1·6.3). 주인공(WP3)·적(WP4)·미리보기·시체(WP5)가 모두 이것을 쓴다.
    /// <paramref name="baseSpeed"/> ≤ 0 이면 1.
    /// </summary>
    public static float WalkRate(Vector2 vel, float baseSpeed) =>
        baseSpeed > 0f ? FacingMath.GroundSpeed(vel) / baseSpeed : 1f;

    public void Play(ClipKind clip, bool loop, float rate = 1f)
    {
        Clip = clip;
        Time = 0f;
        Rate = rate;
        Loop = loop;
        Done = false;
    }

    /// <summary>같은 클립이 재생 중이면 배율만 바꾸고, 아니면 처음부터 재생한다.</summary>
    public void Continue(ClipKind clip, bool loop, float rate = 1f)
    {
        if (Clip == clip && !Done && Loop == loop) { Rate = rate; return; }
        Play(clip, loop, rate);
    }

    /// <summary>dt 초 진행. 이벤트 프레임은 그 프레임의 시작 시각을 넘는 진행에서 한 번만 알린다.</summary>
    /// <param name="eventFrame1">이벤트 프레임(<b>1부터</b>, 데이터 값 그대로). 0 이하 = 이벤트 없음.</param>
    public AnimStep Advance(float dt, float frameTime, int frameCount, int eventFrame1 = 0)
    {
        if (Done || frameTime <= 0f || frameCount <= 0) return AnimStep.None;
        var length = frameTime * frameCount;
        var prev = Time;
        var next = prev + dt * Rate;
        var eventT = eventFrame1 >= 1 && eventFrame1 <= frameCount ? (eventFrame1 - 1) * frameTime : -1f;
        var step = AnimStep.None;

        if (next < length)
        {
            if (eventT >= 0f && prev <= eventT && eventT < next) step |= AnimStep.EventFrame;
            Time = next;
            return step;
        }

        if (eventT >= 0f && prev <= eventT) step |= AnimStep.EventFrame;
        if (!Loop)
        {
            Time = length;
            Done = true;
            return step | AnimStep.Finished;
        }

        var wrapped = (next - length) % length;
        if (eventT >= 0f && eventT < wrapped && (step & AnimStep.EventFrame) == 0) step |= AnimStep.EventFrame;
        Time = wrapped;
        return step | AnimStep.Looped;
    }

    /// <summary>지금 그릴 프레임 번호(0부터).</summary>
    public readonly int Frame(float frameTime, int frameCount)
    {
        if (frameTime <= 0f || frameCount <= 0) return 0;
        var idx = (int)(Time / frameTime);
        return Loop ? idx % frameCount : Math.Min(idx, frameCount - 1);
    }
}
