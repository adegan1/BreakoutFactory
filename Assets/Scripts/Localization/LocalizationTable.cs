using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationTable", menuName = "Localization/Translation Table")]
public class LocalizationTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [TextArea(1, 5)] public string english;
        [TextArea(1, 5)] public string japanese;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;
}
