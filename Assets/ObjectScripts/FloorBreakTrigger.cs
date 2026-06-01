using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 2층 계단 근처 바닥 판자 부서짐 + 지하 씬 전환.
///
/// 사용법:
///  1. 빈 GameObject → 이름: FloorBreakTrigger
///  2. Box Collider 추가 → Is Trigger 체크
///  3. FloorBreakTrigger 컴포넌트 부착
///  4. planks 배열에 판자 큐브들 연결 (Rigidbody 없어도 됨, 자동 추가)
///  5. undergroundSceneName → "Underground" 입력
/// </summary>
public class FloorBreakTrigger : MonoBehaviour
{
    [Header("판자 오브젝트들")]
    [Tooltip("부서질 판자 큐브 배열")]
    public GameObject[] planks;

    [Header("씬 전환")]
    public string undergroundSceneName = "Underground";

    [Header("연출 설정")]
    public float breakDelay      = 0.1f;   // 판자 하나씩 부서지는 딜레이
    public float fallDuration    = 1.5f;   // 떨어지는 시간
    public float fadeOutDuration = 1.0f;   // 암전 시간
    public float sceneLoadDelay  = 2.5f;   // 씬 전환까지 대기

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip   breakSound;

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
        Debug.Log("밟힘!");

        // 1. 판자 하나씩 Rigidbody 활성화 → 물리적으로 떨어짐
        //    플레이어 컨트롤은 그대로 두어, 발 밑이 사라지면 중력으로 자연스럽게 낙하.
        foreach (var plank in planks)
        {
            if (plank == null) continue;

            Rigidbody rb = plank.GetComponent<Rigidbody>();
            if (rb == null) rb = plank.AddComponent<Rigidbody>();
            rb.isKinematic = false;

            rb.AddForce(new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-3f, -5f),
                Random.Range(-1f, 1f)
            ) * 3f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

            if (audioSource != null && breakSound != null)
                audioSource.PlayOneShot(breakSound);

            yield return new WaitForSeconds(breakDelay);
        }

        // 2. 떨어지는 시간 동안 플레이어는 PlayerMove의 중력으로 자연스럽게 낙하
        yield return new WaitForSeconds(fallDuration);

        // 3. 페이드 직전에 입력 차단 (암전 중 카메라 이상 이동 방지)
        var playerMove = player.GetComponent<PlayerMove>();
        if (playerMove != null) playerMove.enabled = false;
        var playerLook = player.GetComponentInChildren<PlayerLook>();
        if (playerLook != null) playerLook.enabled = false;

        // 4. 암전
        yield return StartCoroutine(FadeOut());

        // 5. 씬 전환
        yield return new WaitForSeconds(0.5f);

        // Player를 DontDestroyOnLoad로 보존 → UnderGround SpawnPoint로 텔레포트됨
        var persistence = player.GetComponent<PlayerPersistence>();
        if (persistence == null)
            persistence = player.AddComponent<PlayerPersistence>();
        persistence.MarkPersistent();

        SceneManager.LoadScene(undergroundSceneName);
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
