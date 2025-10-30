using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClubChange : MonoBehaviour
{
    public UnitController clubUnit;
    public EnemyUnit clubEnemy;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public void ChangeClub()
    {
        if (clubEnemy != null && clubUnit != null)
        {
            clubEnemy.gameObject.SetActive(true);
            clubUnit.gameObject.SetActive(false);
        }
    }
}
