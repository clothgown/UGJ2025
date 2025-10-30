using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamondChange : MonoBehaviour
{
    public UnitController diamondUnit;
    public EnemyUnit diamondEnemy;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void ChangeDiamond()
    {
        if(diamondEnemy!= null && diamondUnit!= null)
        {
            diamondEnemy.gameObject.SetActive(true);
            diamondUnit.gameObject.SetActive(false);
        }
    }
}
