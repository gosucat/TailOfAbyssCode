using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SceneType
{
    MainMenu,
    Battlefield,
    Village,

}

public class GameManager : MonoBehaviour
{
    #region 싱글톤
    public static GameManager Instance;
    #endregion

    public SceneType CurrentScene;
    //페이드인아웃
    [SerializeField] Image Fade;
    float shaderTime;
    float shaderUnscaledTime;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        DontDestroyOnLoad(gameObject);

        DOTween.Init();
        DOTween.useSmoothDeltaTime = true;
    }

    /// <summary>
    /// 나중에 캐릭터 선택 관련해서 생각좀 해야할듯
    /// </summary>
    public Character MyCharacter { get; set; } = Character.Bini;

    private void Start()
    {
        StartCoroutine(LoadMainMenu());
    }


    //이제 1씬 게임으로 전환합니다.
    //로드 씬에서 메인 씬으로 이동 후, 씬 자체는 변하지 않습니다.
    IEnumerator LoadMainMenu()
    {
        yield return StartCoroutine(FadeOut());

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainScene");
        // 로딩이 끝날 때까지 대기
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        CurrentScene = SceneType.MainMenu;
        StartCoroutine(LoadSceneCo(SceneType.MainMenu));
    }

    void Update()
    {

        shaderTime += Time.deltaTime; // 스케일된 시간
        Shader.SetGlobalFloat("_GlobalBobTime", shaderTime);
        shaderUnscaledTime += Time.unscaledDeltaTime;
        Shader.SetGlobalFloat("_GlobalUnscaledTime", shaderUnscaledTime);

        if(Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log(Time.timeScale);
        }
    }

    public void LoadScene(SceneType sceneType)
    {
        //Debug.Log($"{sceneType} 전환");
        StartCoroutine(LoadSceneCo(sceneType));
    }

    /// <summary>
    /// 원하는 화면으로 전환합니다.
    /// </summary>
    IEnumerator LoadSceneCo(SceneType sceneType)
    {
        yield return StartCoroutine(FadeOut());

        //기존 요소 해제
        if (CurrentScene == SceneType.Battlefield || CurrentScene == SceneType.Village)
        {
            BattleSceneManager.Instance.Dispose();
        }
        else if (CurrentScene == SceneType.MainMenu)
        { 
            MainMenu.Instance.Dispose();
        }

        CurrentScene = sceneType;

        //새 요소 초기화
        if (sceneType == SceneType.MainMenu)
        {
            //1.MainMenuUI 초기화
            MainMenu.Instance.Init();
            //2.카메라 조정
        }
        else if(sceneType == SceneType.Battlefield)
        {
            //필드 UI 초기화작업
            BattleSceneManager.Instance.InitBattlefield();
        }
        else if (sceneType == SceneType.Village)
        {
            ////필드 UI 초기화작업
            //BattleSceneManager.Instance.
            BattleSceneManager.Instance.InitVillage();
            ////임시 테스트
        }

        yield return Utility.WaitForSeconds(1.0f);
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeOut()
    {
        Fade.gameObject.SetActive(true);
        float alpha = Fade.color.a;

        float delta = 0.02f;

        while(alpha < 1)
        {
            Time.timeScale = 1f;
            Fade.color = new Vector4(0, 0, 0, alpha);
            alpha += delta;
            yield return null;
        }
    }

    IEnumerator FadeIn()
    {
        float alpha = Fade.color.a;

        float delta = 0.02f;

        while (alpha >= 0)
        {
            Time.timeScale = 1f;
            Fade.color = new Vector4(0, 0, 0, alpha);
            alpha -= delta;
            yield return null;
        }

        Fade.gameObject.SetActive(false);
    }


}