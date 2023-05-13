using Unity.Entities;
using UnityEngine;

class AutoDestoryAuthoring : MonoBehaviour
{
    public float destoryTime = 3f;
    public int score = 1;
}

class AutoDestoryBaker : Baker<AutoDestoryAuthoring>
{
    public override void Bake(AutoDestoryAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new AutoDestory
        {
            destoryTime = authoring.destoryTime,
            score = authoring.score,
            added = false,
            xpadd = 0
        });
    }
}