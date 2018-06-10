using System;
using System.Collections.Generic;

namespace LogentriesCore
{
    public class SettingsLookup
    {
        public delegate string SettingLookupDelegate(string settingKey);

        readonly List<KeyValuePair<string, SettingLookupDelegate>> SettingStores = new List<KeyValuePair<string, SettingLookupDelegate>>();

        public string GetSettingValue(string settingKey, out string settingStoreName)
        {
            foreach (var settings in SettingStores)
            {
                try
                {
                    string settingValue = settings.Value.Invoke(settingKey);
                    if (settingValue != null && !string.IsNullOrEmpty(settingValue.Trim()))
                    {
                        settingStoreName = settings.Key;
                        return settingValue;
                    }
                }
                catch
                {
                    // Setting store not available
                }
            }

            settingStoreName = string.Empty;
            return string.Empty;
        }

        public void RegisterSettingStore(string settingStoreName, SettingLookupDelegate lookup)
        {
            SettingStores.Insert(0, new KeyValuePair<string, SettingLookupDelegate>(settingStoreName, lookup));
        }

        public void ClearSettingStores()
        {
            SettingStores.Clear();
        }
    }
}
