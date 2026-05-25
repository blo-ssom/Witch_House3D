using System.Collections;
using UnityEngine;

/// <summary>
/// 2층 친구의 방 거울 미스디렉션 — 7단계 축약본 (GAME_DESIGN.md).
///
/// 조건 3개 충족 → 시퀀스 발동:
///  (a) 엄마 메인 일기 읽기 (NoteItem.OnNoteRead, motherDiaryNoteID)
///  (b) 메인 열쇠 줍기 (PlayerInventory.HasKey(requiredKey))
///  (c) 책상 서랍 안 친구의 마지막 메모 읽기 (NoteItem.OnNoteRead, friendNoteID)
///       — 서랍은 키 없이 열림 (작은 열쇠 DrawerSmall 폐기, 2026-05-24). 조건은 메모 '읽기'로만 추적
///
/// 시퀀스:
///  1. 조건 3개 충족
///  2. south 입구 문 자동 잠금
///  3. 방 조명 페이드 다운 + 거울 주변 스포트라이트 페이드 인
///  4. WaitUntil(거울 응시) → 거울 속 책상 뒤 귀신 등장 (MirrorOnly 레이어)
///  5. WaitUntil(고개 돌림) → 실제 책상 쪽엔 아무것도 없음 (스크립트 작업 X — 그냥 안 만들면 됨)
///  6. WaitUntil(다시 거울 응시) → 거울 속 귀신 사라짐 + north 문 자동 개방
///  7. 플레이어가 north 문 근접 (Floor2NorthDoorTrigger 콜라이더)
///     → 거울 깨짐 효과 + 현실 귀신 활성화 + GhostChase.StartChase()
///
/// 추격은 north 추격 전용 복도 → FloorBreakTrigger → UnderGround 씬으로 이어짐 (기존 시스템 그대로).
/// </summary>
public class Floor2MirrorEvent : MonoBehaviour
{
    [Header("트리거 조건")]
    [Tooltip("엄마 메인 일기 NoteItem의 noteID")]
    public string motherDiaryNoteID = "floor2_mother_diary";
    [Tooltip("서랍 안 친구의 마지막 메모 NoteItem의 noteID")]
    public string friendNoteID = "floor2_friend_final_note";
    [Tooltip("메인 열쇠 KeyType")]
    public KeyType requiredKey = KeyType.Floor2;

    [Header("문")]
    public DoorInteract entranceDoor;   // south 입구
    public DoorInteract northDoor;      // 추격 복도 출구
    [Tooltip("north 문 개방 시 강제로 한 번 열어줄지 여부")]
    public bool forceOpenNorth = true;

    [Header("거울 응시 판정")]
    public Transform mirror;
    public Camera playerCamera;
    [Range(0f, 1f)]
    [Tooltip("카메라 forward · 거울 방향 dot 임계값. 0.75 = 약 41도 이내")]
    public float gazeDot = 0.75f;

    [Header("조명")]
    public Light mirrorSpotlight;
    public Light[] roomLights;
    [Range(0f, 1f)]
    [Tooltip("방 조명 페이드 다운 후 곱해질 비율")]
    public float roomDimRatio = 0.15f;
    public float lightFadeDuration = 1.2f;
    public float spotlightFadeIn  = 1.2f;

    [Header("거울 속 귀신 (MirrorOnly 레이어)")]
    [Tooltip("거울 속에서만 보일 귀신 GameObject. 시작 시 비활성.")]
    public GameObject mirrorGhost;
    public Transform mirrorGhostPosition;
    public float ghostMinHoldOnFirstGaze = 1.5f;

    [Header("현실 귀신 (Default 레이어)")]
    [Tooltip("실제 추격할 귀신 GameObject. 시작 시 비활성.")]
    public GameObject realGhost;
    public Transform realGhostSpawn;
    public GhostChase ghostChase;

    [Header("거울 깨짐 효과")]
    [Tooltip("거울 깨짐 파편 파티클 / 셰이더 효과 GameObject. 시작 시 비활성.")]
    public GameObject mirrorShatterFx;
    [Tooltip("거울 깨짐 후 본체 거울 Renderer 비활성화 (선택)")]
    public Renderer mirrorRenderer;

