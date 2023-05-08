using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct AddCharacterData : IComponentData
{
    public CharacterAddDataEnum type;
    public int value;
    public float AddCD;
    public float nextAddTime;
    public bool AddOnce;
    public float minDis;
    public bool infDis;
}