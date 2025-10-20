using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInMap : MonoBehaviour
{
    public Vector2Int startPos;
    public Vector2Int currentPos;
    private void Awake()
    {
        currentPos = startPos;
    }
}
