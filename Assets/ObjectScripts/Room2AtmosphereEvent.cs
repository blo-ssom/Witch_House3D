using System.Collections;
using UnityEngine;

/// <summary>
/// 방2 해프닝 연출 (MVP).
/// Visage 스타일 "조각마다 짧은 환경 해프닝" + MADiSON 스타일 "완성된 사진이 진실 공개".
/// 음성 대사 없음. SFX + 텍스트(NoteUI) + 시각 효과만 사용.
///
/// PhotoPuzzleManager 이벤트 구독:
///  - OnPieceCollected(0) → 조명 깜빡임 + 벽난로 불 순간 커짐
///  - OnPieceCollected(1) → 책장에서 책 떨어짐 + 저음 드론
///  - OnAllPiecesCollected → (조각 3) 벽난로 불꽃 + 엄마 일기 팝업 → 완성 사진 전환 → 보관함 개방
///
/// 사용법:
///  1. 빈 GameObject에 이 컴포넌트 부착
///  2. photoPuzzle 연결
///  3. roomLights: 방 천장 조명들
///  4. fireplaceLight: 벽난로 Light
///  5. bookOnShelf / bookOnFloor: 책장 위 / 바닥 책 오브젝트 (bookOnFloor는 시작 시 비활성)
///  6. completedPhotoRenderer + completedPhotoRevealed: 완성된 사진 Renderer와 교체 텍스처
///  7. motherDiaryText: 조각 3 수집 후 NoteUI에 뜰 텍스트
///  8. SFX 클립 할당
/// </summary>
public class Room2AtmosphereEvent : MonoBehaviour
{
    [Header("퍼즐 연결")]
    public PhotoPuzzleManager photoPuzzle;

    [Header("방 조명")]
    public Light[] roomLights;
    public float flickerDuration = 1f;
    public float flickerInterval = 0.08f;

    [Header("벽난로")]
    public Light fireplaceLight;
    [Tooltip("불 잠깐 커질 때 배율")]
    public float fireplaceBoostMultiplier = 1.8f;
    public float fireplaceBoostDuration = 0.6f;

    [Header("조각 2 — 책장")]
    [Tooltip("시작 시 활성. 조각 2 수집 시 비활성화")]
    public GameObject bookOnShelf;
    [Tooltip("시작 시 비활성. 조각 2 수집 시 활성화")]
    public GameObject bookOnFloor;

    [Header("조각 3 — 엄마 일기")]
    [TextArea(2, 5)]
    public string motherDiaryText = "왜 자꾸 돌아오는 거니...\n\n여긴 네가 올 곳이 아니야.";

    [Header("완성 연출 — 사진 전환")]
    [Tooltip("완성된 사진이 그려지는 Renderer (보관함 안 사진 등)")]
    public Renderer completedPhotoRenderer;
    [Tooltip("전환될 텍스처 — 엄마와 아들 사진")]
    public Texture completedPhotoRevealed;
    public float photoRevealDelay = 0.5f;
    public float photoRevealHoldTime = 1f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip flickerSfx;
    public AudioClip droneSfx;
    public AudioClip bookDropSfx;
    public AudioClip fireplaceCrackleSfx;

    private float[] originalRoomIntensities;
    private float fireplaceOriginalIntensity;

    private void Start()
    {
        if (bookOnFloor != null) bookOnFloor.SetActive(false);
        if (bookOnShelf != null) bookOnShelf.SetActive(true);

        if (roomLights != null)
        {
            originalRoomIntensities = new float[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null) originalRoomIntensities[i] = roomLights[i].intensity;
        }

        if (fireplaceLight != null)
            fireplaceOriginalIntensity = fireplaceLight.intensity;

        if (photoPuzzle != null)
        {
            photoPuzzle.OnPieceCollected += HandlePieceCollected;
            photoPuzzle.OnAllPiecesCollected += HandleAllCollected;
        }
    }

