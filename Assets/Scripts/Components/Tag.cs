using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct Tag : IComponentData
{
    public FixedString64Bytes tag;
}
