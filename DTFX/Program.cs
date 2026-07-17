using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using IF.Batch.Common.Diagnostics;
using IF.Batch.Common.Configuration;
using IF.Batch.DTFX.Helper;
using IF.Batch.DTFX.Service;
using System.Configuration;
using IF.Batch.Common.Helper;

namespace IF.Batch.DTFX
{
    class Program
    {
        static int Main(string[] args)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            InputArguments arguments = new InputArguments(args);
            if (arguments.Contains("?") || arguments.Contains("help") || !arguments.Contains("appid"))
            {
                PrintHelpMessage();
                return (int)ResultTypeCode.Success;
            }

            MergeAppSettings(arguments);
            ITraceLogger logger = new TraceLogger();

            string appid = ConfigurationManager.AppSettings["appid"];
            string appname = ConfigurationManager.AppSettings["appname"];
            if (appid != appname)
            {
                appname = string.Format("{0}({1})", appid, appname);
            }

            logger.WriteInfo(method, appname + "を開始します。");

            ResultTypeCode result = ResultTypeCode.Success;
            try
            {
                using (DataTransferService service = new DataTransferService(logger))
                {
                    if (!service.EnsureServiceConfigurations())
                    {
                        logger.WriteError(method, "環境設定が正しくありません。");
                        result = ResultTypeCode.Error;
                    }
                    else if (!service.InitService())
                    {
                        logger.WriteError(method, "初期化に失敗しました。");
                        result = ResultTypeCode.Error;
                    }
                    else
                    {
                        service.ExecuteService();
                        result = service.ServiceResult;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.WriteError(method, "アプリケーション実行中予期せぬエラーが発生しました。");
                logger.WriteException(ex);
                result = ResultTypeCode.Error;
            }
            switch (result)
            {
                case ResultTypeCode.Success:
                    logger.WriteInfo(method, appname + "を正常終了しました。");
                    break;
                case ResultTypeCode.Error:
                    logger.WriteInfo(method, appname + "を異常終了しました。");
                    break;
                case ResultTypeCode.Warning:
                    logger.WriteInfo(method, appname + "を警告終了しました。");
                    break;
            }

            return (int)result;
        }

        /// <summary>
        /// プログラムメッセージを出力します。
        /// </summary>
        private static void PrintHelpMessage()
        {
            string appName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            Console.WriteLine(string.Format("{0}[.exe] [-appid <APPID>]", appName));
        }

        /// <summary>
        /// プログラム引数をAppSettingsにマージします。
        /// </summary>
        /// <param name="arguments">プログラム引数</param>
        private static void MergeAppSettings(InputArguments arguments)
        {
            try
            {
                string appdirectory = arguments["appdirectory"];
                if (string.IsNullOrWhiteSpace(appdirectory))
                {
                    appdirectory = ConfigurationManager.AppSettings["appdirectory"];
                }
                if (string.IsNullOrWhiteSpace(appdirectory))
                {
                    appdirectory = FileHelper.GetExecuteDirectory();
                }

                string configFilePath = Path.Combine(appdirectory, arguments["appid"] + ".config");
                if (File.Exists(configFilePath))
                {
                    var config = ConfigurationManagerHelper.OpenMappedExeConfiguration(configFilePath);
                    ConfigurationManagerHelper.MergeAppSettings(config.AppSettings.Settings);
                }
                ConfigurationManagerHelper.MergeAppSettings(arguments.GetPeeledArguments());

                Dictionary<string, string> settings = new Dictionary<string, string>();
                settings.Add("appdirectory", appdirectory);
                if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["appname"]))
                {
                    settings.Add("appname", ConfigurationManager.AppSettings["appid"]);
                }
                ConfigurationManagerHelper.MergeAppSettings(settings);
            }
            catch
            {
                // 起動時の既存動作を維持し、構成エラーの報告はサービス初期化へ委ねます。
            }
        }
    }
}
