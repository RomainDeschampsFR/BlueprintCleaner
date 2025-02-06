using Il2Cpp;
using MelonLoader;
using System;
using Il2CppSystem.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Il2CppTLD.Gear;
using Il2CppNodeCanvas.Tasks.Conditions;
using UnityEngine;
using Il2CppTLD.UI.Scroll;
using Il2CppTLD.Cooking;
using Il2CppNodeCanvas.Tasks.Actions;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using Il2CppRewired.Utils;
using Il2CppRewired.ComponentControls.Data;
using System.Security;

namespace BlueprintCleaner
{
    internal class Patches
    {
        public static bool alreadyRefreshed = false;
        public static float? scrollValue;
        public static int? firstLine;
        public static int? lastIndex;
        public static string? lastBlueprintSelected;
        public static string? lastRecipeSelected;
        public static int? lastCookableItemSelected;

        // Dictionnary to store item names and corresponding BlueprintData
        public static Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<string>> blueprintDuplicates = new Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<string>>();

        // Dictionnary for active blueprint
        public static Il2CppSystem.Collections.Generic.Dictionary<string, string> activeBlueprint = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();

        // Dictionnary to store item names and corresponding Recipe
        public static Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<string>> recipeDuplicates = new Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<string>>();

        // Dictionnary for active recipe
        public static Il2CppSystem.Collections.Generic.Dictionary<string, string> activeRecipe = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();

        // Dictionnary to store item names and corresponding cookableItem
        public static Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<int>> cookableItemDuplicates = new Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<int>>();

        // Dictionnary for active cookable (without recipe)
        public static Il2CppSystem.Collections.Generic.Dictionary<string, int> activeCookableItem = new Il2CppSystem.Collections.Generic.Dictionary<string, int>();

        /////////////////////////////////////////////////////////////////////////////////
        // --------------------------  CRAFTING PATCHES  ----------------------------- //
        /////////////////////////////////////////////////////////////////////////////////

