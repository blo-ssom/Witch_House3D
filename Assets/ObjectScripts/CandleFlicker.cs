using UnityEngine;

/// <summary>
/// 촛불 깜빡임 — Perlin Noise 기반.
/// 단순 랜덤보다 자연스러운 유기적 흔들림.
///
/// 사용법:
///  1. Point Light 또는 Spot Light 오브젝트에 부착
///  2. Light가 같은 오브젝트에 없으면 light 슬롯에 수동 연결
///  3. (선택) flameTransform에 불꽃 메시 연결하면 위치도 살짝 흔들림
/// </summary>
[RequireComponent(typeof(Light))]
public class CandleFlicker : MonoBehaviour
{
    [Header("Light (자동 인식)")]
    public Light targetLight;

    [Header("강도 흔들림")]
    [Tooltip("기준 강도 — 이 값 주변에서 변동.")]
    public float baseIntensity = 1.5f;
    [Tooltip("강도 변동 폭 (±). 너무 크면 부자연스러움.")]
    public float intensityVariance = 0.4f;
    [Tooltip("흔들림 속도 (높을수록 빠르게 깜빡).")]
    public float intensitySpeed = 3f;

    [Header("색온도 흔들림 (선택)")]
    [Tooltip("밝을 때 따뜻한 색, 어두울 때 살짝 차가운 색으로.")]
    public bool varyColor = true;
    public Color brightColor = new Color(1f, 0.78f, 0.45f);
    public Color dimColor    = new Color(1f, 0.55f, 0.30f);

    [Header("범위 흔들림 (선택)")]
    public bool varyRange = false;
    public float baseRange = 5f;
    public float rangeVariance = 0.5f;

    [Header("불꽃 메시 위치 흔들림 (선택)")]
    [Tooltip("불꽃 메시 Transform 연결 시 살짝 흔들림.")]
    public Transform flameTransform;
    public float flameSwayAmplitude = 0.015f;
    public float flameSwaySpeed = 2.5f;

    [Header("드물게 확 꺼지는 효과 (선택)")]
    [Tooltip("간헐적으로 짧게 매우 어두워지는 깜빡임.")]
    public bool occasionalDrops = false;
    [Range(0f, 0.05f)] public float dropChancePerSecond = 0.01f;
    public float dropDuration = 0.08f;
    [Range(0f, 1f)] public float dropIntensityFactor = 0.2f;

    // 각 효과에 독립 Perlin 시드를 주어 서로 겹치지 않게
    private float seedIntensity;
    private float seedFlameX;
    private float seedFlameZ;
    private float seedRange;

    private float dropTimer = 0f;
    private bool isDropping = false;
    private Vector3 flameBasePos;

    private void Awake()
    {
        if (targetLight == null) targetLight = GetComponent<Light>();

        seedIntensity = Random.Range(0f, 1000f);
        seedFlameX    = Random.Range(0f, 1000f);
        seedFlameZ    = Random.Range(0f, 1000f);
        seedRange     = Random.Range(0f, 1000f);

        if (flameTransform != null)
            flameBasePos = flameTransform.localPosition;
    }

    private void Update()
    {
        float t = Time.time * intensitySpeed;

        // Perlin Noise는 0~1 → -1~1로 변환
        float n = Mathf.PerlinNoise(seedIntensity + t, 0f) * 2f - 1f;
        float intensity = baseIntensity + n * intensityVariance;

        // 드물게 확 어두워지는 효과
        if (occasionalDrops)
        {
            if (isDropping)
            {
                dropTimer -= Time.deltaTime;
                if (dropTimer <= 0f) isDropping = false;
                intensity *= dropIntensityFactor;
            }
            else
            {
                if (Random.value < dropChancePerSecond * Time.deltaTime * 60f)
                {
                    isDropping = true;
                    dropTimer = dropDuration;
                }
            }
        }

        targetLight.intensity = Mathf.Max(0f, intensity);

        // 색온도 흔들림
        if (varyColor)
        {
            float brightness01 = Mathf.InverseLerp(
                baseIntensity - intensityVariance,
                baseIntensity + intensityVariance,
                intensity);
            targetLight.color = Color.Lerp(dimColor, brightColor, brightness01);
        }

        // 범위 흔들림
        if (varyRange)
        {
            float nr = Mathf.PerlinNoise(seedRange + t * 0.7f, 0f) * 2f - 1f;
            targetLight.range = baseRange + nr * rangeVariance;
        }

        // 불꽃 메시 위치 흔들림
        if (flameTransform != null)
        {
            float fx = (Mathf.PerlinNoise(seedFlameX + Time.time * flameSwaySpeed, 0f) - 0.5f) * 2f;
            float fz = (Mathf.PerlinNoise(seedFlameZ + Time.time * flameSwaySpeed, 0f) - 0.5f) * 2f;
            flameTransform.localPosition = flameBasePos +
                new Vector3(fx * flameSwayAmplitude, 0f, fz * flameSwayAmplitude);
        }
    }
}
