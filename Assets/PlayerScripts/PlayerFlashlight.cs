using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    [Header("Flashlight Settings")]
    public Light flashlight;
    public KeyCode toggleKey = KeyCode.F;

    [Header("Light Properties")]
    public float spotAngle = 60f;
    public float innerSpotAngle = 35f;
    public float range = 25f;
    public float intensity = 5f;
    public Color lightColor = new Color(1f, 0.95f, 0.85f); // 약간 따뜻한 톤

    private bool isOn = true;

    private void Start()
    {
        if (flashlight == null)
        {
            flashlight = CreateFlashlight();
        }

        ApplySettings();
        flashlight.enabled = isOn;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        isOn = !isOn;
        flashlight.enabled = isOn;
    }

    public void SetOn(bool on)
    {
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
        flashlight.shadows = LightShadows.Soft;
    }

    private Light CreateFlashlight()
    {
        GameObject lightObj = new GameObject("Flashlight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0.2f, -0.1f, 0.3f);
        lightObj.transform.localRotation = Quaternion.identity;
        return lightObj.AddComponent<Light>();
    }
}
