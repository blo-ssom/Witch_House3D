using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

/// <summary>
/// 거울 텍스처 캡처용 임시 헬퍼.
/// 임시 카메라에 부착 → 우클릭 → Capture as PNG → Assets/Textures/{fileName}.png 생성.
///
/// 사용 흐름:
///  1. Hierarchy에 빈 GameObject + Camera 컴포넌트 추가
///  2. 카메라 위치/회전을 거울 시점으로 (거울이 의자 보는 각도)
///  3. 이 컴포넌트 부착
///  4. fileName = "Mirror_Empty"
///  5. PhotoPiece_3 비활성화 (의자만 보임)
///  6. 컴포넌트 우클릭 → Capture as PNG
///  7. PhotoPiece_3 활성화
///  8. fileName = "Mirror_WithPiece"로 변경
///  9. 다시 Capture as PNG
/// 10. 캡처 끝나면 임시 카메라 + 이 컴포넌트 삭제
///
/// 결과물: Assets/Textures/Mirror_Empty.png, Mirror_WithPiece.png
/// </summary>
[RequireComponent(typeof(Camera))]
public class MirrorCaptureHelper : MonoBehaviour
{
    [Header("캡처 설정")]
    public int width = 1024;
    public int height = 1024;
    [Tooltip("PNG 파일명 (확장자 제외). Assets/Textures/ 폴더에 저장됨")]
    public string fileName = "Mirror_Empty";

#if UNITY_EDITOR
    [ContextMenu("Capture as PNG")]
    public void Capture()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("[MirrorCaptureHelper] Camera 컴포넌트가 없음");
            return;
        }

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        RenderTexture prevTarget = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;
        cam.targetTexture = prevTarget;

        string dir = Application.dataPath + "/Textures";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string safeName = string.IsNullOrEmpty(fileName) ? "MirrorCapture" : fileName;
        string path = dir + "/" + safeName + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());

        DestroyImmediate(tex);
        rt.Release();
        DestroyImmediate(rt);

        AssetDatabase.Refresh();
        Debug.Log("[MirrorCaptureHelper] 저장 완료: " + path);
    }
#endif
}
