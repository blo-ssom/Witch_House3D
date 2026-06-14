using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 전역 밝기 컨트롤러 (URP).
///
/// 기존 분위기 후처리(Global Volume)는 건드리지 않고, 별도의 "밝기 전용 전역 Volume"을
/// 높은 priority로 하나 더 띄워 그 위에 Post Exposure만 덮어쓴다.
///
/// - RuntimeInitializeOnLoadMethod로 게임 시작 시 자동 생성(씬 배치 불필요)
/// - DontDestroyOnLoad로 메뉴/WH/지하 등 모든 씬에 일관 적용
/// - 값은 SettingsManager(PlayerPrefs "Settings.Brightness", EV 단위)에서 읽음
///
/// 설정 슬라이더에서 즉시 반영하려면 SettingsManager.OnBrightnessChanged → BrightnessController.Apply 호출.
/// </summary>
public class BrightnessController : MonoBehaviour
{
    private static BrightnessController _instance;

    private Volume _volume;
    private ColorAdjustments _colorAdj;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("~BrightnessController");
        _instance = go.AddComponent<BrightnessController>();
        Object.DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        Object.DontDestroyOnLoad(gameObject);

        EnsureVolume();
        Apply(SettingsManager.LoadBrightness());
    }

    private void EnsureVolume()
    {
        if (_colorAdj != null) return;

        _volume = gameObject.GetComponent<Volume>();
        if (_volume == null) _volume = gameObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 100f; // 기존 분위기 Volume 위에 덮어쓰도록 높게

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "BrightnessProfile (runtime)";
        _volume.profile = profile;

        _colorAdj = profile.Add<ColorAdjustments>(true);
        _colorAdj.postExposure.overrideState = true;
        _colorAdj.postExposure.value = 0f;
    }

    /// <summary>밝기 적용. ev = Post Exposure (EV). 0 = 원래 밝기, +면 밝게 -면 어둡게.</summary>
    public static void Apply(float ev)
    {
        if (_instance == null)
        {
            // 아직 부트스트랩 전이면 값만 저장돼 있다가 Awake에서 적용됨
            return;
        }
        _instance.EnsureVolume();
        _instance._colorAdj.postExposure.value = ev;
    }
}
