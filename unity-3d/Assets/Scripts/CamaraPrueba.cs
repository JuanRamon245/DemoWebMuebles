using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraPrueba : MonoBehaviour
{
    public float velocidadMove = 5f;
    public float velocidadRotacion = 0.1f;

    void Update()
    {
        if (Keyboard.current != null)
        {
            float moveH = 0f;
            float moveV = 0f;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveV = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveV = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveH = 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveH = -1f;

            Vector3 direccion = new Vector3(moveH, 0, moveV) * velocidadMove * Time.deltaTime;
            transform.Translate(direccion);
        }

        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float mouseX = mouseDelta.x * velocidadRotacion;
            float mouseY = -mouseDelta.y * velocidadRotacion;

            transform.Rotate(0, mouseX, 0, Space.World);
            transform.Rotate(mouseY, 0, 0, Space.Self);
        }
    }
}