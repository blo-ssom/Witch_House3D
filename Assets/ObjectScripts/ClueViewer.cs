using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 벽/바닥의 그림 단서를 E키로 화면 가득 크게 보여주는 뷰어 (지하 촛불 순서 단서 등).
/// 텍스트가 아니라 '이미지'를 보여줘야 하는 단서용 — NoteUI(텍스트)를 대체한다.
///
/// 사용법:
///  - 단서 Quad(Renderer 있는 오브젝트)에 이 컴포넌트 부착 + Layer=Interactable + Collider
///  - clueTexture를 비워두면 이 오브젝트의 머티리얼 메인 텍스처를 그대로 사용
///  - E로 열고 ESC로 닫는다 (NoteUI와 동일 UX)
/// </summary>
public class ClueViewer : Interactable
{
    [Tooltip("크게 보여줄 단서 텍스처. 비우면 이 오브젝트 Renderer의 메인 텍스처 사용")]
    public Texture clueTexture;
    [Range(0.3f, 0.95f)]
    [Tooltip("화면 높이 대비 단서 이미지 높이 비율")]
    public float screenHeightRatio = 0.82f;
    [Tooltip("이미지 회전 각도(도). 단서가 거꾸로 보이면 180 입력")]
    public float imageRotation = 0f;

    private static GameObject overlayGO;
    private static RawImage rawImage;
    private static bool isOpen;
    public static bool IsViewerOpen => isOpen;

    public override string GetInteractPrompt()
    {
        return isOpen ? "" : "[E] : 살펴보기";
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (isOpen) return;   // 열려 있으면 무시 (닫기는 ESC)
        Open();
    }

    private void Open()
    {
        Texture tex = clueTexture != null ? clueTexture : GetTexFromRenderer();
        if (tex == null) { Debug.LogWarning("[ClueViewer] 보여줄 텍스처가 없음", this); return; }

        EnsureOverlay();
        rawImage.texture = tex;

        float h = Screen.height * screenHeightRatio;
        float aspect = tex.height != 0 ? (float)tex.width / tex.height : 1f;
        rawImage.rectTransform.sizeDelta = new Vector2(h * aspect, h);
        rawImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, imageRotation);

        overlayGO.SetActive(true);
        isOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Close()
    {
        if (overlayGO != null) overlayGO.SetActive(false);
        isOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private Texture GetTexFromRenderer()
    {
        var r = GetComponent<Renderer>();
        return r != null && r.sharedMaterial != null ? r.sharedMaterial.mainTexture : null;
    }

    private void EnsureOverlay()
    {
        if (overlayGO != null) return;

        var canvasGO = new GameObject("ClueViewerCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bg = new GameObject("ClueDim");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.92f);
        bgImg.raycastTarget = false;
        var bgrt = bgImg.rectTransform;
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;

        var imgGO = new GameObject("ClueImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        rawImage = imgGO.AddComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.rectTransform.anchorMin = rawImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rawImage.rectTransform.anchoredPosition = Vector2.zero;

        // 하단 안내
        var hintGO = new GameObject("ClueCloseHint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        var hint = hintGO.AddComponent<Text>();
        hint.text = "[ESC] 닫기";
        hint.alignment = TextAnchor.LowerCenter;
        hint.color = new Color(1f, 1f, 1f, 0.5f);
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 20;
        hint.raycastTarget = false;
        var hrt = hint.rectTransform;
        hrt.anchorMin = new Vector2(0.5f, 0f); hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0f, 30f);
        hrt.sizeDelta = new Vector2(400f, 40f);

        overlayGO = canvasGO;
    }
}
