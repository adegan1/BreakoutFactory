using UnityEngine;

[CreateAssetMenu(fileName = "New Generator Settings", menuName = "Factory/Building Settings/Generator")]
public class GeneratorBuildingSettings : ScriptableObject
{
    [Header("Item Generation")]
    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField, Min(1)] private int maxItemsToSpawn = 10;
    [SerializeField, Min(1)] private int quantityPerSpawn = 1;
    [SerializeField, Min(0.01f)] private float spawnIntervalSeconds = 1f;

    [Header("Output")]
    [SerializeField] private GeneratorBuilding.OutputSide outputSide = GeneratorBuilding.OutputSide.Right;

    public ItemDefinition ItemDefinition => itemDefinition;
    public int MaxItemsToSpawn => maxItemsToSpawn;
    public int QuantityPerSpawn => quantityPerSpawn;
    public float SpawnIntervalSeconds => spawnIntervalSeconds;
    public GeneratorBuilding.OutputSide OutputSide => outputSide;

    private void OnValidate()
    {
        maxItemsToSpawn = Mathf.Max(1, maxItemsToSpawn);
        quantityPerSpawn = Mathf.Max(1, quantityPerSpawn);
        spawnIntervalSeconds = Mathf.Max(0.01f, spawnIntervalSeconds);
    }
}