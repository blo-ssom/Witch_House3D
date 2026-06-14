using System.Collections;
using TMPro;
using UnityEngine;

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
