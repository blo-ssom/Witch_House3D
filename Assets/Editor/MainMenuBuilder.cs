using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// 메인메뉴 UI를 현재 씬에 한 번에 자동 생성.
/// 메뉴: Witch House / Build Main Menu UI
///
/// 빌드되는 것:
///  - Canvas (ScreenSpaceOverlay, 1920x1080 ref)
///  - LeftGradient (PNG sprite)
///  - Title (TMP)
///  - MenuButtonsPanel (StartButton / SettingsButton / QuitButton)
///  - SettingsPanel (Volume / Sensitivity 슬라이더 + BackButton)
///  - FadePanel (CanvasGroup)
///  - EventSystem (없으면 추가)
///  - BGM/SFX AudioSource 2개
///  - MainMenuManager (MainMenuController 부착, 슬롯 자동 와이어)
///  - 모든 버튼 OnClick / 슬라이더 OnValueChanged 자동 연결
/// </summary>
public static class MainMenuBuilder
{
    private static readonly Color TextNormal = new Color(0.75f, 0.69f, 0.62f);
    private static readonly Color TextHover  = Color.white;
    private static readonly Color TextDim    = new Color(0.5f, 0.44f, 0.37f);

    [MenuItem("Witch House/Build Main Menu UI")]
    public static void BuildMainMenu()
    {
        // 0. TMP 폰트 확인
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            EditorUtility.DisplayDialog("TMP 필수 리소스 없음",
                "Window → TextMeshPro → Import TMP Essential Resources 를 먼저 실행하세요.\n" +
                "(기본 폰트가 임포트돼야 메뉴를 만들 수 있습니다.)",
                "OK");
            return;
        }

