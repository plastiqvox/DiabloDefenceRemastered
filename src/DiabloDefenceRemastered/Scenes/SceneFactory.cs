using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 시작 씬 고르기(타이틀 / --stage·--room 바로 시작 / --preview / DataError) (소유 WP6, PreviewScene 은 WP5).
/// GameApp(WP7)은 이 파일만 부른다(+ 화면 흐름 맥락 <see cref="GameFlow.Of"/> — 자동 조작 운전자·화면 신호·종료 요청).
/// v0.7(8.1): StageSelect 없음 — 새 게임 = <c>PlayScene(RunSession)</c>(직업 단계면 HeroSelect → Play). <c>PlayScene</c> 은 <c>RunSession</c> 을 가진다.
/// v0.7.1: <c>PlayScene</c> 은 <c>Advance</c> 앞뒤의 표시 이벤트 소비·<see cref="IWorldBound.Attach"/> 순서(7.11)와, <c>StageStep.Finished</c> 를 처음 받은 앱 틱에 한 번
/// <c>RunSession.Conclude()</c>(판 끝 정산 — 결과·<c>RESULT</c>·<c>RecordResult</c> 는 그 뒤의 <c>RunState</c> 만 읽음)를 지킨다.
/// </summary>
public static class SceneFactory
{
    /// <summary>가져오기 명령 한 줄(1.4 끝 — 자원 없음 안내).</summary>
    public const string ImportCommand = @"tools\import-all.ps1 -Diabdat <diabdat.mpq> -Exe <DiabloDefence.exe>";

    /// <summary>
    /// 시작 씬: 데이터 오류면 DataError, <c>--preview</c> 면 Preview(WP5), <c>--stage</c>·<c>--room</c> 이면 그 판을 바로(타이틀 없이), 아니면 타이틀.
    /// <paramref name="content"/> = 시작 검사(WP7)가 만든 있는 묶음(꺼진 직업·타이틀 한 줄) — 없으면 모두 있음.
    /// </summary>
    public static IScene CreateStart(AppServices app, ContentAvailability? content = null)
    {
        var flow = GameFlow.Of(app);
        if (content is not null) flow.Content = content;
        flow.NextSeed = app.Options.Seed;
        var (master, sfx, music) = flow.Volumes(app.Save.Settings);
        app.Audio?.SetVolumes(master, sfx, music);   // 시작 때 음량(8.3 — 저장에 없으면 rules.audio.defaults)
        if (app.HasDataErrors) return CreateDataError(app);
        if (app.Options.Preview is { } target) return new PreviewScene(app, target);
        if (app.Options.StageId is not null || app.Options.StartRoom is not null)
            return TitleScene.StartRun(flow, flow.ResolveHero());
        return new TitleScene(flow);
    }

    public static IScene CreateDataError(AppServices app) => DataErrorScene.ForDataErrors(GameFlow.Of(app));

    /// <summary>
    /// 자원 없음·낡음 안내(1.4 끝 ①~⑤ — DataError 화면 재사용): <paramref name="problems"/> = 무엇이 없는지(시작 검사 WP7 의 문장), 끝에 가져오기 명령 한 줄.
    /// </summary>
    public static IScene CreateAssetError(AppServices app, IReadOnlyList<string> problems) =>
        new DataErrorScene(GameFlow.Of(app), "자원이 없거나 낡았다", problems, ImportCommand);
}
