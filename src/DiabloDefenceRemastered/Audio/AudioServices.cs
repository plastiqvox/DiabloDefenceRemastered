using System.Runtime.InteropServices;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Core.Audio;
using Microsoft.Xna.Framework.Audio;

namespace DiabloDefenceRemastered.Audio;

/// <summary>
/// 소리 서비스 만들기 (소유 WP8, 설계서 7.9). GameApp(WP7)은 이 파일만 부른다.
/// <see cref="AudioSystem"/>(신호 → 소리) = Core <see cref="SoundPipeline"/>(연결 표·고르기) + <see cref="SoundBank"/>(WAV·목소리 칸) + <see cref="MusicPlayer"/>(페이드·덕킹).
/// <para>
/// 무음(<see cref="AppOptions.Muted"/>, 개발 옵션 실행의 기본값)이면 효과음 파일을 읽지 않고 재생하지 않지만 고르기는 그대로 돌린다 — 길이 색인(<c>d1/sound/index.json</c>)이
/// 같으므로 <c>AUDIO</c> 수가 소리 켬과 같다. 장치를 못 열면(<see cref="NoAudioHardwareException"/> 등) 경고 후 무음으로 계속한다.
/// 자원 폴더 = 실행 폴더의 <c>Assets</c>(csproj 가 <c>DdrAssetDir</c> 을 그리로 복사, 1.3).
/// </para>
/// <para>
/// 로그: 만들 때 <c>AUDIO device="&lt;장치 이름&gt;"</c>(null 드라이버면 <c>No Output</c>), 끝낼 때(<see cref="IDisposable.Dispose"/>)
/// <c>AUDIO muted=… requested=… played=… dropped_cap=… dropped_interval=… dropped_offscreen=…</c>, 곡을 바꿀 때 <c>AUDIO music=…</c>.
/// </para>
/// </summary>
public static class AudioServices
{
    public const string SoundIndexPath = "d1/sound/index.json";
    public const string ManifestPath = "d1/manifest.json";

    public static IAudioServices Create(AppServices app) => Create(app, Path.Combine(AppContext.BaseDirectory, "Assets"));

    /// <summary>자원 폴더를 따로 주는 판(도구·시험 실행).</summary>
    public static IAudioServices Create(AppServices app, string assetRoot)
    {
        var log = app.Log;
        var rules = app.Defs.Rules.Audio;
        if (rules is null)
        {
            log.Warn("AUDIO rules.audio 가 없다 — 소리 없이 계속");
            return new SilentAudioServices();
        }

        var muted = app.Options.Muted;
        var device = DeviceName();
        log.Info($"AUDIO device=\"{device}\"");
        if (!muted && !DeviceUsable(log)) muted = true;

        var index = ReadSoundIndex(assetRoot, log);
        var availability = ReadAvailability(assetRoot, log);
        return new AudioSystem(app.Defs, rules, assetRoot, new SoundIndexDurations(index), availability, app.Options.Seed, muted, log);
    }

    private static SoundIndex ReadSoundIndex(string assetRoot, IAppLog log)
    {
        var path = Path.Combine(assetRoot, SoundIndexPath);
        try
        {
            if (File.Exists(path))
            {
                var index = SoundIndex.Read(path);
                foreach (var e in index.Validate()) log.Warn($"AUDIO 소리 색인: {e}");
                return index;
            }
            log.Warn($"AUDIO 소리 색인이 없다({SoundIndexPath}) — 모든 소리 길이를 1틱으로 센다");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            log.Warn($"AUDIO 소리 색인을 읽지 못했다({ex.Message}) — 모든 소리 길이를 1틱으로 센다");
        }
        return new SoundIndex { Format = SoundIndex.CurrentFormat };
    }

    private static ContentAvailability ReadAvailability(string assetRoot, IAppLog log)
    {
        var path = Path.Combine(assetRoot, ManifestPath);
        try
        {
            if (File.Exists(path)) return ContentAvailability.From(AssetManifest.Read(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            log.Warn($"AUDIO manifest 를 읽지 못했다({ex.Message}) — 묶음은 모두 있는 것으로 본다");
        }
        return ContentAvailability.All;
    }

    /// <summary>MonoGame 이 연 OpenAL 장치를 쓸 수 있나. 못 쓰면 경고하고 거짓(무음으로 계속, 7.9).</summary>
    private static bool DeviceUsable(IAppLog log)
    {
        try
        {
            _ = SoundEffect.MasterVolume;   // 장치 초기화가 실패했으면 여기서 던진다
            return true;
        }
        catch (Exception ex) when (ex is NoAudioHardwareException or InstancePlayLimitException or InvalidOperationException or DllNotFoundException or TypeInitializationException)
        {
            log.Warn($"AUDIO 오디오 장치를 열지 못했다({ex.GetType().Name}: {ex.Message}) — 무음으로 계속");
            return false;
        }
    }

    /// <summary>
    /// 지금 OpenAL 장치 이름(<c>alcGetString(ALC_DEVICE_SPECIFIER)</c>). MonoGame DesktopGL 이 싣는 OpenAL Soft(<c>openal</c>)에 직접 묻는다 —
    /// null 드라이버(<c>ALSOFT_DRIVERS=null</c>)면 <c>No Output</c>. 묻지 못하면 환경 변수로 짐작한다.
    /// </summary>
    public static string DeviceName()
    {
        try
        {
            var context = OpenAl.alcGetCurrentContext();
            if (context != IntPtr.Zero)
            {
                var device = OpenAl.alcGetContextsDevice(context);
                var name = device == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(OpenAl.alcGetString(device, OpenAl.DeviceSpecifier));
                if (!string.IsNullOrEmpty(name)) return name;
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
        }
        return string.Equals(Environment.GetEnvironmentVariable("ALSOFT_DRIVERS"), "null", StringComparison.OrdinalIgnoreCase) ? "No Output" : "unknown";
    }

    private static class OpenAl
    {
        public const int DeviceSpecifier = 0x1005;

        [DllImport("openal", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr alcGetCurrentContext();

        [DllImport("openal", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr alcGetContextsDevice(IntPtr context);

        [DllImport("openal", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr alcGetString(IntPtr device, int param);
    }

    /// <summary>
    /// 무음 무동작(<c>rules.audio</c> 가 없는 정의 — 시험·도구 실행). 고르기도 하지 않으므로 <c>AUDIO</c> 수는 0 이다.
    /// 보통 실행은 <see cref="AudioSystem"/> 이다(무음이어도 고르기는 돈다).
    /// </summary>
    private sealed class SilentAudioServices : IAudioServices
    {
        public bool Muted => true;
        public void PreloadStage(StageDef stage) { }
        public void Attach(World world) { }
        public void Consume(PresentationBuffer buffer, World world) { }
        public void PlayUi(string key) { }
        public void PlayMusic(string key) { }
        public void SetPaused(bool paused) { }
        public void SetVolumes(float master, float sfx, float music) { }
        public void Update(AppTick now) { }
        public void Dispose() { }
    }
}