        // 0.5 기존 빌드 정리
        var existing = GameObject.Find("MainMenuManager");
        if (existing != null)
        {
            bool ok = EditorUtility.DisplayDialog("기존 메뉴 발견",
                "MainMenuManager가 이미 씬에 있습니다.\n삭제하고 다시 빌드할까요?",
                "삭제하고 다시 빌드", "취소");
            if (!ok) return;
            string[] names = { "MainMenuCanvas", "MainMenuManager",
                               "MainMenuBgmSource", "MainMenuSfxSource" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        // 1. EventSystem
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // 2. Canvas
        var canvasGO = new GameObject("MainMenuCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 3. LeftGradient
        var gradSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/MainMenu/left_gradient.png");
        var gradGO = NewUIChild("LeftGradient", canvasGO.transform);
        var gradImg = gradGO.AddComponent<Image>();
        gradImg.sprite = gradSprite;
        gradImg.preserveAspect = false;
        gradImg.raycastTarget = false;
        var gradRT = (RectTransform)gradGO.transform;
        gradRT.anchorMin = new Vector2(0, 0);
        gradRT.anchorMax = new Vector2(0, 1);
        gradRT.pivot = new Vector2(0, 0.5f);
        gradRT.anchoredPosition = Vector2.zero;
        gradRT.sizeDelta = new Vector2(800, 0);

        // 4. Title
        var titleGO = NewUIChild("TitleText", canvasGO.transform);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "WITCH\nHOUSE";
        titleTMP.font = font;
        titleTMP.fontSize = 88;
        titleTMP.color = TextNormal;
        titleTMP.alignment = TextAlignmentOptions.Left;
        titleTMP.characterSpacing = 15;
        titleTMP.lineSpacing = -20;
        titleTMP.fontStyle = FontStyles.Normal;
        var titleRT = titleTMP.rectTransform;
        titleRT.anchorMin = new Vector2(0, 0.5f);
        titleRT.anchorMax = new Vector2(0, 0.5f);
        titleRT.pivot     = new Vector2(0, 1);
        titleRT.anchoredPosition = new Vector2(120, 200);
        titleRT.sizeDelta = new Vector2(600, 260);

        // 5. MenuButtonsPanel
        var btnPanelGO = NewUIChild("MenuButtonsPanel", canvasGO.transform);
        var vlg = btnPanelGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 18;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        var btnPanelRT = (RectTransform)btnPanelGO.transform;
        btnPanelRT.anchorMin = new Vector2(0, 0.5f);
        btnPanelRT.anchorMax = new Vector2(0, 0.5f);
        btnPanelRT.pivot     = new Vector2(0, 0.5f);
        btnPanelRT.anchoredPosition = new Vector2(120, -50);
        btnPanelRT.sizeDelta = new Vector2(400, 300);

        var startBtn    = CreateMenuButton(btnPanelGO.transform, "StartButton",    "시작",   font);
        var settingsBtn = CreateMenuButton(btnPanelGO.transform, "SettingsButton", "설정",   font);
        var quitBtn     = CreateMenuButton(btnPanelGO.transform, "QuitButton",     "나가기", font);

        // 6. SettingsPanel
        var settingsPanelGO = CreateSettingsPanel(canvasGO.transform, font,
            out Slider volumeSlider, out Slider sensSlider,
            out TextMeshProUGUI volumeText, out TextMeshProUGUI sensText,
            out SettingsManager settingsMgr, out Button backBtn);
        settingsPanelGO.SetActive(false);

        // 7. FadePanel
        var fadePanelGO = NewUIChild("FadePanel", canvasGO.transform);
        var fadeImg = fadePanelGO.AddComponent<Image>();
        fadeImg.color = Color.black;
        fadeImg.raycastTarget = false;
        var fadeCG = fadePanelGO.AddComponent<CanvasGroup>();
        fadeCG.alpha = 1f;
        fadeCG.blocksRaycasts = false;
        var fadeRT = (RectTransform)fadePanelGO.transform;
        Stretch(fadeRT);

        // 8. AudioSources
        var bgmGO = new GameObject("MainMenuBgmSource");
        var bgmSrc = bgmGO.AddComponent<AudioSource>();
        bgmSrc.playOnAwake = false;
        bgmSrc.loop = true;
        bgmSrc.volume = 0f;

        var sfxGO = new GameObject("MainMenuSfxSource");
        var sfxSrc = sfxGO.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;

        // 9. MainMenuManager
        var mgrGO = new GameObject("MainMenuManager");
        var mgr = mgrGO.AddComponent<MainMenuController>();
        mgr.fadePanel = fadeCG;
        mgr.menuButtonsPanel = btnPanelGO;
        mgr.settingsPanel = settingsPanelGO;
        mgr.bgmSource = bgmSrc;
        mgr.sfxSource = sfxSrc;
        mgr.gameSceneName = "WH";

        // 10. 슬롯 와이어 (SettingsManager)
        settingsMgr.volumeSlider = volumeSlider;
        settingsMgr.sensitivitySlider = sensSlider;
        settingsMgr.volumeText = volumeText;
        settingsMgr.sensitivityText = sensText;

        // 11. 이벤트 와이어
        UnityEventTools.AddPersistentListener(startBtn.onClick,    mgr.OnStartButton);
        UnityEventTools.AddPersistentListener(settingsBtn.onClick, mgr.OnSettingsButton);
        UnityEventTools.AddPersistentListener(quitBtn.onClick,     mgr.OnQuitButton);
        UnityEventTools.AddPersistentListener(backBtn.onClick,     mgr.OnCloseSettings);

        UnityAction<float> volumeAction = settingsMgr.OnVolumeChanged;
        UnityAction<float> sensAction   = settingsMgr.OnSensitivityChanged;
        UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, volumeAction);
        UnityEventTools.AddPersistentListener(sensSlider.onValueChanged,   sensAction);

        // 12. 마무리
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = mgrGO;

        EditorUtility.DisplayDialog("완료",
            "메인메뉴 UI 빌드 완료!\n\n" +
            "다음 단계:\n" +
            "1. Main Camera에 MainMenuCameraBreath 부착\n" +
            "2. 카메라를 방2 가구 정면에 배치\n" +
            "3. Directional Light 끄고 촛불 Point Light + CandleFlicker 추가\n" +
            "4. Ctrl+S로 씬을 MainMenu.unity 로 저장\n" +
            "5. File → Build Profiles에 MainMenu.unity 등록 (인덱스 0)\n" +
            "6. MainMenuManager의 BGM Clip / SFX Clip 슬롯에 오디오 드래그",
            "OK");
    }

    // ------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------

    private static GameObject NewUIChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Button CreateMenuButton(Transform parent, string name, string label, TMP_FontAsset font)
    {
        var btnGO = NewUIChild(name, parent);
        var rt = (RectTransform)btnGO.transform;
        rt.sizeDelta = new Vector2(320, 60);

        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // 투명 (raycast용)
        img.raycastTarget = true;

        var btn = btnGO.AddComponent<Button>();

        var labelGO = NewUIChild("Label", btnGO.transform);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = label;
        labelTMP.font = font;
        labelTMP.fontSize = 42;
        labelTMP.color = TextNormal;
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.characterSpacing = 15;
        Stretch(labelTMP.rectTransform);

        btn.targetGraphic = labelTMP;
        var c = btn.colors;
        c.normalColor      = TextNormal;
        c.highlightedColor = TextHover;
        c.pressedColor     = TextDim;
        c.selectedColor    = TextHover;
        c.disabledColor    = new Color(0.25f, 0.25f, 0.25f);
        c.fadeDuration     = 0.25f;
        btn.colors = c;

        return btn;
    }

    private static GameObject CreateSettingsPanel(Transform parent, TMP_FontAsset font,
        out Slider volumeSlider, out Slider sensSlider,
        out TextMeshProUGUI volumeText, out TextMeshProUGUI sensText,
        out SettingsManager settingsMgr, out Button backBtn)
    {
        var panelGO = NewUIChild("SettingsPanel", parent);
        Stretch((RectTransform)panelGO.transform);

        // BG (반투명 검정)
        var bgGO = NewUIChild("BG", panelGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.78f);
        Stretch(bgImg.rectTransform);

        // Title
        var titleGO = NewUIChild("Title", panelGO.transform);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Settings.";
        titleTMP.font = font;
        titleTMP.fontSize = 72;
        titleTMP.color = TextNormal;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = 12;
        var titleRT = titleTMP.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot     = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -180);
        titleRT.sizeDelta = new Vector2(700, 100);

        // Rows container
        var rowsGO = NewUIChild("Rows", panelGO.transform);
        var rowsVLG = rowsGO.AddComponent<VerticalLayoutGroup>();
        rowsVLG.spacing = 30;
        rowsVLG.childAlignment = TextAnchor.MiddleCenter;
        rowsVLG.childControlWidth = false;
        rowsVLG.childControlHeight = false;
        rowsVLG.childForceExpandWidth = false;
        rowsVLG.childForceExpandHeight = false;
        var rowsRT = (RectTransform)rowsGO.transform;
        rowsRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowsRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowsRT.pivot     = new Vector2(0.5f, 0.5f);
        rowsRT.anchoredPosition = Vector2.zero;
        rowsRT.sizeDelta = new Vector2(900, 300);

        // Volume Row
        volumeSlider = CreateSettingRow(rowsGO.transform, "VolumeRow", "Volume",
            font, 0f, 1f, 0.8f, false, out volumeText, "80%");

        // Sensitivity Row
        sensSlider = CreateSettingRow(rowsGO.transform, "SensitivityRow", "Sensitivity",
            font, 50f, 600f, 200f, true, out sensText, "200");

        // SettingsBinder
        var binderGO = new GameObject("SettingsBinder");
        binderGO.transform.SetParent(panelGO.transform, false);
        settingsMgr = binderGO.AddComponent<SettingsManager>();
        settingsMgr.sensitivityMin = 50f;
        settingsMgr.sensitivityMax = 600f;

        // BackButton
        var backGO = NewUIChild("BackButton", panelGO.transform);
        var backRT = (RectTransform)backGO.transform;
        backRT.anchorMin = new Vector2(0.5f, 0f);
        backRT.anchorMax = new Vector2(0.5f, 0f);
        backRT.pivot     = new Vector2(0.5f, 0f);
        backRT.anchoredPosition = new Vector2(0, 100);
        backRT.sizeDelta = new Vector2(280, 60);
        var backImg = backGO.AddComponent<Image>();
        backImg.color = new Color(0, 0, 0, 0);
        backBtn = backGO.AddComponent<Button>();

        var backLabelGO = NewUIChild("Label", backGO.transform);
        var backLabel = backLabelGO.AddComponent<TextMeshProUGUI>();
        backLabel.text = "Back.";
        backLabel.font = font;
        backLabel.fontSize = 38;
        backLabel.color = TextNormal;
        backLabel.alignment = TextAlignmentOptions.Center;
        backLabel.characterSpacing = 12;
        Stretch(backLabel.rectTransform);

        backBtn.targetGraphic = backLabel;
        var bc = backBtn.colors;
        bc.normalColor = TextNormal;
        bc.highlightedColor = TextHover;
        bc.pressedColor = TextDim;
        bc.selectedColor = TextHover;
        bc.fadeDuration = 0.25f;
        backBtn.colors = bc;

        return panelGO;
    }

