using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 2층 복도 바닥 판자 붕괴 → 지하 추락 → 씬 전환의 시네마틱 연출.
///
/// 연출 단계
///  A. 예고(Telegraph) : 삐걱 SFX + 미세 흔들림 + 판자 떨림 (무너질 거라는 빌드업)
///  B. 붕괴(Collapse)   : 굉음 + 강한 흔들림 + 먼지 폭발 + 판자 순차 낙하
///  C. 추락(Fall)       : 시야를 발밑으로 꺾음 + 롤 흔들림 + FOV 펀치 + 바람소리 + 가속 낙하
///  D. 충격(Impact)     : 착지 쿵 + 강한 흔들림 + 암전 → UnderGround 로드
///
/// 사용법:
///  1. 빈 GameObject → Box Collider(IsTrigger) → 이 컴포넌트 부착
///  2. planks 배열에 판자 큐브들 연결 (Rigidbody 없어도 자동 추가)
///  3. undergroundSceneName = "UnderGround"
///  4. 카메라/CameraShake/SFX는 비워두면 자동 탐색·무음 처리 (있으면 더 풍부)
/// </summary>
public class FloorBreakTrigger : MonoBehaviour
{
    [Header("판자 오브젝트들")]
    [Tooltip("부서질 판자 큐브 배열")]
    public GameObject[] planks;

    [Header("씬 전환")]
    public string undergroundSceneName = "UnderGround";

    [Header("타이밍")]
    [Tooltip("붕괴 전 삐걱대는 예고 시간")]
    public float telegraphDuration = 0.7f;
    [Tooltip("판자가 하나씩 무너지는 간격")]
    public float breakDelay = 0.06f;
    [Tooltip("추락 연출 길이")]
    public float fallDuration = 1.6f;
    [Tooltip("암전 시간")]
    public float fadeOutDuration = 0.7f;
    [Tooltip("착지 충격 후 씬 전환까지 여운")]
    public float postImpactDelay = 0.35f;

    [Header("추락 연출 강도")]
    [Tooltip("플레이어가 떨어지는 거리(m)")]
    public float fallDistance = 14f;
    [Tooltip("추락 중 시야가 발밑으로 꺾이는 각도")]
    public float lookDownAngle = 72f;
    [Tooltip("추락 중 FOV 증가량(빨려드는 느낌)")]
    public float fovPunch = 22f;
    [Tooltip("추락 중 좌우 롤 흔들림 최대 각")]
    public float fallRollAmount = 6f;

    [Header("카메라/흔들림 (비우면 자동 탐색)")]
    public Camera playerCamera;
    public CameraShake cameraShake;

    [Header("먼지 (선택 — 프리팹 없으면 런타임 생성)")]
    public GameObject dustBurstPrefab;

    [Header("SFX (선택 — 비우면 무음)")]
    public AudioSource audioSource;
    [Tooltip("예고: 낮게 삐걱대는 나무")]
    public AudioClip creakSound;
    [Tooltip("판자 하나씩 부서질 때")]
    public AudioClip breakSound;
    [Tooltip("붕괴 순간 굉음")]
    public AudioClip collapseSound;
    [Tooltip("추락 중 바람/휘몰아침")]
    public AudioClip fallWindSound;
    [Tooltip("착지 충격음")]
    public AudioClip impactSound;

