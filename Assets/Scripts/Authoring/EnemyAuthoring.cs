using Unity.Entities;
using UnityEngine;

class EnemyAuthoring : MonoBehaviour
{
    public int hp = 30;
}

class EnemyBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new Enemy
        {
            hp = authoring.hp
        });
    }
}