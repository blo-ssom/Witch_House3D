using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 매니저 — 볼륨/마우스 감도 저장(PlayerPrefs) + 적용.
///
/// 셋업:
///  1. SettingsPanel에 빈 GameObject "SettingsBinder" 생성, 이 컴포넌트 부착
///  2. volumeSlider / sensitivitySlider 슬라이더 연결
///  3. 슬라이더 OnValueChanged에 OnVolumeChanged / OnSensitivityChanged 등록
///  4. (선택) volumeText / sensitivityText TextMeshProUGUI 연결 시 숫자 표시
///
/// 다른 씬에서 적용:
///  - PlayerLook.Start()에서 SettingsManager.LoadSensitivity() 호출하면 됨
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private const string KEY_VOLUME       = "Settings.Volume";
    private const string KEY_SENSITIVITY  = "Settings.MouseSensitivity";

    public const float DEFAULT_VOLUME      = 0.8f;
    public const float DEFAULT_SENSITIVITY = 200f;

    [Header("UI Bindings")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;

    [Header("Sensitivity Range")]
    public float sensitivityMin = 50f;
    public float sensitivityMax = 600f;

    [Header("Labels (선택)")]
    public TMPro.TextMeshProUGUI volumeText;
    public TMPro.TextMeshProUGUI sensitivityText;

    private void OnEnable()
    {
        // 패널 열릴 때마다 슬라이더에 저장된 값 반영
        float v = LoadVolume();
        float s = LoadSensitivity();

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

        UpdateLabels(v, s);
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

    private void UpdateLabels(float v, float s)
    {
        if (volumeText != null)
            volumeText.text = $"{Mathf.RoundToInt(v * 100)}%";
        if (sensitivityText != null)
            sensitivityText.text = Mathf.RoundToInt(s).ToString();
    }

    // ========================================================================
    // Static API (다른 씬에서도 호출)
    // ========================================================================

    public static float LoadVolume()
        => PlayerPrefs.GetFloat(KEY_VOLUME, DEFAULT_VOLUME);

    public static float LoadSensitivity()
        => PlayerPrefs.GetFloat(KEY_SENSITIVITY, DEFAULT_SENSITIVITY);

    /// <summary>시작 시 한 번 호출 — 저장된 값으로 시스템에 적용.</summary>
    public static void ApplyAll()
    {
        AudioListener.volume = LoadVolume();
    }

    /// <summary>설정 패널 닫을 때 호출 — 디스크 플러시 보장.</summary>
    public static void SaveAll()
    {
        PlayerPrefs.Save();
    }
}
