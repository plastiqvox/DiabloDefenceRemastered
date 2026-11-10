namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>
/// 장판 반복 소리 (설계서 7.9, 기획 12.7 "장판 수와 상관없이 하나" — 순수 로직, 소유 WP8). 프레임마다 <c>World.Hazards</c> 에서
/// <c>hazard_loops</c> 에 연결된 이펙트의 장판이 하나라도 살아 있는지 보고, 소리 ID 마다 반복 하나를 켜 두거나 끈다.
/// 반복 소리는 <see cref="SoundMixer"/> 의 동시 수에 들지 않는다(재생 쪽 반복 칸 — 7.9 "목소리 칸").
/// </summary>
public sealed class HazardLoops(SoundRouter router)
{
    private readonly HashSet<string> _want = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, SoundDef> _active = new(StringComparer.Ordinal);
    private readonly List<string> _scratch = [];

    /// <summary>지금 켜 둔 반복 소리(소리 ID 서수 순).</summary>
    public IEnumerable<SoundDef> Active => _active.Values;

    public int Count => _active.Count;

    /// <summary>
    /// 월드의 장판으로 켤 반복 소리를 다시 센다. 새로 켤 것은 <paramref name="started"/>(장판 저장소 순서), 끌 것은 <paramref name="stopped"/>(소리 ID 서수 순)에 더한다.
    /// </summary>
    public void Update(World world, List<SoundDef> started, List<SoundDef> stopped)
    {
        var loops = router.Defs.SoundBindings.HazardLoops;
        _want.Clear();
        if (loops.Count > 0)
        {
            foreach (var h in world.Hazards.Items)
            {
                if (h.Removed || h.Effect is null || !loops.TryGetValue(h.Effect.Id, out var r)) continue;
                if (router.Resolve(r) is { } sound && _want.Add(sound.Id) && !_active.ContainsKey(sound.Id))
                {
                    _active[sound.Id] = sound;
                    started.Add(sound);
                }
            }
        }

        _scratch.Clear();
        foreach (var id in _active.Keys)
            if (!_want.Contains(id)) _scratch.Add(id);
        foreach (var id in _scratch)
        {
            stopped.Add(_active[id]);
            _active.Remove(id);
        }
    }

    /// <summary>모두 끈다(새 월드를 받을 때 — 옛 월드의 장판에 묶인 반복). 끈 것은 <paramref name="stopped"/> 에.</summary>
    public void Clear(List<SoundDef> stopped)
    {
        stopped.AddRange(_active.Values);
        _active.Clear();
    }
}
