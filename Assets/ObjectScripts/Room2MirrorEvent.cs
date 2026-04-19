using System.Collections;
using UnityEngine;

/// <summary>
/// 방2 거울 연출 이벤트 (종합판).
/// PhotoPuzzleManager.OnAllPiecesCollected 구독 → 조각 3개 완성 시 발동.
///
/// 합친 연출 요소:
///  - 벽난로 녹색 불 (원안)
///  - 조명 깜빡임 + 스포트라이트 + 문 잠김 (Unmourned)
///  - 거울 응시/뒤돌아봄 3사이클 미스디렉션 (Unmourned)
///  - 사이클마다 현실 방 변형 (Layers of Fear)
///  - 거울 속 엄마 실루엣 점점 가까워짐 (Silent Hill 3)
///  - 마지막 뒤돌아봄에 진짜 귀신 등장
///  - 암전 → 조명 복귀 → 의자 회전 잔흔 유지
///  - 퇴장 시 뒤에서 의자 넘어지는 소리 (P.T. 여운) — Room2ExitCue 별도 컴포넌트
///
/// 전체 흐름:
///  1. 대기
///  2. 벽난로 녹색으로 강해짐
///  3. 진입 문 잠김
///  4. 방 조명 깜빡임
///  5. 방 조명 모두 꺼짐 + 거울 스포트라이트만
///  6. [사이클 1] 거울 응시 → 거울 속 핏빛 + 엄마 실루엣 (보관함 쪽 가리킴)
///  7. [사이클 1] 뒤돌아봄 → 벽난로 불 꺼짐 (방 변형 1단계)
///  8. [사이클 2] 거울 응시 → 거울 속 비어있음
///  9. [사이클 2] 뒤돌아봄 → 의자 2개가 거울 쪽으로 돌아감 (방 변형 2단계)
/// 10. [사이클 3] 거울 응시 → 엄마가 더 가까이서 나타남
/// 11. [사이클 3] 뒤돌아봄 → 진짜 귀신이 뒤에 서있음
/// 12. 완전 암전 → 정적
/// 13. 조명 복귀, 귀신/거울 연출 해제, 벽난로 원래 색
/// 14. 방 변형 잔흔 유지 (의자 돌아간 채로)
/// 15. 문 잠금 해제 + PhotoPuzzleManager.RevealKey()
/// 16. Room2ExitCue가 퇴장 순간 의자 넘어지는 SFX 재생
/// </summary>
public class Room2MirrorEvent : MonoBehaviour
{
    [Header("퍼즐 연결")]
    public PhotoPuzzleManager photoPuzzle;
    public DoorInteract entranceDoor;

    [Header("거울 응시 판정")]
    public Transform mirror;
    public Camera playerCamera;
    [Range(0f, 1f)]
    [Tooltip("카메라 forward와 거울 방향의 dot 임계값. 0.75 = 약 41도 이내")]
    public float gazeDot = 0.75f;

    [Header("방 조명")]
    public Light mirrorSpotlight;
    public Light[] roomLights;

    [Header("벽난로")]
    public Light fireplaceLight;
    public GameObject fireplaceFireObject;
    public Color fireplaceGreenColor = new Color(0.2f, 1f, 0.4f);
    public float fireplaceIntensityBoost = 2f;
    public float fireplaceTransitionDuration = 1.5f;

    [Header("거울 속 오브젝트 (MirrorOnly 레이어)")]
    [Tooltip("사이클 1 — 엄마 실루엣이 보관함 쪽 가리킴")]
    public GameObject mirrorGhostPointing;
    [Tooltip("사이클 3 — 엄마가 더 가까이서 나타남")]
    public GameObject mirrorGhostCloser;
    [Tooltip("거울 속 방 핏빛 오버레이 (선택)")]
    public GameObject mirrorBloodOverlay;

    [Header("현실 귀신 (Default 레이어)")]
    public GameObject realGhost;
    public Transform realGhostSpawn;

    [Header("방 변형")]
    [Tooltip("사이클 2 — 거울 쪽으로 회전할 의자들")]
    public Transform[] phase2Chairs;
    public float chairRotationDuration = 0.5f;

    [Header("타이밍")]
    public float preDelay = 1f;
    public float flickerDuration = 2.5f;
    public float spotlightFadeIn = 1f;
    public float ghostInMirrorMinHold = 1.5f;
    public float mirrorEmptyMinHold = 1.5f;
    public float ghostCloserMinHold = 1.5f;
    public float ghostBehindHold = 1f;
    public float blackoutDuration = 1.5f;
    public float silenceDuration = 2f;
    public float lightsReturnDuration = 1.5f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip flickerSfx;
    public AudioClip doorLockSfx;
    public AudioClip whisperSfx;
    public AudioClip whisperCloserSfx;
    public AudioClip silenceAmbient;
    public AudioClip lightOutSfx;
    public AudioClip chairRotateSfx;

