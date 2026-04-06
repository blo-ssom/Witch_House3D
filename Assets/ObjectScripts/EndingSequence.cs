using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 엔딩 연출: 편지를 다시 보지만 이름이 사라지고 내용이 변해있다.
///
/// 사용법:
///  1. 엔딩 전용 씬 또는 WH.unity에 빈 GameObject → 이 컴포넌트 부착
///  2. EscapeTrigger에서 endingSceneName으로 이 씬을 지정하거나,
///     같은 씬이면 EscapeTrigger.endingPanel 대신 이 스크립트의 StartEnding()을 호출
///  3. UI 연결:
///     - fadePanel: 암전 CanvasGroup (시작 시 alpha=1 상태)
///     - letterPanel: 편지 UI CanvasGroup
///     - letterText: 편지 내용 TextMeshProUGUI
///     - creditsPanel: 크레딧/타이틀 복귀 UI
/// </summary>
public class EndingSequence : MonoBehaviour
{
    public static EndingSequence Instance { get; private set; }

    [Header("암전")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1.5f;

    [Header("편지 UI")]
    public CanvasGroup letterPanel;
    public TextMeshProUGUI letterText;

    [Header("편지 내용")]
    [TextArea(5, 15)]
    public string originalLetter = "○○에게,\n\n나 여기 있어.\n이 편지가 도착하면 꼭 와줘.\n주소는 뒷면에 적어뒀어.\n\n기다리고 있을게.\n\n— A";

    [TextArea(5, 15)]
    public string changedLetter = "  에게,\n\n...오지 마.\n이 편지가 도착하면... 도망쳐.\n\n\n\n—   ";

    [Header("텍스트 변화 연출")]
    public float showOriginalDuration = 3f;
    public float glitchDuration = 1.5f;
    public float showChangedDuration = 4f;

    [Header("크레딧")]
    public CanvasGroup creditsPanel;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip letterSound;
    public AudioClip glitchSound;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (letterPanel != null)
        {
            letterPanel.alpha = 0f;
            letterPanel.gameObject.SetActive(false);
        }

        if (creditsPanel != null)
        {
            creditsPanel.alpha = 0f;
            creditsPanel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 외부에서 호출하여 엔딩 시작 (EscapeTrigger 등에서)
    /// </summary>
    public void StartEnding()
    {
        StartCoroutine(EndingCoroutine());
    }

    private IEnumerator EndingCoroutine()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. 암전 상태에서 시작 → 페이드인
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.alpha = 1f;
        }

        yield return new WaitForSeconds(1f);

        // 2. 편지 표시 (원본)
        if (letterPanel != null)
        {
            letterPanel.gameObject.SetActive(true);
            if (letterText != null)
                letterText.text = originalLetter;

            yield return StartCoroutine(FadeCanvasGroup(letterPanel, 0f, 1f, 1f));
        }

        // 페이드인 (편지가 보이는 상태에서 배경도 살짝 밝아짐)
        if (fadePanel != null)
            yield return StartCoroutine(FadeCanvasGroup(fadePanel, 1f, 0.85f, 1f));

        // 3. 원본 편지를 잠시 보여줌
        yield return new WaitForSeconds(showOriginalDuration);

        // 4. 글리치 연출 → 텍스트 변화
        if (audioSource != null && glitchSound != null)
            audioSource.PlayOneShot(glitchSound);

        yield return StartCoroutine(GlitchTransition());

        // 5. 변화된 편지 표시
        if (letterText != null)
            letterText.text = changedLetter;

        if (audioSource != null && letterSound != null)
            audioSource.PlayOneShot(letterSound);

        yield return new WaitForSeconds(showChangedDuration);

        // 6. 편지 페이드아웃
        if (letterPanel != null)
            yield return StartCoroutine(FadeCanvasGroup(letterPanel, 1f, 0f, 1.5f));

        yield return new WaitForSeconds(1f);

        // 7. 완전 암전
        if (fadePanel != null)
            yield return StartCoroutine(FadeCanvasGroup(fadePanel, fadePanel.alpha, 1f, 1f));

        yield return new WaitForSeconds(1f);

        // 8. 크레딧 표시
        if (creditsPanel != null)
        {
            creditsPanel.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(creditsPanel, 0f, 1f, 2f));
        }
    }

    private IEnumerator GlitchTransition()
    {
        if (letterText == null) yield break;

        string original = originalLetter;
        string target = changedLetter;
        float elapsed = 0f;

        while (elapsed < glitchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / glitchDuration;

            // 글리치: 원본과 변경본을 랜덤으로 섞어서 보여줌
            char[] display = original.ToCharArray();
            for (int i = 0; i < display.Length && i < target.Length; i++)
            {
                if (Random.value < t)
                    display[i] = target[i];
            }

            letterText.text = new string(display);
            yield return new WaitForSeconds(0.05f);
        }

        letterText.text = target;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }
}
