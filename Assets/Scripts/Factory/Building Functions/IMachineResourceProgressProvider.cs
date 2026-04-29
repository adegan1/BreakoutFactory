using UnityEngine;

public interface IMachineResourceProgressProvider
{
    int CurrentResourceAmount { get; }
    int MaxResourceAmount { get; }
    float NormalizedResourceAmount { get; }
    Color ResourceTint { get; }
}