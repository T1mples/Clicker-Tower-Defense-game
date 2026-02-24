using UnityEngine;

[ExecuteAlways]
public class CameraSize : MonoBehaviour
{
    private enum FitMode
    {
        MatchHeight,
        MatchWidth,
        MatchBothBlend
    }

    [Header("Reference Resolution")]
    [SerializeField, Range(320f, 7680f)] private float targetSizeX = 1280f;
    [SerializeField, Range(200f, 4320f)] private float targetSizeY = 720f;
    [SerializeField, Range(50f, 1000f)] private float halfSize = 200f;

    [Header("Fit")]
    [SerializeField] private FitMode fitMode = FitMode.MatchHeight;
    [SerializeField, Range(0f, 1f)] private float matchBlend = 0.5f; // 0 = height, 1 = width
    [SerializeField, Range(0.1f, 3f)] private float sizeMultiplier = 1f;
    [SerializeField] private bool updateOnResolutionChange = true;
    [Header("Fixed Viewport (Free Aspect Safe)")]
    [SerializeField] private bool keepConstantCameraFraming = true;

    private Camera cachedCamera;
    private int lastWidth = -1;
    private int lastHeight = -1;

    private void Awake()
    {
        CacheCamera();
        CameraResize();
    }

    private void OnEnable()
    {
        CacheCamera();
        CameraResize();
    }

    private void Update()
    {
        if (!updateOnResolutionChange)
        {
            return;
        }

        if (lastWidth != Screen.width || lastHeight != Screen.height)
        {
            CameraResize();
        }
    }

    private void OnValidate()
    {
        targetSizeX = Mathf.Max(1f, targetSizeX);
        targetSizeY = Mathf.Max(1f, targetSizeY);
        halfSize = Mathf.Max(1f, halfSize);
        sizeMultiplier = Mathf.Max(0.01f, sizeMultiplier);
        CacheCamera();
        CameraResize();
    }

    [ContextMenu("Apply Preset 1280x720")]
    private void ApplyPreset1280x720()
    {
        targetSizeX = 1280f;
        targetSizeY = 720f;
        halfSize = 200f; // 720 / 200 = 3.6 (current gameplay framing)
        fitMode = FitMode.MatchHeight;
        matchBlend = 0.5f;
        sizeMultiplier = 1f;
        updateOnResolutionChange = true;
        keepConstantCameraFraming = true;
        CameraResize();
    }

    [ContextMenu("Apply Camera Resize")]
    private void CameraResize()
    {
        if (cachedCamera == null)
        {
            return;
        }

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetRatio = targetSizeX / targetSizeY;

        ApplyViewportRect(screenRatio, targetRatio);

        float sizeByHeight = targetSizeY / halfSize;
        float sizeByWidth = (targetSizeX / screenRatio) / halfSize;

        switch (fitMode)
        {
            case FitMode.MatchWidth:
                Resize(sizeByWidth);
                break;
            case FitMode.MatchBothBlend:
                Resize(Mathf.Lerp(sizeByHeight, sizeByWidth, matchBlend));
                break;
            default:
                if (keepConstantCameraFraming || screenRatio >= targetRatio)
                {
                    Resize(sizeByHeight);
                }
                else
                {
                    float differentSize = targetRatio / screenRatio;
                    Resize(sizeByHeight * differentSize);
                }
                break;
        }
    }

    private void Resize(float baseOrthoSize)
    {
        cachedCamera.orthographicSize = baseOrthoSize * sizeMultiplier;
    }

    private void CacheCamera()
    {
        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }
    }

    private void ApplyViewportRect(float screenRatio, float targetRatio)
    {
        if (cachedCamera == null)
        {
            return;
        }

        if (!keepConstantCameraFraming)
        {
            cachedCamera.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        if (screenRatio > targetRatio)
        {
            // Wider screen -> pillarbox left/right
            float normalizedWidth = targetRatio / screenRatio;
            float x = (1f - normalizedWidth) * 0.5f;
            cachedCamera.rect = new Rect(x, 0f, normalizedWidth, 1f);
        }
        else if (screenRatio < targetRatio)
        {
            // Taller screen -> letterbox top/bottom
            float normalizedHeight = screenRatio / targetRatio;
            float y = (1f - normalizedHeight) * 0.5f;
            cachedCamera.rect = new Rect(0f, y, 1f, normalizedHeight);
        }
        else
        {
            cachedCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }
}
