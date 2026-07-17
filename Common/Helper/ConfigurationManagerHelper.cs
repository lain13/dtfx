using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Internal;
using System.Reflection;
using System.Collections.Generic;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// 外部構成ファイルの読み込みと、実行中の AppSettings のマージを支援します。
    /// </summary>
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

        /// <summary>
        /// 指定されたファイルを実行ファイル形式の構成として開きます。
        /// </summary>
        /// <param name="filename">読み込む構成ファイルのパス。</param>
        /// <returns>読み込まれた構成。</returns>
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
        /// <returns>実行中に更新できる AppSettings コレクション。</returns>
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
