using System.Collections;
using Unity.Entities;
using UnityEngine;

class BoxAuthoring : MonoBehaviour
{
    public int hp = 10;
}

class BoxBaker : Baker<BoxAuthoring>
{
    public override void Bake(BoxAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new Box
        {
            hp = authoring.hp
        });
    }
}