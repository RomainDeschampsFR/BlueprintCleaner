using ModSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BlueprintCleaner
{
    internal class CleanerSettings : JsonModSettings
    {
        [Name("Toggle display button")]
        [Description("Toggles between extended view (Vanilla view) and compact view (modified view).")]
        public KeyCode viewKey = KeyCode.H;

        [Name("Hold button to hide/show blueprints")]
        [Description("Hold this when clicking on a blueprint/recipe (Left mouse button) in order to remove/add them to the list.")]
        public KeyCode HoldKey = KeyCode.LeftAlt;

        [Name("Next blueprint key")]
        [Description("Displays the next blueprint when stacked.")]
        public KeyCode leftKey = KeyCode.RightArrow;

        [Name("Previous blueprint Key")]
        [Description("Displays the previous blueprint when stacked.")]
        public KeyCode rightKey = KeyCode.LeftArrow;
    }
    internal static class Settings
    {
        internal static CleanerSettings settings;

        public static void OnLoad()
        {
            settings = new CleanerSettings();
            settings.AddToModSettings("Blueprint Cleaner");
        }
    }
}
