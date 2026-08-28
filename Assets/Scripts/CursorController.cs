using UnityEngine;

public class CursorController : MonoBehaviour
{
    public Texture2D normalCursor;
    public Texture2D clickCursor;

    public Vector2 hotspot = Vector2.zero;
    public CursorMode cursorMode = CursorMode.Auto;

    void Start()
    {
        Cursor.SetCursor(normalCursor, hotspot, cursorMode);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.SetCursor(clickCursor, hotspot, cursorMode);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Cursor.SetCursor(normalCursor, hotspot, cursorMode);
        }
    }
}

/* TODO:
 */