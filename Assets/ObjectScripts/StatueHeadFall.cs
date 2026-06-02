using UnityEngine;

/// <summary>
/// 석상 머리 낙하 — Drop() 호출 시 머리에 물리가 적용되어 바닥으로 떨어지고,
/// 땅에 부딪히는 순간 '쿵' 사운드를 1회 재생.
///
/// 열쇠(KeyType.Room2) 픽업 시 Room3StatueManager가 Drop()을 호출하도록 연결.
///
/// 셋업 (Unity 에디터):
///  1. 떨어질 머리 GameObject 에 이 컴포넌트 부착
///  2. Rigidbody 추가 → Is Kinematic 체크(시작 시 안 떨어지게, 스크립트가 풀어줌)
///  3. Collider 필수 (바닥 충돌/쿵 감지용)
///     · MeshCollider 를 쓸 거면 반드시 Convex 체크 (Non-Convex + Rigidbody 는 충돌 불가 → 바닥 통과)
///     · 권장: BoxCollider / SphereCollider 같은 단순 콜라이더로 머리를 감싸기
///  4. (선택) audioSource + thudSound 에 '쿵' 소리 연결
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class StatueHeadFall : MonoBehaviour
{
    [Header("낙하")]
    [Tooltip("비우면 자동으로 이 오브젝트의 Rigidbody 사용")]
    public Rigidbody headRigidbody;
    [Tooltip("떨어질 때 살짝 밀어주는 힘(자연스러운 기울어짐). 0이면 그냥 수직 낙하")]
    public float pushForce = 0.4f;
    [Tooltip("미는 방향(로컬 기준). 보통 앞쪽(0,0,1)으로 두면 앞으로 고꾸라짐")]
    public Vector3 pushDirection = new Vector3(0f, 0.1f, 1f);
    [Tooltip("떨어질 때 머리를 부모에서 분리(권장 — 부모 영향 제거)")]
    public bool detachFromParent = true;

    [Header("목 피 연출 (선택)")]
    [Tooltip("머리 낙하 시 발동할 목 절단면 피 연출. 비워두면 없음.")]
    public NeckBloodEffect neckBlood;

    [Header("쿵 사운드")]
    public AudioSource audioSource;
    [Tooltip("바닥에 닿을 때 재생할 '쿵' 소리")]
    public AudioClip thudSound;
    [Tooltip("이 속도 미만의 약한 접촉은 무시(미세 튕김에 소리 안 나게)")]
    public float minImpactSpeed = 1.5f;
    [Range(0f, 1f)] public float thudVolume = 1f;

    private bool dropped = false;
    private bool thudPlayed = false;

    private void Start()
    {
        if (headRigidbody == null) headRigidbody = GetComponent<Rigidbody>();

        // 시작 시 안 떨어지게 고정 (인스펙터 누락 방지)
        if (headRigidbody != null)
        {
            headRigidbody.isKinematic = true;
            headRigidbody.useGravity = false;
        }
    }

    /// <summary>
    /// 외부(Room3StatueManager 등)에서 호출 — 머리에 물리 적용 → 낙하 시작.
    /// </summary>
    public void Drop()
    {
        if (dropped) return;
        dropped = true;

        if (detachFromParent)
            transform.SetParent(null, true);

        if (headRigidbody == null)
        {
            Debug.LogWarning($"{name}: Rigidbody가 없어 낙하 불가");
            return;
        }

        headRigidbody.isKinematic = false;
        headRigidbody.useGravity = true;

        if (pushForce > 0f)
        {
            Vector3 worldDir = transform.TransformDirection(pushDirection.normalized);
            headRigidbody.AddForce(worldDir * pushForce, ForceMode.Impulse);
            headRigidbody.AddTorque(Random.insideUnitSphere * pushForce, ForceMode.Impulse);
        }

        // 목 절단면 피 연출 발동
        if (neckBlood != null)
            neckBlood.Play();

        Debug.Log($"{name}: 머리 낙하 시작");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!dropped || thudPlayed) return;
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;

        thudPlayed = true;

        if (thudSound != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(thudSound, thudVolume);
            else
                AudioSource.PlayClipAtPoint(thudSound, transform.position, thudVolume);
        }
    }
}
