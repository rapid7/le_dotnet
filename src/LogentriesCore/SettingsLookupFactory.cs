using System;
#if !NETSTANDARD1_3
using System.Configuration;
#endif
#if !NET35 && !NETSTANDARD1_3 && !NETSTANDARD2_0
using Microsoft.Azure;
#endif

namespace LogentriesCore
{
    static class SettingsLookupFactory
    {
        public static SettingsLookup Create()
        {
            SettingsLookup settingsLookup = new SettingsLookup();
            settingsLookup.RegisterSettingStore("Environment Variable", CreateEnvironmentVariableLookup());
#if !NETSTANDARD1_3 && !NETSTANDARD2_0
            settingsLookup.RegisterSettingStore("App Settings", CreateAppSettingsLookup());
#endif
#if !NET35 && !NETSTANDARD1_3 && !NETSTANDARD2_0
            if (Environment.OSVersion.Platform != PlatformID.Unix && Type.GetType("Mono.Runtime") != null)
                settingsLookup.RegisterSettingStore("Cloud Configuration", CreateCloudConfigurationManagerLookup());
#endif
            return settingsLookup;
        }

        static SettingsLookup.SettingLookupDelegate CreateEnvironmentVariableLookup()
        {
            return new SettingsLookup.SettingLookupDelegate((settingKey) => System.Environment.GetEnvironmentVariable(settingKey));
        }

#if !NETSTANDARD1_3 && !NETSTANDARD2_0
        static SettingsLookup.SettingLookupDelegate CreateAppSettingsLookup()
        {
            return new SettingsLookup.SettingLookupDelegate((settingKey) => ConfigurationManager.AppSettings.Get(settingKey));
        }
#endif

#if !NET35 && !NETSTANDARD1_3 && !NETSTANDARD2_0
        static SettingsLookup.SettingLookupDelegate CreateCloudConfigurationManagerLookup()
        {
            return new SettingsLookup.SettingLookupDelegate((settingKey) => CloudConfigurationManager.GetSetting(settingKey));
        }
#endif
    }
}