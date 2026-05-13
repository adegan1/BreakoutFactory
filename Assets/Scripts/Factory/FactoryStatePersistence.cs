using System;
using System.Collections.Generic;
using UnityEngine;

// Saves and restores the placed buildings in the factory scene using PlayerPrefs.
// Attach this to a persistent GameObject in the factory scene alongside FactoryBuildingPlacer.
// Populate the Building Registry with every BuildingDefinition asset that can be placed.
[DisallowMultipleComponent]
public class FactoryStatePersistence : MonoBehaviour
{
    private const string SaveKey = "FactorySaveData";

    [Serializable]
    private class SaveData
    {
        public List<FactoryBuildingPlacer.FactoryBuildingEntry> buildings = new List<FactoryBuildingPlacer.FactoryBuildingEntry>();
    }

    [SerializeField] private FactoryBuildingPlacer buildingPlacer;

    [Tooltip("All BuildingDefinition assets that can appear in the factory. Used to look up definitions by name when loading.")]
    [SerializeField] private List<BuildingDefinition> buildingRegistry = new List<BuildingDefinition>();

    private readonly Dictionary<string, BuildingDefinition> registryByName = new Dictionary<string, BuildingDefinition>();

    private void Awake()
    {
        foreach (BuildingDefinition definition in buildingRegistry)
        {
            if (definition != null && !string.IsNullOrEmpty(definition.name))
            {
                registryByName[definition.name] = definition;
            }
        }
    }

    private void Start()
    {
        LoadFactory();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveFactory();
        }
    }

    private void OnDestroy()
    {
        SaveFactory();
    }

    // Serializes all currently placed buildings to PlayerPrefs.
    public void SaveFactory()
    {
        if (buildingPlacer == null)
        {
            return;
        }

        SaveData data = new SaveData
        {
            buildings = buildingPlacer.GetSaveData()
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    // Reads PlayerPrefs and restores previously saved buildings via FactoryBuildingPlacer.
    public void LoadFactory()
    {
        if (buildingPlacer == null)
        {
            return;
        }

        if (InventoryManager.HasInstance)
        {
            InventoryManager.Instance.ClearStoredMachineResources();
        }

        string json = PlayerPrefs.GetString(SaveKey, null);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FactoryStatePersistence: Failed to parse save data. {e.Message}");
            return;
        }

        if (data?.buildings == null)
        {
            return;
        }

        foreach (FactoryBuildingPlacer.FactoryBuildingEntry entry in data.buildings)
        {
            if (!registryByName.TryGetValue(entry.definitionName, out BuildingDefinition definition))
            {
                Debug.LogWarning($"FactoryStatePersistence: Could not find BuildingDefinition named '{entry.definitionName}'. Add it to the Building Registry.");
                continue;
            }

            buildingPlacer.RestoreBuilding(
                definition,
                new Vector2Int(entry.topLeftX, entry.topLeftY),
                entry.rotationQuarterTurns);
        }

        buildingPlacer.RefreshAllConveyorVisuals();
    }

    // Deletes the saved factory state. Useful for new-game resets.
    [ContextMenu("Clear Factory Save")]
    public void ClearSave()
    {
        ClearSaveData();
        Debug.Log("FactoryStatePersistence: Save data cleared.");
    }

    public static void ClearSaveData()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
