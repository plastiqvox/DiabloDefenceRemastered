namespace DiabloDefenceRemastered.Audio;

/// <summary>
/// 소리 서비스 만들기 (소유 WP8, 설계서 7.9). GameApp(WP7)은 이 파일만 부른다. WP1b 가 만든 빈 연결 파일.
/// 구현은 AudioSystem(신호 → 소리, 연결 표)·SoundBank(WAV)·MusicPlayer(페이드·덕킹)와 Core <c>SoundMixer</c>(고르기)로 채운다.
/// WP8(단계 2)이 들어오기 전까지는 던지지 않고 무음 무동작 구현을 돌려준다 → 단계 1 의 GameApp(WP7)·PlayScene(WP6)이 그대로 부를 수 있다.
/// </summary>
public static class AudioServices
{
    public static IAudioServices Create(AppServices app) => new SilentAudioServices();

    /// <summary>무음 무동작(WP8 이 바꾼다). 고르기도 하지 않으므로 <c>AUDIO</c> 수는 0 이다.</summary>
    private sealed class SilentAudioServices : IAudioServices
    {
        public bool Muted => true;
        public void PreloadStage(StageDef stage) { }
        public void Consume(PresentationBuffer buffer, World world) { }
        public void PlayUi(string key) { }
        public void PlayMusic(string key) { }
        public void SetPaused(bool paused) { }
        public void SetVolumes(float master, float sfx, float music) { }
        public void Update(AppTick now) { }
    }
}
