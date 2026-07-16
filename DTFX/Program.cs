/************************************************************************
* ファイル名:	Program.cs
* 概要: 汎用データ連携バッチのエントリポイント
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*
*************************************************************************/
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
            // 引数が?又はhelp又はappidがない場合、メッセージを出力して正常終了します。
            if (arguments.Contains("?") || arguments.Contains("help") || !arguments.Contains("appid"))
            {
                PrintHelpMessage();
                return (int)ResultTypeCode.Success;
            }

            MergeAppSettings(arguments);

            // アプリケーションIDとアプリケーション名を取得する
            string appid = ConfigurationManager.AppSettings["appid"];
            string appname = ConfigurationManager.AppSettings["appname"];
            if (appid != appname)
            {
                appname = string.Format("{0}({1})", appid, appname);
            }

            TraceLog.WriteInfo(method, appname + "を開始します。");

            ResultTypeCode result = ResultTypeCode.Success;
            try
            {
                // データ連携サービス生成
                using (DataTransferService service = new DataTransferService())
                {
                    // 環境設定の検証
                    if (!service.EnsureServiceConfigurations())
                    {
                        TraceLog.WriteError(method, "環境設定が正しくありません。");
                        result = ResultTypeCode.Error;
                    }
                    // 初期化の検証
                    else if (!service.InitService())
                    {
                        TraceLog.WriteError(method, "初期化に失敗しました。");
                        result = ResultTypeCode.Error;
                    }
                    else
                    {
                        // 実行
                        service.ExecuteService();
                        result = service.ServiceResult;
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteError(method, "アプリケーション実行中予期せぬエラーが発生しました。");
                TraceLog.WriteException(ex);
                result = ResultTypeCode.Error;
            }
            switch (result)
            {
                case ResultTypeCode.Success:
                    TraceLog.WriteInfo(method, appname + "を正常終了しました。");
                    break;
                case ResultTypeCode.Error:
                    TraceLog.WriteInfo(method, appname + "を異常終了しました。");
                    break;
                case ResultTypeCode.Warning:
                    TraceLog.WriteInfo(method, appname + "を警告終了しました。");
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
                // 何もしない
            }
        }
    }
}
