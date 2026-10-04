using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform player;
    public float mouseInfluence = 0.5f; // 0 = player only, 1 = halfway to cursor
    public float smoothSpeed = 5f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        // Mouse position in world space
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // Point between player and mouse
        Vector3 targetPosition = Vector3.Lerp(
        player.position,
        mouseWorld,
        mouseInfluence
        );

        // Keep camera's Z position
        targetPosition.z = transform.position.z;

        // Smooth movement
        transform.position = Vector3.Lerp(
        transform.position,
        targetPosition,
        smoothSpeed * Time.deltaTime
        );
    }
}