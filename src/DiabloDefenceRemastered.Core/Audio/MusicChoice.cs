namespace DiabloDefenceRemastered.Core.Audio;

/// <summary>
/// 곡 고르기 (설계서 7.9 <c>MusicPlayer</c> 의 결정 부분 — 순수 로직, 소유 WP8). 게임 <c>MusicPlayer</c> 는 이 결과대로 읽고·줄이고·튼다.
/// 연결 표 <c>music.&lt;키&gt;</c> → 묶음이 없으면 <c>fallback</c>(6.15·6.17) → <b>지금 곡과 같으면 바꾸지 않음</b>(v0.7 — 같은 액트의 방 이동에서 이어 틂).
/// 로그 줄: <c>AUDIO music=&lt;튼 곡&gt;</c>, 바꿨으면 <c>AUDIO music=&lt;튼 곡&gt; fallback=&lt;원래 곡&gt;</c>.
/// </summary>
public sealed class MusicChoice(SoundRouter router, Func<ContentBundle, bool> has)
{
    /// <summary>지금 곡 ID(없으면 null).</summary>
    public string? Current { get; private set; }

    /// <summary>
    /// 키로 곡을 고른다. 바꿔야 하면 참과 새 곡·로그 줄, 같은 곡이면 거짓(<paramref name="log"/> null).
    /// 곡이 없거나 묶음이 없고 대체 곡도 없으면 거짓과 경고 줄(<paramref name="warning"/>).
    /// </summary>
    public bool TryChange(string key, out MusicDef? music, out string? log, out string? warning)
    {
        music = null;
        log = warning = null;
        if (router.Music(key) is not { } requested)
        {
            warning = $"AUDIO music 키 '{key}' 에 곡이 없다";
            return false;
        }
        if (router.Playable(requested, has) is not { } playable)
        {
            warning = $"AUDIO music={requested.Id} 의 묶음이 없고 대체 곡도 없다";
            return false;
        }
        if (string.Equals(playable.Id, Current, StringComparison.Ordinal)) return false;

        Current = playable.Id;
        music = playable;
        log = ReferenceEquals(playable, requested) ? $"AUDIO music={playable.Id}" : $"AUDIO music={playable.Id} fallback={requested.Id}";
        return true;
    }

    /// <summary>미리 읽을 곡(바꾸지 않음). 없으면 null.</summary>
    public MusicDef? Peek(string key) => router.Music(key) is { } m ? router.Playable(m, has) : null;
}
