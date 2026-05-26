using System.Collections;
using UnityEngine;

/// <summary>
/// 방2 중앙 기록 보관함.
///
/// 흐름:
///  1. 시작: 자물쇠 잠긴 상태. E키 누르면 "종이가 찢겨 있다. 세 조각이 비어 있다." 안내만.
///  2. PhotoPuzzleManager.OnAllPiecesCollected 수신 → 종이판에 조각 끼워지는 연출 + 자물쇠 떨어짐 + 벽난로 불 꺼짐.
///  3. 보관함 E키 → 양면 메모를 들어올림 → FlipNoteUI.Open(앞, 뒷) 자동 진입.
///  4. FlipNoteUI 닫힘 → 메인홀 열쇠 등장 + 분위기 변화 (방 조명 어두워짐, 거울 손자국 활성화).
///
/// 기존 Room2AtmosphereEvent의 RevealKey 호출과 이중 호출이 일어나도
/// PhotoPuzzleManager 측에서 puzzleSolved 플래그로 한 번만 동작하므로 안전.
/// </summary>
public class MemoryBox : Interactable
{
    [Header("퍼즐 연결")]
    public PhotoPuzzleManager photoPuzzle;

    [Header("종이판 / 자물쇠")]
    [Tooltip("조각 3개가 끼워지는 슬롯들. 시작 시 비활성. 조각 수집 시 PhotoPuzzleManager가 활성화.")]
    public GameObject[] pieceSlotsOnPaper = new GameObject[3];
    [Tooltip("자물쇠 GameObject — OnAllPiecesCollected 시 떨어지거나 비활성화.")]
    public GameObject lockObject;
    [Tooltip("자물쇠 떨어지는 잔해(선택). lockObject 떨어진 자리에 활성화될 prefab/오브젝트.")]
    public GameObject lockFallenObject;

    [Header("양면 메모")]
    [Tooltip("보관함 안에 떠 있는 양면 메모 GameObject (시각용). 픽업 시 비활성화.")]
    public GameObject memoVisual;
    [Tooltip("앞면 텍스처 — 친구 글씨체 경고")]
    public Sprite memoFront;
    [Tooltip("뒷면 텍스처 — 실종자 명부")]
    public Sprite memoBack;

    [Header("열쇠")]
    [Tooltip("메모 닫힘 후 등장할 KeyItem 오브젝트 (시작 시 비활성). PhotoPuzzleManager.keyToReveal과 동일 가능.")]
    public GameObject keyToReveal;

    [Header("분위기 변화 — 메모 닫힘 후")]
    public Light fireplaceLight;
    public GameObject fireplaceFireObject;
    [Tooltip("거울 손자국 데칼 — 메모 닫힘 후 활성화될 자식 오브젝트")]
    public GameObject mirrorHandprint;
    public Light[] roomLights;
    [Range(0f, 1f)]
    [Tooltip("메모 닫힘 후 방 조명 어둡게 곱해질 비율")]
    public float lightsDimRatio = 0.6f;

    [Header("프롬프트")]
    public string promptLocked     = "E : 보관함을 살펴본다";
    public string promptReady      = "E : 양면 메모를 들어올린다";
    [TextArea]
    public string lockedNoteText   = "종이가 찢겨 있다.\n\n세 조각이 비어 있다.";

    [Header("타이밍")]
    public float lockFallDelay     = 0.5f;
    public float memoLiftDelay     = 0.3f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip pieceFitSfx;
    public AudioClip lockFallSfx;
    public AudioClip memoLiftSfx;
    public AudioClip fireplaceOutSfx;
    public AudioClip handprintSfx;

    private bool unlocked    = false;
    private bool memoReadDone = false;

    private void Start()
    {
        if (lockObject != null) lockObject.SetActive(true);
        if (lockFallenObject != null) lockFallenObject.SetActive(false);
        if (memoVisual != null) memoVisual.SetActive(false);
        if (mirrorHandprint != null) mirrorHandprint.SetActive(false);
        if (keyToReveal != null) keyToReveal.SetActive(false);

        if (photoPuzzle != null)
            photoPuzzle.OnAllPiecesCollected += HandleAllCollected;
    }

