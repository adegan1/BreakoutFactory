using System.Collections.Generic;
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
        if (a == null || b == null)
        {
            return null;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            FusionReactorRecipe recipe = recipes[i];
            if (recipe != null && recipe.Matches(a, b))
            {
                return recipe;
            }
        }

        return null;
    }
}
