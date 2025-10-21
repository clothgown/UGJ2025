using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
public class CursorChange : MonoBehaviour
{
    private Texture2D cursorTexture;
    private bool IsPointerOver;
    void Start()
    {
        cursorTexture = Resources.Load("CursorImage") as Texture2D;
        Cursor.SetCursor(null, new Vector2(43f, 0f), CursorMode.Auto);
        IsPointerOver = false;
        //Cursor.SetCursor(cursorTexture, new Vector2(43f, 0f), CursorMode.Auto);
    }
    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            OnMouseEnter();
        else
            OnMouseExit();
    }
    private void OnMouseEnter()
    {
        if (!IsPointerOver)
        {
            Cursor.SetCursor(cursorTexture, new Vector2(43f, 0f), CursorMode.Auto);
            IsPointerOver = true;
            //Debug.Log("bark");
        }
    }
    void OnMouseExit()
    {
        if (IsPointerOver)
        {
            Cursor.SetCursor(null, new Vector2(43f, 0f), CursorMode.Auto);
            IsPointerOver = false;
            //Debug.Log("Gou Jiao");
        }
    }
}
