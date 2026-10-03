using System;
using BepInEx.Configuration;

namespace KKDailyOutfits
{
    // Configuration Manager recognizes this exact type name and public fields.
    internal sealed class ConfigurationManagerAttributes
    {
        public string DispName;
        public string Category;
        public Action<ConfigEntryBase> CustomDrawer;
        public bool? Browsable;
        public int? Order;
    }
}
