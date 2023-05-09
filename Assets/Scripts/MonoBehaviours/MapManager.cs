using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static uint MapSeed = 1919191;
    public static int Length = 30;
    public static int Width = 30;

    public GameObject goPlane = null;
    public static float2 planeSize = float2.zero;
    public Material[] planeMaterials;
    private int[] planeMaterialsID;

    private Unity.Mathematics.Random random;
    private EntityManager entityManager;

    private void Awake()
    {
        GameManager.mapManager = this;
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
        Entity planeParent = GameManager.GetEntityForTag("Plane");
        Entity planePrefab = GameManager.GetEntityForTag("PlanePrefab");
        Entity boxParent = GameManager.GetEntityForTag("Box");
        Entity BoxPrefab = GameManager.GetEntityForTag("BoxPrefab");
        if (planeParent == Entity.Null || planePrefab == Entity.Null || BoxPrefab == Entity.Null || boxParent == Entity.Null)
        {
            Debug.LogError("None Prefab For CreatPlanMap");
            return;
        }

        //BOX ENTITY
        Entity BoxHp = GameManager.GetEntityForTag("BoxHp");
        Entity BoxMp = GameManager.GetEntityForTag("BoxMp");
        Entity BoxCoin = GameManager.GetEntityForTag("BoxCoin");

        
        random = new Unity.Mathematics.Random(MapSeed);

        float posx = planeSize.x * Length / 2;
        for (int i = 0; i < Width; i ++)
        {
            float posz = planeSize.y * Width / 2;
            for (int j = 0; j < Length; j ++)
            {
                //生成实体
                Entity plane = entityManager.Instantiate(planePrefab);
 
                entityManager.AddComponentData(plane, new Parent { Value = planeParent });
                //修改坐标
                entityManager.SetComponentData(plane, LocalTransform.FromPosition(new float3(posx, 0, posz)));
                //修改材质  目前是随机
                int index = random.NextInt(0, planeMaterials.Length);
                MaterialMeshInfo meshInfo = entityManager.GetComponentData<MaterialMeshInfo>(plane);
                meshInfo.Material = planeMaterialsID[index];
                entityManager.SetComponentData(plane, meshInfo);

                //生成世界装饰
                //奖励箱
                if(random.NextInt(0, 100) < 30)
                {
                    Entity box = entityManager.Instantiate(BoxPrefab);
                    entityManager.AddComponentData(box, new Parent { Value = boxParent });
                    LocalTransform transform = entityManager.GetComponentData<LocalTransform>(box);
                    transform.Position.x = posx + (planeSize.x / 2 - 2) * random.NextFloat() * (random.NextBool() ? 1 : -1);
                    transform.Position.y = 0;
                    transform.Position.z = posz + (planeSize.y / 2 - 2) * random.NextFloat() * (random.NextBool() ? 1 : -1);
                    transform.Rotation.value = quaternion.RotateY(random.NextFloat() * 360).value;
                    entityManager.SetComponentData(box, transform);

                    //设置箱子里爆出来的物品
                    Box boxBox = entityManager.GetComponentData<Box>(box);
                    int boxRandomPrefab = random.NextInt(0, 100);
                    if (boxRandomPrefab < 50)
                        boxBox.spawnEntity = BoxHp;
                    else if(boxRandomPrefab < 80)
                        boxBox.spawnEntity = BoxMp;
                    else if (boxRandomPrefab < 90)
                        boxBox.spawnEntity = BoxCoin;
                    else //爆的技能，先用coin代替
                        boxBox.spawnEntity = BoxCoin;
                    entityManager.SetComponentData(box, boxBox);
                }


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