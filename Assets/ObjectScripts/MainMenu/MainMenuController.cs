using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 메인메뉴 컨트롤러 — Start / Settings / Quit + 페이드 + BGM.
///
/// 셋업:
///  1. MainMenu.unity 씬에 빈 GameObject "MainMenuManager" 생성, 이 컴포넌트 부착
///  2. 필드 슬롯들 인스펙터에서 연결
///  3. 버튼 OnClick에 OnStartButton / OnSettingsButton / OnQuitButton 등록
///  4. SettingsPanel 안의 BackButton OnClick에 OnCloseSettings 등록
///  5. Build Settings에 MainMenu.unity 가장 위 + WH.unity 두 번째로 등록
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("씬 전환")]
    public string gameSceneName = "WH";

    [Header("UI Panels")]
    public CanvasGroup fadePanel;          // 검정 페이드용
    public GameObject menuButtonsPanel;    // Start/Settings/Quit 버튼 부모
    public GameObject settingsPanel;       // 설정 패널 (초기 비활성)
    public GameObject creditsPanel;        // (선택) 크레딧 패널

    [Header("페이드 타이밍")]
    public float fadeInDuration = 1.5f;
    public float fadeOutDuration = 1.0f;
    public float startDelayBeforeLoad = 0.3f;

    [Header("BGM")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmFadeInVolume = 0.6f;
    public float bgmFadeInDuration = 2f;

    [Header("Hover SFX (선택)")]
    public AudioSource sfxSource;
    public AudioClip hoverClip;
    public AudioClip clickClip;

    private bool isTransitioning = false;

    private void Start()
    {
        // 페이드 패널 초기화 (검정 → 점점 보임)
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.alpha = 1f;
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // 마우스 커서 표시 (게임 중 잠겨있을 수 있으므로)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 설정값 적용 (저장된 볼륨/감도가 있으면 그걸로)
        SettingsManager.ApplyAll();

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // BGM 페이드인
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.volume = 0f;
            bgmSource.loop = true;
            bgmSource.Play();
            StartCoroutine(FadeAudio(bgmSource, 0f, bgmFadeInVolume, bgmFadeInDuration));
        }

        // 페이드인 (검정 → 투명)
        if (fadePanel != null)
        {
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(1f, 0f, t / fadeInDuration);
                yield return null;
            }
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }
    }

    // ========================================================================
    // Button Handlers
    // ========================================================================

    public void OnStartButton()
    {
        if (isTransitioning) return;
        PlayClick();
        StartCoroutine(TransitionToGame());
    }

    public void OnSettingsButton()
    {
        if (isTransitioning) return;
        PlayClick();
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(false);
    }

    public void OnCloseSettings()
    {
        PlayClick();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);
        SettingsManager.SaveAll();
    }

    public void OnCreditsButton()
    {
        if (isTransitioning) return;
        PlayClick();
        if (creditsPanel != null) creditsPanel.SetActive(true);
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(false);
    }

    public void OnCloseCredits()
    {
        PlayClick();
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);
    }

    public void OnQuitButton()
    {
        if (isTransitioning) return;
        PlayClick();
        StartCoroutine(QuitSequence());
    }

    public void OnHover()
    {
        // 버튼 EventTrigger의 PointerEnter에 등록하면 호버 사운드 재생
        if (sfxSource != null && hoverClip != null)
            sfxSource.PlayOneShot(hoverClip);
    }

    // ========================================================================
    // Sequences
    // ========================================================================

    private IEnumerator TransitionToGame()
    {
        isTransitioning = true;

        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            yield return FadeCanvasGroup(fadePanel, 0f, 1f, fadeOutDuration);
        }

        if (bgmSource != null)
            StartCoroutine(FadeAudio(bgmSource, bgmSource.volume, 0f, fadeOutDuration * 0.8f));

        yield return new WaitForSeconds(startDelayBeforeLoad);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator QuitSequence()
    {
        isTransitioning = true;

        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            yield return FadeCanvasGroup(fadePanel, 0f, 1f, fadeOutDuration);
        }

        yield return new WaitForSeconds(0.1f);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private void PlayClick()
    {
        if (sfxSource != null && clickClip != null)
            sfxSource.PlayOneShot(clickClip);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator FadeAudio(AudioSource src, float from, float to, float duration)
    {
        float t = 0f;
        src.volume = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        src.volume = to;
    }
}
