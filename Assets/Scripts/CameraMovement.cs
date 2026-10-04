using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public Transform player;
    public float mouseInfluence = 0.01f;
    public float smoothSpeed = 5f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector3 mouseWorld = cam.ScreenToWorldPoint(mousePos);
        mouseWorld.z = 0;

        Vector3 targetPosition = Vector3.Lerp(
        player.position,
        mouseWorld,
        mouseInfluence
        );

        targetPosition.z = transform.position.z;

        transform.position = Vector3.Lerp(
        transform.position,
        targetPosition,
        smoothSpeed * Time.deltaTime
        );
    }
}