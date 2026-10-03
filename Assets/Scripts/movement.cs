using UnityEngine;

public class movement : MonoBehaviour
{
    public KeyInput keyInput;
    public float speed = 5f;

    void Update()
    {
        Vector2 moveDirection = new Vector2(
        keyInput.moveX,
        keyInput.moveY
        ).normalized;

        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }
}