    private void OnDestroy()
    {
        if (photoPuzzle != null)
        {
            photoPuzzle.OnPieceCollected -= HandlePieceCollected;
            photoPuzzle.OnAllPiecesCollected -= HandleAllCollected;
        }
    }

    private void HandlePieceCollected(int id)
    {
        // 조각 3(id=2)는 HandleAllCollected에서 함께 처리
        if (id == 0) StartCoroutine(Piece1Event());
        else if (id == 1) StartCoroutine(Piece2Event());
    }

    private void HandleAllCollected()
    {
        StartCoroutine(Piece3AndFinalReveal());
    }

    private IEnumerator Piece1Event()
    {
        yield return WaitForNoteClose();

        if (audioSource != null && flickerSfx != null)
            audioSource.PlayOneShot(flickerSfx);

        Coroutine boost = null;
        if (fireplaceLight != null)
            boost = StartCoroutine(FireplaceBoost());

        yield return StartCoroutine(FlickerRoomLights(flickerDuration));

        if (boost != null) yield return boost;
    }

    private IEnumerator Piece2Event()
    {
        yield return WaitForNoteClose();

        if (audioSource != null && droneSfx != null)
            audioSource.PlayOneShot(droneSfx);

        yield return new WaitForSeconds(0.3f);

        if (bookOnShelf != null) bookOnShelf.SetActive(false);
        if (bookOnFloor != null) bookOnFloor.SetActive(true);

        if (audioSource != null && bookDropSfx != null)
            audioSource.PlayOneShot(bookDropSfx);
    }

    private IEnumerator Piece3AndFinalReveal()
    {
        yield return WaitForNoteClose();

        // 벽난로 불꽃 튐 + 순간 커짐
        if (audioSource != null && fireplaceCrackleSfx != null)
            audioSource.PlayOneShot(fireplaceCrackleSfx);

        if (fireplaceLight != null)
            StartCoroutine(FireplaceBoost());

        yield return new WaitForSeconds(0.4f);

        // 엄마 일기 NoteUI 팝업
        if (NoteUI.Instance != null && !string.IsNullOrEmpty(motherDiaryText))
        {
            NoteUI.Instance.OpenNote(motherDiaryText);
            yield return WaitForNoteClose();
        }

        yield return new WaitForSeconds(photoRevealDelay);

        // 완성된 사진 텍스처 전환 (친구 → 엄마+아들)
        if (completedPhotoRenderer != null && completedPhotoRevealed != null)
            completedPhotoRenderer.material.mainTexture = completedPhotoRevealed;

        yield return new WaitForSeconds(photoRevealHoldTime);

        // 보관함 개방 + 열쇠 등장 (PhotoPuzzleManager 기존 로직)
        if (photoPuzzle != null)
            photoPuzzle.RevealKey();

        Debug.Log("[Room2AtmosphereEvent] 완료 → 메인홀 열쇠 개방");
    }

    private IEnumerator WaitForNoteClose()
    {
        // OpenNote 호출과 경쟁 피하려고 한 프레임 대기
        yield return null;
        while (NoteUI.Instance != null && NoteUI.Instance.IsOpen())
            yield return null;
    }

    private IEnumerator FlickerRoomLights(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            SetRoomLights(Random.value > 0.5f);
            yield return new WaitForSeconds(flickerInterval);
            t += flickerInterval;
        }
        SetRoomLights(true);
    }

    private void SetRoomLights(bool on)
    {
        if (roomLights == null) return;
        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i] != null) roomLights[i].enabled = on;
    }

    private IEnumerator FireplaceBoost()
    {
        if (fireplaceLight == null) yield break;

        float target = fireplaceOriginalIntensity * fireplaceBoostMultiplier;
        float half = fireplaceBoostDuration * 0.5f;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            fireplaceLight.intensity = Mathf.Lerp(fireplaceOriginalIntensity, target, t / half);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            fireplaceLight.intensity = Mathf.Lerp(target, fireplaceOriginalIntensity, t / half);
            yield return null;
        }

        fireplaceLight.intensity = fireplaceOriginalIntensity;
    }
}
