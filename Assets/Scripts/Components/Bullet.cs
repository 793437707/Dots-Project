using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct Bullet : IComponentData
{
    public int damage;
}