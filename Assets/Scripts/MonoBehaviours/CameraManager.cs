﻿using Cinemachine;
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
    private const float MAXFOV = 90;
    private const float MINFOV = 30;

    void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _entityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<LocalToWorld>());
        GameManager.cameraManager = GetComponent<CameraManager>();
    }

    private void LateUpdate()
    {
        UpdateCameraPos();
        UpdateCameraFOV();
    }

    public void UpdateCameraPos()
    {
        var entities = _entityQuery.ToEntityArray(Allocator.TempJob);
        if (entities.Length > 0)
        {
            var translation = _entityManager.GetComponentData<LocalToWorld>(entities[0]);
            virtualCamera.transform.position = translation.Position + cameraDis;
            virtualCamera.transform.LookAt(translation.Position);
        }
    }

    private void UpdateCameraFOV()
    {
        float lastFOV = virtualCamera.m_Lens.FieldOfView;
        lastFOV -= Input.GetAxis("Mouse ScrollWheel") * 10;
        lastFOV = math.max(lastFOV, MINFOV);
        lastFOV = math.min(lastFOV, MAXFOV);
        virtualCamera.m_Lens.FieldOfView = lastFOV;
    }
}