/************************************************************************
* ファイル名:	AppConfigConstants.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   21.3-001-01     2021/07/05  姜　恵遠    Biz-A Step1.5対応
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.Common.Configuration
{
    /// <summary>
    /// 共通クラスが使用するアプリケーション構成ファイルの定数
    /// </summary>
    public static class AppConfigConstants
    {
        #region AppSettingsのキー
        public static class AppSettings
        {
            public static class Trace
            {
                /// <summary>
                /// トレースファイルのパス
                /// </summary>
                public static readonly string TraceTemplatePath = "trace.templatepath";

                /// <summary>
                /// トレースレベル
                /// </summary>
                public static readonly string TraceSourceLevel = "trace.level";

                /// <summary>
                /// トレース自動フラッシュ可否
                /// </summary>
                public static readonly string TraceAutoFlush = "trace.autoflush";

                /// <summary>
                /// 想定外エラー時のイベントID
                /// </summary>
                public static readonly string ErrorEventId = "trace.erroreventid";

                /// <summary>
                /// イベントソース
                /// </summary>
                public static readonly string EventSource = "trace.eventsource";

                /// <summary>
                /// 想定外エラー時のイベントレベル
                /// </summary>
                public static readonly string ErrorEventEntryType = "trace.errorevententrytype";

                /// <summary>
                /// トレースログ最大サイズ
                /// </summary>
                public static readonly string MaxSize = "trace.maxsize";

                /// <summary>
                /// トレースログのエンコード
                /// </summary>
                public static readonly string Encoding = "trace.encoding";

                /// <summary>
                /// トレースログのバッファサイズ
                /// </summary>
                public static readonly string BufferSize = "trace.buffersize";

                /// <summary>
                /// GZIP圧縮利用可否
                /// </summary>
                public static readonly string UseGzip = "trace.usegzip";

                /// <summary>
                /// 既存のファイルに追加
                /// </summary>
                public static readonly string Append = "trace.append";
            }

            /// <summary>
            /// 組織サービスへのURL
            /// </summary>
            public static readonly string OrganizationServiceUrl = "organizationServiceUrl";

            /// <summary>
            /// SQLコマンドのタイムアウト
            /// </summary>
            public static readonly string SqlCommandTimeout = "sqlcommandtimeout";

            /// <summary>
            /// マルチスレッドサービスのワーカースレッド数
            /// </summary>
            public static readonly string WorkerMultiplicity = "workermultiplicity";

            /// <summary>
            /// ファイル名
            /// </summary>
            public static readonly string Filename = "filename";

            /// <summary>
            /// エンコード
            /// </summary>
            public static readonly string Encoding = "encoding";

            /// <summary>
            /// 項目間の区切り文字
            /// </summary>
            public static readonly string Delimiter = "delimiter";

            /// <summary>
            /// レコード間の区切り文字(改行文字)
            /// </summary>
            public static readonly string RowDelimiter = "rowdelimiter";

            /// <summary>
            /// 区切り記号入りファイルに出力する場合に、
            /// 常にフィールドを引用符で囲むかどうかを示します。
            /// </summary>
            public static readonly string AlwaysFieldsEncloseInQuotes = "alwaysfieldsencloseinquotes";

            /// <summary>
            /// フィールド値から前後の空白をトリムするかどうかを示します。
            /// </summary>
            public static readonly string TrimWhiteSpace = "trimwhitespace";

            /// <summary>
            /// ファイル読み飛ばし行数
            /// </summary>
            public static readonly string SkipReadRows = "skipreadrows";

            /// <summary>
            /// 最大読み込み行数
            /// </summary>
            public static readonly string MaxReadRows = "maxreadrows";

            /// <summary>
            /// 最大出力レコード数
            /// </summary>
            public static readonly string MaxWriteRows = "maxwriterows";

            /// <summary>
            /// バックアップディレクトリ
            /// </summary>
            public static readonly string BackupDirectory = "backupdirectory";

            /// <summary>
            /// エラーディレクトリ
            /// </summary>
            public static readonly string ErrorDirectory = "errordirectory";

            /// <summary>
            /// GZIP利用可否
            /// </summary>
            public static readonly string UseGzip = "usegzip";

            /// <summary>
            /// 出力ディレクトリ
            /// </summary>
            public static readonly string OutputDirectory = "outputdirectory";

            /// <summary>
            /// 入力ディレクトリ
            /// </summary>
            public static readonly string InputDirectory = "inputdirectory";

            /// <summary>
            /// NULL置換テキスト
            /// </summary>
            public static readonly string NullText = "nulltext";

            /// <summary>
            /// ヘッダ出力可否
            /// </summary>
            public static readonly string WriteHeaders = "writeheaders";

            /// <summary>
            /// 常にファイルを作成する
            /// </summary>
            public static readonly string AlwaysCreateFile = "alwayscreatefile";

            /// <summary>
            /// アプリケーションID
            /// </summary>
            public static readonly string Appid = "appid";

            /// <summary>
            /// アプリケーション名
            /// </summary>
            public static readonly string Appname = "appname";

            /// <summary>
            /// アプリケーションパス
            /// </summary>
            public static readonly string Appdirectory = "appdirectory";

            // 21.3-001-01 ADD START
            /// <summary>
            /// BOMなし(UTF-8文字コードのみ)
            /// </summary>
            public static readonly string WithoutBom = "withoutbom";
            // 21.3-001-01 ADD END
        }
        #endregion

        #region connectionStringsの名前
        public static class ConnectionStrings
        {
            /// <summary>
            /// CRM組織DBへの接続文字列
            /// </summary>
            public static readonly string CrmDb = "crmdb";

            /// <summary>
            /// CRMカスタムDBの接続文字列
            /// </summary>
            public static readonly string CrmCustomDb = "crmcustomdb";

            /// <summary>
            /// CRM接続文字列
            /// </summary>
            public static readonly string Crm = "crm";
        }
        #endregion
    }
}
