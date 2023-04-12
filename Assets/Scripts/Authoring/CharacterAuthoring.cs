using Unity.Entities;
using UnityEngine;

class CharacterAuthoring:MonoBehaviour
{
    public float moveSpeed = 300f;
}

class CharacterBaker:Baker<CharacterAuthoring>
{
    public override void Bake(CharacterAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        //AddComponent<Character>(e);
        AddComponent(e, new Character
        {
            moveSpeed = authoring.moveSpeed
        });
    }
}