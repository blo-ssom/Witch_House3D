using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 게임오버 UI 관리 + 재시작 버튼.
///
/// 사용법:
///  1. GameOverUI 패널 오브젝트에 부착
///  2. RestartButton의 OnClick() → GameOverManager.Restart() 연결
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        Instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// GhostChase에서 호출
    /// </summary>
    public void ShowGameOver()
    {
        EnsureGameOverUI();   // 패널 미연결(지하 등)이면 런타임 생성
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // 플레이어 멈추기
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// gameOverPanel이 비어 있으면(지하 등 패널 미배치 씬) 런타임으로 게임오버 UI를 만든다.
    /// </summary>
    private void EnsureGameOverUI()
    {
        if (gameOverPanel != null) return;

        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("GameOverCanvas_Runtime");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 검정 배경
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGO.transform, false);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one; bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

        // 텍스트
        var txtGO = new GameObject("GameOverText", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(canvasGO.transform, false);
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f); trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 70f); trt.sizeDelta = new Vector2(900f, 180f);
        var txt = txtGO.GetComponent<Text>();
        txt.text = "YOU DIED"; txt.font = font; txt.fontSize = 84; txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter; txt.color = new Color(0.62f, 0.05f, 0.05f);

        // 재시작 버튼
        var btnGO = new GameObject("RetryButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(canvasGO.transform, false);
        var brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f); brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, -90f); brt.sizeDelta = new Vector2(280f, 72f);
        btnGO.GetComponent<Image>().color = new Color(0.16f, 0.05f, 0.05f, 0.96f);
        btnGO.GetComponent<Button>().onClick.AddListener(Restart);

        var lblGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        lblGO.transform.SetParent(btnGO.transform, false);
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var lbl = lblGO.GetComponent<Text>();
        lbl.text = "RETRY"; lbl.font = font; lbl.fontSize = 32; lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = new Color(0.85f, 0.8f, 0.75f);

        gameOverPanel = canvasGO;
        gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// 재시작 버튼 OnClick에 연결
    /// </summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
