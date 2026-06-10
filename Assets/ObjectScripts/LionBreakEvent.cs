using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방3 중앙 사자상 붕괴 연출 — 시선 퍼즐 완성 시 StatueGazePuzzleManager가 TriggerBreak() 호출.
///
/// 방식: 진짜 파괴 메시 없이 '파편 더미'로 부서짐을 표현.
///  1. 균열음 + 사자상이 부르르 떨림 (점점 강해짐)
///  2. 붕괴 순간: 본체 렌더러 OFF + 사자 재질의 돌 파편(랜덤 큐브)들이 와르르 터져 떨어짐
///     + 쿵 SFX + 카메라 셰이크
///  3. 파편이 가라앉으면 고정(잔해 더미로 남음 — 증거물)
///  4. keyRevealDelay 후 잔해 한가운데 열쇠 등장
///
/// 셋업 (Unity 에디터):
///  1. 사자상 GameObject에 이 컴포넌트 부착
///  2. keyObject = 등장할 열쇠. 시작 시 자동 비활성됨
///  3. (선택) debrisMaterial — 비우면 사자상 재질 자동 사용
///  4. (선택) cameraShake / audioSource + crackSound/thudSound
/// </summary>
public class LionBreakEvent : MonoBehaviour
{
    [Header("진동 (붕괴 전조)")]
    [Tooltip("떨리는 시간(s)")]
    public float shakeDuration = 1.6f;
    [Tooltip("떨림 최대 진폭(m). 석상이라 아주 작게")]
    public float shakeAmount = 0.02f;

    [Header("파편")]
    [Tooltip("파편 개수")]
    public int debrisCount = 16;
    [Tooltip("파편 최소/최대 크기(m)")]
    public float debrisMinSize = 0.06f;
    public float debrisMaxSize = 0.28f;
    [Tooltip("파편이 바깥으로 터지는 속도(m/s)")]
    public float burstSpeed = 1.3f;
    [Tooltip("파편 모양 일그러뜨리기(0=반듯한 사각형, 0.3=거친 돌조각)")]
    [Range(0f, 0.4f)]
    public float debrisDeform = 0.25f;
    [Tooltip("파편 재질. 비우면 사자상 재질 자동 사용")]
    public Material debrisMaterial;
    [Tooltip("파편이 가라앉길 기다렸다 고정하는 시간(s)")]
    public float settleTime = 2.5f;

    [Header("열쇠")]
    [Tooltip("잔해 한가운데 등장할 열쇠. 시작 시 자동 비활성")]
    public GameObject keyObject;
    [Tooltip("붕괴 후 열쇠 등장까지 딜레이(s)")]
    public float keyRevealDelay = 0.8f;

    [Header("카메라 셰이크 (선택)")]
    public CameraShake cameraShake;
    public float camShakeIntensity = 0.15f;
    public float camShakeDuration = 0.5f;

    [Header("SFX")]
    public AudioSource audioSource;
    [Tooltip("진동 시작 시 — 돌 갈라지는 소리")]
    public AudioClip crackSound;
    [Tooltip("붕괴 순간 — 와르르/쿵")]
    public AudioClip thudSound;
    [Range(0f, 1f)] public float thudVolume = 1f;

    private bool broken = false;

    private void Start()
    {
        // 혹시 남아 있는 Rigidbody는 잠궈둠 (본체는 물리 미사용)
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (keyObject != null)
            keyObject.SetActive(false);
    }

    /// <summary>퍼즐 완성 시 StatueGazePuzzleManager가 호출.</summary>
    public void TriggerBreak()
    {
        if (broken) return;
        broken = true;
        StartCoroutine(BreakSequence());
    }

    private IEnumerator BreakSequence()
    {
        if (audioSource != null && crackSound != null)
            audioSource.PlayOneShot(crackSound);

        // 1. 진동 — 점점 강해짐
        Vector3 originPos = transform.localPosition;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float ramp = elapsed / shakeDuration;
            Vector3 jitter = Random.insideUnitSphere * shakeAmount * ramp;
            jitter.y = 0f;
            transform.localPosition = originPos + jitter;
            yield return null;
        }
        transform.localPosition = originPos;

