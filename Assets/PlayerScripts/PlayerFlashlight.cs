using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerFlashlight : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public Light flashlight;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Light Properties")]
    public float spotAngle = 45f;
    public float innerSpotAngle = 22f;
    public float range = 14f;
    public float intensity = 8f;
    public Color lightColor = new Color(1f, 0.82f, 0.55f);
    public LightShadows shadowMode = LightShadows.Hard;

    [Header("Shadow Bias (Peter Panning 방지)")]
    [Range(0f, 0.5f)] public float shadowBias = 0.02f;
    [Range(0f, 0.5f)] public float shadowNormalBias = 0.1f;
    [Range(0.05f, 1f)] public float shadowNearPlane = 0.1f;

    [Header("Mount")]
    public Transform swayTarget;
    public Vector3 handOffset = new Vector3(0.2f, -0.05f, 0.3f);

    [Header("Sway (Handheld Feel)")]
    public bool enableSway = true;
    public float swayAmplitudeMove = 1.6f;
    public float swayAmplitudeIdle = 0.35f;
    public float swayFrequencyMove = 6f;
    public float swayFrequencyIdle = 1.2f;
    public float swaySpeedRef = 4f;

    [Header("Rotation Lag")]
    public bool enableRotationLag = true;
    public float rotationLagSpeed = 12f;

    [Header("Flicker (Battery Feel)")]
    public bool enableFlicker = true;
    [Range(0f, 0.3f)] public float flickerStrength = 0.08f;
    public float flickerSpeed = 4f;

    private bool isOn = true;
    private float baseIntensity;
    private Quaternion currentRotation;
    private Vector3 lastTargetPosition;

    // 추격 연출용: 강제 OFF(토글 잠금) / 밝기 감쇠
    private bool lockedOff = false;
    private float dimMultiplier = 1f;

    private void Start()
    {
        if (flashlight == null)
        {
            flashlight = CreateFlashlight();
        }

        // swayTarget이 비어 있으면 카메라를 따라가게 (상하 시선까지 반영).
        // 몸체(yaw만 회전)를 따라가면 손전등이 위아래를 안 따라가는 문제 방지.
        if (swayTarget == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            swayTarget = cam != null ? cam.transform : transform;
        }

        flashlight.transform.SetParent(null, true);

        ApplySettings();
        baseIntensity = intensity;
        flashlight.enabled = isOn;

        currentRotation = swayTarget.rotation;
        lastTargetPosition = swayTarget.position;
        flashlight.transform.position = swayTarget.position + swayTarget.TransformVector(handOffset);
        flashlight.transform.rotation = currentRotation;
    }

    private void Update()
    {
        if (lockedOff) return;  // 강제 OFF 중엔 토글 불가

        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    private void LateUpdate()
    {
        if (flashlight == null || swayTarget == null) return;

        flashlight.transform.position = swayTarget.position + swayTarget.TransformVector(handOffset);

        Quaternion targetRot = swayTarget.rotation;
        currentRotation = enableRotationLag
            ? Quaternion.Slerp(currentRotation, targetRot, Time.deltaTime * rotationLagSpeed)
            : targetRot;

        Quaternion finalRot = currentRotation;

        if (enableSway && flashlight.enabled)
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = (swayTarget.position - lastTargetPosition).magnitude / dt;
            float moveBlend = Mathf.Clamp01(speed / Mathf.Max(swaySpeedRef, 0.01f));
            float amp = Mathf.Lerp(swayAmplitudeIdle, swayAmplitudeMove, moveBlend);
            float freq = Mathf.Lerp(swayFrequencyIdle, swayFrequencyMove, moveBlend);

            float t = Time.time * freq;
            float sx = Mathf.Sin(t) * amp;
            float sy = Mathf.Sin(t * 0.5f) * amp * 0.7f;
            finalRot *= Quaternion.Euler(sx, sy, 0f);
        }

        lastTargetPosition = swayTarget.position;
        flashlight.transform.rotation = finalRot;

        if (flashlight.enabled)
        {
            if (enableFlicker)
            {
                float n = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
                float modulation = 1f + (n - 0.5f) * 2f * flickerStrength;
                flashlight.intensity = baseIntensity * modulation * dimMultiplier;
            }
            else
            {
                flashlight.intensity = baseIntensity * dimMultiplier;
            }
        }
    }

    /// <summary>추격 연출: 손전등을 강제로 끄고 토글을 잠근다 (2층 추격용).</summary>
    public void ForceOff()
    {
        lockedOff = true;
        SetOn(false);
    }

    /// <summary>강제 OFF 해제 — 다시 토글 가능.</summary>
    public void ReleaseForce()
    {
        lockedOff = false;
    }

    /// <summary>밝기 감쇠 배수 설정 (1=원래, 0.5=절반). 지하 추격용.</summary>
    public void SetDim(float multiplier)
    {
        dimMultiplier = Mathf.Clamp01(multiplier);
    }

    public void Toggle()
    {
        if (flashlight == null) return;
        isOn = !isOn;
        flashlight.enabled = isOn;
    }

    public void SetOn(bool on)
    {
        if (flashlight == null) return;
        if (lockedOff && on) return;  // 강제 OFF 중엔 켤 수 없음
        isOn = on;
        flashlight.enabled = isOn;
    }

    private void ApplySettings()
    {
        flashlight.type = LightType.Spot;
        flashlight.spotAngle = spotAngle;
        flashlight.innerSpotAngle = innerSpotAngle;
        flashlight.range = range;
        flashlight.intensity = intensity;
        flashlight.color = lightColor;
        flashlight.shadows = shadowMode;
        flashlight.shadowBias = shadowBias;
        flashlight.shadowNormalBias = shadowNormalBias;
        flashlight.shadowNearPlane = shadowNearPlane;

        var urpData = flashlight.GetUniversalAdditionalLightData();
        if (urpData != null)
        {
            urpData.usePipelineSettings = false;
        }
    }

    private Light CreateFlashlight()
    {
        GameObject lightObj = new GameObject("Flashlight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = handOffset;
        lightObj.transform.localRotation = Quaternion.identity;
        return lightObj.AddComponent<Light>();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && flashlight != null)
        {
            ApplySettings();
            baseIntensity = intensity;
        }
    }

    private void OnDestroy()
    {
        if (flashlight != null && flashlight.transform.parent == null)
        {
            Destroy(flashlight.gameObject);
        }
    }
}
