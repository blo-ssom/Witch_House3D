using UnityEngine;

/// <summary>
/// 방2 사진 액자에 붙이는 상호작용 컴포넌트.
/// E키 → SlidingPuzzleUI 열기. 완성 시 PhotoPuzzleManager.RevealKey() 호출(메인홀 열쇠 등장).
///
/// 사용법:
///  1. 방2 액자/그림 오브젝트(콜라이더 + Interact 레이어)에 부착
///  2. puzzleImage : 맞출 사진 Sprite 연결 (비워두면 SlidingPuzzleUI.defaultImage 사용)
///  3. 씬에 SlidingPuzzleUI, PhotoPuzzleManager 존재해야 함
/// </summary>
public class SlidingPuzzleInteract : Interactable
{
    [Header("퍼즐")]
    [Tooltip("맞출 사진 Sprite (비우면 SlidingPuzzleUI의 기본 이미지 사용)")]
    public Sprite puzzleImage;

    [Header("프롬프트")]
    public string unsolvedPrompt = "E : 흐트러진 사진을 맞추다";
    public string solvedPrompt   = "E : 완성된 사진";

    [Header("완성 시 분위기")]
    [Tooltip("완성 시 끌 오브젝트들 (벽난로 불 파티클·라이트 등) — SetActive(false)")]
    public GameObject[] turnOffOnSolve;
    [Tooltip("방2에 가둔 입구 문 — 완성 시 봉인/잠금 해제")]
    public DoorInteract exitDoor;

    private bool solved = false;

    private void Start()
    {
        interactPrompt = unsolvedPrompt;
    }

    public override string GetInteractPrompt()
    {
        return solved ? solvedPrompt : unsolvedPrompt;
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (solved)
        {
            // 이미 완성됨 — 다시 봐도 그냥 사진만 보여주고 싶으면 여기서 처리
            return;
        }

        if (SlidingPuzzleUI.Instance == null)
        {
            Debug.LogError("[SlidingPuzzleInteract] 씬에 SlidingPuzzleUI가 없습니다.");
            return;
        }

        SlidingPuzzleUI.Instance.OpenPuzzle(puzzleImage, OnSolved);
    }

    /// <summary>[치트/외부] 퍼즐을 즉시 완성 처리 (UI 안 열고 바로).</summary>
    public void ForceSolve()
    {
        if (solved) return;
        OnSolved();
    }

    private void OnSolved()
    {
        solved = true;
        interactPrompt = solvedPrompt;

        Debug.Log("[SlidingPuzzleInteract] 퍼즐 완성 → 메인홀 열쇠 등장 요청");

        if (PhotoPuzzleManager.Instance != null)
            PhotoPuzzleManager.Instance.RevealKey();
        else
            Debug.LogWarning("[SlidingPuzzleInteract] PhotoPuzzleManager가 없어 열쇠를 등장시키지 못함");

        // 벽난로 불·라이트 끄기
        if (turnOffOnSolve != null)
            foreach (var go in turnOffOnSolve)
                if (go != null) go.SetActive(false);

        // 가둔 입구 문 봉인/잠금 해제 (이제 나갈 수 있음)
        if (exitDoor != null)
        {
            exitDoor.isSealed = false;
            exitDoor.isLocked = false;
        }
    }
}
