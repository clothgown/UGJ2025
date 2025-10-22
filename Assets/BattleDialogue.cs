using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleDialogue : MonoBehaviour
{
    public TextAsset dialogDataFile; // 指向CSV/TSV文件
    public bool isHealthDialogue;
    public float TrigerHealth = 0f;
    public EnemyUnit enemyUnit;
    public bool isTrigger = false;
    // Start is called before the first frame update
    void Start()
    {
        enemyUnit = GetComponent<EnemyUnit>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CheckTrigger()
    {
        if (enemyUnit != null && isHealthDialogue)
        {
            if (enemyUnit.currentHealth <= TrigerHealth && isTrigger == false)
            {
                Debug.Log(1);
                DialogueSystem dialogueSystem = FindAnyObjectByType<DialogueSystem>();
                dialogueSystem.battleDialogDataFile = dialogDataFile;
                dialogueSystem.StartNewDialogue();
                isTrigger = true;
            }
        }
    }
}