    [Header("퇴장 여운 (선택)")]
    [Tooltip("이벤트 완료 시 활성화되는 퇴장 트리거. Room2ExitCue 컴포넌트가 붙어있어야 함.")]
    public Room2ExitCue exitCue;

    private float[] originalRoomIntensities;
    private Color fireplaceOriginalColor;
    private float fireplaceOriginalIntensity;
    private bool eventTriggered = false;

    private void Start()
    {
        if (mirrorGhostPointing != null) mirrorGhostPointing.SetActive(false);
        if (mirrorGhostCloser != null) mirrorGhostCloser.SetActive(false);
        if (mirrorBloodOverlay != null) mirrorBloodOverlay.SetActive(false);
        if (realGhost != null) realGhost.SetActive(false);
        if (mirrorSpotlight != null) mirrorSpotlight.enabled = false;

        if (roomLights != null)
        {
            originalRoomIntensities = new float[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null) originalRoomIntensities[i] = roomLights[i].intensity;
        }

        if (fireplaceLight != null)
        {
            fireplaceOriginalColor = fireplaceLight.color;
            fireplaceOriginalIntensity = fireplaceLight.intensity;
        }

        if (exitCue != null) exitCue.SetArmed(false);

        if (photoPuzzle != null)
            photoPuzzle.OnAllPiecesCollected += StartEvent;
    }

    private void OnDestroy()
    {
        if (photoPuzzle != null)
            photoPuzzle.OnAllPiecesCollected -= StartEvent;
    }

    public void StartEvent()
    {
        if (eventTriggered) return;
        eventTriggered = true;
        StartCoroutine(EventSequence());
    }

    private IEnumerator EventSequence()
    {
        yield return new WaitForSeconds(preDelay);

        // 벽난로 녹색 전환
        if (fireplaceLight != null)
            yield return StartCoroutine(FadeFireplace(
                fireplaceGreenColor,
                fireplaceOriginalIntensity * fireplaceIntensityBoost,
                fireplaceTransitionDuration));

        // 진입 문 잠금
        if (entranceDoor != null)
        {
            entranceDoor.isLocked = true;
            entranceDoor.requiredKey = KeyType.None;
            if (audioSource != null && doorLockSfx != null)
                audioSource.PlayOneShot(doorLockSfx);
        }

        // 조명 깜빡임
        if (audioSource != null && flickerSfx != null)
            audioSource.PlayOneShot(flickerSfx);
        yield return StartCoroutine(FlickerLights(flickerDuration));

        // 방 조명 모두 꺼짐 + 스포트라이트 페이드인
        SetRoomLights(false);
        if (mirrorSpotlight != null)
        {
            float targetIntensity = mirrorSpotlight.intensity;
            mirrorSpotlight.intensity = 0f;
            mirrorSpotlight.enabled = true;
            yield return StartCoroutine(FadeSpotlight(0f, targetIntensity, spotlightFadeIn));
        }

        // === 사이클 1 ===
        yield return new WaitUntil(IsLookingAtMirror);
        if (mirrorBloodOverlay != null) mirrorBloodOverlay.SetActive(true);
        if (mirrorGhostPointing != null) mirrorGhostPointing.SetActive(true);
        if (audioSource != null && whisperSfx != null)
            audioSource.PlayOneShot(whisperSfx);
        yield return new WaitForSeconds(ghostInMirrorMinHold);

        yield return new WaitUntil(() => !IsLookingAtMirror());
        // 현실 방 변형 1단계: 벽난로 불 꺼짐
        if (fireplaceLight != null) fireplaceLight.enabled = false;
        if (fireplaceFireObject != null) fireplaceFireObject.SetActive(false);

        // === 사이클 2 ===
        yield return new WaitUntil(IsLookingAtMirror);
        if (mirrorGhostPointing != null) mirrorGhostPointing.SetActive(false);
        yield return new WaitForSeconds(mirrorEmptyMinHold);

        yield return new WaitUntil(() => !IsLookingAtMirror());
        // 현실 방 변형 2단계: 의자들이 거울 쪽으로 회전
        if (audioSource != null && chairRotateSfx != null)
            audioSource.PlayOneShot(chairRotateSfx);
        StartCoroutine(RotateChairsTowardMirror());

        // === 사이클 3 ===
        yield return new WaitUntil(IsLookingAtMirror);
        if (mirrorGhostCloser != null) mirrorGhostCloser.SetActive(true);
        if (audioSource != null && whisperCloserSfx != null)
            audioSource.PlayOneShot(whisperCloserSfx);
        yield return new WaitForSeconds(ghostCloserMinHold);

        yield return new WaitUntil(() => !IsLookingAtMirror());
        if (mirrorGhostCloser != null) mirrorGhostCloser.SetActive(false);
        if (realGhost != null)
        {
            if (realGhostSpawn != null)
                realGhost.transform.position = realGhostSpawn.position;
            realGhost.SetActive(true);
        }
        yield return new WaitForSeconds(ghostBehindHold);

        // 완전 암전
        if (audioSource != null && lightOutSfx != null)
            audioSource.PlayOneShot(lightOutSfx);
        if (mirrorSpotlight != null)
            yield return StartCoroutine(FadeSpotlight(mirrorSpotlight.intensity, 0f, 0.2f));

        yield return new WaitForSeconds(blackoutDuration);

        // 정적
        if (audioSource != null && silenceAmbient != null)
            audioSource.PlayOneShot(silenceAmbient);
        yield return new WaitForSeconds(silenceDuration);

        // 정리 (의자 회전 잔흔은 유지)
        if (realGhost != null) realGhost.SetActive(false);
        if (mirrorBloodOverlay != null) mirrorBloodOverlay.SetActive(false);
        if (mirrorSpotlight != null) mirrorSpotlight.enabled = false;

        // 벽난로 원래 상태 복원
        if (fireplaceLight != null)
        {
            fireplaceLight.enabled = true;
            if (fireplaceFireObject != null) fireplaceFireObject.SetActive(true);
            yield return StartCoroutine(FadeFireplace(
                fireplaceOriginalColor,
                fireplaceOriginalIntensity,
                1f));
        }

        // 방 조명 복귀
        yield return StartCoroutine(RestoreRoomLights(lightsReturnDuration));

        // 문 해제 + 열쇠 등장
        if (entranceDoor != null)
            entranceDoor.isLocked = false;
        if (photoPuzzle != null)
            photoPuzzle.RevealKey();

        // 퇴장 여운 무장
        if (exitCue != null) exitCue.SetArmed(true);

        Debug.Log("[Room2MirrorEvent] 완료 → 메인홀 열쇠 개방");
    }

