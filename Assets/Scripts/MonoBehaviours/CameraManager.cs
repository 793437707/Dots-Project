using Cinemachine;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public float3 cameraDis = new float3(0, 7, -2);

    private CinemachineVirtualCamera virtualCamera;

    private EntityManager _entityManager;
    private EntityQuery _entityQuery;

    void Start()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<LocalToWorld>());
    }

    private void LateUpdate()
    {
        var entities = _entityQuery.ToEntityArray(Allocator.TempJob);
        if (entities.Length > 0)
        {
            var translation = _entityManager.GetComponentData<LocalToWorld>(entities[0]);
            virtualCamera.transform.position = translation.Position + cameraDis;
            virtualCamera.transform.LookAt(translation.Position);
        }
    }
}