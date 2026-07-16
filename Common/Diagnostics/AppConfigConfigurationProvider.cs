/************************************************************************
* ファイル名:	AppConfigConfigurationProvider.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Configuration;
using C = IF.Batch.Common.Configuration.AppConfigConstants;

namespace IF.Batch.Common.Diagnostics
{
    public class AppConfigConfigurationProvider : ITraceLogConfiguration, ITraceEventConfiguration
    {
        /// <summary>
        /// トレースファイルのテンプレート
        /// AppSettings の trace.templatepath をキーとして取得
        /// サンプル値:C:\SfaTraceLog\Web\Audit.Web_%YYYYMMDD%.log
        /// </summary>
        public string TracePathTemplate
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.TraceTemplatePath];
                return s;
            }
        }

        /// <summary>
        /// トレースレベル
        /// AppSettings の trace.level をキーとして取得
        /// サンプル値:Information
        /// </summary>
        public SourceLevels TraceSourceLevels
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.TraceSourceLevel];
                if (string.IsNullOrWhiteSpace(s))
                {
                    return SourceLevels.Information;
                }
                return (SourceLevels)Enum.Parse(typeof(SourceLevels), s);

            }
        }

        /// <summary>
        /// トレースファイルのautoflush設定
        /// 既定値false
        /// </summary>
        public bool AutoFlush
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.TraceAutoFlush];
                if (string.IsNullOrEmpty(s))
                {
                    return false;
                }
                return bool.Parse(s);
            }
        }

        /// <summary>
        /// 予期しないエラー発生時のイベントID
        /// </summary>
        public int ErrorEventId
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.ErrorEventId];
                if (string.IsNullOrEmpty(s))
                {
                    return 1;
                }
                return int.Parse(s);
            }
        }

        /// <summary>
        /// イベントソース
        /// </summary>
        public string EventSource
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.EventSource];
                if (string.IsNullOrEmpty(s))
                {
                    return "EventSource";
                }
                return s;
            }
        }

        /// <summary>
        /// 予期しないエラー発生時のイベントの種別
        /// </summary>
        public EventLogEntryType ErrorEventEntryType
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.ErrorEventEntryType];
                if (string.IsNullOrWhiteSpace(s))
                {
                    return EventLogEntryType.Warning;
                }
                return (EventLogEntryType)Enum.Parse(typeof(EventLogEntryType), s);
            }
        }

        /// <summary>
        /// ログファイルの最大サイズ
        /// </summary>
        public long MaxSize
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.MaxSize];
                long maxsize = 0;
                if (!string.IsNullOrWhiteSpace(s))
                {
                    long.TryParse(s, out maxsize);
                }
                return maxsize;
            }
        }

        /// <summary>
        /// ログファイルのバッファサイズ
        /// </summary>
        public int BufferSize
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.BufferSize];
                int bufferSize = 0;
                if (!string.IsNullOrWhiteSpace(s))
                {
                    int.TryParse(s, out bufferSize);
                }
                return bufferSize;
            }
        }

        /// <summary>
        /// トレースログのエンコーディング
        /// </summary>
        public System.Text.Encoding Encoding
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.Encoding];
                System.Text.Encoding encoding = null;
                if (!string.IsNullOrWhiteSpace(s))
                {
                    try
                    {
                        encoding = System.Text.Encoding.GetEncoding(s);
                    }
                    catch
                    {
                        // Do nothing.
                    }
                }
                return encoding;
            }
        }

        /// <summary>
        /// GZIP圧縮利用可否
        /// 既定値false
        /// </summary>
        public bool UseGzip
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.UseGzip];
                if (string.IsNullOrEmpty(s))
                {
                    return false;
                }
                return bool.Parse(s);
            }
        }

        /// <summary>
        /// 既存のファイルに追加
        /// 既定値false
        /// </summary>
        public bool Append
        {
            get
            {
                string s = ConfigurationManager.AppSettings[C.AppSettings.Trace.Append];
                if (string.IsNullOrEmpty(s))
                {
                    return false;
                }
                return bool.Parse(s);
            }
        }
    }
}
