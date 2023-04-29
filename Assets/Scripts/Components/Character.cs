using Unity.Entities;

struct Character: IComponentData
{
    public float moveSpeed;
    public int hp;
    public int hpMax;
    public int mp;
    public int mpMax;
    public int level;
    public int exp;
}