    private bool IsLookingAtMirror()
    {
        if (mirror == null || playerCamera == null) return false;
        Vector3 toMirror = (mirror.position - playerCamera.transform.position).normalized;
        return Vector3.Dot(playerCamera.transform.forward, toMirror) > gazeDot;
    }

    private IEnumerator FlickerLights(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            float wait = Random.Range(0.05f, 0.25f);
            bool on = Random.value > 0.4f;
            SetRoomLights(on);
            yield return new WaitForSeconds(wait);
            t += wait;
        }
        SetRoomLights(true);
    }

    private void SetRoomLights(bool on)
    {
        if (roomLights == null) return;
        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i] != null) roomLights[i].enabled = on;
    }

    private IEnumerator FadeSpotlight(float from, float to, float duration)
    {
        if (mirrorSpotlight == null || duration <= 0f)
        {
            if (mirrorSpotlight != null) mirrorSpotlight.intensity = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mirrorSpotlight.intensity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        mirrorSpotlight.intensity = to;
    }

    private IEnumerator FadeFireplace(Color targetColor, float targetIntensity, float duration)
    {
        if (fireplaceLight == null) yield break;

        Color startColor = fireplaceLight.color;
        float startIntensity = fireplaceLight.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fireplaceLight.color = Color.Lerp(startColor, targetColor, t);
            fireplaceLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            yield return null;
        }

        fireplaceLight.color = targetColor;
        fireplaceLight.intensity = targetIntensity;
    }

    private IEnumerator RestoreRoomLights(float duration)
    {
        if (roomLights == null || originalRoomIntensities == null) yield break;

        SetRoomLights(true);
        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i] != null) roomLights[i].intensity = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null)
                    roomLights[i].intensity = Mathf.Lerp(0f, originalRoomIntensities[i], t);
            yield return null;
        }

        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i] != null)
                roomLights[i].intensity = originalRoomIntensities[i];
    }

    private IEnumerator RotateChairsTowardMirror()
    {
        if (phase2Chairs == null || mirror == null) yield break;

        Quaternion[] starts = new Quaternion[phase2Chairs.Length];
        Quaternion[] ends = new Quaternion[phase2Chairs.Length];

        for (int i = 0; i < phase2Chairs.Length; i++)
        {
            if (phase2Chairs[i] == null) continue;
            starts[i] = phase2Chairs[i].rotation;
            Vector3 dir = mirror.position - phase2Chairs[i].position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) { ends[i] = starts[i]; continue; }
            ends[i] = Quaternion.LookRotation(dir.normalized);
        }

        float elapsed = 0f;
        while (elapsed < chairRotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / chairRotationDuration);
            for (int i = 0; i < phase2Chairs.Length; i++)
                if (phase2Chairs[i] != null)
                    phase2Chairs[i].rotation = Quaternion.Slerp(starts[i], ends[i], t);
            yield return null;
        }

        for (int i = 0; i < phase2Chairs.Length; i++)
            if (phase2Chairs[i] != null)
                phase2Chairs[i].rotation = ends[i];
    }
}
