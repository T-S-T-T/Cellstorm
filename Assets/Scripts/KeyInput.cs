using UnityEngine;
using UnityEngine.InputSystem;

public class KeyInput : MonoBehaviour
{
    public bool attack1;
    public bool attack2;
    public bool skill1;
    public bool skill2;

    public float moveX;
    public float moveY;

    void Update()
    {
        attack1 = Mouse.current.leftButton.isPressed;
        attack2 = Mouse.current.rightButton.isPressed;

        skill1 = Keyboard.current.qKey.isPressed;
        skill2 = Keyboard.current.eKey.isPressed;

        moveX = 0;
        moveY = 0;

        if (Keyboard.current.aKey.isPressed)
            moveX -= 1;

        if (Keyboard.current.dKey.isPressed)
            moveX += 1;

        if (Keyboard.current.sKey.isPressed)
            moveY -= 1;

        if (Keyboard.current.wKey.isPressed)
            moveY += 1;

        Debug.Log($"attack1:{attack1} attack2:{attack2} skill1:{skill1} skill2:{skill2} moveX:{moveX} moveY:{moveY}");
    }
}