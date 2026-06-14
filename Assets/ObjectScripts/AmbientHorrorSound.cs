using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 분위기 사운드 위협 시스템 (경량).
///
/// 5종 레이어를 랜덤 간격으로 재생:
///  - 삐걱임(creak): 자주, 무방향 2D, 집이 살아있는 느낌
///  - 노크(knock)  : 가끔, 플레이어 주변 3D, 빌드업 신호
///  - 발소리(foot) : 가끔, 주변 3D, 방향성
///  - 숨소리(breath): 드물게, 가까이 3D, 위협 임박
///  - 속삭임(whisper): 드물게, 주변 3D, 서사
///
/// - 게임 씬(Player 태그 존재)에 자동 부트스트랩, 메인메뉴 제외 (PauseMenu와 동일 방식).
/// - 클립은 Resources/Ambient/<카테고리>/ 에서 자동 로드(빌드 포함). 파일만 추가하면 자동 반영.
/// - 일시정지(timeScale=0)/컷신 중엔 재생 안 함.
/// </summary>
public class AmbientHorrorSound : MonoBehaviour
{
    private static AmbientHorrorSound _instance;
    private const string MAIN_MENU_SCENE = "MainMenu";

    private class Layer
    {
        public string name;
        public AudioClip[] clips;
        public float minGap, maxGap, volume;
        public bool spatial;      // true=플레이어 주변 3D, false=무방향 2D
        public float nextTime;
    }

    private readonly List<Layer> layers = new List<Layer>();
    private AudioSource src2d;
    private Transform player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySpawn();
    }

    private static void OnSceneLoaded(Scene s, LoadSceneMode m) => TrySpawn();

    private static void TrySpawn()
    {
        if (_instance != null) return;
        if (SceneManager.GetActiveScene().name == MAIN_MENU_SCENE) return;
        if (GameObject.FindWithTag("Player") == null) return;

        var go = new GameObject("~AmbientHorrorSound");
        _instance = go.AddComponent<AmbientHorrorSound>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        src2d = gameObject.AddComponent<AudioSource>();
        src2d.playOnAwake = false;
        src2d.spatialBlend = 0f;

        AddLayer("Creak",    "Ambient/Creak",    18f, 40f,  0.28f, false);
        AddLayer("Knock",    "Ambient/Knock",    45f, 95f,  0.55f, true);
        AddLayer("Footstep", "Ambient/Footstep", 30f, 70f,  0.45f, true);
        AddLayer("Breath",   "Ambient/Breath",   55f, 120f, 0.5f,  true);
        AddLayer("Whisper",  "Ambient/Whisper",  50f, 110f, 0.55f, true);
    }

    private void AddLayer(string name, string resPath, float minGap, float maxGap, float vol, bool spatial)
    {
        var clips = Resources.LoadAll<AudioClip>(resPath);
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[AmbientHorrorSound] 클립 없음: Resources/{resPath}");
            return;
        }
        var layer = new Layer
        {
            name = name, clips = clips, minGap = minGap, maxGap = maxGap,
            volume = vol, spatial = spatial,
        };
        // 첫 재생은 약간의 초기 지연 + 랜덤 (시작하자마자 쏟아지지 않게)
        layer.nextTime = Time.time + 8f + Random.Range(minGap, maxGap);
        layers.Add(layer);
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return; // 일시정지/게임오버/컷신 중 멈춤

        float now = Time.time;
        foreach (var layer in layers)
        {
            if (now < layer.nextTime) continue;
            PlayLayer(layer);
            layer.nextTime = now + Random.Range(layer.minGap, layer.maxGap);
        }
    }

    private void PlayLayer(Layer layer)
    {
        var clip = layer.clips[Random.Range(0, layer.clips.Length)];
        if (clip == null) return;

        if (layer.spatial && player != null)
        {
            // 플레이어 주변 랜덤 위치에서 3D 재생 → 방향감
            Vector2 dir = Random.insideUnitCircle.normalized;
            Vector3 pos = player.position + new Vector3(dir.x, Random.Range(-0.3f, 0.5f), dir.y) * Random.Range(3.5f, 8f);
            AudioSource.PlayClipAtPoint(clip, pos, layer.volume);
        }
        else
        {
            src2d.PlayOneShot(clip, layer.volume);
        }
    }
}
