using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform targetPosition;
    [SerializeField] float smoothTime = 0.2f;
    [SerializeField] float minX = 0f;
    [SerializeField] float maxX = 40f;
    [SerializeField] float fixedY = 0f;

    Vector3 velocity = Vector3.zero;
    Vector3 shakeOffset = Vector3.zero;
    Vector3 basePosition;

    public static CameraController Instancia { get; private set; }

    void Awake()
    {
        Instancia = this;
        basePosition = transform.position;
    }

    void LateUpdate()
    {
        float x = Mathf.Clamp(targetPosition.position.x, minX, maxX);
        basePosition = Vector3.SmoothDamp(
            basePosition,
            new Vector3(x, fixedY, transform.position.z),
            ref velocity,
            smoothTime
        );
        transform.position = basePosition + shakeOffset;
    }

    public void Shake(float duration, float magnitude) => StartCoroutine(ShakeRoutine(duration, magnitude));

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float t = 0f;
        while (t < duration)
        {
            float forca = magnitude * (1f - t / duration);
            shakeOffset = (Vector3)(Random.insideUnitCircle * forca);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}