        [HarmonyPatch(typeof(BlueprintManager), nameof(BlueprintManager.LoadAddressableBlueprints))]
        internal static class BlueprintManager_LoadAddressableBlueprints
        {
            private static void Postfix(BlueprintManager __instance)
            {
                // SKIP 
                if (Main.vanillaDisplay) return;

                blueprintDuplicates.Clear();
                activeBlueprint.Clear();

                foreach (BlueprintData bpi in __instance.m_AllBlueprints)
                {
                    string? itemName = null;

                    if (bpi.m_CraftedResultDecoration != null)
                    {
                        itemName = bpi.m_CraftedResultDecoration.name;
                    }
                    else if (bpi.m_CraftedResultGear != null)
                    {
                        itemName = bpi.m_CraftedResultGear.name;
                    }

                    if (itemName != null)
                    {
                        if (blueprintDuplicates.ContainsKey(itemName))
                        {
                            blueprintDuplicates[itemName].Add(bpi.name);
                        }
                        else
                        {
                            blueprintDuplicates[itemName] = new Il2CppSystem.Collections.Generic.List<string>();
                            blueprintDuplicates[itemName].Add(bpi.name);
                        }
                    }
                }

                var keysToRemove = new Il2CppSystem.Collections.Generic.List<string>(); // List to keep track of keys to remove

                foreach (var pair in blueprintDuplicates)
                {
                    if (pair.Value.Count == 1)
                    {
                        keysToRemove.Add(pair.Key); // Mark the key for removal if it has only one blueprint name
                    }
                }

                // Now remove the marked keys from the dictionary
                foreach (var key in keysToRemove)
                {
                    blueprintDuplicates.Remove(key);
                }


                var keysToUpdate = new Il2CppSystem.Collections.Generic.List<string>();

                foreach (var blueprint in Main.blueprintsRemoved)
                {

                    foreach (var result in blueprintDuplicates)
                    {
                        if (result.Value != null && result.Value.Contains(blueprint))
                        {
                            result.Value.Remove(blueprint);
                            if (result.Value == null || result.Value.Count == 0)
                            {
                                keysToUpdate.Add(result.Key);
                            }
                        }
                    }
                }

                foreach (var key in keysToUpdate)
                {
                    blueprintDuplicates[key] = new Il2CppSystem.Collections.Generic.List<string>();
                }

                // Log the final dictionary content for debugging
                foreach (var pair in blueprintDuplicates)
                {
                    //populate activeBlueprint dictionary with the first blueprints for each item
                    activeBlueprint[pair.Key] = pair.Value != null && pair.Value.Count > 0 ? pair.Value[0] : "";

                    /// LOG PART ///
                    /*Il2CppSystem.Collections.Generic.List<string> blueprintNamesList = pair.Value;
                    string blueprintNames = string.Empty;

                    for (int i = 0; i < blueprintNamesList.Count; i++)
                    {

                        blueprintNames += blueprintNamesList[i];

                        if (i < blueprintNamesList.Count - 1)
                        {
                            blueprintNames += ", ";
                        }
                    }
                    MelonLogger.Msg($"Item: {pair.Key}, Blueprints: {blueprintNames}");*/
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.ItemPassesFilter))]
        internal static class Panel_Crafting_ItemPassesFilter
        {
            private static void Postfix(Panel_Crafting __instance, BlueprintData bpi, ref bool __result)
            {
                // SKIP 
                if (Main.vanillaDisplay) return;

                if (__result)
                {
                    if (bpi.m_CraftedResultDecoration != null)
                    {
                        string resultName = bpi.m_CraftedResultDecoration.name;
                        string blueprintName = bpi.name;

                        if (blueprintDuplicates.ContainsKey(resultName))
                        {
                            if (activeBlueprint[resultName] == blueprintName)
                            {
                                __result = true;
                            }
                            else
                            {
                                __result = false;
                            }
                        }
                        else if (Main.blueprintsRemoved.Contains(blueprintName))
                        {
                             __result = false;
                        }
                    }
                    else if (bpi.m_CraftedResultGear != null)
                    {
                        string resultName = bpi.m_CraftedResultGear.name;
                        string blueprintName = bpi.name;

                        if (blueprintDuplicates.ContainsKey(resultName))
                        {
                            if (activeBlueprint[resultName] == blueprintName)
                            {
                                __result = true;
                            }
                            else
                            {
                                __result = false;
                            }
                        }
                        else if(Main.blueprintsRemoved.Contains(blueprintName))
                        {
                             __result = false;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.HandleInput))]
        internal static class Panel_Crafting_HandleInput
        {
            private static void Postfix(Panel_Crafting __instance)
            {
                // ENABLE/DISABLE vanilla view (and the whole mod)
                if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.settings.viewKey))
                {
                    Main.vanillaDisplay = !Main.vanillaDisplay;
                    __instance.OnCategoryChanged(__instance.m_CategoryNavigation.m_CurrentIndex);
                }

                // SKIP 
                if (Main.vanillaDisplay) return;

                if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.settings.rightKey))
                {
                    if (__instance.SelectedBPI.m_CraftedResultDecoration != null)
                    {
                        string resultName = __instance.SelectedBPI.m_CraftedResultDecoration.name;
                        string blueprintName = __instance.SelectedBPI.name;
                        int blueprintIndex = 0;

                        if (activeBlueprint.ContainsKey(resultName))
                        {
                            int blueprintCount = blueprintDuplicates[resultName].Count;
                            
                            for (int i = 0; i < blueprintCount; i++)
                            {
                                if (blueprintDuplicates[resultName][i] == blueprintName) blueprintIndex = i;
                            }

                            if (blueprintIndex == blueprintCount - 1)
                            {
                                activeBlueprint[resultName] = blueprintDuplicates[resultName][0];
                            }
                            else
                            {
                                activeBlueprint[resultName] = blueprintDuplicates[resultName][blueprintIndex + 1];
                            }

                            lastBlueprintSelected = activeBlueprint[resultName];
                            __instance.OnCategoryChanged(__instance.m_CategoryNavigation.m_CurrentIndex);
                        }
                    }
                    else if (__instance.SelectedBPI.m_CraftedResultGear != null)
                    {
                        string resultName = __instance.SelectedBPI.m_CraftedResultGear.name;
                        string blueprintName = __instance.SelectedBPI.name;
                        int blueprintIndex = 0;

                        if (activeBlueprint.ContainsKey(resultName))
                        {
                            int blueprintCount = blueprintDuplicates[resultName].Count;

                            for (int i = 0; i < blueprintCount; i++)
                            {
                                if (blueprintDuplicates[resultName][i] == blueprintName) blueprintIndex = i;
                            }

                            if (blueprintIndex == blueprintCount - 1)
                            {
                                activeBlueprint[resultName] = blueprintDuplicates[resultName][0];
                            }
                            else
                            {
                                activeBlueprint[resultName] = blueprintDuplicates[resultName][blueprintIndex + 1];
                            }

                            lastBlueprintSelected = activeBlueprint[resultName];
                            __instance.OnCategoryChanged(__instance.m_CategoryNavigation.m_CurrentIndex);
                        }
                    }
                }
                else if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.settings.leftKey))
                {
                    if (__instance.SelectedBPI.m_CraftedResultDecoration != null)
                    {
                        string resultName = __instance.SelectedBPI.m_CraftedResultDecoration.name;
                        string blueprintName = __instance.SelectedBPI.name;
                        int blueprintIndex = 0;

                        if (activeBlueprint.ContainsKey(resultName))
                        {
                            int blueprintCount = blueprintDuplicates[resultName].Count;

                            for (int i = 0; i < blueprintCount; i++)
                            {
                                if (blueprintDuplicates[resultName][i] == blueprintName) blueprintIndex = i;
                            }

                            if (blueprintIndex == 0)
                            {
                                activeBlueprint[resultName] = blueprintDuplicates[resultName][blueprintCount - 1];
                            }
                            else
                            {
                                activeBlueprint[resultName] = blueprintDuplicates[resultName][blueprintIndex - 1];
                            }

                            lastBlueprintSelected = activeBlueprint[resultName];
                            __instance.OnCategoryChanged(__instance.m_CategoryNavigation.m_CurrentIndex);
                        }
                    }
                    else if (__instance.SelectedBPI.m_CraftedResultGear != null)
                    {
                        string resultName = __instance.SelectedBPI.m_CraftedResultGear.name;
                        string blueprintName = __instance.SelectedBPI.name;
                        int blueprintIndex = 0;

                        if (activeBlueprint.ContainsKey(resultName))
                        {
                            int blueprintCount = blueprintDuplicates[resultName].Count;

                            for (int i = 0; i < blueprintCount; i++)
                            {
                                if (blueprintDuplicates[resultName][i] == blueprintName) blueprintIndex = i;
                            }

                            if (blueprintIndex == 0)
                            {
                                activeBlueprint[resultName] = blueprintDuplicates[resultName][blueprintCount - 1];
                            }
                            else
                            {
                                activeBlueprint[resultName] = blueprintDuplicates[resultName][blueprintIndex - 1];
                            }

                            lastBlueprintSelected = activeBlueprint[resultName];
                            __instance.OnCategoryChanged(__instance.m_CategoryNavigation.m_CurrentIndex);

                        }
                    }
                }
            }
        }

        // This patch will allow to keep the blueprint selected
        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.OnCategoryChanged))]
        internal static class Panel_Crafting_OnCategoryChanged
        {
            private static void Postfix(Panel_Crafting __instance)
            {
                if (scrollValue != null)
                {
                    __instance.m_ScrollBehaviour.m_ScrollBar.scrollValue = (float)scrollValue;
                    scrollValue = null;
                }

                if (lastBlueprintSelected != null)
                {
                    int i = 0;
                    foreach (BlueprintData bpi in __instance.m_FilteredBlueprints)
                    {
                        if (bpi.name == lastBlueprintSelected)
                        {
                            __instance.m_ScrollBehaviour.SetSelectedIndex(i, true, true);
                            lastBlueprintSelected = null;
                            return;
                        }
                        i = i + 1;
                    }
                    lastBlueprintSelected = null;
                }
            }
        }

        // Remove 'FILTER' Label in Panel_Crafting
        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.Enable), new Type[] { typeof(bool) , typeof(bool) })]
        internal static class Panel_Crafting_Enable
        {
            private static void Postfix(Panel_Crafting __instance, bool enable)
            {
                __instance.m_FilterLabel.color = new Color(0f, 0f, 0f, 0f);
            }
        }

        // This allow to update the name displayed
        [HarmonyPatch(typeof(BlueprintData), nameof(BlueprintData.GetDisplayedNameWithCount))]
        internal static class BlueprintData_GetDisplayedNameWithCount
        {
            private static void Postfix(BlueprintData __instance, ref string __result)
            {

                int indexParenthese = __result.IndexOf('(');
                if (indexParenthese != -1)
                {
                    __result = __result.Substring(0, indexParenthese - 1);
                }

                // SKIP 
                if (Main.vanillaDisplay) return;

                string gearName = __instance.m_CraftedResultGear.name;

                int rank = 0;
                int i = 0;

                if (__instance.m_CraftedResultDecoration != null)
                {
                    string decoName = __instance.m_CraftedResultDecoration.name;
                    if (blueprintDuplicates.ContainsKey(decoName))
                    {
                        foreach (string blueprint in blueprintDuplicates[decoName])
                        {
                            i += 1;
                            if (__instance.name == blueprint)
                            {
                                rank = i;
                            }
                        }
                        __result += $" [{rank}/{i}]";
                    }
                }
                else
                {
                    if (blueprintDuplicates.ContainsKey(gearName))
                    {
                        foreach (string blueprint in blueprintDuplicates[gearName])
                        {
                            i += 1;
                            if (__instance.name == blueprint)
                            {
                                rank = i;
                            }
                        }
                        __result += $" [{rank}/{i}]";
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintDisplayItem), nameof(BlueprintDisplayItem.OnItemClick))]
        internal static class BlueprintDisplayItem_OnItemClick
        {
            private static void Postfix(BlueprintDisplayItem __instance)
            {
                if (Input.GetKey(Settings.settings.HoldKey))
                {
                    if (__instance.m_BlueprintData != null)
                    {
                        string blueprintName = __instance.m_BlueprintData.name;
                        Panel_Crafting panel = InterfaceManager.GetPanel<Panel_Crafting>();
                        scrollValue = panel.m_ScrollBehaviour.m_ScrollBar.scrollValue;

                        if (Main.blueprintsRemoved.Contains(blueprintName))
                        {
                            Main.blueprintsRemoved.Remove(blueprintName);
                            if (__instance.m_BlueprintData.m_CraftedResultDecoration != null)
                            {
                                string resultName = __instance.m_BlueprintData.m_CraftedResultDecoration.name;
                                if (blueprintDuplicates.ContainsKey(resultName))
                                {
                                    if (blueprintDuplicates[resultName] == null || blueprintDuplicates[resultName].Count == 0)
                                    {
                                        blueprintDuplicates[resultName] = new Il2CppSystem.Collections.Generic.List<string>();
                                        blueprintDuplicates[resultName].Add(blueprintName);
                                    }
                                    else
                                    {
                                        blueprintDuplicates[resultName].Add(blueprintName);
                                    }
                                }
                            }
                            else if (__instance.m_BlueprintData.m_CraftedResultGear != null)
                            {
                                string resultName = __instance.m_BlueprintData.m_CraftedResultGear.name;
                                if (blueprintDuplicates.ContainsKey(resultName))
                                {
                                    if (blueprintDuplicates[resultName] == null || blueprintDuplicates[resultName].Count == 0)
                                    {
                                        blueprintDuplicates[resultName] = new Il2CppSystem.Collections.Generic.List<string>();
                                        blueprintDuplicates[resultName].Add(blueprintName);
                                        activeBlueprint[resultName] = blueprintDuplicates[resultName][0];
                                    }
                                    else
                                    {
                                        blueprintDuplicates[resultName].Add(blueprintName);
                                        activeBlueprint[resultName] = blueprintDuplicates[resultName][0];
                                    }
                                }
                            }
                        }
                        else
                        {
                            Main.blueprintsRemoved.Add(blueprintName);
                            if (__instance.m_BlueprintData.m_CraftedResultDecoration != null)
                            {
                                string resultName = __instance.m_BlueprintData.m_CraftedResultDecoration.name;
                                if (blueprintDuplicates.ContainsKey(resultName))
                                {
                                    blueprintDuplicates[resultName].Remove(blueprintName);
                                    if (blueprintDuplicates[resultName] == null || blueprintDuplicates[resultName].Count == 0)
                                    {
                                        activeBlueprint[resultName] = "";
                                    }
                                    else
                                    {
                                        activeBlueprint[resultName] = blueprintDuplicates[resultName][0];
                                    }
                                }
                            }
                            else if (__instance.m_BlueprintData.m_CraftedResultGear != null)
                            {
                                string resultName = __instance.m_BlueprintData.m_CraftedResultGear.name;
                                if (blueprintDuplicates.ContainsKey(resultName))
                                {
                                    blueprintDuplicates[resultName].Remove(blueprintName);
                                    if (blueprintDuplicates[resultName] == null || blueprintDuplicates[resultName].Count == 0)
                                    {
                                        activeBlueprint[resultName] = "";
                                    }
                                    else
                                    {
                                        activeBlueprint[resultName] = blueprintDuplicates[resultName][0];
                                    }
                                }
                            }
                        }
                        panel.OnCategoryChanged(panel.m_CategoryNavigation.m_CurrentIndex);

                        Main.SaveListToJson(Main.blueprintsRemoved);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        // --------------------------  COOKING PATCHES  ----------------------------- ///
        /////////////////////////////////////////////////////////////////////////////////

        [HarmonyPatch(typeof(RecipeBook), nameof(RecipeBook.OnRecipesLoaded))]
        internal static class RecipeBook_OnRecipesLoaded
        {
            private static void Postfix(RecipeBook __instance)
            {
                // SKIP 
                if (Main.vanillaDisplay) return;

                recipeDuplicates.Clear();
                activeRecipe.Clear();

                foreach (RecipeData recipe in __instance.AllRecipes)
                {
                    string itemName = recipe.m_DishBlueprint.m_CraftedResultGear.name;

                    if (itemName != null)
                    {
                        if (recipeDuplicates.ContainsKey(itemName))
                        {
                            // If the name is already in the dictionary, it's a duplicate, add the blueprint name
                            recipeDuplicates[itemName].Add(recipe.name);
                        }
                        else
                        {
                            // Otherwise, create a new dictionary entry with an empty list and add the blueprint name
                            recipeDuplicates[itemName] = new Il2CppSystem.Collections.Generic.List<string>();
                            recipeDuplicates[itemName].Add(recipe.name);
                        }
                    }
                }

                // Remove entries from the dictionary that have only one recipe name
                var keysToRemove = new Il2CppSystem.Collections.Generic.List<string>(); // List to keep track of keys to remove

                foreach (var pair in recipeDuplicates)
                {
                    if (pair.Value.Count == 1)
                    {
                        keysToRemove.Add(pair.Key); // Mark the key for removal if it has only one recipe name
                    }
                }

                // Now remove the marked keys from the dictionary
                foreach (var key in keysToRemove)
                {
                    recipeDuplicates.Remove(key);
                }

                var keysToUpdate = new Il2CppSystem.Collections.Generic.List<string>();

                foreach (var recipe in Main.blueprintsRemoved)
                {

                    foreach (var result in recipeDuplicates)
                    {
                        if (result.Value != null && result.Value.Contains(recipe))
                        {
                            result.Value.Remove(recipe);
                            if (result.Value == null || result.Value.Count == 0)
                            {
                                keysToUpdate.Add(result.Key);
                            }
                        }
                    }
                }

                foreach (var key in keysToUpdate)
                {
                    recipeDuplicates[key] = new Il2CppSystem.Collections.Generic.List<string>();
                }

                // Log the final dictionary content for debugging
                foreach (var pair in recipeDuplicates)
                {
                    //populate activeRecipe dictionary with the first recipes for each item
                    activeRecipe[pair.Key] = pair.Value != null && pair.Value.Count > 0 ? pair.Value[0] : "";

                    /// LOG PART ///
                    /*Il2CppSystem.Collections.Generic.List<string> recipeNamesList = pair.Value;
                    string recipeNames = string.Empty;

                    for (int i = 0; i < recipeNamesList.Count; i++)
                    {

                        recipeNames += recipeNamesList[i];

                        if (i < recipeNamesList.Count - 1)
                        {
                            recipeNames += ", ";
                        }
                    }
                    MelonLogger.Msg($"Item: {pair.Key}, recipes: {recipeNames}");*/
                }
            }
        }

        [HarmonyPatch(typeof(CookingToolPanelFilterButton), nameof(CookingToolPanelFilterButton.IsGearItemInFilter))]
        internal static class CookingToolPanelFilterButton_IsGearItemInFilter
        {
            private static void Postfix(CookingToolPanelFilterButton __instance, ref bool __result)
            {
                if (__result)
                {
                    // SKIP 
                    if (Main.vanillaDisplay) return;

                    cookableItemDuplicates.Clear();
                    activeCookableItem.Clear();

                    Il2CppSystem.Collections.Generic.List<GearItemObject> GearItems = GameManager.GetInventoryComponent().m_Items;

                    foreach (GearItemObject gio in GearItems)
                    {
                        if (gio.m_GearItem != null)
                        {
                            if (gio.m_GearItem.m_Cookable != null)
                            {
                                string itemName = gio.m_GearItem.name;
                                if (cookableItemDuplicates.ContainsKey(itemName))
                                {
                                    cookableItemDuplicates[itemName].Add(gio.m_GearItem.GetInstanceID());
                                }
                                else
                                {
                                    cookableItemDuplicates[itemName] = new Il2CppSystem.Collections.Generic.List<int>();
                                    cookableItemDuplicates[itemName].Add(gio.m_GearItem.GetInstanceID());
                                }
                                //MelonLogger.Msg($"{itemName}----{gio.m_GearItem.GetInstanceID()}");
                            }
                        }
                    }

                    var keysToRemove = new Il2CppSystem.Collections.Generic.List<string>(); // List to keep track of keys to remove

                    foreach (var pair in cookableItemDuplicates)
                    {
                        if (pair.Value.Count == 1)
                        {
                            keysToRemove.Add(pair.Key); // Mark the key for removal if it has only one recipe name
                        }
                    }

                    // Now remove the marked keys from the dictionary
                    foreach (var key in keysToRemove)
                    {
                        cookableItemDuplicates.Remove(key);
                    }

                    // Log the final dictionary content for debugging
                    foreach (var pair in cookableItemDuplicates)
                    {
                        //populate activeRecipe dictionary with the first recipes for each item
                        activeCookableItem[pair.Key] = pair.Value[0];
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CookingToolPanelFilterButton), nameof(CookingToolPanelFilterButton.PassesFilter))]
        internal static class CookingToolPanelFilterButton_PassesFilter
        {
            private static void Postfix(CookingToolPanelFilterButton __instance, CookableItem cookableItem, ref bool __result)
            {
                // SKIP 
                if (Main.vanillaDisplay) return;

                if (__result)
                {
                    string resultName = cookableItem.m_GearItem.name;
                    //MelonLogger.Msg($"{resultName}----{cookableItem.m_GearItem.GetInstanceID()}");

                    if (cookableItem.m_Recipe != null)
                    {
                        string recipeName = cookableItem.m_Recipe.name;
                        if (recipeDuplicates.ContainsKey(resultName))
                        {
                            if (activeRecipe[resultName] == recipeName)
                            {
                                __result = true;
                            }
                            else
                            {
                                __result = false;
                            }
                        }
                        else if (Main.blueprintsRemoved.Contains(recipeName))
                        {
                            __result = false;
                        }
                    }
                    else
                    {
                        if (Main.blueprintsRemoved.Contains(resultName))
                        {
                            __result = false;
                        }
                        else if (cookableItemDuplicates.ContainsKey(resultName))
                        {
                            if (activeCookableItem[resultName] == cookableItem.m_GearItem.GetInstanceID())
                            {
                                __result = true;
                            }
                            else
                            {
                                __result = false;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Cooking), nameof(Panel_Cooking.Update))]
        internal static class Panel_Cooking_Update
        {
            private static void Postfix(Panel_Cooking __instance)
            {
                // ENABLE/DISABLE vanilla view (and the whole mod)
                if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.settings.viewKey))
                {
                    Main.vanillaDisplay = !Main.vanillaDisplay;
                    __instance.RefreshFoodList();
                }

                // SKIP 
                if (Main.vanillaDisplay) return;

                if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.settings.rightKey))
                {
                    if (__instance.GetSelectedCookableItem().m_Recipe != null)
                    {
                        string resultName = __instance.GetSelectedCookableItem().m_GearItem.name;
                        string recipeName = __instance.GetSelectedCookableItem().m_Recipe.name;
                        int recipeIndex = 0;

                        if (activeRecipe.ContainsKey(resultName))
                        {
                            int recipeCount = recipeDuplicates[resultName].Count;

                            for (int i = 0; i < recipeCount; i++)
                            {
                                if (recipeDuplicates[resultName][i] == recipeName) recipeIndex = i;
                            }

                            if (recipeIndex == recipeCount - 1)
                            {
                                activeRecipe[resultName] = recipeDuplicates[resultName][0];
                            }
                            else
                            {
                                activeRecipe[resultName] = recipeDuplicates[resultName][recipeIndex + 1];
                            }

                            lastRecipeSelected = activeRecipe[resultName];
                            __instance.RefreshFoodList();
                        }
                    }
                    else if (__instance.GetSelectedCookableItem().m_GearItem != null)
                    {
                        string resultName = __instance.GetSelectedCookableItem().m_GearItem.name;
                        int gearItemID = __instance.GetSelectedCookableItem().m_GearItem.GetInstanceID();
                        int cookableItemIndex = 0;

                        if (activeCookableItem.ContainsKey(resultName))
                        {
                            int cookableItemCount = cookableItemDuplicates[resultName].Count;

                            for (int i = 0; i < cookableItemCount; i++)
                            {
                                if (cookableItemDuplicates[resultName][i] == gearItemID) cookableItemIndex = i;
                            }

                            if (cookableItemIndex == cookableItemCount - 1)
                            {
                                activeCookableItem[resultName] = cookableItemDuplicates[resultName][0];
                            }
                            else
                            {
                                activeCookableItem[resultName] = cookableItemDuplicates[resultName][cookableItemIndex + 1];
                            }

                            lastCookableItemSelected = activeCookableItem[resultName];
                            __instance.RefreshFoodList();
                        }
                    }
                }
                else if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.settings.leftKey))
                {
                    if (__instance.GetSelectedCookableItem().m_Recipe != null)
                    {
                        string resultName = __instance.GetSelectedCookableItem().m_GearItem.name;
                        string recipeName = __instance.GetSelectedCookableItem().m_Recipe.name;
                        int recipeIndex = 0;

                        if (activeRecipe.ContainsKey(resultName))
                        {
                            int recipeCount = recipeDuplicates[resultName].Count;

                            for (int i = 0; i < recipeCount; i++)
                            {
                                if (recipeDuplicates[resultName][i] == recipeName) recipeIndex = i;
                            }

                            if (recipeIndex == 0)
                            {
                                activeRecipe[resultName] = recipeDuplicates[resultName][recipeCount - 1];
                            }
                            else
                            {
                                activeRecipe[resultName] = recipeDuplicates[resultName][recipeIndex - 1];
                            }

                            lastRecipeSelected = activeRecipe[resultName];
                            __instance.RefreshFoodList();
                        }
                    }
                    else if (__instance.GetSelectedCookableItem().m_GearItem != null)
                    {
                        string resultName = __instance.GetSelectedCookableItem().m_GearItem.name;
                        int gearItemID = __instance.GetSelectedCookableItem().m_GearItem.GetInstanceID();
                        int cookableItemIndex = 0;

                        if (activeCookableItem.ContainsKey(resultName))
                        {
                            int cookableItemCount = cookableItemDuplicates[resultName].Count;

                            for (int i = 0; i < cookableItemCount; i++)
                            {
                                if (cookableItemDuplicates[resultName][i] == gearItemID) cookableItemIndex = i;
                            }

                            if (cookableItemIndex == 0)
                            {
                                activeCookableItem[resultName] = cookableItemDuplicates[resultName][cookableItemCount - 1];
                            }
                            else
                            {
                                activeCookableItem[resultName] = cookableItemDuplicates[resultName][cookableItemIndex - 1];
                            }

                            lastCookableItemSelected = activeCookableItem[resultName];
                            __instance.RefreshFoodList();
                        }
                    }
                }
            }
        }

        // This patch will allow to keep the blueprint selected wether or not they are craftable
        [HarmonyPatch(typeof(Panel_Cooking), nameof(Panel_Cooking.RefreshFoodList))]
        internal static class Panel_Cooking_RefreshFoodList
        {
            private static void Postfix(Panel_Cooking __instance)
            {

                if (scrollValue != null)
                {
                    __instance.m_ScrollBehaviour.m_ScrollBar.scrollValue = (float)scrollValue;
                    scrollValue = null;
                }

                if (lastRecipeSelected != null)
                {
                    int i = 0;
                    foreach (CookableItem cookableItem in __instance.m_FoodList)
                    {
                        if (cookableItem.m_Recipe != null)
                        {
                            if (cookableItem.m_Recipe.name == lastRecipeSelected)
                            {
                                __instance.m_ScrollBehaviour.SetSelectedIndex(i, true, true);
                                lastRecipeSelected = null;
                                return;
                            }
                        }
                        i = i + 1;
                    }
                    lastRecipeSelected = null;
                }
                else if (lastCookableItemSelected != null)
                {
                    int i = 0;
                    foreach (CookableItem cookableItem in __instance.m_FoodList)
                    {
                        if (cookableItem.m_GearItem.GetInstanceID() == lastCookableItemSelected)
                        {
                            __instance.m_ScrollBehaviour.SetSelectedIndex(i, true, true);
                            lastCookableItemSelected = null;
                            return;
                        }
                        i = i + 1;
                    }
                    lastCookableItemSelected = null;
                }
            }
        }

        // This allow to update the name displayed
        [HarmonyPatch(typeof(CookableItem), nameof(CookableItem.GetDisplayName))]
        internal static class CookableItem_GetDisplayName
        {
            private static void Postfix(CookableItem __instance, ref string __result)
            {

                // SKIP 
                if (Main.vanillaDisplay) return;

                string itemName = __instance.m_GearItem.name;
                int rank = 0;
                int i = 0;

                if (__instance.m_Recipe != null)
                {
                    if (recipeDuplicates.ContainsKey(itemName))
                    {
                        foreach (string recipe in recipeDuplicates[itemName])
                        {
                            i += 1;
                            if (__instance.m_Recipe.name == recipe)
                            {
                                rank = i;
                            }
                        }
                        __result += $" [{rank}/{i}]";
                    }
                }
                else
                {
                    if (cookableItemDuplicates.ContainsKey(itemName))
                    {
                        foreach (int cookableItemID in cookableItemDuplicates[itemName])
                        {
                            i += 1;
                            if (__instance.m_GearItem.GetInstanceID() == cookableItemID)
                            {
                                rank = i;
                            }
                        }
                        __result += $" [{rank}/{i}]";
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ScrollBehaviourItem), nameof(ScrollBehaviourItem.OnClick))]
        internal static class ScrollBehaviourItem_OnItemClick
        {
            private static void Postfix(ScrollBehaviourItem __instance)
            {
                if (GameManager.IsBootSceneActive()) return;
                if (GameManager.IsEmptySceneActive()) return;
                if (GameManager.IsMainMenuActive()) return;
                if (__instance == null) return;
                if (__instance.gameObject == null) return;

                // If LeftAlt key is pressed and Panel_Cooking is active
                if (Input.GetKey(Settings.settings.HoldKey))
                {
                    if (InterfaceManager.TryGetPanel<Panel_Cooking>(out Panel_Cooking panel))
                    {
                        // Try to get CookableListItem component
                        if (__instance.gameObject.TryGetComponent<CookableListItem>(out CookableListItem cookableListItem))
                        {
                            if (cookableListItem.m_Cookable != null)
                            {
                                scrollValue = panel.m_ScrollBehaviour.m_ScrollBar.scrollValue;

                                string cookable = cookableListItem.m_Cookable.m_Recipe != null ? cookableListItem.m_Cookable.m_Recipe.name : cookableListItem.m_Cookable.m_GearItem.name;

                                if (Main.blueprintsRemoved.Contains(cookable))
                                {
                                    Main.blueprintsRemoved.Remove(cookable);
                                    if (cookableListItem.m_Cookable.m_Recipe != null)
                                    {
                                        string resultName = cookableListItem.m_Cookable.m_GearItem.name;
                                        if (recipeDuplicates.ContainsKey(resultName))
                                        {
                                            if (recipeDuplicates[resultName] == null || recipeDuplicates[resultName].Count == 0)
                                            {
                                                recipeDuplicates[resultName] = new Il2CppSystem.Collections.Generic.List<string>();
                                                recipeDuplicates[resultName].Add(cookable);
                                            }
                                            else
                                            {
                                                recipeDuplicates[resultName].Add(cookable);
                                            }
                                        }
                                    }
                                    else if (cookableListItem.m_Cookable.m_GearItem != null)
                                    {
                                        string resultName = cookableListItem.m_Cookable.m_GearItem.name;
                                    }
                                }
                                else
                                {
                                    Main.blueprintsRemoved.Add(cookable);
                                    if (cookableListItem.m_Cookable.m_Recipe != null)
                                    {
                                        string resultName = cookableListItem.m_Cookable.m_GearItem.name;
                                        if (recipeDuplicates.ContainsKey(resultName))
                                        {
                                            recipeDuplicates[resultName].Remove(cookable);
                                            if (recipeDuplicates[resultName] == null || recipeDuplicates[resultName].Count == 0)
                                            {
                                                activeRecipe[resultName] = "";
                                            }
                                            else
                                            {
                                                activeRecipe[resultName] = recipeDuplicates[resultName][0];
                                            }
                                        }
                                    }
                                    else if (cookableListItem.m_Cookable.m_GearItem != null)
                                    {
                                        string resultName = cookableListItem.m_Cookable.m_GearItem.name;
                                    }
                                }
                                panel.RefreshFoodList();
                                Main.SaveListToJson(Main.blueprintsRemoved);
                            }
                        }
                    }
                }
            }
        }
       
        /////////////////////////////////////////////////////////////////////////////////
        // ---------------------  CRAFTING & COOKING PATCHES  ------------------------ //
        /////////////////////////////////////////////////////////////////////////////////
        
        [HarmonyPatch(typeof(ScrollBehaviour), nameof(ScrollBehaviour.RefreshItems))]
        internal static class ScrollBehaviour_RefreshItems
        {
            private static void Postfix(ScrollBehaviour __instance)
            {
                if (__instance.m_VisibleItems != null)
                {
                    for (int i = 0; i < __instance.m_VisibleItems.Count; i++)
                    {

                        if (__instance.m_VisibleItems[i].TryGetComponent<BlueprintDisplayItem>(out BlueprintDisplayItem blueprint))
                        {
                            var item = blueprint;
                            item.m_Button.SetState(UIButtonColor.State.Normal, true);

                            if (item.m_DisplayName.mText == "")
                            {
                                item.m_Button.mDefaultColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.defaultColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.mStartingColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.hover = new Color(0.431f, 0.513f, 0.486f, 0.078f);
                            }
                            else if (Main.blueprintsRemoved.Contains(item.m_BlueprintData.name))
                            {
                                item.m_Button.mDefaultColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                item.m_Button.defaultColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                item.m_Button.mStartingColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                item.m_Button.hover = new Color(1f, 0.5f, 0.5f, 0.2f);
                            }
                            else
                            {
                                item.m_Button.mDefaultColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.defaultColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.mStartingColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.hover = new Color(0.431f, 0.513f, 0.486f, 0.078f);
                            }
                        }
                        else if (__instance.m_VisibleItems[i].TryGetComponent<CookableListItem>(out CookableListItem cookable))
                        {
                            var item = cookable;
                            item.m_Button.SetState(UIButtonColor.State.Normal, true);

                            if (item.m_Cookable.m_Recipe == null)
                            {
                                if (item.m_Cookable.m_GearItem == null) return;

                                if (Main.blueprintsRemoved.Contains(item.m_Cookable.m_GearItem.name))
                                {
                                    item.m_Button.mDefaultColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                    item.m_Button.defaultColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                    item.m_Button.mStartingColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                    item.m_Button.hover = new Color(1f, 0.5f, 0.5f, 0.2f);
                                }
                                else
                                {
                                    item.m_Button.mDefaultColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                    item.m_Button.defaultColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                    item.m_Button.mStartingColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                    item.m_Button.hover = new Color(0.431f, 0.513f, 0.486f, 0.078f);
                                }
                            }
                            else if (Main.blueprintsRemoved.Contains(item.m_Cookable.m_Recipe.name))
                            {
                                item.m_Button.mDefaultColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                item.m_Button.defaultColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                item.m_Button.mStartingColor = new Color(1f, 0.5f, 0.5f, 0.2f);
                                item.m_Button.hover = new Color(1f, 0.5f, 0.5f, 0.2f);
                            }
                            else
                            {
                                item.m_Button.mDefaultColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.defaultColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.mStartingColor = new Color(0.235f, 0.235f, 0.235f, 0.274f);
                                item.m_Button.hover = new Color(0.431f, 0.513f, 0.486f, 0.078f);
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}
