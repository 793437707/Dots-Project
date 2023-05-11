using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

struct AutoDestory : IComponentData
{
    public float destoryTime;
    public int score;
    public bool added;
}