    [Header("암전 UI")]
    [Tooltip("검정 Panel UI (FadePanel)")]
    public CanvasGroup fadePanel;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(FloorBreakSequence(other.gameObject));
    }

    private IEnumerator FloorBreakSequence(GameObject player)
    {
        Debug.Log("[FloorBreak] 밟힘! 붕괴 시퀀스 시작.");

        // 참조 자동 탐색
        if (playerCamera == null) playerCamera = Camera.main;
        if (cameraShake == null && playerCamera != null)
            cameraShake = playerCamera.GetComponent<CameraShake>();

        // AudioSource가 비어 있으면 자동 생성 → 클립만 슬롯에 넣으면 바로 울림
        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;   // 스크립트 연출음이라 2D로 확실히 들리게
        }

        var playerMove = player.GetComponent<PlayerMove>();
        var playerLook = player.GetComponentInChildren<PlayerLook>();
        var controller = player.GetComponent<CharacterController>();

        // ─────────────────────────────────────────────
        // A. 예고 — 삐걱 + 미세 흔들림 + 판자 떨림
        // ─────────────────────────────────────────────
        PlaySfx(creakSound, 0.9f);
        if (cameraShake != null) cameraShake.Shake(0.035f, telegraphDuration);

        // 판자 원래 위치/회전 기억 후 떨림
        var orig = new Vector3[planks.Length];
        for (int i = 0; i < planks.Length; i++)
            if (planks[i] != null) orig[i] = planks[i].transform.position;

        float tEl = 0f;
        while (tEl < telegraphDuration)
        {
            tEl += Time.deltaTime;
            float ramp = tEl / telegraphDuration;               // 갈수록 격해짐
            float amp  = Mathf.Lerp(0.005f, 0.025f, ramp);
            for (int i = 0; i < planks.Length; i++)
            {
                if (planks[i] == null) continue;
                Vector3 j = new Vector3(
                    (Random.value - 0.5f) * amp,
                    (Random.value - 0.5f) * amp,
                    (Random.value - 0.5f) * amp);
                planks[i].transform.position = orig[i] + j;
            }
            yield return null;
        }

        // ─────────────────────────────────────────────
        // B. 붕괴 — 굉음 + 강한 흔들림 + 먼지 + 판자 순차 낙하
        // ─────────────────────────────────────────────
        PlaySfx(collapseSound, 1f);
        if (cameraShake != null) cameraShake.Shake(0.16f, 0.55f);
        SpawnDust(orig);

        foreach (var plank in planks)
        {
            if (plank == null) continue;

            Rigidbody rb = plank.GetComponent<Rigidbody>();
            if (rb == null) rb = plank.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity  = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            // 양옆 벽/이웃 판자와 끼이지 않도록 충돌 비활성 → 어둠 속으로 그대로 낙하
            rb.detectCollisions = false;

            // 거의 수직 낙하 + 약한 텀블
            rb.linearVelocity = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-3f, -5f),
                Random.Range(-0.3f, 0.3f));
            rb.AddTorque(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);

            PlaySfx(breakSound, 0.7f);
            yield return new WaitForSeconds(breakDelay);
        }

        // ─────────────────────────────────────────────
        // C. 추락 — 시야 꺾기 + 롤 + FOV 펀치 + 바람 + 가속 낙하
        // ─────────────────────────────────────────────
        if (playerMove != null) playerMove.enabled = false;   // 직접 낙하 제어
        if (playerLook != null) playerLook.enabled = false;   // 카메라 직접 구동

        PlaySfx(fallWindSound, 0.85f);

        float baseFov = playerCamera != null ? playerCamera.fieldOfView : 60f;
        Quaternion camStartRot = playerCamera != null ? playerCamera.transform.localRotation : Quaternion.identity;
        Quaternion camLookDown = Quaternion.Euler(lookDownAngle, 0f, 0f);

        float fEl = 0f;
        float lastShake = 0f;
        while (fEl < fallDuration)
        {
            float dt = Time.deltaTime;
            fEl += dt;
            float t = Mathf.Clamp01(fEl / fallDuration);

            // 가속 낙하 (ease-in) — 점점 빨라지는 추락
            float fallSpeed = Mathf.Lerp(2f, fallDistance / fallDuration * 2.2f, t);
            if (controller != null) controller.Move(Vector3.down * fallSpeed * dt);
            else player.transform.position += Vector3.down * fallSpeed * dt;

            if (playerCamera != null)
            {
                // 시야를 발밑으로 (앞쪽 0.35 구간에 빠르게 꺾임)
                float lookT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
                Quaternion pitch = Quaternion.Slerp(camStartRot, camLookDown, lookT);
                // 좌우 롤 흔들림 (방향 감각 상실)
                float roll = Mathf.Sin(fEl * 13f) * fallRollAmount * (0.4f + 0.6f * t);
                playerCamera.transform.localRotation = pitch * Quaternion.Euler(0f, 0f, roll);

                // FOV 펀치 — 빠르게 늘었다 살짝 되돌아옴
                float fovT = Mathf.Sin(t * Mathf.PI);          // 0→1→0
                playerCamera.fieldOfView = baseFov + fovPunch * fovT;
            }

            // 추락 중 간헐 흔들림
            if (cameraShake != null && fEl - lastShake > 0.25f)
            {
                cameraShake.Shake(0.05f, 0.25f);
                lastShake = fEl;
            }
            yield return null;
        }

        // ─────────────────────────────────────────────
        // D. 충격 — 착지 쿵 + 강한 흔들림 + 암전
        // ─────────────────────────────────────────────
        PlaySfx(impactSound, 1f);
        if (cameraShake != null) cameraShake.Shake(0.28f, 0.4f);

        // 카메라 원복 (지하로 FOV/롤이 새어나가지 않도록)
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = baseFov;
            playerCamera.transform.localRotation = Quaternion.identity;
        }

        yield return StartCoroutine(FadeOut());
        yield return new WaitForSeconds(postImpactDelay);

        // Player를 DontDestroyOnLoad로 보존 → UnderGround SpawnPoint로 텔레포트
        // (PlayerPersistence.TeleportTo가 PlayerMove/PlayerLook을 다시 켜준다)
        var persistence = player.GetComponent<PlayerPersistence>();
        if (persistence == null)
            persistence = player.AddComponent<PlayerPersistence>();
        persistence.MarkPersistent();

        SceneManager.LoadScene(undergroundSceneName);
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }

    /// <summary>먼지 폭발. 프리팹 있으면 그걸, 없으면 런타임 파티클 생성(셰이더 못 찾으면 스킵).</summary>
    private void SpawnDust(Vector3[] plankPositions)
    {
        // 판자들의 중심 위치 계산
        Vector3 center = Vector3.zero;
        int n = 0;
        foreach (var p in plankPositions) { center += p; n++; }
        if (n > 0) center /= n;
        else center = transform.position;

        if (dustBurstPrefab != null)
        {
            var inst = Instantiate(dustBurstPrefab, center, Quaternion.identity);
            Destroy(inst, 4f);
            return;
        }

        // 런타임 더스트 — URP/Built-in 파티클 셰이더 안전 탐색
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) return; // 셰이더 못 찾으면 먼지 생략(자홍색 방지)

        var go = new GameObject("FloorBreakDust");
        go.transform.position = center + Vector3.up * 0.2f;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.duration = 1.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.32f, 0.28f, 0.22f, 0.55f),
            new Color(0.18f, 0.16f, 0.13f, 0.55f));
        main.gravityModifier = 0.15f;
        main.maxParticles = 120;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5f, 0.2f, 4f);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(sh);
        renderer.sortingFudge = 0f;

        ps.Play();
        Destroy(go, 4f);
    }

    private IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            yield return null;
        }

        fadePanel.alpha = 1f;
    }
}
