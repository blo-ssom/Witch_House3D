using UnityEngine;

public class Interactor : MonoBehaviour
{
    [Header("설정")]
    public Transform flashlightTransform; // 스포트라이트 오브젝트를 드래그해서 넣으세요
    public float interactionDistance = 4f; // 상호작용 가능 거리

    [Header("상태")]
    public GameObject flashlightGO; // 실제 불빛(Spot Light) 오브젝트
    private bool hasFlashlight = false;

    void Update()
    {
        // 1. 레이저(Ray) 생성: 손전등의 위치에서 손전등이 바라보는 앞방향으로!
        // (만약 손전등을 얻기 전이라도 이 위치를 기준으로 레이저가 나갑니다)
        Ray ray = new Ray(flashlightTransform.position, flashlightTransform.forward);
        RaycastHit hit;

        // 씬(Scene) 창에서 노란색 레이저를 시각적으로 확인 (실제 게임엔 안 보임)
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.yellow);

        // 2. 레이저 발사!
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // E 키를 눌렀을 때 상호작용 실행
            if (Input.GetKeyDown(KeyCode.E))
            {
                PerformInteraction(hit);
            }
        }
    }

    void PerformInteraction(RaycastHit hit)
    {
        // CASE A: 손전등 아이템을 주웠을 때
        if (hit.collider.CompareTag("FlashlightItem"))
        {
            hasFlashlight = true;
            flashlightGO.SetActive(true); // 불빛 켜기
            Destroy(hit.collider.gameObject); // 바닥 아이템 삭제
            Debug.Log("손전등 획득! 이제 어둠이 두렵지 않습니다.");
        }

        // CASE B: 문을 조사했을 때 (부모에 Door 스크립트가 있는지 확인)
        Door doorScript = hit.collider.GetComponentInParent<Door>();
        if (doorScript != null)
        {
            doorScript.Interaction();
        }
        
        // 추가: 일반적인 조사 로그
        if (hit.collider.CompareTag("Interactable"))
        {
            Debug.Log(hit.collider.name + "을(를) 조사했습니다.");
        }
    }
}