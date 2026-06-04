using UnityEngine;

[DisallowMultipleComponent]
public class LocalizationTextExclusion : MonoBehaviour
{
    [SerializeField] private bool excludeTranslation = true;
    [SerializeField] private bool excludeFontSwap = true;

    public bool ExcludeTranslation => excludeTranslation;
    public bool ExcludeFontSwap => excludeFontSwap;
}
