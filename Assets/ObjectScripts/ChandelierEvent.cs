using System.Collections;
using UnityEngine;

/// <summary>
/// 방1 클리어(방3 열쇠 획득) 직후 허브에서 자동 발동하는 샹들리에 이벤트.
///
/// 사용법:
///  1. 빈 GameObject (HubEventManager 등) 또는 샹들리에 오브젝트에 부착
///  2. chandelierObject : 실제 샹들리에 메쉬
///  3. debrisKey        : 잔해 옆 방2통로 열쇠 오브젝트 (시작 시 비활성화)
///  4. hubLights        : 허브 조명들 (꺼질 Light 배열)
///  5. KeyPickup(Room3 열쇠) 획득 시 TriggerEvent() 호출
///
/// 연동 방법:
///  KeyPickup.cs Interact() 마지막에 아래 추가:
///      var chandelier = FindObjectOfType<ChandelierEvent>();
///      if (chandelier != null) chandelier.TriggerEvent();
/// </summary>
public class ChandelierEvent : MonoBehaviour
{
    [Header("Objects")]
    public GameObject chandelierObject;
    [Tooltip("잔해 오브젝트 (상호작용 가능 → PathToRoom2 열쇠 획득)")]
    public GameObject debrisObject;

    [Header("Lighting")]
    public Light[] hubLights;
    [Range(0f, 1f)]
    public float dimmedIntensityRatio = 0.2f;   // 이벤트 후 조명 밝기 비율
    public float flickerDuration = 1.5f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip   crashSound;
    public AudioClip   flickerSound;

    [Header("Camera Shake (optional)")]
    public CameraShake cameraShake;
    public float shakeIntensity = 0.4f;
    public float shakeDuration  = 0.6f;

    private bool triggered = false;
    private float[] originalIntensities;

    private void Start()
    {
        // 잔해는 이벤트 전까지 비활성화
        if (debrisObject != null)
            debrisObject.SetActive(false);

        // 조명 원래 밝기 저장
        if (hubLights != null)
        {
            originalIntensities = new float[hubLights.Length];
            for (int i = 0; i < hubLights.Length; i++)
                originalIntensities[i] = hubLights[i].intensity;
        }
    }

    /// <summary>
    /// 방3 열쇠 획득 직후 외부에서 호출
    /// </summary>
    public void TriggerEvent()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(PlayChandelierEvent());
    }

    private IEnumerator PlayChandelierEvent()
    {
        // 0.5초 지연 (방에서 나오는 시간)
        yield return new WaitForSeconds(0.5f);

        // 1. 충격음

        // 2. 카메라 흔들기
        if (cameraShake != null)
            cameraShake.Shake(shakeIntensity, shakeDuration);

        // 3. 조명 깜빡임
        yield return StartCoroutine(FlickerLights());

        // 4. 샹들리에 낙하 연출 (아래로 이동)
        if (chandelierObject != null)
            yield return StartCoroutine(DropChandelier());

        // 5. 잔해 등장
        if (debrisObject != null)
            debrisObject.SetActive(true);

        // 6. 조명 어둡게 유지
        DimLights();

        Debug.Log("[ChandelierEvent] 이벤트 완료 → 잔해 조사 가능");
    }

    private IEnumerator FlickerLights()
    {
        if (hubLights == null || hubLights.Length == 0) yield break;
        if (audioSource != null && flickerSound != null)
            audioSource.PlayOneShot(flickerSound);

        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            bool on = (Mathf.Sin(elapsed * 40f) > 0f);
            foreach (var l in hubLights)
                if (l != null) l.enabled = on;

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var l in hubLights)
            if (l != null) l.enabled = true;
    }

    private IEnumerator DropChandelier()
    {
        float dropDistance = 5.5f;
        float duration     = 0.3f;
        Vector3 startPos   = chandelierObject.transform.position;
        Vector3 endPos     = startPos + Vector3.down * dropDistance;
        float elapsed      = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;
            chandelierObject.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        if (audioSource != null && crashSound != null)
        {
            audioSource.PlayOneShot(crashSound);
        }
            
        chandelierObject.transform.position = endPos;
    }

    private void DimLights()
    {
        if (hubLights == null) return;
        for (int i = 0; i < hubLights.Length; i++)
        {
            if (hubLights[i] != null && originalIntensities != null)
                hubLights[i].intensity = originalIntensities[i] * dimmedIntensityRatio;
        }
    }
}
