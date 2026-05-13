using UnityEngine;

// A single recipe for the Fusion Reactor machine.
// Define two input items (order does not matter) and the resulting output item.
[CreateAssetMenu(fileName = "New Fusion Recipe", menuName = "Factory/Fusion Reactor/Recipe")]
public class FusionReactorRecipe : ScriptableObject
{
    [Header("Inputs (order does not matter)")]
    [SerializeField] private ItemDefinition inputA;
    [SerializeField] private ItemDefinition inputB;

    [Header("Output")]
    [SerializeField] private ItemDefinition output;
    [SerializeField, Min(1)] private int outputQuantity = 1;

    [Header("Cost Per Craft")]
    [Tooltip("How many of Input A are consumed per output.")]
    [SerializeField, Min(1)] private int costA = 1;
    [Tooltip("How many of Input B are consumed per output.")]
    [SerializeField, Min(1)] private int costB = 1;

    public ItemDefinition InputA => inputA;
    public ItemDefinition InputB => inputB;
    public ItemDefinition Output => output;
    public int OutputQuantity => outputQuantity;
    public int CostA => costA;
    public int CostB => costB;

    // Returns true if this recipe can process the given pair of items (in any order).
    public bool Matches(ItemDefinition a, ItemDefinition b)
    {
        if (a == null || b == null || inputA == null || inputB == null)
        {
            return false;
        }

        return (a == inputA && b == inputB) || (a == inputB && b == inputA);
    }
}
