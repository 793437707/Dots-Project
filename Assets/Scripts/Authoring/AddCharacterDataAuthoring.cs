using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class AddCharacterDataAuthoring : MonoBehaviour
{
    public CharacterAddDataEnum type = CharacterAddDataEnum.Damage;
    public int value = 100;
    public float AddCD = -1;
    public float nextAddTime = 10000;
    public bool AddOnce = true;
    public float minDis = 3;
    public bool infDis = false;
}

class AddCharacterDataBaker : Baker<AddCharacterDataAuthoring>
{
    public override void Bake(AddCharacterDataAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new AddCharacterData
        {
            type = authoring.type,
            value = authoring.value,
            AddCD = authoring.AddCD,
            nextAddTime = authoring.nextAddTime,
            AddOnce = authoring.AddOnce,
            minDis = authoring.minDis,
            infDis = authoring.infDis,
        });
    }
}