    [Header("north 문 근접 트리거")]
    [Tooltip("north 문 앞 트리거 콜라이더 GameObject. 7단계까지 비활성, 6단계에서 활성화.")]
    public GameObject northDoorTriggerObject;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip doorLockSfx;
    public AudioClip ghostAppearSfx;
    public AudioClip ghostVanishSfx;
    public AudioClip northDoorOpenSfx;
    public AudioClip mirrorShatterSfx;
    public AudioClip chaseStartSfx;

    private float[] originalRoomIntensities;
    private float originalSpotlightIntensity;

    private bool diaryRead = false;
    private bool keyPickedUp = false;
    private bool friendNoteRead = false;
    private bool sequenceStarted = false;
    private bool chaseStarted = false;

    private PlayerInventory playerInventory;

    private void Start()
    {
        if (mirrorGhost != null) mirrorGhost.SetActive(false);
        if (realGhost != null) realGhost.SetActive(false);
        if (mirrorShatterFx != null) mirrorShatterFx.SetActive(false);
        if (northDoorTriggerObject != null) northDoorTriggerObject.SetActive(false);

        if (roomLights != null)
        {
            originalRoomIntensities = new float[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null) originalRoomIntensities[i] = roomLights[i].intensity;
        }

        if (mirrorSpotlight != null)
        {
            originalSpotlightIntensity = mirrorSpotlight.intensity;
            mirrorSpotlight.intensity = 0f;
            mirrorSpotlight.enabled = false;
        }

        playerInventory = FindObjectOfType<PlayerInventory>();
    }

    private void OnEnable()
    {
        NoteItem.OnNoteRead += OnNoteRead;
    }

    private void OnDisable()
    {
        NoteItem.OnNoteRead -= OnNoteRead;
    }

    private void Update()
    {
        if (sequenceStarted || playerInventory == null) return;
        if (!keyPickedUp && playerInventory.HasKey(requiredKey))
        {
            keyPickedUp = true;
            CheckTrigger();
        }
    }

    private void OnNoteRead(string noteID)
    {
        if (sequenceStarted) return;
        if (!diaryRead && noteID == motherDiaryNoteID)
        {
            diaryRead = true;
            CheckTrigger();
        }
        else if (!friendNoteRead && noteID == friendNoteID)
        {
            friendNoteRead = true;
            CheckTrigger();
        }
    }

    private void CheckTrigger()
    {
        if (sequenceStarted) return;
        if (diaryRead && keyPickedUp && friendNoteRead)
        {
            sequenceStarted = true;
            StartCoroutine(EventSequence());
        }
    }

