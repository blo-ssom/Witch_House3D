using System.Collections;
using UnityEngine;

/// <summary>
/// 카메라(또는 플레이어 카메라 오브젝트)에 부착.
/// ChandelierEvent 등 충격 연출 시 Shake() 호출.
/// </summary>
public class CameraShake : MonoBehaviour
{
    private Vector3 originalLocalPos;

    private void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    public void Shake(float intensity, float duration)
    {
        StartCoroutine(DoShake(intensity, duration));
    }

    private IEnumerator DoShake(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float fade = 1f - (elapsed / duration);          // 점점 약해짐
            Vector3 offset = Random.insideUnitSphere * intensity * fade;
            offset.z = 0f;                                   // 앞뒤 흔들림 제거
            transform.localPosition = originalLocalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
    }
}
