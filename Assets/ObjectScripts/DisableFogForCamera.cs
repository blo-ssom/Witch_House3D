using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 이 카메라가 렌더되는 동안에만 전역 Fog를 잠깐 끈다.
///
/// URP는 카메라별 Fog 토글이 없고 Fog가 전역(RenderSettings.fog)이라,
/// 미러룸을 비추는 RenderTexture 카메라까지 안개에 잠겨 거울이 뿌옇게
/// 보인다. 이 컴포넌트를 미러 카메라에 붙이면, 그 카메라 렌더 직전에
/// Fog를 끄고 끝나면 원래대로 되돌려 메인 화면 Fog는 그대로 유지된다.
///
/// 사용법: 미러룸을 찍는 Camera에 부착만 하면 끝. (슬롯 없음)
/// </summary>
[RequireComponent(typeof(Camera))]
public class DisableFogForCamera : MonoBehaviour
{
    private Camera cam;
    private bool prevFog;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
    }

    private void OnBeginCamera(ScriptableRenderContext ctx, Camera c)
    {
        if (c != cam) return;
        prevFog = RenderSettings.fog;   // 현재 상태 기억
        RenderSettings.fog = false;     // 미러 카메라엔 안개 끔
    }

    private void OnEndCamera(ScriptableRenderContext ctx, Camera c)
    {
        if (c != cam) return;
        RenderSettings.fog = prevFog;   // 원래대로 복원 (메인 화면 Fog 유지)
    }
}
