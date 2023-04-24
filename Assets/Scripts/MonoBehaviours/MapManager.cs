using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

public class MapManager : MonoBehaviour
{
    public static uint MapSeed = 1919191;
    public static int Length = 10;
    public static int Width = 10;

    public GameObject goPlane = null;
    public static float2 planeSize = float2.zero;
    public Material[] planeMaterials;
    private int[] planeMaterialsID;

    private Unity.Mathematics.Random random;
    private EntityManager entityManager;

    private void Awake()
    {
        if(goPlane != null)
        {
            planeSize.x = goPlane.GetComponent<MeshFilter>().sharedMesh.bounds.size.x * goPlane.transform.localScale.x;
            planeSize.y = goPlane.GetComponent<MeshFilter>().sharedMesh.bounds.size.z * goPlane.transform.localScale.z;
        }
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        //注册地板材质并保存
        var hybridRenderer = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        planeMaterialsID = new int[planeMaterials.Length];
        for(int i = 0; i < planeMaterials.Length; i ++)
            planeMaterialsID[i] = (int)hybridRenderer.RegisterMaterial(planeMaterials[i]).value;
    }

    public void CreatPlantMap()
    {
        Entity parent = GameManager.GetEntityForTag("Plane");
        if(entityManager.HasComponent<Child>(parent) == false)
        {
            Debug.LogError("None Prefab For Map/Plane");
            return;
        }
        Entity planePrefab = entityManager.GetBuffer<Child>(parent)[0].Value;
        random = new Unity.Mathematics.Random(MapSeed);

        float posx = planeSize.x * Length / 2;
        for (int i = 0; i < Width; i ++)
        {
            float posz = planeSize.y * Width / 2;
            for (int j = 0; j < Length; j ++)
            {
                //生成实体
                Entity plane = entityManager.Instantiate(planePrefab);
                entityManager.SetComponentData(plane, new Parent { Value = parent });
                //修改坐标
                entityManager.SetComponentData(plane, LocalTransform.FromPosition(new float3(posx, 0, posz)));
                //修改材质  目前是随机
                int index = random.NextInt(0, planeMaterials.Length);
                MaterialMeshInfo meshInfo = entityManager.GetComponentData<MaterialMeshInfo>(plane);
                meshInfo.Material = planeMaterialsID[index];
                entityManager.SetComponentData(plane, meshInfo);

                posz -= planeSize.y;
            }
            posx -= planeSize.x;
        }
    }

    public void CreateMap()
    {
        Debug.Log("Start CreatMap For Seed " + MapSeed);
        CreatPlantMap();
        Debug.Log("End CreateMap For Seed " + MapSeed);
    }
}