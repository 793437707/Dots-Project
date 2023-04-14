using Unity.Entities;

readonly partial struct AutoDestoryAspects : IAspect
{
    public readonly Entity self;
    public readonly RefRW<AutoDestory> autoDestory;
    public float lastTime
    {
        get => autoDestory.ValueRO.destoryTime;
        set => autoDestory.ValueRW.destoryTime = value;
    }
}