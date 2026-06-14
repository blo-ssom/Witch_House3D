using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 오프닝 — 검은 화면에서 "원본 편지"를 보여주고, 입력하면 페이드되며 플레이 시작.
///
/// 엔딩(EndingSequence)에서 이 편지가 "...오지 마 / 도망쳐"로 변하는 회수 연출이 있으므로,
/// 시작에서 원본을 보여줘야 엔딩의 임팩트가 산다. (full IntroSequence와 달리
/// 카메라/문을 건드리지 않고 편지 오버레이만 — 게임 시작을 망가뜨릴 위험 최소화)
///
/// 사용법: WH 씬에 빈 GameObject 하나에 부착하면 끝. UI는 런타임 자동 생성.
/// (letterFont를 비우면 씬의 기존 TMP 폰트를 자동 차용 — 한글 깨짐 방지)
/// </summary>
public class OpeningLetterIntro : MonoBehaviour
{
    [Header("편지 내용 (엔딩 originalLetter와 동일하게 유지)")]
    [TextArea(5, 15)]
    public string letterContent =
        "○○에게,\n\n나 여기 있어.\n이 편지가 도착하면 꼭 와줘.\n주소는 뒷면에 적어뒀어.\n\n기다리고 있을게.\n\n— A";

    [Header("타이밍")]
    public float letterFadeDuration = 1.4f;
    public float minReadTime = 1.5f;     // 최소 표시(즉시 스킵 방지)
    public float revealFadeDuration = 1.6f;

    [Header("폰트 (비우면 자동 차용)")]
    public TMP_FontAsset letterFont;

    public bool playOnStart = true;

    [Tooltip("테스트/세이프티용 — true면 입력 없이도 다음으로 진행")]
    public bool forceContinue = false;

    private void Start()
    {
        if (playOnStart) StartCoroutine(Run());
    }

    public IEnumerator Run()
    {
        // ── 플레이어 조작 잠금 ──
        var move = FindObjectOfType<PlayerMove>();
        var look = FindObjectOfType<PlayerLook>();
        var interact = FindObjectOfType<PlayerInteraction>();
        if (move != null) move.enabled = false;
        if (look != null) look.enabled = false;
        if (interact != null) interact.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        // ── UI 자동 생성 ──
        TMP_FontAsset font = letterFont;
        if (font == null)
        {
            var anyTmp = FindObjectOfType<TextMeshProUGUI>();
            if (anyTmp != null) font = anyTmp.font;
        }

        var canvasGO = new GameObject("OpeningLetterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 검은 배경
        var bgGO = new GameObject("Black", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(canvasGO.transform, false);
        Stretch(bgGO.GetComponent<RectTransform>());
        var bg = bgGO.GetComponent<Image>();
        bg.color = Color.black;

        // 편지 텍스트
        var letterGO = new GameObject("LetterText", typeof(RectTransform));
        letterGO.transform.SetParent(canvasGO.transform, false);
        var letter = letterGO.AddComponent<TextMeshProUGUI>();
        if (font != null) letter.font = font;
        letter.text = letterContent;
        letter.fontSize = 40f;
        letter.alignment = TextAlignmentOptions.Center;
        letter.color = new Color(0.88f, 0.85f, 0.78f, 0f);
        letter.lineSpacing = 12f;
        var lrt = letter.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = new Vector2(0f, 30f);
        lrt.sizeDelta = new Vector2(1100f, 700f);

        // 하단 힌트
        var hintGO = new GameObject("Hint", typeof(RectTransform));
        hintGO.transform.SetParent(canvasGO.transform, false);
        var hint = hintGO.AddComponent<TextMeshProUGUI>();
        if (font != null) hint.font = font;
        hint.text = "아무 키나 눌러 계속";
        hint.fontSize = 22f;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.6f, 0.57f, 0.52f, 0f);
        var hrt = hint.rectTransform;
        hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0f, 80f);
        hrt.sizeDelta = new Vector2(800f, 40f);

        // ── 시퀀스 ──
        // 편지 페이드인
        yield return FadeText(letter, 0f, 1f, letterFadeDuration);
        // 최소 읽기 시간
        yield return new WaitForSecondsRealtime(minReadTime);
        // 힌트 등장
        StartCoroutine(FadeText(hint, 0f, 0.8f, 0.8f));

        // 입력 대기
        bool waiting = true;
        while (waiting)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || forceContinue) waiting = false;
            yield return null;
        }

        // 편지/힌트 페이드아웃
        StartCoroutine(FadeText(hint, hint.color.a, 0f, 0.5f));
        yield return FadeText(letter, 1f, 0f, 0.7f);

        // 검은 배경 페이드아웃 → 게임 공개
        float t = 0f;
        while (t < revealFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            var c = bg.color; c.a = Mathf.Lerp(1f, 0f, t / revealFadeDuration); bg.color = c;
            yield return null;
        }

        // ── 조작 복구 (무조건 켬) ──
        if (move != null) move.enabled = true;
        if (look != null) look.enabled = true;
        if (interact != null) interact.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Destroy(canvasGO);
        Debug.Log("[OpeningLetterIntro] 오프닝 완료 → 플레이 시작");
    }

    private IEnumerator FadeText(TMP_Text txt, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            var c = txt.color; c.a = Mathf.Lerp(from, to, t / dur); txt.color = c;
            yield return null;
        }
        var fc = txt.color; fc.a = to; txt.color = fc;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
