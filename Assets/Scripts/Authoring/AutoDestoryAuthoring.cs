using Unity.Entities;
using UnityEngine;

class AutoDestoryAuthoring : MonoBehaviour
{
    public float destoryTime = 3f;
}

class AutoDestoryBaker : Baker<AutoDestoryAuthoring>
{
    public override void Bake(AutoDestoryAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new AutoDestory
        {
            destoryTime = authoring.destoryTime
        });
    }
}