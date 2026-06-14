using System.Collections;
using UnityEngine;

/// <summary>
/// 복도 갑옷 기사상: 플레이어가 앞을 지나가면 들고 있던 칼이 Rigidbody 물리로 낙하,
/// 바닥에 닿는 순간 1회 쨍그랑 SFX 재생.
///
/// 동작:
///  1. 플레이어가 트리거 존 진입 → dropDelay 후 칼 Rigidbody.isKinematic = false
///  2. dropTorque/dropImpulse로 살짝 회전·튕기며 자연 낙하
///  3. 칼이 바닥과 충돌 시 (임팩트 임계값 이상) clangSound 1회 재생
///
/// 인스펙터 셋업:
///  - 빈 오브젝트에 부착 + BoxCollider(IsTrigger 체크)로 기사상 앞 통로를 덮을 것
///  - swordBody: 칼 오브젝트의 Rigidbody (시작 시 스크립트가 isKinematic = true 강제)
///  - 칼에 Collider 필수 (MeshCollider는 Convex 체크), 바닥에도 Collider 필요
///  - audioSource: 비워두면 칼 오브젝트에서 자동으로 찾고, 없으면 칼에 3D AudioSource 자동 생성
/// </summary>
public class SwordDropTrigger : MonoBehaviour
{
    [Header("Sword Rigidbody (시작 시 isKinematic 강제)")]
    public Rigidbody swordBody;

    [Header("Drop Physics")]
    [Tooltip("트리거 진입 후 낙하까지 지연(초). 플레이어가 막 지나친 직후 떨어지게 하려면 0.3~0.6 권장.")]
    public float dropDelay = 0.4f;
    [Tooltip("떨어질 때 회전 임펄스. (2,0,0) 정도면 앞으로 기울며 떨어지듯.")]
    public Vector3 dropTorque = new Vector3(2f, 0f, 0f);
    [Tooltip("떨어질 때 위치 임펄스. 손에서 살짝 미끄러져 나오게 하려면 작은 값을 줄 것.")]
    public Vector3 dropImpulse = Vector3.zero;

    [Header("SFX")]
    [Tooltip("비워두면 칼 오브젝트의 AudioSource를 찾고, 없으면 자동 생성(3D).")]
    public AudioSource audioSource;
    public AudioClip clangSound;
    [Tooltip("이 값 이상의 충돌 임팩트가 들어와야 clang 재생. 0이면 첫 충돌에 무조건 재생.")]
    public float clangImpactThreshold = 1f;

    private bool fired = false;
    private bool clangPlayed = false;

    private void Start()
    {
        if (swordBody != null)
        {
            swordBody.isKinematic = true;
            swordBody.useGravity = true;

            // 바닥 충돌을 칼 쪽에서 받아 이쪽으로 전달하는 릴레이 부착
            var relay = swordBody.gameObject.GetComponent<SwordImpactRelay>();
            if (relay == null)
                relay = swordBody.gameObject.AddComponent<SwordImpactRelay>();
            relay.owner = this;

            if (audioSource == null)
                audioSource = swordBody.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = swordBody.gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D 사운드
            }
        }
        else
        {
            Debug.LogWarning("[SwordDropTrigger] swordBody가 비어 있음 - 인스펙터에서 칼 Rigidbody를 연결할 것");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired || swordBody == null) return;
        if (!other.CompareTag("Player")) return;

        fired = true;
        StartCoroutine(DropAfterDelay());
    }

    private IEnumerator DropAfterDelay()
    {
        if (dropDelay > 0f)
            yield return new WaitForSeconds(dropDelay);

        swordBody.isKinematic = false;
        if (dropTorque != Vector3.zero)
            swordBody.AddTorque(dropTorque, ForceMode.Impulse);
        if (dropImpulse != Vector3.zero)
            swordBody.AddForce(dropImpulse, ForceMode.Impulse);

        Debug.Log("[SwordDropTrigger] Rigidbody 해제 - 칼 낙하 시작");
    }

    /// <summary>
    /// SwordImpactRelay가 칼의 OnCollisionEnter를 전달.
    /// </summary>
    public void OnSwordImpact(Collision collision)
    {
        if (!fired || clangPlayed) return;
        if (clangImpactThreshold > 0f && collision.relativeVelocity.magnitude < clangImpactThreshold) return;

        if (audioSource != null && clangSound != null)
            audioSource.PlayOneShot(clangSound);

        clangPlayed = true;
        Debug.Log("[SwordDropTrigger] 바닥 충돌 - clang SFX 재생");
    }
}

/// <summary>
/// 칼 오브젝트에 런타임 자동 부착되어 충돌 이벤트를 SwordDropTrigger로 전달.
/// 직접 부착할 필요 없음.
/// </summary>
public class SwordImpactRelay : MonoBehaviour
{
    [HideInInspector] public SwordDropTrigger owner;

    private void OnCollisionEnter(Collision collision)
    {
        if (owner != null)
            owner.OnSwordImpact(collision);
    }
}
