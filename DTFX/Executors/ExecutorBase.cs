/************************************************************************
* ファイル名:	ExecutorBase.cs
* 概要: 全Executorの基底クラス。XML解析・CSV出力・ファイル操作の共通機能を提供する
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   25.1-001-02     2013/10/07  姜　恵遠    25年度2期
*   21.3-001-01     2021/07/05  姜　恵遠    Biz-A Step1.5対応
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.DTFX.Service;
using System.Xml.Linq;
using IF.Batch.Common.Configuration;
using IF.Batch.DTFX.Helper;
using System.IO;
using System.Reflection;
using IF.Batch.Common.Diagnostics;
using IF.Batch.Common.Helper;
using IF.Batch.Common.Service;
using System.Data.Common;
using IF.Batch.DTFX.Elements;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// ベースクラス
    /// </summary>
    public abstract class ExecutorBase : ITaskExecutor<XElement>,
        IUseServiceContext<DataTransferContext>
    {
        private ITraceLogger _logger = new TraceLogger();

        /// <summary>
        /// この Executor が使用するトレースロガーです。
        /// </summary>
        protected ITraceLogger Logger
        {
            get { return _logger; }
        }

        /// <summary>
        /// サービスコンテキスト
        /// </summary>
        public DataTransferContext ServiceContext { get; set; }

        /// <summary>
        /// ファクトリが生成した Executor に共有ロガーを設定します。
        /// </summary>
        internal void SetLogger(ITraceLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        private ExpressionParser _parser;
        /// <summary>
        /// 表現式を解析
        /// </summary>
        protected ExpressionParser Parser
        {
            get
            {
                if (_parser == null)
                {
                    _parser = new ExpressionParser(ServiceContext.SharedVariable);
                }
                return _parser;
            }
        }

        private CsvFormatter _formatter;
        /// <summary>
        /// CSVの出力フォーマット
        /// </summary>
        protected CsvFormatter Formatter
        {
            get
            {
                if (_formatter == null)
                {
                    _formatter = new CsvFormatter(ServiceContext.Delimiter, ServiceContext.RowDelimiter, ServiceContext.AlwaysFieldsEncloseInQuotes, ServiceContext.TrimWhiteSpace);
                }
                return _formatter;
            }
        }

        public abstract ResultTypeCode Execute(XElement rawElement);

        /// <summary>
        /// 解析された文字列を返却します。
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        protected string GetParsedStringValue(XElement element, string attributeName = null)
        {
            string value = null;
            if (attributeName == null)
            {
                value = element.Value == null ? string.Empty : element.Value.Trim();
            }
            else
            {
                value = GetRawStringValue(element, attributeName);
            }
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return Parser.ParseString(value);
        }

        /// <summary>
        /// 元の文字列を返却します。
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected string GetRawStringValue(XElement element, string attributeName, string defaultValue = null)
        {
            XAttribute attribute = element.Attribute(attributeName);
            if (attribute != null)
            {
                return string.IsNullOrWhiteSpace(attribute.Value) ? defaultValue : attribute.Value;
            }
            return defaultValue;
        }

        /// <summary>
        /// NULL可能なbool値を返却します。
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected bool? GetBooleanValue(XElement element, string attributeName, bool? defaultValue = null)
        {
            string value = GetRawStringValue(element, attributeName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if ("1" == value)
            {
                return true;
            }
            else if ("0" == value)
            {
                return false;
            }
            bool boolValue = false;
            if (bool.TryParse(value, out boolValue))
            {
                return boolValue;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// NULL可能なint値を返却します。
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attributeName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected int? GetIntValue(XElement element, string attributeName, int? defaultValue = null)
        {
            string value = GetRawStringValue(element, attributeName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            int intValue = 0;
            if (int.TryParse(value, out intValue))
            {
                return intValue;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 異常・警告・正常順で返却します。
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        protected ResultTypeCode MergeResultTypeCode(params ResultTypeCode[] codes)
        {
            if (codes.Contains(ResultTypeCode.Error))
            {
                return ResultTypeCode.Error;
            }
            if (codes.Contains(ResultTypeCode.Warning))
            {
                return ResultTypeCode.Warning;
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// NULLを特定の文字列に置換する。
        /// </summary>
        /// <param name="rows">文字列の配列</param>
        /// <param name="nulltext">置換する文字列</param>
        protected void ReplaceNullText(string[] rows, string nulltext)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null)
                {
                    rows[i] = nulltext;
                }
            }
        }

        /// <summary>
        /// 出力ファイルのFullPathを取得します。
        /// 出力ファイルがパス名を指定している場合、出力ファイルを返却します。
        /// 出力ファイルがパス名を指定してない場合、出力フォルダ名＋出力ファイルを返却します。
        /// </summary>
        /// <param name="filepath">出力ファイル</param>
        /// <param name="overwrite">上書き</param>
        /// <returns>出力ファイルのFullPath</returns>
        protected string GetOutputPathFullName(string filepath, bool? overwrite = false)
        {
            if (ServiceContext == null ||
                string.IsNullOrEmpty(ServiceContext.OutputDirectory) ||
                !string.IsNullOrEmpty(Path.GetDirectoryName(filepath)))
            {
                if (overwrite == true)
                {
                    return filepath;
                }
                return FileHelper.NextFileName(filepath);
            }
            else
            {
                var newFilePath = Path.Combine(ServiceContext.OutputDirectory, filepath);
                if (overwrite == true)
                {
                    return newFilePath;
                }
                else
                {
                    return FileHelper.NextFileName(newFilePath);
                }
            }
        }

        /// <summary>
        /// ファイルをバックアップします。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="backupedFile"></param>
        /// <returns></returns>
        protected bool TryBackupFile(FileInfo file, out FileInfo backupedFile)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            backupedFile = null;
            string outFilePath = null;
            for (int i = 0; i < 3; i++)
            {
                if (!System.IO.File.Exists(file.FullName))
                {
                    throw new FileNotFoundException(file.FullName);
                }
                bool result = FileHelper.NextFileMove(file.FullName, ServiceContext.BackupDirectory, null, out outFilePath);
                if (result)
                {
                    break;
                }
                // 2秒待機
                Logger.WriteDebug(method, "ファイル移動失敗 {0} {1} {2}", i, file.FullName, ServiceContext.BackupDirectory);
                System.Threading.Thread.Sleep(2000);
            }
            if (string.IsNullOrEmpty(outFilePath))
            {
                Logger.WriteInfo(method, "ファイルの移動に失敗しました。ファイル名＝{0}, 移動先＝{1}", file.FullName, ServiceContext.BackupDirectory);
                return false;
            }
            else
            {
                backupedFile = new FileInfo(outFilePath);
                Logger.WriteInfo(method, "ファイルを移動しました。ファイル＝{0}, 移動先＝{1}", file.FullName, ServiceContext.BackupDirectory);
                return true;
            }
        }

        /// <summary>
        /// パターンと一致するファイルを返却します。
        /// </summary>
        /// <param name="searchPattern">検索パターン</param>
        /// <returns>ファイルリスト</returns>
        // 21.3-001-01 MOD START
        protected virtual FileInfo[] GetFiles(string searchPattern)
        {
        // 21.3-001-01 MOD END
            MethodBase method = MethodInfo.GetCurrentMethod();
            if (File.Exists(searchPattern))
            {
                return new FileInfo[1] { new FileInfo(searchPattern) };
            }
            string path = null;
            string pattern = null;
            FileHelper.TryExtractPathAndPattern(searchPattern, out path, out pattern);
            if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(ServiceContext.InputDirectory))
            {
                path = ServiceContext.InputDirectory;
            }
            if (!Directory.Exists(path))
            {
                Logger.WriteError(method, "対象ファイルのパスが正しくありません。{0}", searchPattern);
                return new FileInfo[0];
            }

            return FileHelper.FindFiles(path, pattern);
        }

        /// <summary>
        /// 取得したSQLのカラム名を取得します。
        /// </summary>
        /// <param name="reader">SqlDataReader</param>
        /// <returns>カラム名配列</returns>
        protected string[] GetFieldNames(DbDataReader reader)
        {
            int fieldCount = reader.FieldCount;
            string[] fieldNames = new string[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                fieldNames[i] = reader.GetName(i);
            }
            return fieldNames;
        }

        /// <summary>
        /// 文字列の'をエスケープする
        /// </summary>
        /// <param name="fields">文字列の配列</param>
        /// <returns>置換された文字列の配列</returns>
        protected string[] EscapeSingleQuotes(string[] fields)
        {
            string[] escapedFields = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                string field = fields[i];
                if (field != null && field.Contains('\''))
                {
                    escapedFields[i] = field.Replace(@"'", @"''");
                }
                else
                {
                    escapedFields[i] = field;
                }
            }
            return escapedFields;
        }
        
        /// <summary>
        /// ログを出力する。
        /// </summary>
        /// <param name="method">MethodBase</param>
        /// <param name="eventType">情報/警告/エラー/デバック/なし</param>
        /// <param name="message">出力するメッセージ</param>
        protected void WriteTraceLog(MethodBase method, string eventType, string message)
        {
            if (XSqlElementConstants.AttributeValue.error.Equals(eventType, StringComparison.OrdinalIgnoreCase))
            {
                Logger.WriteError(method, message);
            }
            else if (XSqlElementConstants.AttributeValue.information.Equals(eventType, StringComparison.OrdinalIgnoreCase))
            {
                Logger.WriteInfo(method, message);
            }
            else if (XSqlElementConstants.AttributeValue.warning.Equals(eventType, StringComparison.OrdinalIgnoreCase))
            {
                Logger.WriteWarning(method, message);
            }
            else if (XSqlElementConstants.AttributeValue.verbose.Equals(eventType, StringComparison.OrdinalIgnoreCase))
            {
                Logger.WriteDebug(method, message);
            }
            else if (XSqlElementConstants.AttributeValue.off.Equals(eventType, StringComparison.OrdinalIgnoreCase))
            {
                // 何もしない
            }
        }
    }
}
