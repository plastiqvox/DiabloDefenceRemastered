namespace DiabloDefenceRemastered.App.Contracts;

/// <summary>
/// 씬 스택 (설계서 8.1). FixedTick 은 위에서부터, 오버레이가 PausesBelow 면 아래로 내려가지 않는다.
/// Draw 는 DrawsBelow 가 이어지는 가장 아래 씬부터 위로. 돌고 있는 도중의 Push·Pop 은 그 차례가 끝난 뒤 적용한다
/// (PlayScene 이 레벨업 오버레이를 올리면 같은 앱 틱 끝에 올라간다).
/// </summary>
public sealed class SceneStack
{
    private readonly List<IScene> _scenes = [];
    private readonly List<(OpKind Kind, IScene? Scene)> _pending = [];
    private int _iterating;
    private bool _flushing;

    private enum OpKind : byte { Push, Pop, Replace, Reset }

    public int Count => _scenes.Count;
    public IScene? Top => _scenes.Count > 0 ? _scenes[^1] : null;
    public IReadOnlyList<IScene> Scenes => _scenes;

    /// <summary>씬이 들어갈 때마다(로그 <c>SCENE &lt;이름&gt; app_tick=…</c>).</summary>
    public Action<IScene>? Entered { get; set; }

    public void Push(IScene scene) => Do(OpKind.Push, scene);
    public void Pop() => Do(OpKind.Pop, null);
    /// <summary>맨 위를 바꾼다.</summary>
    public void Replace(IScene scene) => Do(OpKind.Replace, scene);
    /// <summary>모두 비우고 하나만 둔다.</summary>
    public void Reset(IScene scene) => Do(OpKind.Reset, scene);

    public void FixedTick(in AppTick t, InputFrame input)
    {
        _iterating++;
        try
        {
            for (var i = _scenes.Count - 1; i >= 0; i--)
            {
                var scene = _scenes[i];
                scene.FixedTick(t, input);
                if (scene.PausesBelow) break;
            }
        }
        finally { EndIteration(); }
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (_scenes.Count == 0) return;
        _iterating++;
        try
        {
            var start = _scenes.Count - 1;
            while (start > 0 && _scenes[start].DrawsBelow) start--;
            for (var i = start; i < _scenes.Count; i++) _scenes[i].Draw(rc, alpha);
        }
        finally { EndIteration(); }
    }

    public void AutoStep(IAutoplayDriver bot)
    {
        if (Top is not { } top) return;
        _iterating++;
        try { top.AutoStep(bot); }
        finally { EndIteration(); }
    }

    private void Do(OpKind kind, IScene? scene)
    {
        if (_iterating > 0 || _flushing) _pending.Add((kind, scene));
        else Apply(kind, scene);
    }

    private void EndIteration()
    {
        if (--_iterating > 0 || _flushing) return;
        // 적용 중 Enter 가 또 Push 해도 같은 목록 끝에 붙어 차례로 처리된다
        _flushing = true;
        try
        {
            for (var i = 0; i < _pending.Count; i++) Apply(_pending[i].Kind, _pending[i].Scene);
            _pending.Clear();
        }
        finally { _flushing = false; }
    }

    private void Apply(OpKind kind, IScene? scene)
    {
        switch (kind)
        {
            case OpKind.Push:
                Enter(scene!);
                break;
            case OpKind.Pop:
                PopTop();
                break;
            case OpKind.Replace:
                PopTop();
                Enter(scene!);
                break;
            case OpKind.Reset:
                while (_scenes.Count > 0) PopTop();
                Enter(scene!);
                break;
        }
    }

    private void Enter(IScene scene)
    {
        _scenes.Add(scene);
        _iterating++;
        try
        {
            scene.Enter(this);
            Entered?.Invoke(scene);
        }
        finally { EndIteration(); }
    }

    private void PopTop()
    {
        if (_scenes.Count == 0) return;
        var top = _scenes[^1];
        _scenes.RemoveAt(_scenes.Count - 1);
        top.Exit();
    }
}
