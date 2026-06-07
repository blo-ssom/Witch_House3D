using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 방2 슬라이딩 퍼즐 UI (싱글톤).
/// 이미지 1장을 받아 grid x grid 조각으로 자동 분할 → 타일 클릭으로 빈칸 옆 타일 이동.
/// 완성 시 OpenPuzzle에 넘긴 콜백 호출.
///
/// 사용법 (자동 생성 모드 - 권장):
///  1. 빈 GameObject → 이름: SlidingPuzzleUI, 이 컴포넌트 부착 (그게 끝!)
///  2. autoBuildUI = true 이면 Canvas/패널/보드/닫기버튼/EventSystem을 코드가 자동 생성
///  3. defaultImage 또는 SlidingPuzzleInteract.puzzleImage 중 하나에 사진 Sprite만 넣으면 됨
///
/// 사용법 (수동 모드):
///  - panel/boardRoot를 직접 만들어 연결하면 autoBuildUI여도 그걸 그대로 사용
/// </summary>
public class SlidingPuzzleUI : MonoBehaviour
{
    public static SlidingPuzzleUI Instance;

    [Header("자동 생성")]
    [Tooltip("켜면 panel/boardRoot가 비어있을 때 UI를 코드로 자동 생성")]
    public bool autoBuildUI = true;
    [Tooltip("자동 생성 시 보드 한 변 크기(px). 정사각.")]
    public float boardPixelSize = 600f;

    [Header("UI 참조 (수동 셋업 시에만)")]
    [Tooltip("퍼즐 전체 패널 (시작 시 비활성화). 비우면 자동 생성")]
    public GameObject panel;
    [Tooltip("타일이 배치될 정사각형 RectTransform. 비우면 자동 생성")]
    public RectTransform boardRoot;
    [Tooltip("(선택) 닫기 버튼. 비우면 자동 생성")]
    public Button closeButton;

    [Header("퍼즐 설정")]
    [Tooltip("한 변의 칸 수 (3 = 3x3)")]
    public int gridSize = 3;
    [Tooltip("타일 사이 간격(px)")]
    public float tileGap = 4f;
    [Tooltip("셔플 시 빈칸 무작위 이동 횟수")]
    public int shuffleMoves = 80;
    [Tooltip("타일 슬라이드 애니메이션 시간(초)")]
    public float slideDuration = 0.12f;
    [Tooltip("기본 퍼즐 이미지 (Interact에서 안 넘기면 사용)")]
    public Sprite defaultImage;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip slideSound;
    public AudioClip solveSound;

    // 보드 상태: board[pos] = tileId, 빈칸은 -1
    private int[] board;
    private int blankPos;
    private int tileCount;                 // gridSize * gridSize
    private float tileSize;                // boardRoot 기준 한 칸 픽셀
    private readonly Dictionary<int, RectTransform> tileRects = new Dictionary<int, RectTransform>();

    private bool isOpen = false;
    private bool isAnimating = false;
    private bool solved = false;
    private Action onSolvedCallback;

    // 퍼즐 여는 동안 잠깐 끄는 플레이어 조작들
    private readonly List<Behaviour> disabledControls = new List<Behaviour>();

