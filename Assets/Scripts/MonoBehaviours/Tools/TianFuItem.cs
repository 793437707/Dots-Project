using System.Collections;
using UnityEngine;

public class TianFuItem : MonoBehaviour
{
    int id;
    private void Awake()
    {
        
    }

    public void Init(int id = -1)
    {
        if (id == -1)
            id = this.id;
        this.id = id;
    }
}