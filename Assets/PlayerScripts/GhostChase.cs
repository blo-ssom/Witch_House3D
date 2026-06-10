using System.Collections;
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

    private NavMeshAgent agent;
    private bool isCaught = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.enabled = false;  // 시작 시 비활성화
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

        Debug.Log("[GhostChase] 추격 시작!");
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

        // 잡히는 거리 체크
        if (dist <= catchDistance)
            CatchPlayer();
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