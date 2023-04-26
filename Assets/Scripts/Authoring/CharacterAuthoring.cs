using Unity.Entities;
using UnityEngine;

class CharacterAuthoring : MonoBehaviour
{
    public float moveSpeed = 6f;
    public int hp = 100;
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
            hp = authoring.hp
        });
    }
}