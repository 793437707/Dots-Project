using Unity.Entities;
using UnityEngine;

class TagAuthoring : MonoBehaviour
{
    public string TAG = "Root";
}

class TagBaker : Baker<TagAuthoring>
{
    public override void Bake(TagAuthoring authoring)
    {
        var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(e, new Tag
        {
            tag = authoring.TAG
        });
    }
}
