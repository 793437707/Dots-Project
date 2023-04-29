using Unity.Entities;
using UnityEngine;

class CharacterAuthoring : MonoBehaviour
{
    public float moveSpeed = 6f;
    public int hp = 100;
    public int hpMax = 100;
    public int mp = 100;
    public int mpMax = 100;
}

class CharacterBaker : Baker<CharacterAuthoring>
{
    public override void Bake(CharacterAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        //AddComponent<Character>(e);
        AddComponent(e, new Character
        {
            moveSpeed = authoring.moveSpeed,
            hp = authoring.hp,
            hpMax = authoring.hpMax,
            mp = authoring.mp,
            mpMax = authoring.mpMax,
            level = 1,
            exp = 0
        });
    }
}