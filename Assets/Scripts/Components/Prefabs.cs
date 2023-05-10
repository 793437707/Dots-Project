using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct Prefabs : IComponentData
{
    public Entity Entity;
    public FixedString64Bytes name;
}