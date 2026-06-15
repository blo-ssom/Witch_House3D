using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 귀신 추격 AI.
/// 플레이어가 감지 범위 안에 들어오면 추격 시작.
/// 잡히면 게임오버.
///
/// 사용법:
///  1. Ghost_Lena 오브젝트에 부착
///  2. NavMesh Agent 컴포넌트도 같이 부착
///  3. player 슬롯에 플레이어 Transform 연결
///  4. gameOverUI 슬롯에 게임오버 패널 연결
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GhostChase : MonoBehaviour
{
    [Header("플레이어")]
    public Transform player;

    [Header("추격 설정")]
    public float chaseSpeed    = 4f;     // 추격 속도 (rubberBand 꺼졌을 때만 사용)
    public float detectRange   = 10f;    // 감지 범위
    public float catchDistance = 1.2f;   // 잡히는 거리

    [Header("고무줄 추격 — 멀면 빠르게, 가까우면 살짝 느리게")]
    [Tooltip("끄면 기존처럼 chaseSpeed 고정. 켜면 거리에 따라 closeSpeed~farSpeed 사이로 자동 조절")]
    public bool  rubberBand  = true;
    [Tooltip("이 거리 안 = closeSpeed (아슬아슬 근접 유지)")]
    public float closeRange  = 3.5f;
    [Tooltip("이 거리 밖 = farSpeed (순식간에 따라붙음)")]
    public float farRange    = 10f;
    [Tooltip("플레이어 달리기(3.2)보다 살짝 느리게 — 달리는 동안엔 간신히 거리가 벌어진다")]
    public float closeSpeed  = 2.9f;
    [Tooltip("한눈팔거나 막히면 이 속도로 따라붙는다")]
    public float farSpeed    = 4.8f;

    [Header("근접 루프 사운드 (선택 — 비워두면 무시)")]
    [Tooltip("가까울수록 커지는 루프 SFX (심장박동/숨소리). Loop 켜둔 별도 AudioSource 연결")]
    public AudioSource proximityLoop;
    [Tooltip("이 거리 밖이면 근접 사운드 볼륨 0")]
    public float proximityMaxDistance = 12f;

    [Header("활성화")]
    [Tooltip("false면 추격 안 함 → 이벤트에서 true로 변경")]
    public bool isChasing = false;

    [Header("게임오버")]
    public GameObject gameOverUI;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip   chaseSound;
    public AudioClip   catchSound;

    [Header("애니메이션 — 이동속도에 크롤 재생속도 연동 (발 미끄러짐 방지)")]
    [Tooltip("비워두면 자식에서 자동 검색")]
    public Animator animator;
    [Tooltip("크롤 클립이 자연스럽게 보이는 기준 이동속도. 발이 밀리면 줄이고, 종종걸음이면 늘린다")]
    public float animMoveReference = 3.5f;
    [Tooltip("재생속도 하한/상한 — 너무 느리거나 빨라 보이지 않게 클램프")]
    public float animSpeedMin = 0.6f;
    public float animSpeedMax = 1.6f;

    [Tooltip("비워두면 컨트롤러 기본 클립(Crawl) 사용. 지하 마지막 추격은 여기에 Running Crawl 클립을 넣는다")]
    public AnimationClip chaseClipOverride;

    public enum FlashlightChaseMode { None, ForceOff, Dim }
    [Header("추격 중 플래시라이트")]
    [Tooltip("None=그대로 / ForceOff=완전히 꺼짐(2층) / Dim=밝기 감쇠(지하)")]
    public FlashlightChaseMode flashlightOnChase = FlashlightChaseMode.None;
    [Tooltip("Dim 모드일 때 밝기 배수 (0.5 = 절반)")]
    [Range(0f, 1f)] public float flashlightDimMultiplier = 0.45f;

    [Header("추격 중 깜빡임 (사라졌다 나타남)")]
    [Tooltip("켜면 추격 중 유령이 주기적으로 잠깐 사라졌다 다시 나타난다")]
    public bool blinkDuringChase = true;
    [Tooltip("깜빡임 사이 간격(초). 약간의 랜덤이 더해진다")]
    public float blinkInterval = 3.5f;
    [Tooltip("사라져 있는 시간(초)")]
    public float blinkHideDuration = 0.45f;
    [Tooltip("켜면 사라진 동안 플레이어 주변(뒤/옆)으로 순간이동해서 다시 나타난다")]
    public bool teleportOnBlink = false;
    [Tooltip("순간이동 시 플레이어로부터의 최소/최대 거리")]
    public float teleportMinDistance = 5f;
    public float teleportMaxDistance = 9f;

    [Header("추격 중 빨간 화면")]
    [Tooltip("켜면 추격 동안 화면이 빨갛게 물든다(GameUI 사용)")]
    public bool redScreenOnChase = true;

    private NavMeshAgent agent;
    private bool isCaught = false;
    private Renderer[] ghostRenderers;
    private PlayerFlashlight playerFlashlight;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.enabled = false;  // 시작 시 비활성화

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        ghostRenderers = GetComponentsInChildren<Renderer>(true);

        ApplyClipOverride();
    }

    // chaseClipOverride가 있으면 컨트롤러의 클립을 런타임에 교체 (2층=Crawl 기본 / 지하=Running Crawl)
    private void ApplyClipOverride()
    {
        if (chaseClipOverride == null || animator == null || animator.runtimeAnimatorController == null)
            return;

        var aoc = new AnimatorOverrideController(animator.runtimeAnimatorController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        aoc.GetOverrides(overrides);
        if (overrides.Count > 0)
        {
            aoc[overrides[0].Key] = chaseClipOverride;
            animator.runtimeAnimatorController = aoc;
        }
    }

    private void Update()
    {
        if (isCaught || player == null) return;

        if (!isChasing)
        {
            // 감지 범위 안에 들어오면 자동 추격 시작
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= detectRange)
                StartChase();
            return;
        }

        Chase();
    }

    /// <summary>
    /// 외부에서 추격 시작 (이벤트 트리거용)
    /// </summary>
    public void StartChase()
    {
        if (isChasing) return;

        // 플레이어가 다른 씬에서 넘어온 경우(지하) 인스펙터 연결이 비어 있음 → 태그로 자동 검색
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        isChasing = true;

        agent.enabled = true;

        if (audioSource != null && chaseSound != null)
            audioSource.PlayOneShot(chaseSound);

        // 플래시라이트 제어 (2층=강제 OFF / 지하=Dim)
        ApplyFlashlightMode();

        // 빨간 화면 시작
        if (redScreenOnChase && GameUI.Instance != null)
            GameUI.Instance.SetChaseDanger(true);

        // 깜빡임 시작
        if (blinkDuringChase)
            StartCoroutine(BlinkRoutine());

        Debug.Log("[GhostChase] 추격 시작!");
    }

    private void ApplyFlashlightMode()
    {
        if (flashlightOnChase == FlashlightChaseMode.None) return;

        if (playerFlashlight == null)
            playerFlashlight = FindObjectOfType<PlayerFlashlight>();
        if (playerFlashlight == null) return;

        if (flashlightOnChase == FlashlightChaseMode.ForceOff)
            playerFlashlight.ForceOff();
        else if (flashlightOnChase == FlashlightChaseMode.Dim)
            playerFlashlight.SetDim(flashlightDimMultiplier);
    }

    // 추격 중 잠깐 사라졌다 다시 나타남 (+선택적 순간이동)
    private IEnumerator BlinkRoutine()
    {
        while (!isCaught && isChasing)
        {
            yield return new WaitForSeconds(blinkInterval + Random.Range(-0.8f, 0.8f));
            if (isCaught) yield break;

            SetGhostVisible(false);

            if (teleportOnBlink)
                TryTeleportNearPlayer();

            yield return new WaitForSeconds(blinkHideDuration);

            SetGhostVisible(true);
        }
    }

    private void SetGhostVisible(bool visible)
    {
        if (ghostRenderers == null) return;
        foreach (var r in ghostRenderers)
            if (r != null) r.enabled = visible;
    }

    // 사라진 동안 플레이어 주변 NavMesh 위로 순간이동
    private void TryTeleportNearPlayer()
    {
        if (player == null || !agent.enabled) return;

        for (int i = 0; i < 6; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(teleportMinDistance, teleportMaxDistance);
            Vector3 candidate = player.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                return;
            }
        }
    }

    private void Chase()
    {
        if (!agent.enabled) return;

        // 플레이어 위치로 이동
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        // 고무줄 속도 — 가까우면 살짝 느리게(아슬아슬), 멀면 빠르게(공포 유지)
        if (rubberBand)
        {
            float t = Mathf.InverseLerp(closeRange, farRange, dist);
            agent.speed = Mathf.Lerp(closeSpeed, farSpeed, t);
        }

        UpdateProximityAudio(dist);
        UpdateCrawlAnimSpeed();

        // 빨간 화면 강도 — 가까울수록 진하게
        if (redScreenOnChase && GameUI.Instance != null)
        {
            float closeness = 1f - Mathf.Clamp01(dist / proximityMaxDistance);
            GameUI.Instance.SetDangerLevel(closeness);
        }

        // 잡히는 거리 체크
        if (dist <= catchDistance)
            CatchPlayer();
    }

    // 실제 이동속도에 크롤 재생속도를 맞춰 발 미끄러짐 제거
    private void UpdateCrawlAnimSpeed()
    {
        if (animator == null) return;

        // 실제 수평 이동속도 기준 (막히면 0 → 애니도 거의 멈춤)
        Vector3 v = agent.velocity;
        v.y = 0f;
        float speed = v.magnitude;

        animator.speed = Mathf.Clamp(speed / animMoveReference, animSpeedMin, animSpeedMax);
    }

    // 거리 기반 근접 사운드 — 가까울수록 볼륨/피치 상승
    private void UpdateProximityAudio(float dist)
    {
        if (proximityLoop == null) return;

        float closeness = 1f - Mathf.Clamp01(dist / proximityMaxDistance);
        proximityLoop.volume = closeness;
        proximityLoop.pitch  = Mathf.Lerp(0.9f, 1.25f, closeness);

        if (!proximityLoop.isPlaying)
            proximityLoop.Play();
    }

    private void CatchPlayer()
    {
        if (isCaught) return;
        isCaught = true;

        agent.isStopped = true;

        if (animator != null)
            animator.speed = 1f;  // 잡는 순간 크롤 정상속도로 복귀

        SetGhostVisible(true);  // 잡힐 땐 반드시 보이게

        // 빨간 화면 강한 플래시
        if (redScreenOnChase && GameUI.Instance != null)
            GameUI.Instance.FlashCatch();

        if (proximityLoop != null)
            proximityLoop.Stop();

        if (audioSource != null && catchSound != null)
            audioSource.PlayOneShot(catchSound);

        StartCoroutine(GameOver());
        Debug.Log("[GhostChase] 플레이어 잡힘 → 게임오버");
    }

    private IEnumerator GameOver()
    {
        yield return new WaitForSeconds(0.5f);

        if (GameOverManager.Instance != null){
        GameOverManager.Instance.ShowGameOver();
        }
        // 게임오버 UI 표시
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        // 플레이어 이동 멈춤
        var playerMove = player.GetComponent<PlayerMove>();
        if (playerMove != null)
            playerMove.enabled = false;

        var playerLook = player.GetComponent<PlayerLook>();
        if (playerLook != null)
            playerLook.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 감지 범위 시각화 (씬 뷰에서 확인용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}