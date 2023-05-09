using System.Collections;
using Unity.Entities;
using UnityEngine;

public struct Box : IComponentData
{
    public int hp;
    public Entity spawnEntity;
}