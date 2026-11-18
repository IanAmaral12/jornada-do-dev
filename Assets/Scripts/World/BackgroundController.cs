using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] float parallax = 0.05f;

    MeshRenderer mesh;
    float ultimoX;

    void Start()
    {
        mesh = GetComponent<MeshRenderer>();
        ultimoX = cameraTransform.position.x;
    }

    void Update()
    {
        float dx = cameraTransform.position.x - ultimoX;
        ultimoX = cameraTransform.position.x;
        mesh.material.mainTextureOffset += new Vector2(dx * parallax, 0f);
        transform.position = new Vector3(cameraTransform.position.x, transform.position.y, transform.position.z);
    }
}
