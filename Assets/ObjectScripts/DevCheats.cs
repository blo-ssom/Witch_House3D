using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 개발자용 치트키. 빈 GameObject 하나에 붙여두면 됨(어느 씬이든).
/// 빌드(릴리스)에는 자동으로 비활성화되어 들어가지 않음.
///
/// 단축키
///  F1 : 모든 열쇠 지급 (KeyType 전부)
///  F2 : 씬 안의 모든 문 잠금 해제(DoorInteract.ForceUnlock)
///  F3 : 이동 속도 부스트 토글 (빠른 이동)
///  F4 : 다음 씬 로드 (Build Settings 순서 기준)
///  F5 : 현재 씬 재시작
///  F6 : 바라보는 지점으로 순간이동 (노클립 점프)
///  F9 : 화면 도움말 표시/숨김
/// </summary>
public class DevCheats : MonoBehaviour
{
    [Header("켜고 끄기")]
    public bool enableCheats = true;

    [Header("속도 부스트 배율")]
    public float boostMultiplier = 4f;

    private bool showHelp = true;
    private bool boosted = false;
    private float[] savedSpeeds; // walkSpeed, runSpeed

    private void Awake()
    {
        // 릴리스 빌드에서는 통째로 제거
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        Destroy(gameObject);
        return;
#endif
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!enableCheats) return;

        if (Input.GetKeyDown(KeyCode.F1)) GiveAllKeys();
        if (Input.GetKeyDown(KeyCode.F2)) UnlockAllDoors();
        if (Input.GetKeyDown(KeyCode.F3)) ToggleSpeedBoost();
        if (Input.GetKeyDown(KeyCode.F4)) LoadNextScene();
        if (Input.GetKeyDown(KeyCode.F5)) ReloadScene();
        if (Input.GetKeyDown(KeyCode.F6)) TeleportToLook();
        if (Input.GetKeyDown(KeyCode.F9)) showHelp = !showHelp;
    }

    private void GiveAllKeys()
    {
        var inv = FindObjectOfType<PlayerInventory>();
        if (inv == null) { Log("PlayerInventory를 못 찾음"); return; }

        int count = 0;
        foreach (KeyType k in System.Enum.GetValues(typeof(KeyType)))
        {
            if (k == KeyType.None) continue;
            inv.AddKey(k);
            count++;
        }
        Log($"[F1] 모든 열쇠 지급 ({count}개)");
    }

    private void UnlockAllDoors()
    {
        var doors = FindObjectsOfType<DoorInteract>();
        foreach (var d in doors)
            d.ForceUnlock();
        Log($"[F2] 문 {doors.Length}개 잠금 해제");
    }

    private void ToggleSpeedBoost()
    {
        var move = FindObjectOfType<PlayerMove>();
        if (move == null) { Log("PlayerMove를 못 찾음"); return; }

        if (!boosted)
        {
            savedSpeeds = new[] { move.walkSpeed, move.runSpeed };
            move.walkSpeed *= boostMultiplier;
            move.runSpeed *= boostMultiplier;
            boosted = true;
            Log($"[F3] 속도 부스트 ON (x{boostMultiplier})");
        }
        else
        {
            move.walkSpeed = savedSpeeds[0];
            move.runSpeed = savedSpeeds[1];
            boosted = false;
            Log("[F3] 속도 부스트 OFF");
        }
    }

    private void LoadNextScene()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            Log("[F4] 다음 씬 없음 (마지막 씬)");
            return;
        }
        Log($"[F4] 다음 씬 로드 (index {next})");
        SceneManager.LoadScene(next);
    }

    private void ReloadScene()
    {
        Log("[F5] 현재 씬 재시작");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void TeleportToLook()
    {
        var cam = Camera.main;
        if (cam == null) { Log("Main Camera를 못 찾음"); return; }

        var inv = FindObjectOfType<PlayerInventory>();
        if (inv == null) { Log("PlayerInventory를 못 찾음"); return; }

        Transform player = inv.transform;
        Vector3 target;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 100f))
            target = hit.point + Vector3.up * 1.0f; // 바닥에서 살짝 띄움
        else
            target = cam.transform.position + cam.transform.forward * 5f;

        // CharacterController가 있으면 잠깐 끄고 위치 이동
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.position = target;
        if (cc != null) cc.enabled = true;

        Log("[F6] 바라보는 곳으로 순간이동");
    }

    private void Log(string msg) => Debug.Log($"<color=yellow>[DevCheats]</color> {msg}");

    private void OnGUI()
    {
        if (!enableCheats || !showHelp) return;

        const float w = 240f, h = 158f;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.Box(new Rect(8, 8, w, h), GUIContent.none);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(16, 14, w - 16, h - 8));
        GUILayout.Label("<b>DEV CHEATS</b>" + (boosted ? "  <color=lime>[SPEED]</color>" : ""),
            RichLabel());
        GUILayout.Label("F1  모든 열쇠 지급", RichLabel());
        GUILayout.Label("F2  모든 문 잠금 해제", RichLabel());
        GUILayout.Label("F3  속도 부스트 토글", RichLabel());
        GUILayout.Label("F4  다음 씬", RichLabel());
        GUILayout.Label("F5  씬 재시작", RichLabel());
        GUILayout.Label("F6  바라보는 곳 순간이동", RichLabel());
        GUILayout.Label("F9  도움말 숨김/표시", RichLabel());
        GUILayout.EndArea();
    }

    private static GUIStyle _style;
    private GUIStyle RichLabel()
    {
        if (_style == null)
            _style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };
        return _style;
    }
}