    private static Slider CreateSettingRow(Transform parent, string name, string labelText,
        TMP_FontAsset font, float min, float max, float value, bool wholeNumbers,
        out TextMeshProUGUI valueText, string initialValueText)
    {
        var rowGO = NewUIChild(name, parent);
        var rowRT = (RectTransform)rowGO.transform;
        rowRT.sizeDelta = new Vector2(900, 80);

        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Label
        var lblGO = NewUIChild("Label", rowGO.transform);
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text = labelText;
        lblTMP.font = font;
        lblTMP.fontSize = 32;
        lblTMP.color = TextNormal;
        lblTMP.alignment = TextAlignmentOptions.MidlineLeft;
        lblTMP.characterSpacing = 10;
        ((RectTransform)lblGO.transform).sizeDelta = new Vector2(220, 60);

        // Slider
        var sliderGO = NewUIChild("Slider", rowGO.transform);
        var sliderRT = (RectTransform)sliderGO.transform;
        sliderRT.sizeDelta = new Vector2(450, 30);

        var slider = sliderGO.AddComponent<Slider>();
        var bgGO = NewUIChild("Background", sliderGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        Stretch(bgImg.rectTransform);

        var fillAreaGO = NewUIChild("Fill Area", sliderGO.transform);
        var fillAreaRT = (RectTransform)fillAreaGO.transform;
        fillAreaRT.anchorMin = new Vector2(0, 0.25f);
        fillAreaRT.anchorMax = new Vector2(1, 0.75f);
        fillAreaRT.offsetMin = new Vector2(8, 0);
        fillAreaRT.offsetMax = new Vector2(-8, 0);
        var fillGO = NewUIChild("Fill", fillAreaGO.transform);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.63f, 0.56f, 0.50f, 1f);
        Stretch(fillImg.rectTransform);

        var handleAreaGO = NewUIChild("Handle Slide Area", sliderGO.transform);
        var handleAreaRT = (RectTransform)handleAreaGO.transform;
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(8, 0);
        handleAreaRT.offsetMax = new Vector2(-8, 0);
        var handleGO = NewUIChild("Handle", handleAreaGO.transform);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRT = (RectTransform)handleGO.transform;
        handleRT.sizeDelta = new Vector2(20, 28);

        slider.fillRect = (RectTransform)fillGO.transform;
        slider.handleRect = (RectTransform)handleGO.transform;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.value = value;

        // Value text
        var valGO = NewUIChild("ValueText", rowGO.transform);
        valueText = valGO.AddComponent<TextMeshProUGUI>();
        valueText.text = initialValueText;
        valueText.font = font;
        valueText.fontSize = 28;
        valueText.color = TextHover;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        ((RectTransform)valGO.transform).sizeDelta = new Vector2(140, 60);

        return slider;
    }
}
