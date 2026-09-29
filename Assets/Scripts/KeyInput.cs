using UnityEngine;

public class KeyInput : MonoBehaviour
{
    [Header("Actions")]
    public bool attack1;
    public bool attack2;
    public bool skill1;
    public bool skill2;

    [Header("Movement")]
    public float moveX;
    public float moveY;

    void Update()
    {
        // Mouse buttons
        attack1 = Input.GetMouseButton(0); // Left Click
        attack2 = Input.GetMouseButton(1); // Right Click

        // Skills
        skill1 = Input.GetKey(KeyCode.Q);
        skill2 = Input.GetKey(KeyCode.E);

        // Movement axes
        moveX = Input.GetAxisRaw("Horizontal"); // A = -1, D = 1
        moveY = Input.GetAxisRaw("Vertical"); // S = -1, W = 1
    }
}