/************************************************************************
* ファイル名:	DataTransferService.cs
* 概要: XMLベースのデータ連携サービス。環境設定の検証・初期化・実行を管理する
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   25.1-001-02     2013/10/07  姜　恵遠    レコード間の区切り文字(改行文字)追加
*   21.3-001-01     2021/07/05  姜　恵遠    Biz-A Step1.5対応
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.DTFX.Service;
using IF.Batch.DTFX.Helper;
using IF.Batch.Common.Diagnostics;
using System.Reflection;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;
using IF.Batch.DTFX.Elements;
using IF.Batch.DTFX.Executors;
using C = IF.Batch.Common.Configuration.AppConfigConstants;
using System.IO;
using System.Configuration;
using IF.Batch.Common.Configuration;
using IF.Batch.Common.Helper;
using IF.Batch.Common.Service;
using IF.Batch.DTFX.Exceptions;

namespace IF.Batch.DTFX.Service
{
    public class DataTransferService : IService
    {
        private readonly IDataTransferContextFactory _contextFactory;
        private readonly IExecutorFactory _executorFactory;

        /// <summary>
        /// アプリケーション共有コンテキスト
        /// </summary>
        public DataTransferContext ServiceContext
        {
            get;
            private set;
        }

        /// <summary>
        /// アプリケーション実行結果
        /// </summary>
        public ResultTypeCode ServiceResult
        {
            get;
            private set;
        }

        public DataTransferService()
            : this(new DataTransferContextFactory(), new ExecutorFactory())
        {
        }

        public DataTransferService(
            IDataTransferContextFactory contextFactory,
            IExecutorFactory executorFactory)
        {
            if (contextFactory == null)
            {
                throw new ArgumentNullException("contextFactory");
            }
            if (executorFactory == null)
            {
                throw new ArgumentNullException("executorFactory");
            }

            _contextFactory = contextFactory;
            _executorFactory = executorFactory;
        }

        /// <summary>
        /// 環境設定の検証
        /// </summary>
        /// <returns>成功可否</returns>
        public virtual bool EnsureServiceConfigurations()
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            ServiceContext = _contextFactory.Create();
            if (ServiceContext == null)
            {
                throw new InvalidOperationException("The context factory returned null.");
            }

            ServiceContext.Appid = ConfigurationManager.AppSettings[C.AppSettings.Appid];
            string value = null;

            try
            {

                // 共有変数にAppSettingsを設定する。
                foreach (string key in ConfigurationManager.AppSettings.AllKeys)
                {
                    ServiceContext.SharedVariable.SetValue(key, ConfigurationManager.AppSettings[key]);
                }

                // DB接続情報を設定する。
                foreach (ConnectionStringSettings settings in ConfigurationManager.ConnectionStrings)
                {
                    ServiceContext.AddConnectionStringSettings(settings);
                }

                // アプリケーション名
                ServiceContext.Appname = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.Appname);

                // アプリケーションパス
                ServiceContext.Appdirectory = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.Appdirectory);

                // ジョブ固有の接続文字列はアプリケーションの既定値より優先する。
                string jobConfigPath = Path.Combine(ServiceContext.Appdirectory, ServiceContext.Appid + ".config");
                if (File.Exists(jobConfigPath))
                {
                    var jobConfig = ConfigurationManagerHelper.OpenMappedExeConfiguration(jobConfigPath);
                    foreach (ConnectionStringSettings settings in jobConfig.ConnectionStrings.ConnectionStrings)
                    {
                        ServiceContext.AddConnectionStringSettings(settings);
                    }
                }

                string xmlFilePath = Path.Combine(ServiceContext.Appdirectory, ServiceContext.Appid + ".xml");
                if (!File.Exists(xmlFilePath))
                {
                    TraceLog.WriteError(method, string.Format("SQL定義ファイルが存在しません。{0}", xmlFilePath));
                    return false;
                }

                ServiceContext.RootElement = XElement.Load(xmlFilePath);
                if (ServiceContext.RootElement.Name != "Application")
                {
                    TraceLog.WriteError(method, string.Format("SQL定義ファイルが正しくありません。{0}", xmlFilePath));
                    return false;
                }

                // 共通環境設定変数の取得

                // ファイル名
                ServiceContext.Filename = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.Filename);

                // 21.3-001-01 ADD START
                // BOMなし(UTF-8文字コードのみ)
                ServiceContext.WithoutBom = ServiceContext.SharedVariable.GetBooleanValue(C.AppSettings.WithoutBom, false).Value;
                // 21.3-001-01 ADD END

                // 文字コード
                value = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.Encoding);
                // 21.3-001-01 MOD START
                Encoding encoding = string.IsNullOrWhiteSpace(value) ? Encoding.Default : Encoding.GetEncoding(value);

                // 文字コードがUTF-8エンコードでBOMなしの場合
                if (new UTF8Encoding(true).Equals(encoding) && ServiceContext.WithoutBom)
                {
                    // BOMなしのUTF-8エンコードを設定する
                    ServiceContext.Encoding = new UTF8Encoding(false);
                }
                else
                {
                    ServiceContext.Encoding = encoding;
                }
                // 21.3-001-01 MOD END

                // 項目間の区切り文字
                value = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.Delimiter);
                ServiceContext.Delimiter = string.IsNullOrEmpty(value) ? "," : System.Text.RegularExpressions.Regex.Unescape(value);

                // レコード間の区切り文字(改行文字)
                value = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.RowDelimiter);
                ServiceContext.RowDelimiter = string.IsNullOrEmpty(value) ? null : System.Text.RegularExpressions.Regex.Unescape(value);

                // 常にフィールドを引用符で囲む
                ServiceContext.AlwaysFieldsEncloseInQuotes = ServiceContext.SharedVariable.GetBooleanValue(C.AppSettings.AlwaysFieldsEncloseInQuotes, false).Value;

                // 空白をトリムする
                ServiceContext.TrimWhiteSpace = ServiceContext.SharedVariable.GetBooleanValue(C.AppSettings.TrimWhiteSpace, false).Value;

                // GZIP圧縮可否
                ServiceContext.UseGzip = ServiceContext.SharedVariable.GetBooleanValue(C.AppSettings.UseGzip, false).Value;

                // 入力フォルダ
                ServiceContext.InputDirectory = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.InputDirectory);

                // 出力フォルダ
                ServiceContext.OutputDirectory = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.OutputDirectory);

                // バックアップフォルダ
                ServiceContext.BackupDirectory = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.BackupDirectory);

                if (!string.IsNullOrWhiteSpace(ServiceContext.BackupDirectory))
                {
                    try
                    {
                        FileHelper.CreateDirectoryIfNotExists(ServiceContext.BackupDirectory);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteError(method, "バックアップディレクトリを作成できません。{0}", ServiceContext.BackupDirectory);
                        TraceLog.WriteException(ex);
                        return false;
                    }
                }

                // 出力フォルダ
                ServiceContext.ErrorDirectory = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.ErrorDirectory);

                if (!string.IsNullOrWhiteSpace(ServiceContext.ErrorDirectory))
                {
                    try
                    {
                        FileHelper.CreateDirectoryIfNotExists(ServiceContext.ErrorDirectory);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteError(method, "エラーディレクトリを作成できません。{0}", ServiceContext.ErrorDirectory);
                        TraceLog.WriteException(ex);
                        return false;
                    }
                }

                // ファイル読み飛ばし行数
                ServiceContext.SkipReadRows = ServiceContext.SharedVariable.GetLongValue(C.AppSettings.SkipReadRows, 0).Value;

                // 最大読み込みレコード数
                long maxReadRows = ServiceContext.SharedVariable.GetLongValue(C.AppSettings.MaxReadRows, 0).Value;
                ServiceContext.MaxReadRows = maxReadRows > 0 ? maxReadRows : long.MaxValue;

                // 最大出力レコード数
                long longValue = ServiceContext.SharedVariable.GetLongValue(C.AppSettings.MaxWriteRows, 0).Value;
                ServiceContext.MaxWriteRows = longValue > 0 ? longValue : long.MaxValue;

                // NULL置換テキスト
                ServiceContext.NullText = ServiceContext.SharedVariable.GetStringValue(C.AppSettings.NullText);

                // ヘッダ出力可否
                ServiceContext.WriteHeaders = ServiceContext.SharedVariable.GetBooleanValue(C.AppSettings.WriteHeaders, false).Value;

                // 常にファイルを作成する
                ServiceContext.AlwaysCreateFile = ServiceContext.SharedVariable.GetBooleanValue(C.AppSettings.AlwaysCreateFile, false).Value;

                // SQLコマンドのタイムアウト
                int intValue = ServiceContext.SharedVariable.GetIntValue(C.AppSettings.SqlCommandTimeout, 0).Value;
                ServiceContext.SqlCommandTimeout = intValue > 0 ? intValue : 600;
            }
            catch (Exception ex)
            {
                TraceLog.WriteError(method, "バッチ初期化中エラーが発生しました。", ex.Message);
                TraceLog.WriteException(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        /// <returns>成功可否</returns>
        public virtual bool InitService()
        {
            return true;
        }

        /// <summary>
        /// バッチ実行処理
        /// </summary>
        public void ExecuteService()
        {
            MethodBase method = MethodInfo.GetCurrentMethod();

            try
            {
                var executor = _executorFactory.CreateApplicationExecutor(ServiceContext);
                this.ServiceResult = executor.Execute(ServiceContext.RootElement);
            }
            catch (AppExitException ex)
            {
                this.ServiceResult = (ResultTypeCode)ex.Element.Result;
            }
            catch (AppConfigurationException ex)
            {
                this.ServiceResult = ResultTypeCode.Error;
                TraceLog.WriteError(method, ex.Message);
            }
            catch
            {
                this.ServiceResult = ResultTypeCode.Error;
                throw;
            }
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Dispose()
        {
            if (ServiceContext != null)
            {
                bool commit = this.ServiceResult != ResultTypeCode.Error;
                ServiceContext.DisposeTransactions(commit);
                ServiceContext.Dispose();
            }
        }
    }
}
