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
    public float chaseSpeed    = 4f;     // 추격 속도
    public float detectRange   = 10f;    // 감지 범위
    public float catchDistance = 1.2f;   // 잡히는 거리

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

        // 잡히는 거리 체크
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= catchDistance)
            CatchPlayer();
    }

    private void CatchPlayer()
    {
        if (isCaught) return;
        isCaught = true;

        agent.isStopped = true;

        if (audioSource != null && catchSound != null)
            audioSource.PlayOneShot(catchSound);

        StartCoroutine(GameOver());
        Debug.Log("[GhostChase] 플레이어 잡힘 → 게임오버");
    }

    private IEnumerator GameOver()
    {
        yield return new WaitForSeconds(0.5f);

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