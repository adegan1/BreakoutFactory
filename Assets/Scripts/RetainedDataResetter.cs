using UnityEngine;

[DisallowMultipleComponent]
public class RetainedDataResetter : MonoBehaviour
{
    [Header("Optional Explicit References")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private FactoryBuildingPlacer factoryBuildingPlacer;
    [SerializeField] private LevelSettings levelSettings;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameSettings gameSettings;

    [Header("Factory Reset")]
    [SerializeField] private bool clearLooseFactoryItems = true;

    [ContextMenu("Reset All Retained Data")]
    public void ResetAllRetainedData()
    {
        EnsureReferences();

        // Clear persisted factory save even if factory scene objects are not loaded.
        FactoryStatePersistence.ClearSaveData();

        if (factoryBuildingPlacer != null)
        {
            factoryBuildingPlacer.ClearAllPlacedBuildings(false, clearLooseFactoryItems);
        }

        if (inventoryManager != null)
        {
            inventoryManager.ResetToStartingData();
        }

        if (levelSettings != null)
        {
            levelSettings.ResetToDefaults();
        }

        if (playerStats != null)
        {
            playerStats.ResetToStartingValues();
        }

        if (gameSettings != null)
        {
            gameSettings.ResetToDefaults();
        }

        Debug.Log("RetainedDataResetter: All retained data has been reset.", this);
    }

    private void EnsureReferences()
    {
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.HasInstance
                ? InventoryManager.Instance
                : FindFirstObjectByType<InventoryManager>();
        }

        if (factoryBuildingPlacer == null)
        {
            factoryBuildingPlacer = FindFirstObjectByType<FactoryBuildingPlacer>();
        }

        if (levelSettings == null)
        {
            levelSettings = LevelSettings.Instance;
            if (levelSettings == null)
            {
                levelSettings = FindFirstObjectByType<LevelSettings>();
            }
        }

        if (playerStats == null)
        {
            playerStats = PlayerStats.HasInstance
                ? PlayerStats.Instance
                : FindFirstObjectByType<PlayerStats>();
        }

        if (gameSettings == null)
        {
            gameSettings = GameSettings.HasInstance
                ? GameSettings.Instance
                : FindFirstObjectByType<GameSettings>();
        }
    }
}
