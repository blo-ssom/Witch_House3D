using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 양면 메모 UI — NoteUI의 확장형.
/// 앞면(친구 글씨)/뒷면(실종자 명부) 두 장의 Sprite를 들고
/// R키 또는 우클릭으로 뒤집기 (scaleX 0.5초 토글 + sprite swap).
/// Esc로 닫기 + 닫힘 콜백 호출.
///
/// 인스펙터 구성:
///  - panel             : 전체 UI 부모 (시작 시 비활성)
///  - memoImage         : 메모 Sprite를 보여줄 Image (panel 자식)
///  - hintText          : 안내 텍스트 (TMP) — "R/우클릭: 뒤집기   Esc: 닫기"
///
/// 사용:
///  FlipNoteUI.Instance.Open(frontSprite, backSprite, onClosed);
/// </summary>
public class FlipNoteUI : MonoBehaviour
{
    public static FlipNoteUI Instance;

    [Header("UI 요소")]
    public GameObject panel;
    public Image memoImage;
    public TextMeshProUGUI hintText;

    [Header("애니메이션")]
    [Tooltip("뒤집기 전체 길이 (절반 지점에서 sprite swap)")]
    public float flipDuration = 0.5f;

    [Header("SFX (선택)")]
    public AudioSource audioSource;
    public AudioClip flipSfx;
    public AudioClip openSfx;
    public AudioClip closeSfx;

    [Header("안내 문구")]
    public string hintMessage = "R / 우클릭: 뒤집기    Esc: 닫기";

    private Sprite frontSprite;
    private Sprite backSprite;
    private bool showingFront = true;
    private bool isOpen = false;
    private bool isFlipping = false;
    private Action onClosed;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (!isFlipping && (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1)))
        {
            StartCoroutine(FlipRoutine());
        }
    }

    public void Open(Sprite front, Sprite back, Action onClosedCallback = null)
    {
        frontSprite = front;
        backSprite  = back;
        onClosed    = onClosedCallback;
        showingFront = true;

        if (memoImage != null)
        {
            memoImage.sprite = front;
            var s = memoImage.rectTransform.localScale;
            s.x = 1f;
            memoImage.rectTransform.localScale = s;
        }

        if (hintText != null) hintText.text = hintMessage;

        if (panel != null) panel.SetActive(true);
        isOpen = true;
        isFlipping = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (audioSource != null && openSfx != null)
            audioSource.PlayOneShot(openSfx);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        if (panel != null) panel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (audioSource != null && closeSfx != null)
            audioSource.PlayOneShot(closeSfx);

        var cb = onClosed;
        onClosed = null;
        cb?.Invoke();
    }

    public bool IsOpen() => isOpen;

    private IEnumerator FlipRoutine()
    {
        if (memoImage == null) yield break;

        isFlipping = true;

        if (audioSource != null && flipSfx != null)
            audioSource.PlayOneShot(flipSfx);

        RectTransform rt = memoImage.rectTransform;
        float half = flipDuration * 0.5f;

        // 첫 절반: scaleX 1 → 0
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            var s = rt.localScale;
            s.x = Mathf.Lerp(1f, 0f, k);
            rt.localScale = s;
            yield return null;
        }

        // sprite swap
        showingFront = !showingFront;
        memoImage.sprite = showingFront ? frontSprite : backSprite;

        // 둘째 절반: scaleX 0 → 1
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            var s = rt.localScale;
            s.x = Mathf.Lerp(0f, 1f, k);
            rt.localScale = s;
            yield return null;
        }

        var final = rt.localScale;
        final.x = 1f;
        rt.localScale = final;

        isFlipping = false;
    }
}
