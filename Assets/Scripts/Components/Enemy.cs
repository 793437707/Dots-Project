using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct Enemy : IComponentData
{
    public int hp;
    public int maxHp;
    public int damage;
    public float attackCD;
    public float attackTime;
    public float attackSize;
    public float runSize;
    public float runSpeed;
    public float walkSize;
    public float walkSpeed;
    public float deadStayTime;
    public EnemyAnimatior animatior;
    public EnemyMobs mob;
}

public enum EnemyAnimatior { Idle, Walk, Run, AttackFirst, Attack, DeadFirst, Dead }
public enum EnemyMobs { Zombie, NULL }