    private void Awake()
    {
        Instance = this;
        BuildUIIfNeeded();
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePuzzle);
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape) && !solved)
            ClosePuzzle();
    }

    /// <summary>
    /// 퍼즐 열기. image가 null이면 defaultImage 사용. onSolved는 완성 시 1회 호출.
    /// </summary>
    public void OpenPuzzle(Sprite image, Action onSolved)
    {
        if (isOpen) return;

        Sprite src = image != null ? image : defaultImage;
        if (src == null)
        {
            Debug.LogError("[SlidingPuzzle] 퍼즐 이미지가 없습니다 (image/defaultImage 둘 다 null)");
            return;
        }

        onSolvedCallback = onSolved;
        solved = false;
        isOpen = true;

        BuildBoard(src);
        Shuffle();
        RenderInstant();

        if (panel != null) panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerControls(false);
    }

    public void ClosePuzzle()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerControls(true);
    }

    public bool IsOpen() => isOpen;

    // ---------- 보드 생성 ----------

    private void BuildBoard(Sprite src)
    {
        // 기존 타일 제거
        foreach (Transform child in boardRoot) Destroy(child.gameObject);
        tileRects.Clear();

        tileCount = gridSize * gridSize;
        board = new int[tileCount];
        tileSize = Mathf.Min(boardRoot.rect.width, boardRoot.rect.height) / gridSize;

        Texture2D tex = src.texture;
        // 텍스처에서 src가 차지하는 영역 기준으로 분할
        Rect texRect = src.rect;
        float subW = texRect.width / gridSize;
        float subH = texRect.height / gridSize;

        // 타일 0..tileCount-2 생성 (마지막 칸은 빈칸)
        for (int id = 0; id < tileCount - 1; id++)
        {
            int row = id / gridSize;
            int col = id % gridSize;

            // 텍스처 좌표는 아래에서 위로 → 윗줄(row 0)이 텍스처 위쪽
            float px = texRect.x + col * subW;
            float py = texRect.y + (gridSize - 1 - row) * subH;
            Sprite slice = Sprite.Create(
                tex, new Rect(px, py, subW, subH), new Vector2(0.5f, 0.5f), 100f);

            GameObject go = new GameObject($"Tile_{id}", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(boardRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // 좌상단 기준
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(tileSize - tileGap, tileSize - tileGap);

            Image img = go.GetComponent<Image>();
            img.sprite = slice;

            int captured = id;
            go.GetComponent<Button>().onClick.AddListener(() => OnTileClicked(captured));

            tileRects[id] = rt;
        }

        // 초기(완성) 상태: board[i] = i, 마지막은 빈칸
        for (int i = 0; i < tileCount - 1; i++) board[i] = i;
        board[tileCount - 1] = -1;
        blankPos = tileCount - 1;
    }

    // ---------- 셔플 ----------

    private void Shuffle()
    {
        // 완성 상태에서 빈칸을 무작위로 이동 → 항상 풀이 가능한 배치 보장
        int last = -1;
        for (int i = 0; i < shuffleMoves; i++)
        {
            List<int> neighbors = GetNeighbors(blankPos);
            neighbors.RemoveAll(n => n == last); // 직전 위치로 되돌아가는 무의미한 이동 줄이기
            if (neighbors.Count == 0) neighbors = GetNeighbors(blankPos);

            int pick = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
            last = blankPos;
            SwapWithBlank(pick);
        }

        // 혹시 완성 상태로 끝났으면 한 번 더 섞기
        if (IsSolved()) Shuffle();
    }

    // ---------- 입력 ----------

    private void OnTileClicked(int tileId)
    {
        if (!isOpen || isAnimating || solved) return;

        int pos = Array.IndexOf(board, tileId);
        if (pos < 0) return;

        if (IsAdjacent(pos, blankPos))
        {
            StartCoroutine(MoveTile(pos));
        }
    }

    private IEnumerator MoveTile(int pos)
    {
        isAnimating = true;
        int tileId = board[pos];
        RectTransform rt = tileRects[tileId];

        Vector2 from = rt.anchoredPosition;
        Vector2 to = PosToAnchored(blankPos);

        SwapWithBlank(pos);

        if (slideSound != null && audioSource != null)
            audioSource.PlayOneShot(slideSound);

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / slideDuration));
            yield return null;
        }
        rt.anchoredPosition = to;
        isAnimating = false;

        if (IsSolved()) StartCoroutine(OnSolved());
    }

    private IEnumerator OnSolved()
    {
        solved = true;

        if (solveSound != null && audioSource != null)
            audioSource.PlayOneShot(solveSound);

        Debug.Log("[SlidingPuzzle] 퍼즐 완성!");

        yield return new WaitForSecondsRealtime(0.8f);

        ClosePuzzle();
        onSolvedCallback?.Invoke();
    }

    // ---------- 렌더 ----------

    private void RenderInstant()
    {
        for (int pos = 0; pos < tileCount; pos++)
        {
            int tileId = board[pos];
            if (tileId < 0) continue;
            tileRects[tileId].anchoredPosition = PosToAnchored(pos);
        }
    }

    private Vector2 PosToAnchored(int pos)
    {
        int row = pos / gridSize;
        int col = pos % gridSize;
        float x = col * tileSize + tileSize * 0.5f;
        float y = -(row * tileSize + tileSize * 0.5f);
        return new Vector2(x, y);
    }

    // ---------- 보드 헬퍼 ----------

    private void SwapWithBlank(int pos)
    {
        board[blankPos] = board[pos];
        board[pos] = -1;
        blankPos = pos;
    }

    private List<int> GetNeighbors(int pos)
    {
        var list = new List<int>(4);
        int row = pos / gridSize;
        int col = pos % gridSize;
        if (row > 0) list.Add(pos - gridSize);
        if (row < gridSize - 1) list.Add(pos + gridSize);
        if (col > 0) list.Add(pos - 1);
        if (col < gridSize - 1) list.Add(pos + 1);
        return list;
    }

    private bool IsAdjacent(int a, int b)
    {
        int ra = a / gridSize, ca = a % gridSize;
        int rb = b / gridSize, cb = b % gridSize;
        return Mathf.Abs(ra - rb) + Mathf.Abs(ca - cb) == 1;
    }

    private bool IsSolved()
    {
        for (int i = 0; i < tileCount - 1; i++)
            if (board[i] != i) return false;
        return true;
    }

    // ---------- 플레이어 조작 토글 ----------

    private void SetPlayerControls(bool enable)
    {
        if (enable)
        {
            foreach (var c in disabledControls)
                if (c != null) c.enabled = true;
            disabledControls.Clear();
            return;
        }

        DisableControl<PlayerLook>();
        DisableControl<PlayerMove>();
        DisableControl<PlayerInteraction>();
    }

    private void DisableControl<T>() where T : Behaviour
    {
        T comp = FindObjectOfType<T>();
        if (comp != null && comp.enabled)
        {
            comp.enabled = false;
            disabledControls.Add(comp);
        }
    }

    // ---------- UI 자동 생성 ----------

    private void BuildUIIfNeeded()
    {
        if (!autoBuildUI) return;
        if (panel != null && boardRoot != null) return; // 수동 셋업이 이미 있음

        // 1) EventSystem 보장 (버튼 클릭에 필수)
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // 2) 전용 Canvas (다른 UI 위에 그려지도록 sortingOrder 높임)
        var canvasGO = new GameObject("SlidingPuzzleCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 3) 전체 화면 어두운 패널
        var panelGO = new GameObject("SlidingPuzzlePanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        Stretch(panelGO.GetComponent<RectTransform>());
        panelGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
        panel = panelGO;

        // 4) 보드 루트 (중앙 정사각)
        var boardGO = new GameObject("BoardRoot", typeof(RectTransform));
        boardGO.transform.SetParent(panelGO.transform, false);
        var boardRT = boardGO.GetComponent<RectTransform>();
        boardRT.anchorMin = boardRT.anchorMax = new Vector2(0.5f, 0.5f);
        boardRT.pivot = new Vector2(0.5f, 0.5f);
        boardRT.sizeDelta = new Vector2(boardPixelSize, boardPixelSize);
        boardRT.anchoredPosition = Vector2.zero;
        boardRoot = boardRT;

        // 5) 닫기 버튼 (우상단 빨간 사각 — 폰트 의존 없음. ESC로도 닫힘)
        if (closeButton == null)
        {
            var btnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(panelGO.transform, false);
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-40f, -40f);
            brt.sizeDelta = new Vector2(64f, 64f);
            btnGO.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f, 0.95f);
            closeButton = btnGO.GetComponent<Button>();
        }
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
