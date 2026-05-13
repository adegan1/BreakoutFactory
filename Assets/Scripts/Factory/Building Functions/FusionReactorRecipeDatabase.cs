using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Container for all Fusion Reactor recipes.
/// Assign this asset to the FusionReactorBuilding to define what combinations are possible.
/// </summary>
[CreateAssetMenu(fileName = "FusionReactorRecipeDatabase", menuName = "Factory/Fusion Reactor/Recipe Database")]
public class FusionReactorRecipeDatabase : ScriptableObject
{
    [SerializeField] private List<FusionReactorRecipe> recipes = new();

    public IReadOnlyList<FusionReactorRecipe> Recipes => recipes;

    /// <summary>
    /// Finds a recipe that matches the given input pair (order does not matter).
    /// Returns null if no matching recipe exists.
    /// </summary>
    public FusionReactorRecipe FindRecipe(ItemDefinition a, ItemDefinition b)
    {
        return recipes.FirstOrDefault(recipe => recipe != null && recipe.Matches(a, b));
    }
}
