using UnityEngine;

public class PaddleSkinApplier : MonoBehaviour
{
    [Header("Drag your paddle mesh Renderer(s) here")]
    public Renderer[] paddleRenderers;  // supports single or multi-part models

    void Start()
    {
        // Wait a frame to make sure PaddleDatabase has initialized
        StartCoroutine(InitAfterDatabase());
    }

    System.Collections.IEnumerator InitAfterDatabase()
    {
        yield return null; // wait one frame

        if (PaddleDatabase.Instance == null)
        {
            Debug.LogWarning("[Skin] PaddleDatabase not found!");
            yield break;
        }

        // Apply current active paddle skin on startup
        ApplySkin(PaddleDatabase.Instance.ActivePaddle);

        // Listen for future changes
        PaddleDatabase.Instance.OnActivePaddleChanged += ApplySkin;
    }

    void OnDestroy()
    {
        if (PaddleDatabase.Instance != null)
            PaddleDatabase.Instance.OnActivePaddleChanged -= ApplySkin;
    }

    void ApplySkin(PaddleData paddle)
    {
        if (paddle == null)
        {
            Debug.LogWarning("[Skin] ApplySkin called with null paddle");
            return;
        }

        if (paddle.paddleMaterial == null)
        {
            Debug.LogWarning($"[Skin] '{paddle.paddleName}' has no material assigned");
            return;
        }

        foreach (var r in paddleRenderers)
        {
            if (r != null)
                r.material = paddle.paddleMaterial;
        }

        Debug.Log($"[Skin] Applied '{paddle.paddleName}' {paddle.paddleMaterial.name}");
    }
}