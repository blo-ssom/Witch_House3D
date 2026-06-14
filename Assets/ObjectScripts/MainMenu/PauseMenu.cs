using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 인게임 일시정지 메뉴 (ESC).
///
/// - 게임 씬(Player 태그 존재)에 자동 부트스트랩, 메인메뉴에선 생성 안 됨.
/// - UI는 런타임 자동생성(이 프로젝트 관례: OpeningLetterIntro/GameUI). 씬 배치 불필요.
/// - 폰트는 씬의 기존 TMP 폰트를 차용(NanumGothic) → 한글 깨짐 방지.
/// - 구성: [계속하기] [설정(밝기·볼륨·감도)] [메인메뉴로] [게임 종료(앱 닫기)]
/// - 설정은 SettingsManager 재사용(볼륨·밝기 즉시 반영, 감도는 PlayerLook 즉시 갱신).
///
/// ESC 충돌 가드: 노트/퍼즐 UI가 열렸거나(NoteUI/FlipNoteUI/SlidingPuzzleUI),
/// 컷신/게임오버 등으로 조작이 잠겨 있으면(timeScale 0 또는 PlayerLook 비활성) 열지 않음.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private static PauseMenu _instance;

    private const string MAIN_MENU_SCENE = "MainMenu";

    private bool isPaused = false;
    private GameObject canvasGO;
    private GameObject rootPanel;       // 반투명 배경(전체)
    private GameObject mainGroup;       // 메인 버튼 묶음
    private GameObject settingsGroup;   // 설정 서브패널
    private TMP_FontAsset font;
    private SettingsManager settings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySpawn();
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode mode) => TrySpawn();

    private static void TrySpawn()
    {
        if (_instance != null) return;
        if (SceneManager.GetActiveScene().name == MAIN_MENU_SCENE) return;
        if (GameObject.FindWithTag("Player") == null) return; // 게임 씬에만

        var go = new GameObject("~PauseMenu");
        _instance = go.AddComponent<PauseMenu>();
        // DontDestroyOnLoad 안 함 → 씬 전환 시 자동 파괴, 메뉴 복귀 시 잔재 없음
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (isPaused) { Resume(); return; }

        // ── 열기 가드 ──
        if (Time.timeScale == 0f) return;                 // 게임오버/엔딩 등 이미 멈춤
        var look = FindObjectOfType<PlayerLook>();
        if (look == null || !look.enabled) return;        // 컷신/오프닝 중
        if (IsBlockingUIOpen()) return;                   // 노트/퍼즐 열림(ESC는 그쪽이 처리)

        Pause();
    }

    private bool IsBlockingUIOpen()
    {
        if (NoteUI.Instance != null && NoteUI.Instance.IsOpen()) return true;
        var flip = FindObjectOfType<FlipNoteUI>();
        if (flip != null && flip.panel != null && flip.panel.activeSelf) return true;
        var slide = FindObjectOfType<SlidingPuzzleUI>();
        if (slide != null && slide.IsOpen()) return true;
        return false;
    }

    // ========================================================================
    // 일시정지 토글
    // ========================================================================

    public void Pause()
    {
        if (canvasGO == null) BuildUI();

        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = false; // 메뉴 효과음 등 필요 시 들리게(원하면 true로)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerControl(false);

        ShowSettings(false);
        canvasGO.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerControl(true);

        if (canvasGO != null) canvasGO.SetActive(false);
        SettingsManager.SaveAll();
    }

    private void SetPlayerControl(bool on)
    {
        var move = FindObjectOfType<PlayerMove>();
        var look = FindObjectOfType<PlayerLook>();
        var interact = FindObjectOfType<PlayerInteraction>();
        var flash = FindObjectOfType<PlayerFlashlight>();
        if (move != null) move.enabled = on;
        if (look != null) look.enabled = on;
        if (interact != null) interact.enabled = on;
        if (flash != null) flash.enabled = on;
    }

    private void GoMainMenu()
    {
        Time.timeScale = 1f;
        SettingsManager.SaveAll();
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }

    private void QuitGame()
    {
        SettingsManager.SaveAll();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowSettings(bool on)
    {
        if (mainGroup != null) mainGroup.SetActive(!on);
        if (settingsGroup != null) settingsGroup.SetActive(on);
    }

    // ========================================================================
    // UI 빌드 (런타임)
    // ========================================================================

    private void BuildUI()
    {
        // 폰트 차용
        var anyTmp = FindObjectOfType<TextMeshProUGUI>();
        font = anyTmp != null ? anyTmp.font : TMP_Settings.defaultFontAsset;

        canvasGO = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 반투명 검정 배경(클릭 차단)
        rootPanel = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        rootPanel.transform.SetParent(canvasGO.transform, false);
        Stretch((RectTransform)rootPanel.transform);
        rootPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        // 타이틀
        MakeLabel(rootPanel.transform, "일시정지", 64, new Vector2(0.5f, 1f), new Vector2(0f, -180f), 700f, FontStyles.Normal,
            new Color(0.9f, 0.87f, 0.8f, 1f));

        // ── 메인 버튼 그룹 ──
        mainGroup = MakeVGroup("MainGroup", new Vector2(0f, -20f), 24f);
        MakeButton(mainGroup.transform, "계속하기", Resume);
        MakeButton(mainGroup.transform, "설정", () => ShowSettings(true));
        MakeButton(mainGroup.transform, "메인메뉴로", GoMainMenu);
        MakeButton(mainGroup.transform, "게임 종료", QuitGame);

        // ── 설정 서브패널 ──
        settingsGroup = MakeVGroup("SettingsGroup", new Vector2(0f, -20f), 28f);
        settings = gameObject.AddComponent<SettingsManager>();
        settings.enabled = false; // 슬라이더 할당 후 켜서 OnEnable로 초기화

        var volSlider = MakeSliderRow(settingsGroup.transform, "볼륨", 0f, 1f, SettingsManager.LoadVolume(), out var volText);
        var senSlider = MakeSliderRow(settingsGroup.transform, "마우스 감도", settings.sensitivityMin, settings.sensitivityMax, SettingsManager.LoadSensitivity(), out var senText);
        var briSlider = MakeSliderRow(settingsGroup.transform, "밝기", settings.brightnessMin, settings.brightnessMax, SettingsManager.LoadBrightness(), out var briText);

        // SettingsManager 바인딩
        settings.volumeSlider = volSlider; settings.volumeText = volText;
        settings.sensitivitySlider = senSlider; settings.sensitivityText = senText;
        settings.brightnessSlider = briSlider; settings.brightnessText = briText;

        volSlider.onValueChanged.AddListener(settings.OnVolumeChanged);
        senSlider.onValueChanged.AddListener(v =>
        {
            settings.OnSensitivityChanged(v);
            var pl = FindObjectOfType<PlayerLook>();
            if (pl != null) pl.mouseSensitivity = v; // 인게임 즉시 반영
        });
        briSlider.onValueChanged.AddListener(settings.OnBrightnessChanged);

        // 설정 닫기(뒤로)
        MakeButton(settingsGroup.transform, "뒤로", () => ShowSettings(false));

        settings.enabled = true; // OnEnable → 슬라이더/라벨 초기화

        ShowSettings(false);
        canvasGO.SetActive(false);
    }

    // ── UI 헬퍼 ──

    private GameObject MakeVGroup(string name, Vector2 anchoredPos, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(rootPanel.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        var vlg = go.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
        var csf = go.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    private void MakeButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 420f; le.preferredHeight = 64f;
        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        go.AddComponent<UIButtonSound>(); // 클릭/호버 효과음

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        if (font != null) txt.font = font;
        txt.text = label;
        txt.fontSize = 30f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.9f, 0.88f, 0.82f, 1f);
        Stretch(txt.rectTransform);
    }

    private TextMeshProUGUI MakeLabel(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, float width, FontStyles style, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        if (font != null) txt.font = font;
        txt.text = text;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = color;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, size + 20f);
        return txt;
    }

    private Slider MakeSliderRow(Transform parent, string label, float min, float max, float val, out TextMeshProUGUI valueText)
    {
        var row = new GameObject("Row_" + label, typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var rle = row.GetComponent<LayoutElement>();
        rle.preferredWidth = 560f; rle.preferredHeight = 50f;

        // 라벨(왼쪽)
        var lblGO = new GameObject("Label", typeof(RectTransform));
        lblGO.transform.SetParent(row.transform, false);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        if (font != null) lbl.font = font;
        lbl.text = label; lbl.fontSize = 24f;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color = new Color(0.85f, 0.83f, 0.78f, 1f);
        var lrt = lbl.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0.35f, 1f);
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

        // 값(오른쪽)
        var valGO = new GameObject("ValueText", typeof(RectTransform));
        valGO.transform.SetParent(row.transform, false);
        valueText = valGO.AddComponent<TextMeshProUGUI>();
        if (font != null) valueText.font = font;
        valueText.fontSize = 22f;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.color = new Color(0.7f, 0.68f, 0.62f, 1f);
        var vrt = valueText.rectTransform;
        vrt.anchorMin = new Vector2(0.88f, 0f); vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;

        // 슬라이더(가운데)
        var slGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        slGO.transform.SetParent(row.transform, false);
        var srt = (RectTransform)slGO.transform;
        srt.anchorMin = new Vector2(0.37f, 0.25f); srt.anchorMax = new Vector2(0.86f, 0.75f);
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        var slider = slGO.GetComponent<Slider>();

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(slGO.transform, false);
        Stretch((RectTransform)bg.transform);
        bg.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.28f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(slGO.transform, false);
        var fart = (RectTransform)fillArea.transform;
        fart.anchorMin = new Vector2(0f, 0f); fart.anchorMax = new Vector2(1f, 1f);
        fart.offsetMin = new Vector2(0f, 0f); fart.offsetMax = new Vector2(-10f, 0f);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillrt = (RectTransform)fill.transform;
        fillrt.anchorMin = new Vector2(0f, 0f); fillrt.anchorMax = new Vector2(0f, 1f);
        fillrt.sizeDelta = new Vector2(10f, 0f);
        fill.GetComponent<Image>().color = new Color(0.7f, 0.64f, 0.52f, 1f);

        var hsa = new GameObject("Handle Slide Area", typeof(RectTransform));
        hsa.transform.SetParent(slGO.transform, false);
        var hsart = (RectTransform)hsa.transform;
        hsart.anchorMin = new Vector2(0f, 0f); hsart.anchorMax = new Vector2(1f, 1f);
        hsart.offsetMin = new Vector2(10f, 0f); hsart.offsetMax = new Vector2(-10f, 0f);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(hsa.transform, false);
        var hrt = (RectTransform)handle.transform;
        hrt.sizeDelta = new Vector2(22f, 0f);
        handle.GetComponent<Image>().color = new Color(0.95f, 0.93f, 0.88f, 1f);

        slider.fillRect = fillrt;
        slider.handleRect = hrt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max;
        slider.SetValueWithoutNotify(val);
        return slider;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
