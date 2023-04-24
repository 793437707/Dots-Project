using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct Enemy : IComponentData
{
    public int hp;
}