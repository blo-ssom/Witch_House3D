using UnityEditor;
using UnityEngine;

/// <summary>
/// 만들어 둔 Blood Particle(ParticleSystem)을 목 절단면 피 연출용으로 한 번에 세팅.
///
/// 사용법:
///  1. Hierarchy에서 만든 Blood Particle 오브젝트 선택 (ParticleSystem 포함)
///  2. 메뉴 → Witch House → Setup Selected Blood Particle
///  3. 목 절단면 위치에 두고, NeckBloodEffect의 Blood Particles 배열에 등록
///
/// 적용값: 작은 검붉은 핏방울, 아래로 흘러내림(Gravity), 끝에서 페이드,
///         Stretched Billboard(흐르는 줄기), Play On Awake OFF(스크립트가 제어).
/// </summary>
public static class BloodParticleSetup
{
    [MenuItem("Witch House/Setup Selected Blood Particle")]
    private static void SetupSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Blood Particle", "Hierarchy에서 Blood Particle 오브젝트를 먼저 선택하세요.", "확인");
            return;
        }

        var ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            EditorUtility.DisplayDialog("Blood Particle",
                $"'{go.name}'에 ParticleSystem이 없습니다.\nParticle System이 붙은 오브젝트를 선택하세요.", "확인");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(go, "Setup Blood Particle");

        // duration 등은 재생 중에 못 바꾸므로 먼저 완전 정지
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ---- Main ----
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 1.8f;                                     // 오래 천천히 흐름
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);// 졸졸 — 아주 느리게
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.04f);// 가는 핏방울
        main.startColor = new Color(0.45f, 0f, 0f, 1f);                // 검붉은색
        main.gravityModifier = 0.5f;                                   // 부드럽게 흘러내림
        main.maxParticles = 60;
        main.playOnAwake = false;                                      // NeckBloodEffect가 제어
        main.simulationSpace = ParticleSystemSimulationSpace.World;    // 머리/몸 움직여도 피는 제자리

        // ---- Emission ----
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 12f;                                   // 끊김 없이 졸졸
        emission.SetBursts(new ParticleSystem.Burst[0]);               // 초기 왈칵 제거

        // ---- Shape (콘이 아래를 향하게) ----
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(90f, 0f, 0f); // 로컬 -Y(아래) 방향으로 분사

        // ---- Color over Lifetime (끝에서 알파 페이드아웃) ----
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        // ---- Renderer (흐르는 줄기 느낌) ----
        var psr = go.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.renderMode = ParticleSystemRenderMode.Stretch;
            psr.lengthScale = 2f;
            psr.velocityScale = 0.1f; // Speed Scale
        }

        // 변경 후 다시 한 번 정지 상태로 정리
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear(true);

        EditorUtility.SetDirty(go);
        Debug.Log($"[BloodParticleSetup] '{go.name}' 피 파티클 세팅 완료. NeckBloodEffect의 Blood Particles에 등록하세요.");
    }
}