    private IEnumerator EventSequence()
    {
        Debug.Log("[Floor2MirrorEvent] 7단계 시작");

        // (2) south 입구 문 자동 잠금
        if (entranceDoor != null)
        {
            entranceDoor.isLocked = true;
            entranceDoor.requiredKey = KeyType.None;
            if (audioSource != null && doorLockSfx != null)
                audioSource.PlayOneShot(doorLockSfx);
        }

        yield return new WaitForSeconds(0.4f);

        // (3) 방 조명 페이드 다운 + 거울 스포트라이트 페이드 인 (동시)
        Coroutine dim = StartCoroutine(FadeRoomLights(roomDimRatio, lightFadeDuration));
        if (mirrorSpotlight != null)
        {
            mirrorSpotlight.enabled = true;
            yield return StartCoroutine(FadeSpotlight(0f, originalSpotlightIntensity, spotlightFadeIn));
        }
        else
        {
            yield return new WaitForSeconds(lightFadeDuration);
        }
        if (dim != null) yield return dim;

        // (4) 거울 응시 대기 → 거울 속 귀신 등장
        yield return new WaitUntil(IsLookingAtMirror);
        if (mirrorGhost != null)
        {
            if (mirrorGhostPosition != null)
                mirrorGhost.transform.position = mirrorGhostPosition.position;
            mirrorGhost.SetActive(true);
        }
        if (audioSource != null && ghostAppearSfx != null)
            audioSource.PlayOneShot(ghostAppearSfx);

        yield return new WaitForSeconds(ghostMinHoldOnFirstGaze);

        // (5) 뒤돌아봄 대기 — 실제 책상 쪽엔 아무것도 없음 (scene 구성으로 보장)
        yield return new WaitUntil(() => !IsLookingAtMirror());

        // (6) 다시 거울 응시 → 귀신 사라짐 + north 문 자동 개방
        yield return new WaitUntil(IsLookingAtMirror);
        if (mirrorGhost != null) mirrorGhost.SetActive(false);
        if (audioSource != null && ghostVanishSfx != null)
            audioSource.PlayOneShot(ghostVanishSfx);

        yield return new WaitForSeconds(0.3f);

        if (northDoor != null)
        {
            northDoor.isLocked = false;
            northDoor.requiredKey = KeyType.None;

            if (forceOpenNorth && playerCamera != null)
                northDoor.ForceOpen(playerCamera.transform);

            if (audioSource != null && northDoorOpenSfx != null)
                audioSource.PlayOneShot(northDoorOpenSfx);
        }

        if (northDoorTriggerObject != null)
            northDoorTriggerObject.SetActive(true);

        Debug.Log("[Floor2MirrorEvent] 6단계 완료 — north 문 개방, 추격 트리거 대기");
    }

    /// <summary>
    /// Floor2NorthDoorTrigger helper가 호출. 1회성. (7단계)
    /// </summary>
    public void TriggerNorthApproach()
    {
        if (chaseStarted) return;
        chaseStarted = true;
        StartCoroutine(MirrorShatterAndChase());
    }

    private IEnumerator MirrorShatterAndChase()
    {
        // 거울 깨짐 효과
        if (audioSource != null && mirrorShatterSfx != null)
            audioSource.PlayOneShot(mirrorShatterSfx);

        if (mirrorShatterFx != null) mirrorShatterFx.SetActive(true);
        if (mirrorRenderer != null) mirrorRenderer.enabled = false;

        yield return new WaitForSeconds(0.2f);

        // 현실 귀신 활성화
        if (realGhost != null)
        {
            if (realGhostSpawn != null)
                realGhost.transform.position = realGhostSpawn.position;
            realGhost.SetActive(true);
        }

        if (audioSource != null && chaseStartSfx != null)
            audioSource.PlayOneShot(chaseStartSfx);

        // 추격 시작
        if (ghostChase != null)
            ghostChase.StartChase();

        Debug.Log("[Floor2MirrorEvent] 7단계 완료 — 거울 폭발 + 추격 시작");
    }

    private bool IsLookingAtMirror()
    {
        if (mirror == null || playerCamera == null) return false;
        Vector3 toMirror = (mirror.position - playerCamera.transform.position).normalized;
        return Vector3.Dot(playerCamera.transform.forward, toMirror) > gazeDot;
    }

    private IEnumerator FadeRoomLights(float toRatio, float duration)
    {
        if (roomLights == null || originalRoomIntensities == null || duration <= 0f) yield break;

        float[] starts = new float[roomLights.Length];
        float[] ends = new float[roomLights.Length];
        for (int i = 0; i < roomLights.Length; i++)
        {
            if (roomLights[i] == null) continue;
            starts[i] = roomLights[i].intensity;
            ends[i] = originalRoomIntensities[i] * toRatio;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null)
                    roomLights[i].intensity = Mathf.Lerp(starts[i], ends[i], k);
            yield return null;
        }
        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i] != null)
                roomLights[i].intensity = ends[i];
    }

    private IEnumerator FadeSpotlight(float from, float to, float duration)
    {
        if (mirrorSpotlight == null || duration <= 0f)
        {
            if (mirrorSpotlight != null) mirrorSpotlight.intensity = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            mirrorSpotlight.intensity = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        mirrorSpotlight.intensity = to;
    }
}
