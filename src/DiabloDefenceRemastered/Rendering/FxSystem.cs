using DiabloDefenceRemastered.Core.Art;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>재생 중인 표시 이펙트 하나(<see cref="FxEvent"/> 를 그대로 — 나이 = 지금 스테이지 틱 − <see cref="FxEvent.Tick"/>).</summary>
public readonly record struct ActiveFx(FxEvent Event, float Scale, float Life);

/// <summary>
/// 표시 이펙트 (설계서 5.6·7.8, 소유 WP5). 표시 이벤트 버퍼의 이펙트·피해 숫자·시체·흔들림을 가져가 풀에서 재생한다(나이 = stageTick − 항목 Tick).
/// 인스턴스는 <see cref="WorldRenderer.Fx"/>. 버퍼 비우기는 부르는 쪽(PlayScene)이 Consume 뒤에 한다.
/// <list type="bullet">
/// <item>이펙트 풀 256(가득 차면 가장 오래된 것을 바꿈). 반복 이펙트는 표시 이벤트로 오면 intro + 한 바퀴(빛 지속이 더 길면 그만큼)만 — 오래 남는 반복은 장판(Hazard)이 그린다.</item>
/// <item>피해 숫자 120(<see cref="DamageNumberPool"/> — 같은 대상 0.1s 합침), 시체 150(가장 오래된 것부터 바꿈), 흔들림은 남은 것 중 가장 센 것(<see cref="ShakeMath"/>).</item>
/// <item><see cref="Attach"/>(v0.7.1, 7.11): 새 월드 = 남은 항목을 모두 버림(옛 방의 "생긴 틱" 이 새 <c>World.Tick</c> 0 에서 음수 나이가 되지 않게).</item>
/// </list>
/// </summary>
public sealed class FxSystem : IWorldBound
{
    public const int EffectCapacity = 256;
    public const int CorpseCapacity = 150;

    private readonly ActiveFx[] _effects = new ActiveFx[EffectCapacity];
    private int _effectCount;
    private readonly CorpseSpawned[] _corpses = new CorpseSpawned[CorpseCapacity];
    private int _corpseCount;
    private int _corpseNext;
    private readonly List<ShakeEvent> _shakes = [];

    public DamageNumberPool Numbers { get; } = new();

    public ReadOnlySpan<ActiveFx> Effects => _effects.AsSpan(0, _effectCount);

    /// <summary>시체(오래된 순 — 링 버퍼를 펼친 순서는 <see cref="CorpseAt"/>).</summary>
    public int CorpseCount => _corpseCount;

    /// <summary>오래된 순 i 번째 시체.</summary>
    public ref readonly CorpseSpawned CorpseAt(int i)
    {
        var start = _corpseCount < CorpseCapacity ? 0 : _corpseNext;
        return ref _corpses[(start + i) % CorpseCapacity];
    }

    public void Consume(PresentationBuffer buffer, int stageTick)
    {
        foreach (ref readonly var e in buffer.Effects.AsSpan()) AddEffect(e);
        foreach (ref readonly var d in buffer.DamageNumbers.AsSpan()) Numbers.Add(d);
        foreach (ref readonly var c in buffer.Corpses.AsSpan()) AddCorpse(c);
        foreach (ref readonly var s in buffer.Shakes.AsSpan()) _shakes.Add(s);
        Expire(stageTick);
    }

    /// <summary>새 월드: 남은 이펙트·피해 숫자·시체·흔들림을 버린다(7.11).</summary>
    public void Attach(World world) => Clear();

    public void Clear()
    {
        Array.Clear(_effects);
        _effectCount = 0;
        Array.Clear(_corpses);
        _corpseCount = 0;
        _corpseNext = 0;
        _shakes.Clear();
        Numbers.Clear();
    }

    /// <summary>지금 흔들림 세기(px, 남은 것 중 가장 센 것).</summary>
    public float ShakePx(int stageTick) => ShakeMath.Strength(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_shakes), stageTick);

    private void AddEffect(in FxEvent e)
    {
        var def = e.Def;
        var life = EffectPlayback.IntroLength(def) + EffectPlayback.CycleLength(def);
        if (def.Procedural is not null) life = Math.Max(life, 0.5f);
        if (def.Light?.Duration is { } d) life = Math.Max(life, d);
        var item = new ActiveFx(e, EffectPlayback.Scale(def, e.Radius, e.Scale), life);
        if (_effectCount < EffectCapacity) { _effects[_effectCount++] = item; return; }
        var oldest = 0;
        for (var i = 1; i < _effectCount; i++)
            if (_effects[i].Event.Tick < _effects[oldest].Event.Tick) oldest = i;
        _effects[oldest] = item;
    }

    private void AddCorpse(in CorpseSpawned c)
    {
        _corpses[_corpseNext] = c;
        _corpseNext = (_corpseNext + 1) % CorpseCapacity;
        if (_corpseCount < CorpseCapacity) _corpseCount++;
    }

    private void Expire(int stageTick)
    {
        var w = 0;
        for (var i = 0; i < _effectCount; i++)
        {
            var age = (stageTick - _effects[i].Event.Tick) * World.Dt;
            if (age < 0f || age > _effects[i].Life) continue;
            _effects[w++] = _effects[i];
        }
        for (var i = w; i < _effectCount; i++) _effects[i] = default;
        _effectCount = w;
        _shakes.RemoveAll(s => stageTick - s.Tick >= s.Duration * World.TicksPerSecond || stageTick < s.Tick);
        Numbers.Expire(stageTick);
    }
}
