using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 시 Player를 보존하고, UnderGround 씬의 SpawnPoint로 텔레포트.
/// FloorBreakTrigger가 LoadScene 직전에 MarkPersistent() 호출.
///
/// 사용법:
///  - Player 오브젝트(WH 씬)에 별도 부착할 필요 없음.
///    FloorBreakTrigger가 자동으로 AddComponent 함.
///  - UnderGround 씬에는 UndergroundSpawnPoint 컴포넌트가 부착된
///    빈 GameObject 하나가 시작 위치에 놓여있어야 함.
/// </summary>
public class PlayerPersistence : MonoBehaviour
{
    public static PlayerPersistence Instance { get; private set; }

    private bool isPersistent = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 이미 다른 Player가 살아남아 있으면 자기는 죽음
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// FloorBreakTrigger에서 LoadScene 직전에 호출.
    /// 호출 즉시 DontDestroyOnLoad 적용 + sceneLoaded 이벤트 구독.
    /// </summary>
    public void MarkPersistent()
    {
        if (isPersistent) return;
        isPersistent = true;

        // Player가 다른 부모의 자식이면 DontDestroyOnLoad가 안 먹힘 → 루트로 분리
        if (transform.parent != null)
            transform.SetParent(null, worldPositionStays: true);

        DontDestroyOnLoad(gameObject);

        // PlayerFlashlight가 손 떨림 표현을 위해 라이트를 씬 루트로 분리해 둠 → 같이 보존
        var pf = GetComponentInChildren<PlayerFlashlight>();
        if (pf != null && pf.flashlight != null && pf.flashlight.transform.parent == null)
        {
            DontDestroyOnLoad(pf.flashlight.gameObject);
            Debug.Log("[PlayerPersistence] 분리된 Flashlight도 보존.");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[PlayerPersistence] Player를 DontDestroyOnLoad로 보존.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var spawn = FindFirstObjectByType<UndergroundSpawnPoint>();
        if (spawn != null)
        {
            TeleportTo(spawn.transform);
            Debug.Log($"[PlayerPersistence] '{scene.name}' 로드 완료 → SpawnPoint 위치({spawn.transform.position})로 텔레포트.");
        }
        else
        {
            Debug.LogWarning($"[PlayerPersistence] '{scene.name}'에서 UndergroundSpawnPoint를 찾지 못했습니다. Player 위치 유지.");
        }
    }

    private void TeleportTo(Transform target)
    {
        // CharacterController는 transform.position 직접 대입 시 내부 캐시와 충돌 → 잠깐 disable
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.position = target.position;
        // 수직축 회전만 유지 (PlayerLook이 X축 회전을 별도로 관리)
        transform.rotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);

        if (cc != null) cc.enabled = true;

        // FloorBreakTrigger가 disable했던 이동 컴포넌트들 다시 켜기
        var move = GetComponent<PlayerMove>();
        if (move != null) move.enabled = true;

        var look = GetComponentInChildren<PlayerLook>();
        if (look != null) look.enabled = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }
}
