using UnityEngine;
using UnityEngine.UI;

// Keep the 4:3 software canvas over exactly the same part of the Game view.
[ExecuteAlways, DefaultExecutionOrder(-100)]
public class S12_Viewport : MonoBehaviour
{
    public Camera mainCamera;
    public RawImage canvasImage;
    public S12_VirtualCamera_Finish virtualCamera;

    void LateUpdate()
    {
        if (!mainCamera || !canvasImage || !virtualCamera) return;
        float aspect = (float)virtualCamera.width / virtualCamera.height;
        float screenAspect = (float)Mathf.Max(Screen.width, 1) / Mathf.Max(Screen.height, 1);
        Vector2 size = screenAspect > aspect
            ? new Vector2(aspect / screenAspect, 1)
            : new Vector2(1, screenAspect / aspect);
        Vector2 min = (Vector2.one - size) * 0.5f;
        mainCamera.rect = new Rect(min, size);
        mainCamera.aspect = aspect;
        RectTransform rt = canvasImage.rectTransform;
        rt.anchorMin = min;
        rt.anchorMax = min + size;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