        // 2. 붕괴 — 본체 숨기고 파편 생성
        var rend = GetComponentInChildren<Renderer>();
        Bounds b = rend != null ? rend.bounds : new Bounds(transform.position, Vector3.one * 0.5f);
        Material mat = debrisMaterial != null ? debrisMaterial : (rend != null ? rend.sharedMaterial : null);

        if (rend != null) rend.enabled = false;
        var ownCol = GetComponent<Collider>();
        if (ownCol != null) ownCol.enabled = false;       // 잔해 위로 걸어다닐 수 있게

        if (audioSource != null && thudSound != null)
            audioSource.PlayOneShot(thudSound, thudVolume);
        else if (thudSound != null)
            AudioSource.PlayClipAtPoint(thudSound, b.center, thudVolume);

        if (cameraShake != null)
            cameraShake.Shake(camShakeIntensity, camShakeDuration);

        var debris = new List<Rigidbody>();
        for (int i = 0; i < debrisCount; i++)
        {
            var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunk.name = "LionDebris";

            float size = Random.Range(debrisMinSize, debrisMaxSize);
            // 납작하고 길쭉한 비율 — 깨진 돌판 느낌
            chunk.transform.localScale = new Vector3(
                size * Random.Range(0.7f, 1.6f),
                size * Random.Range(0.35f, 0.8f),
                size * Random.Range(0.7f, 1.6f));

            // 사자 바운즈 안쪽(아래쪽 위주)에서 시작
            Vector3 p = b.center + new Vector3(
                Random.Range(-b.extents.x, b.extents.x) * 0.7f,
                Random.Range(-b.extents.y, b.extents.y * 0.3f),
                Random.Range(-b.extents.z, b.extents.z) * 0.7f);
            chunk.transform.position = p;
            chunk.transform.rotation = Random.rotation;

            // 꼭짓점을 일그러뜨려 깨진 돌조각 모양으로 (모서리별로 같은 오프셋 → 면이 안 갈라짐)
            if (debrisDeform > 0f)
            {
                var mf = chunk.GetComponent<MeshFilter>();
                Mesh mesh = Object.Instantiate(mf.sharedMesh);
                Vector3[] verts = mesh.vertices;
                var cornerOffset = new Dictionary<Vector3, Vector3>();
                for (int v = 0; v < verts.Length; v++)
                {
                    if (!cornerOffset.TryGetValue(verts[v], out Vector3 off))
                    {
                        off = Random.insideUnitSphere * debrisDeform;
                        cornerOffset[verts[v]] = off;
                    }
                    verts[v] += off;
                }
                mesh.vertices = verts;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mf.mesh = mesh;
            }

            if (mat != null)
                chunk.GetComponent<Renderer>().sharedMaterial = mat;

            var crb = chunk.AddComponent<Rigidbody>();
            crb.mass = 0.5f;
            // 중심에서 바깥쪽으로 터짐 + 살짝 위로
            Vector3 dir = (p - b.center);
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.001f ? dir.normalized : Random.insideUnitSphere;
            crb.linearVelocity = dir * Random.Range(0.4f, burstSpeed) + Vector3.up * Random.Range(0.1f, 0.5f);
            crb.angularVelocity = Random.insideUnitSphere * 4f;

            debris.Add(crb);
        }

        // 3. 열쇠 등장 (잔해 한가운데)
        yield return new WaitForSeconds(keyRevealDelay);
        if (keyObject != null)
            keyObject.SetActive(true);

        // 4. 파편이 가라앉으면 고정 — 영원히 미세 진동하는 것 방지, 잔해 더미로 남김
        float waited = keyRevealDelay;
        if (settleTime > waited)
            yield return new WaitForSeconds(settleTime - waited);

        foreach (var crb in debris)
        {
            if (crb == null) continue;
            crb.isKinematic = true;
        }
    }
}
