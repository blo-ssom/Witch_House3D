using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 매니저 — 볼륨/마우스 감도/밝기 저장(PlayerPrefs) + 적용.
///
/// 셋업:
///  1. SettingsPanel에 빈 GameObject "SettingsBinder" 생성, 이 컴포넌트 부착
///  2. volumeSlider / sensitivitySlider / brightnessSlider 슬라이더 연결
///  3. 슬라이더 OnValueChanged에 OnVolumeChanged / OnSensitivityChanged / OnBrightnessChanged 등록
///  4. (선택) volumeText / sensitivityText / brightnessText TextMeshProUGUI 연결 시 숫자 표시
///
/// 다른 씬에서 적용:
///  - PlayerLook.Start()에서 SettingsManager.LoadSensitivity() 호출하면 됨
///  - 밝기는 BrightnessController가 RuntimeInitializeOnLoadMethod로 자동 적용(씬 배치 불필요)
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private const string KEY_VOLUME       = "Settings.Volume";
    private const string KEY_SENSITIVITY  = "Settings.MouseSensitivity";
    private const string KEY_BRIGHTNESS   = "Settings.Brightness";

    public const float DEFAULT_VOLUME      = 0.8f;
    public const float DEFAULT_SENSITIVITY = 200f;
    public const float DEFAULT_BRIGHTNESS  = 0f;   // EV (Post Exposure). 0 = 원래 밝기

    [Header("UI Bindings")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Slider brightnessSlider;

    [Header("Sensitivity Range")]
    public float sensitivityMin = 50f;
    public float sensitivityMax = 600f;

    [Header("Brightness Range (EV)")]
    public float brightnessMin = -1f;
    public float brightnessMax = 1f;

    [Header("Labels (선택)")]
    public TMPro.TextMeshProUGUI volumeText;
    public TMPro.TextMeshProUGUI sensitivityText;
    public TMPro.TextMeshProUGUI brightnessText;

    private void OnEnable()
    {
        // 패널 열릴 때마다 슬라이더에 저장된 값 반영
        float v = LoadVolume();
        float s = LoadSensitivity();
        float b = LoadBrightness();

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(v);
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = sensitivityMin;
            sensitivitySlider.maxValue = sensitivityMax;
            sensitivitySlider.SetValueWithoutNotify(s);
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = brightnessMin;
            brightnessSlider.maxValue = brightnessMax;
            brightnessSlider.SetValueWithoutNotify(b);
        }

        UpdateLabels(v, s);
        UpdateBrightnessLabel(b);
    }

    public void OnVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat(KEY_VOLUME, v);
        AudioListener.volume = v;
        UpdateLabels(v, LoadSensitivity());
    }

    public void OnSensitivityChanged(float s)
    {
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, s);
        UpdateLabels(LoadVolume(), s);
    }

    public void OnBrightnessChanged(float b)
    {
        PlayerPrefs.SetFloat(KEY_BRIGHTNESS, b);
        BrightnessController.Apply(b);
        UpdateBrightnessLabel(b);
    }

    private void UpdateLabels(float v, float s)
    {
        if (volumeText != null)
            volumeText.text = $"{Mathf.RoundToInt(v * 100)}%";
        if (sensitivityText != null)
            sensitivityText.text = Mathf.RoundToInt(s).ToString();
    }

    private void UpdateBrightnessLabel(float b)
    {
        if (brightnessText == null) return;
        // EV(min~max)를 0~100%로 환산해 직관적으로 표시 (기본 0EV = 50%)
        float t = Mathf.InverseLerp(brightnessMin, brightnessMax, b);
        brightnessText.text = $"{Mathf.RoundToInt(t * 100)}%";
    }

    // ========================================================================
    // Static API (다른 씬에서도 호출)
    // ========================================================================

    public static float LoadVolume()
        => PlayerPrefs.GetFloat(KEY_VOLUME, DEFAULT_VOLUME);

    public static float LoadSensitivity()
        => PlayerPrefs.GetFloat(KEY_SENSITIVITY, DEFAULT_SENSITIVITY);

    public static float LoadBrightness()
        => PlayerPrefs.GetFloat(KEY_BRIGHTNESS, DEFAULT_BRIGHTNESS);

    /// <summary>시작 시 한 번 호출 — 저장된 값으로 시스템에 적용.</summary>
    public static void ApplyAll()
    {
        AudioListener.volume = LoadVolume();
        BrightnessController.Apply(LoadBrightness());
    }

    /// <summary>설정 패널 닫을 때 호출 — 디스크 플러시 보장.</summary>
    public static void SaveAll()
    {
        PlayerPrefs.Save();
    }
}
