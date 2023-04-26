using Unity.Entities;
using UnityEngine;

class EnemyAuthoring : MonoBehaviour
{
    public int hp = 30;
    public int damage = 10;
    public float attackCD = 0.5f;
    public float attackSize = 2f;
    public float runSize = 7f;
    public float runSpeed = 10f;
    public float walkSize = 25f;
    public float walkSpeed = 4f;
    public float deadStayTime = 5f;
    public EnemyMobs mob = EnemyMobs.NULL;
}

class EnemyBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);

        AddComponent(e, new Enemy
        {
            hp = authoring.hp,
            damage = authoring.damage,
            attackCD = authoring.attackCD,
            attackTime = 0,
            attackSize = authoring.attackSize,
            runSize = authoring.runSize,
            runSpeed = authoring.runSpeed,
            walkSize = authoring.walkSize,
            walkSpeed = authoring.walkSpeed,
            deadStayTime = authoring.deadStayTime,
            animatior = EnemyAnimatior.Idle,
            mob = authoring.mob
        });
    }
}