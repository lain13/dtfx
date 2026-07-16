/************************************************************************
* ファイル名:	ConfigurationManagerHelper.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Internal;
using System.Reflection;
using System.Collections.Generic;

namespace IF.Batch.Common.Helper
{
    public class ConfigurationManagerHelper
    {
        private sealed class ConfigurationManagerProxy : IInternalConfigSystem
        {
            private readonly IInternalConfigSystem baseconfig;
            private NameValueCollection appsettings;
            public ConfigurationManagerProxy(IInternalConfigSystem baseconfig)
            {
                this.baseconfig = baseconfig;
            }

            public object GetSection(string configKey)
            {
                if (configKey == "appSettings" && this.appsettings != null)
                {
                    return this.appsettings;
                }
                object obj = this.baseconfig.GetSection(configKey);
                if (configKey == "appSettings" && obj is NameValueCollection)
                {
                    this.appsettings = new NameValueCollection((NameValueCollection)obj);
                    obj = this.appsettings;
                }
                return obj;
            }

            public void RefreshConfig(string sectionName)
            {
                if (sectionName == "appSettings")
                {
                    this.appsettings = null;
                }
                this.baseconfig.RefreshConfig(sectionName);
            }

            public bool SupportsUserConfig
            {
                get
                {
                    return this.baseconfig.SupportsUserConfig;
                }
            }
        }

        public static System.Configuration.Configuration OpenMappedExeConfiguration(string filename)
        {
            ExeConfigurationFileMap exeFileMap = new ExeConfigurationFileMap { ExeConfigFilename = filename };
            return ConfigurationManager.OpenMappedExeConfiguration(exeFileMap, ConfigurationUserLevel.None);
        }

        /// <summary>
        /// 環境設定のappSettings部分をマージする
        /// </summary>
        /// <param name="settings">KeyValueConfigurationCollection</param>
        public static void MergeAppSettings(KeyValueConfigurationCollection settings)
        {
            NameValueCollection appsettings = GetEditableAppSettings();
            foreach (string key in settings.AllKeys)
            {
                appsettings.Set(key, settings[key].Value);
            }
        }

        /// <summary>
        /// 環境設定のappSettings部分をマージする
        /// </summary>
        /// <param name="settings">settings</param>
        public static void MergeAppSettings(IEnumerable<KeyValuePair<string, string>> settings)
        {
            NameValueCollection appsettings = GetEditableAppSettings();
            foreach (KeyValuePair<string, string> pair in settings)
            {
                appsettings.Set(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// 環境設定を編集可能な状態に変更する。
        /// </summary>
        /// <returns></returns>
        private static NameValueCollection GetEditableAppSettings()
        {
            NameValueCollection appsettings = ConfigurationManager.AppSettings;

            FieldInfo s_configSystem = typeof(ConfigurationManager).GetField("s_configSystem", BindingFlags.Static | BindingFlags.NonPublic);
            IInternalConfigSystem baseconfig = (IInternalConfigSystem)s_configSystem.GetValue(null);
            if (!(baseconfig is ConfigurationManagerProxy))
            {
                s_configSystem.SetValue(null, new ConfigurationManagerProxy(baseconfig));
            }

            return ConfigurationManager.AppSettings;
        }
    }
}