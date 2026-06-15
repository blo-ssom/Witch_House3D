using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    public TextMeshProUGUI interactText;

    [Header("일시 메시지(토스트) — 비우면 런타임 자동 생성")]
    [Tooltip("잠긴 문 등 짧은 상황 메시지를 화면 중앙 하단에 잠깐 띄운다.")]
    public TextMeshProUGUI messageText;
    public float defaultMessageDuration = 2.2f;
    public float messageFadeDuration = 0.4f;

    private Coroutine messageRoutine;

    private void Awake()
    {
        Instance = this;
        HideInteractText();
        if (messageText != null) SetMessageAlpha(0f);
    }

    public void ShowInteractText(string message)
    {
        if (interactText == null) return;

        interactText.text = message;
        interactText.gameObject.SetActive(true);
    }

    public void HideInteractText()
    {
        if (interactText == null) return;

        interactText.text = "";
        interactText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 화면 중앙 하단에 짧은 메시지를 잠깐 띄운다(페이드 인/아웃).
    /// 잠긴 문/봉인 등 "왜 안 되는지"를 콘솔이 아니라 플레이어에게 전달.
    /// messageText 슬롯이 비어 있으면 런타임에 자동 생성(한글 폰트는 interactText에서 상속).
    /// </summary>
    public void ShowMessage(string message, float duration = -1f)
    {
        if (duration < 0f) duration = defaultMessageDuration;
        EnsureMessageText();
        if (messageText == null) return;

        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(message, duration));
    }

    private IEnumerator MessageRoutine(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        yield return Fade(0f, 1f, messageFadeDuration);
        yield return new WaitForSeconds(duration);
        yield return Fade(1f, 0f, messageFadeDuration);

        messageText.gameObject.SetActive(false);
        messageRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        if (dur <= 0f) { SetMessageAlpha(to); yield break; }
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            SetMessageAlpha(Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
        SetMessageAlpha(to);
    }

    private void SetMessageAlpha(float a)
    {
        if (messageText == null) return;
        var c = messageText.color;
        c.a = a;
        messageText.color = c;
    }

    // ───────────────────────── 추격 위험 빨간 화면 ─────────────────────────
    [Header("추격 빨간 화면")]
    [Tooltip("추격 중 화면을 물들이는 색 (기본 핏빛 빨강)")]
    public Color dangerColor = new Color(0.6f, 0f, 0f, 1f);
    [Tooltip("거리가 가까울 때 최대 알파")]
    [Range(0f, 1f)] public float dangerMaxAlpha = 0.5f;
    [Tooltip("거리가 멀 때(추격 시작 시) 최소 알파")]
    [Range(0f, 1f)] public float dangerMinAlpha = 0.12f;
    [Tooltip("숨쉬듯 깜빡이는 속도")]
    public float dangerPulseSpeed = 3.2f;

    private Image dangerOverlay;
    private bool dangerActive = false;
    private float dangerLevel = 0f;   // 0(멀다)~1(가깝다) — 외부에서 갱신
    private float dangerCurrentAlpha = 0f;

    /// <summary>추격 시작/종료 시 빨간 화면 on/off.</summary>
    public void SetChaseDanger(bool on)
    {
        EnsureDangerOverlay();
        dangerActive = on;
        if (!on && dangerOverlay != null)
        {
            dangerLevel = 0f;
            // 부드럽게 꺼지도록 코루틴
            StopCoroutine(nameof(FadeOutDanger));
            StartCoroutine(FadeOutDanger());
        }
    }

    /// <summary>플레이어-유령 근접도(0~1)를 매 프레임 전달 — 가까울수록 더 빨갛게.</summary>
    public void SetDangerLevel(float level)
    {
        dangerLevel = Mathf.Clamp01(level);
    }

    /// <summary>잡히는 순간 강한 빨간 플래시.</summary>
    public void FlashCatch()
    {
        EnsureDangerOverlay();
        if (dangerOverlay == null) return;
        dangerActive = false;
        StopCoroutine(nameof(FadeOutDanger));
        StartCoroutine(CatchFlash());
    }

    private void Update()
    {
        if (dangerOverlay == null || !dangerActive) return;

        // 근접도 기반 목표 알파 + 숨쉬는 펄스
        float pulse = (Mathf.Sin(Time.unscaledTime * dangerPulseSpeed) + 1f) * 0.5f; // 0~1
        float targetAlpha = Mathf.Lerp(dangerMinAlpha, dangerMaxAlpha, dangerLevel);
        // 가까울수록 펄스 폭이 커진다
        float pulseAmount = Mathf.Lerp(0.04f, 0.14f, dangerLevel);
        targetAlpha += (pulse - 0.5f) * 2f * pulseAmount;

        dangerCurrentAlpha = Mathf.Lerp(dangerCurrentAlpha, Mathf.Clamp01(targetAlpha), Time.unscaledDeltaTime * 6f);
        SetDangerAlpha(dangerCurrentAlpha);
    }

    private IEnumerator FadeOutDanger()
    {
        while (dangerCurrentAlpha > 0.01f)
        {
            dangerCurrentAlpha = Mathf.Lerp(dangerCurrentAlpha, 0f, Time.unscaledDeltaTime * 5f);
            SetDangerAlpha(dangerCurrentAlpha);
            yield return null;
        }
        dangerCurrentAlpha = 0f;
        SetDangerAlpha(0f);
    }

    private IEnumerator CatchFlash()
    {
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            dangerCurrentAlpha = Mathf.Lerp(0.85f, 0.5f, t / 0.6f);
            SetDangerAlpha(dangerCurrentAlpha);
            yield return null;
        }
    }

    private void SetDangerAlpha(float a)
    {
        if (dangerOverlay == null) return;
        Color c = dangerColor;
        c.a = a;
        dangerOverlay.color = c;
    }

    private void EnsureDangerOverlay()
    {
        if (dangerOverlay != null) return;

        Canvas canvas = interactText != null ? interactText.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("ChaseDangerOverlay", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(dangerColor.r, dangerColor.g, dangerColor.b, 0f);
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        // 다른 UI(상호작용 텍스트 등)보다 뒤에 깔리도록 맨 앞 인덱스로
        go.transform.SetAsFirstSibling();

        dangerOverlay = img;
    }

    private void EnsureMessageText()
    {
        if (messageText != null) return;

        Canvas canvas = interactText != null ? interactText.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("StatusMessageText", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 34f;
        tmp.color = new Color(0.86f, 0.82f, 0.76f, 0f);
        // 한글이 깨지지 않도록 기존 interactText 폰트/머티리얼 상속
        if (interactText != null && interactText.font != null)
        {
            tmp.font = interactText.font;
            if (interactText.fontSharedMaterial != null)
                tmp.fontSharedMaterial = interactText.fontSharedMaterial;
        }

        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -170f);
        rt.sizeDelta = new Vector2(1000f, 120f);

        messageText = tmp;
        messageText.gameObject.SetActive(false);
    }
}
