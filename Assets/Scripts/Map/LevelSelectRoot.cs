using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelectRoot : MonoBehaviour
{
    public static LevelSelectRoot instance;

    public MapGridManager mapGridManager;
    private void Awake()
    {
        // µ¥ÀýÅÐ¶Ï£¬·ÀÖ¹ÖØ¸´
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // ÇÐ³¡¾°²»Ïú»Ù
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        mapGridManager = FindAnyObjectByType<MapGridManager>();
    }

    public void SetGridManagerTrue()
    {
        mapGridManager.gameObject.SetActive(true);
        mapGridManager.currentGrid.ShowNextGrids();
    }
}
