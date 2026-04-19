using System.Collections;
using UnityEngine;

/// <summary>
/// 방2 퍼즐 매니저.
/// 사진 조각 3개 수집 완료 시 보관함 열리고 열쇠 등장.
///
/// 사용법:
///  1. 빈 GameObject → 이름: PhotoPuzzleManager
///  2. 이 컴포넌트 부착
///  3. lockBox      : 중앙 보관함 오브젝트 연결
///  4. keyToReveal  : KeyItem 오브젝트 연결 (시작 시 비활성화)
///  5. pieceSlots   : 보관함 위에 조각이 끼워지는 슬롯 오브젝트 3개 연결 (시작 시 비활성화)
/// </summary>
public class PhotoPuzzleManager : MonoBehaviour
{
    public static PhotoPuzzleManager Instance;

    [Header("퍼즐 오브젝트")]
    [Tooltip("중앙 보관함/액자 오브젝트")]
    public GameObject lockBox;
    [Tooltip("클리어 시 등장할 KeyItem (시작 시 비활성화)")]
    public GameObject keyToReveal;
    [Tooltip("조각이 끼워지는 슬롯 3개 (시작 시 비활성화)")]
    public GameObject[] pieceSlots = new GameObject[3];

    [Header("보관함 열리는 연출")]
    public float openAngle = 90f;
    public float openSpeed = 2f;

    [Header("조명")]
    public Light[] roomLights;
    [Range(0f, 1f)]
    public float dimRatio = 0.4f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip pieceInsertSound;
    public AudioClip boxOpenSound;
    public AudioClip keyRevealSound;

    /// <summary>
    /// 조각 하나 수집 시 발생 (pieceID 전달). Room2AtmosphereEvent가 구독.
    /// </summary>
    public event System.Action<int> OnPieceCollected;

    /// <summary>
    /// 조각 3개 모두 수집 시 발생. 구독자가 있으면 연출 측에서 RevealKey() 호출 담당.
    /// 구독자가 없으면 즉시 보관함 개방 (기존 동작).
    /// </summary>
    public event System.Action OnAllPiecesCollected;

    private bool[] collected = new bool[3];
    private int collectedCount = 0;
    private bool puzzleSolved = false;

    private void Awake()
    {
        Instance = this;

        // 슬롯 시작 시 비활성화
        foreach (var slot in pieceSlots)
            if (slot != null) slot.SetActive(false);

        if (keyToReveal != null)
            keyToReveal.SetActive(false);
    }

    /// <summary>
    /// PhotoPiece에서 호출
    /// </summary>
    public void CollectPiece(int pieceID)
    {
        if (puzzleSolved) return;
        if (pieceID < 0 || pieceID >= 3) return;
        if (collected[pieceID]) return;

        collected[pieceID] = true;
        collectedCount++;

        // 슬롯에 조각 끼워지는 연출
        if (pieceID < pieceSlots.Length && pieceSlots[pieceID] != null)
            pieceSlots[pieceID].SetActive(true);

        if (audioSource != null && pieceInsertSound != null)
            audioSource.PlayOneShot(pieceInsertSound);

        Debug.Log($"[PhotoPuzzle] {collectedCount}/3 수집");

        // NoteUI로 진행 상황 표시
        if (NoteUI.Instance != null)
        {
            string status = collectedCount < 3
                ? $"사진 조각 {collectedCount}/3\n\n아직 조각이 더 있는 것 같다..."
                : "사진 조각 3/3\n\n모든 조각이 모였다.";
            NoteUI.Instance.OpenNote(status);
        }

        if (OnPieceCollected != null)
            OnPieceCollected.Invoke(pieceID);

        if (collectedCount >= 3)
        {
            if (OnAllPiecesCollected != null)
                OnAllPiecesCollected.Invoke();
            else
                StartCoroutine(SolvePuzzle());
        }
    }

    /// <summary>
    /// Room2MirrorEvent가 거울 연출 완료 후 호출.
    /// </summary>
    public void RevealKey()
    {
        StartCoroutine(SolvePuzzle());
    }

    private IEnumerator SolvePuzzle()
    {
        puzzleSolved = true;

        yield return new WaitForSeconds(1f);

        // 보관함 열기 사운드
        if (audioSource != null && boxOpenSound != null)
            audioSource.PlayOneShot(boxOpenSound);

        // 보관함 열리는 애니메이션
        if (lockBox != null)
            StartCoroutine(OpenBox());

        yield return new WaitForSeconds(0.8f);

        // 조명 어두워짐
        DimLights();

        yield return new WaitForSeconds(0.3f);

        // 열쇠 등장
        if (keyToReveal != null)
        {
            keyToReveal.SetActive(true);

            if (audioSource != null && keyRevealSound != null)
                audioSource.PlayOneShot(keyRevealSound);
        }

        Debug.Log("[PhotoPuzzle] 퍼즐 클리어 → 메인홀 열쇠 등장");
    }

    private IEnumerator OpenBox()
    {
        Quaternion startRot = lockBox.transform.localRotation;
        Quaternion endRot   = startRot * Quaternion.Euler(-openAngle, 0f, 0f);
        float elapsed       = 0f;
        float duration      = 1f / openSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            lockBox.transform.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        lockBox.transform.localRotation = endRot;
    }

    private void DimLights()
    {
        if (roomLights == null) return;
        foreach (var l in roomLights)
            if (l != null) l.intensity *= dimRatio;
    }
}
