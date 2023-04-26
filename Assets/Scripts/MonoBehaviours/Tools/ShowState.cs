using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ShowState : MonoBehaviour
{
   
    TextMeshProUGUI text;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void LateUpdate()
    {
        
        Entity character = GameManager.GetEntityForTag("Character");
        int lastHp = character == Entity.Null ? 0 : World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Character>(character).hp;
        text.text = string.Format("{0:0} HP Last", lastHp);
    }
}
