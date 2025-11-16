using System.Collections;
using UnityEngine;

public class FocusTracker : MonoBehaviour
{
    [Header("References")]
    public Transform vrCamera;          // Main Camera
    public GameObject notesObject;      // Book GameObject (transparent, with collider)
    public CanvasGroup popupCanvas;     // PopupReminder CanvasGroup
    public ParticleSystem fogParticles; // Fog particle system that will fade in or out

    [Header("Settings")]
    public float focusCheckDistance = 10f;
    public float distractionDelay = 3f;
    public float fadeDuration = 1f;

    [Header("Fog Settings")]
    [Tooltip("Maximum alpha for the fog particle Color over Lifetime")]
    public float fogMaxAlpha = 0.8f;

    private float distractionTimer = 0f;
    private Coroutine popupRoutine;

    void Start()
    {
        if (popupCanvas != null)
        {
            popupCanvas.alpha = 0f; // start hidden
        }

        // Optional: initialize fog with zero alpha
        SetFogUniformAlpha(0f);
    }

    void Update()
    {
        if (vrCamera == null || notesObject == null)
            return;

        Ray gazeRay = new Ray(vrCamera.position, vrCamera.forward);
        RaycastHit hit;

        bool focused = false;

        if (Physics.Raycast(gazeRay, out hit, focusCheckDistance))
        {
            if (hit.collider.gameObject == notesObject)
            {
                focused = true;
            }
        }

        if (focused)
        {
            distractionTimer = 0f;
            FadePopupAndFog(0f); // fade popup out, fog out
        }
        else
        {
            distractionTimer += Time.deltaTime;
            if (distractionTimer >= distractionDelay)
            {
                FadePopupAndFog(1f); // fade popup in, fog in
            }
        }
    }

    void FadePopupAndFog(float targetPopupAlpha)
    {
        if (popupCanvas == null && fogParticles == null)
            return;

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        float targetFogAlpha = targetPopupAlpha * fogMaxAlpha;
        popupRoutine = StartCoroutine(
            FadeCanvasAndFog(popupCanvas, targetPopupAlpha, targetFogAlpha)
        );
    }

    IEnumerator FadeCanvasAndFog(CanvasGroup canvas, float targetCanvasAlpha, float targetFogAlpha)
    {
        float startCanvasAlpha = canvas != null ? canvas.alpha : 0f;
        float startFogAlpha = GetCurrentFogAlpha();

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Fade popup
            if (canvas != null)
            {
                canvas.alpha = Mathf.Lerp(startCanvasAlpha, targetCanvasAlpha, t);
            }

            // Fade fog alpha
            float newFogAlpha = Mathf.Lerp(startFogAlpha, targetFogAlpha, t);
            SetFogUniformAlpha(newFogAlpha);

            yield return null;
        }

        // Snap to final values
        if (canvas != null)
        {
            canvas.alpha = targetCanvasAlpha;
        }
        SetFogUniformAlpha(targetFogAlpha);
    }

    // Reads the current alpha from the fog gradient (assumes uniform alpha)
    float GetCurrentFogAlpha()
    {
        if (fogParticles == null)
            return 0f;

        var col = fogParticles.colorOverLifetime;
        if (!col.enabled)
            return 0f;

        Gradient grad = col.color.gradient;
        if (grad == null || grad.alphaKeys == null || grad.alphaKeys.Length == 0)
            return 0f;

        // Assume we are using a uniform alpha over lifetime, so just read the first key
        return grad.alphaKeys[0].alpha;
    }

    // Sets Color over Lifetime to a gradient with uniform alpha
    void SetFogUniformAlpha(float a)
    {
        if (fogParticles == null)
            return;

        var col = fogParticles.colorOverLifetime;
        col.enabled = true;

        Gradient grad = new Gradient();

        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(a, 0f),
                new GradientAlphaKey(a, 1f)
            }
        );

        col.color = grad;
    }
}
