using Unity.Entities;
using UnityEngine;

class BulletAuthoring : MonoBehaviour
{
    public int damage = 10;
}

class BulletBaker : Baker<BulletAuthoring>
{
    public override void Bake(BulletAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new Bullet
        {
            damage = authoring.damage
        });
    }
}