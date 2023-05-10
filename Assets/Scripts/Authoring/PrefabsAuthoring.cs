using Unity.Entities;
using UnityEngine;
using Unity.Collections;

class PrefabsAuthoring : MonoBehaviour
{
    public GameObject _gameObject = null;
    public string _name = "";
}

class PrefabsBaker : Baker<PrefabsAuthoring>
{
    public override void Bake(PrefabsAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new Prefabs
        {
            name = authoring._name,
            Entity = GetEntity(authoring._gameObject, TransformUsageFlags.Dynamic)
        });
    }
}