    private void OnDestroy()
    {
        if (photoPuzzle != null)
            photoPuzzle.OnAllPiecesCollected -= HandleAllCollected;
    }

    public override string GetInteractPrompt()
    {
        if (memoReadDone) return "";
        return unlocked ? promptReady : promptLocked;
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (memoReadDone) return;

        if (!unlocked)
        {
            // 잠긴 상태: 안내 메모만
            if (NoteUI.Instance != null && !string.IsNullOrEmpty(lockedNoteText))
                NoteUI.Instance.OpenNote(lockedNoteText);
            return;
        }

        // 양면 메모 들어올리기
        StartCoroutine(LiftMemoSequence());
    }

    private void HandleAllCollected()
    {
        StartCoroutine(UnlockSequence());
    }

    private IEnumerator UnlockSequence()
    {
        // 종이판에 조각 끼우는 SFX (PhotoPuzzleManager에서 슬롯 활성화는 이미 처리되었음)
        if (audioSource != null && pieceFitSfx != null)
            audioSource.PlayOneShot(pieceFitSfx);

        yield return new WaitForSeconds(lockFallDelay);

        // 자물쇠 떨어짐
        if (audioSource != null && lockFallSfx != null)
            audioSource.PlayOneShot(lockFallSfx);

        if (lockObject != null) lockObject.SetActive(false);
        if (lockFallenObject != null) lockFallenObject.SetActive(true);

        // 벽난로 불 꺼짐 — 조각 3개 수집 즉시
        if (audioSource != null && fireplaceOutSfx != null)
            audioSource.PlayOneShot(fireplaceOutSfx);

        if (fireplaceLight != null) fireplaceLight.enabled = false;
        if (fireplaceFireObject != null) fireplaceFireObject.SetActive(false);

        // 양면 메모 시각화 (떠 있는 효과)
        if (memoVisual != null) memoVisual.SetActive(true);

        unlocked = true;
    }

    private IEnumerator LiftMemoSequence()
    {
        // 중복 방지
        memoReadDone = true;

        if (audioSource != null && memoLiftSfx != null)
            audioSource.PlayOneShot(memoLiftSfx);

        if (memoVisual != null) memoVisual.SetActive(false);

        yield return new WaitForSeconds(memoLiftDelay);

        // FlipNoteUI 자동 진입 + 닫힘 콜백
        if (FlipNoteUI.Instance != null)
        {
            FlipNoteUI.Instance.Open(memoFront, memoBack, OnMemoClosed);
        }
        else
        {
            // FlipNoteUI 없으면 일반 NoteUI로 폴백
            if (NoteUI.Instance != null)
                NoteUI.Instance.OpenNote("(양면 메모 UI 미설정)");
            OnMemoClosed();
        }
    }

    private void OnMemoClosed()
    {
        StartCoroutine(PostMemoSequence());
    }

    private IEnumerator PostMemoSequence()
    {
        // 방 조명 살짝 어둡게 (벽난로 불은 이미 조각 수집 시 꺼짐)
        if (roomLights != null)
        {
            foreach (var l in roomLights)
                if (l != null) l.intensity *= lightsDimRatio;
        }

        yield return new WaitForSeconds(0.3f);

        // 거울 손자국 활성화
        if (mirrorHandprint != null)
        {
            mirrorHandprint.SetActive(true);
            if (audioSource != null && handprintSfx != null)
                audioSource.PlayOneShot(handprintSfx);
        }

        yield return new WaitForSeconds(0.4f);

        // 메인홀 열쇠 등장 — PhotoPuzzleManager가 있으면 그 쪽 RevealKey 활용
        if (photoPuzzle != null)
        {
            photoPuzzle.RevealKey();
        }
        else if (keyToReveal != null)
        {
            keyToReveal.SetActive(true);
        }

        Debug.Log("[MemoryBox] 양면 메모 확인 완료 → 메인홀 열쇠 등장 + 분위기 변화");
    }
}
