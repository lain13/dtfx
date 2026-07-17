using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IF.Batch.Common.Diagnostics;
using IF.Batch.Common.Helper;
using C = IF.Batch.Common.Configuration.AppConfigConstants;

namespace IF.Batch.DTFX.Service
{
    /// <summary>
    /// アプリケーション設定とジョブ定義から DataTransferContext を構成します。
    /// </summary>
    public sealed class DataTransferContextFactory : IDataTransferContextFactory
    {
        public bool TryCreate(out DataTransferContext context)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            context = new DataTransferContext();

            try
            {
                AddApplicationSettings(context);
                AddConnectionStrings(context, ConfigurationManager.ConnectionStrings);

                context.Appid = context.SharedVariable.GetStringValue(C.AppSettings.Appid);
                context.Appname = context.SharedVariable.GetStringValue(C.AppSettings.Appname);
                context.Appdirectory = context.SharedVariable.GetStringValue(C.AppSettings.Appdirectory);

                AddJobConnectionStrings(context);
                if (!TryLoadApplicationDefinition(context, method))
                {
                    return false;
                }

                ApplyCommonSettings(context);
                if (!TryEnsureDirectory(method, context.BackupDirectory, "バックアップディレクトリを作成できません。{0}"))
                {
                    return false;
                }
                if (!TryEnsureDirectory(method, context.ErrorDirectory, "エラーディレクトリを作成できません。{0}"))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.WriteError(method, "バッチ初期化中エラーが発生しました。", ex.Message);
                TraceLog.WriteException(ex);
                return false;
            }
        }

        private static void AddApplicationSettings(DataTransferContext context)
        {
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                context.SharedVariable.SetValue(key, ConfigurationManager.AppSettings[key]);
            }
        }

        private static void AddConnectionStrings(
            DataTransferContext context,
            ConnectionStringSettingsCollection connectionStrings)
        {
            foreach (ConnectionStringSettings settings in connectionStrings)
            {
                context.AddConnectionStringSettings(settings);
            }
        }

        private static void AddJobConnectionStrings(DataTransferContext context)
        {
            string jobConfigPath = Path.Combine(context.Appdirectory, context.Appid + ".config");
            if (!File.Exists(jobConfigPath))
            {
                return;
            }

            Configuration jobConfig = ConfigurationManagerHelper.OpenMappedExeConfiguration(jobConfigPath);
            AddConnectionStrings(context, jobConfig.ConnectionStrings.ConnectionStrings);
        }

        private static bool TryLoadApplicationDefinition(
            DataTransferContext context,
            MethodBase method)
        {
            string xmlFilePath = Path.Combine(context.Appdirectory, context.Appid + ".xml");
            if (!File.Exists(xmlFilePath))
            {
                TraceLog.WriteError(method, "SQL定義ファイルが存在しません。{0}", xmlFilePath);
                return false;
            }

            context.RootElement = XElement.Load(xmlFilePath);
            if (context.RootElement.Name != "Application")
            {
                TraceLog.WriteError(method, "SQL定義ファイルが正しくありません。{0}", xmlFilePath);
                return false;
            }

            return true;
        }

        private static void ApplyCommonSettings(DataTransferContext context)
        {
            context.Filename = context.SharedVariable.GetStringValue(C.AppSettings.Filename);
            context.WithoutBom = context.SharedVariable.GetBooleanValue(C.AppSettings.WithoutBom, false).Value;
            context.Encoding = ResolveEncoding(
                context.SharedVariable.GetStringValue(C.AppSettings.Encoding),
                context.WithoutBom);

            string value = context.SharedVariable.GetStringValue(C.AppSettings.Delimiter);
            context.Delimiter = string.IsNullOrEmpty(value) ? "," : Regex.Unescape(value);

            value = context.SharedVariable.GetStringValue(C.AppSettings.RowDelimiter);
            context.RowDelimiter = string.IsNullOrEmpty(value) ? null : Regex.Unescape(value);

            context.AlwaysFieldsEncloseInQuotes = context.SharedVariable
                .GetBooleanValue(C.AppSettings.AlwaysFieldsEncloseInQuotes, false).Value;
            context.TrimWhiteSpace = context.SharedVariable
                .GetBooleanValue(C.AppSettings.TrimWhiteSpace, false).Value;
            context.UseGzip = context.SharedVariable
                .GetBooleanValue(C.AppSettings.UseGzip, false).Value;

            context.InputDirectory = context.SharedVariable.GetStringValue(C.AppSettings.InputDirectory);
            context.OutputDirectory = context.SharedVariable.GetStringValue(C.AppSettings.OutputDirectory);
            context.BackupDirectory = context.SharedVariable.GetStringValue(C.AppSettings.BackupDirectory);
            context.ErrorDirectory = context.SharedVariable.GetStringValue(C.AppSettings.ErrorDirectory);

            context.SkipReadRows = context.SharedVariable.GetLongValue(C.AppSettings.SkipReadRows, 0).Value;
            long maxReadRows = context.SharedVariable.GetLongValue(C.AppSettings.MaxReadRows, 0).Value;
            context.MaxReadRows = maxReadRows > 0 ? maxReadRows : long.MaxValue;

            long maxWriteRows = context.SharedVariable.GetLongValue(C.AppSettings.MaxWriteRows, 0).Value;
            context.MaxWriteRows = maxWriteRows > 0 ? maxWriteRows : long.MaxValue;

            context.NullText = context.SharedVariable.GetStringValue(C.AppSettings.NullText);
            context.WriteHeaders = context.SharedVariable
                .GetBooleanValue(C.AppSettings.WriteHeaders, false).Value;
            context.AlwaysCreateFile = context.SharedVariable
                .GetBooleanValue(C.AppSettings.AlwaysCreateFile, false).Value;

            int commandTimeout = context.SharedVariable.GetIntValue(C.AppSettings.SqlCommandTimeout, 0).Value;
            context.SqlCommandTimeout = commandTimeout > 0 ? commandTimeout : 600;
        }

        private static Encoding ResolveEncoding(string name, bool withoutBom)
        {
            Encoding encoding = string.IsNullOrWhiteSpace(name)
                ? Encoding.Default
                : Encoding.GetEncoding(name);

            if (withoutBom && new UTF8Encoding(true).Equals(encoding))
            {
                return new UTF8Encoding(false);
            }

            return encoding;
        }

        private static bool TryEnsureDirectory(
            MethodBase method,
            string path,
            string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return true;
            }

            try
            {
                FileHelper.CreateDirectoryIfNotExists(path);
                return true;
            }
            catch (Exception ex)
            {
                TraceLog.WriteError(method, errorMessage, path);
                TraceLog.WriteException(ex);
                return false;
            }
        }
    }
}
