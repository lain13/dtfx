using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Diagnostics;
using System.Reflection;
using IF.Batch.DTFX.Service;
using IF.Batch.Common.Configuration;
using IF.Batch.DTFX.Elements;
using IF.Batch.Common.Diagnostics;
using System.IO;
using IF.Batch.DTFX.Helper;
using System.Collections;
using System.Data;
using IF.Batch.Common.Helper;
using IF.Batch.DTFX.Exceptions;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// CSV読み込み処理(LoadCSV)
    /// CSVファイルを読み込み変数又はローカルテーブルに保存します。
    /// </summary>
    public class LoadCSVExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            var element = CreateElement(rawElement);
            if (!string.IsNullOrEmpty(element.FromFile))
            {
                result = FromFiles(element);
            }
            return result;
        }

        /// <summary>
        /// XElementからLoadCSVElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>LoadCSVElement</returns>
        public LoadCSVElement CreateElement(XElement rawElement)
        {
            LoadCSVElement obj = new LoadCSVElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.FromFile = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.fromFile);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.ToTable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toTable);
            obj.HasHeaders = GetBooleanValue(rawElement, XSqlElementConstants.AttributeName.hasHeaders, false).Value;
            return obj;
        }

        /// <summary>
        /// CSVファイルを読み込み変数又はローカルテーブルに保存します。
        /// </summary>
        /// <param name="element">入力パターンと出力先を含む設定。</param>
        /// <returns>すべての一致ファイルを読み込めた場合は <c>Success</c>。</returns>
        private ResultTypeCode FromFiles(LoadCSVElement element)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            List<string[]> headers = new List<string[]>();
            List<string[]> list = new List<string[]>();
            FileInfo[] files = GetFiles(element.FromFile);
            if (files.Length == 0)
            {
                Logger.WriteInfo(method, "対象ファイルが存在しません。{0}", element.FromFile);
                return ResultTypeCode.Success;
            }
            foreach (FileInfo file in files)
            {
                try
                {
                    result = FromFile(element, file, headers, list);
                    if (result != ResultTypeCode.Success)
                    {
                        return result;
                    }
                }
                catch (FileNotFoundException)
                {
                    // 列挙後に別プロセスが取得したファイルは競合ではなく処理済みとして扱います。
                    Logger.WriteInfo(method, "{0} が見つかりません。", file.FullName);
                    return ResultTypeCode.Success;
                }
            }
            if (!string.IsNullOrEmpty(element.ToTable))
            {
                WriteToTable(headers, list, element);
            }
            else if (!string.IsNullOrEmpty(element.ToVariable))
            {
                ServiceContext.SharedVariable.SetValue(element.ToVariable, list);
                Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}, 要素数:{2}", element.ToVariable, list.GetType(), list.Count);
            }
            return result;
        }

        /// <summary>
        /// CSVファイルを読み込み変数又はローカルテーブルに保存します。
        /// </summary>
        /// <param name="element">CSV 読み込み設定。</param>
        /// <param name="file">処理する CSV または GZIP ファイル。</param>
        /// <param name="headers">ファイルから読み取ったヘッダーの格納先。</param>
        /// <param name="list">データ行の格納先。</param>
        /// <returns>ファイルを読み込めた場合は <c>Success</c>。</returns>
        private ResultTypeCode FromFile(LoadCSVElement element, FileInfo file, List<string[]> headers, List<string[]> list)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            FileInfo backupedFile = null;
            if (!string.IsNullOrEmpty(ServiceContext.BackupDirectory))
            {
                if (!TryBackupFile(file, out backupedFile))
                {
                    return ResultTypeCode.Error;
                }
            }
            else
            {
                backupedFile = file;
            }

            Logger.WriteDebug(method, "[{0}]を読み込みます。", backupedFile);
            bool isGzip = FileHelper.IsGzipFile(backupedFile.FullName);
            using (CsvReader reader = new CsvReader(backupedFile.FullName, ServiceContext.Encoding, isGzip))
            {
                reader.Delimiters = new string[] { ServiceContext.Delimiter };
                reader.TrimWhiteSpace = ServiceContext.TrimWhiteSpace;

                int skipReadRows = 0;
                for (skipReadRows = 0; skipReadRows < ServiceContext.SkipReadRows && !reader.EndOfData; skipReadRows++)
                {
                    reader.ReadFields();
                }
                if (skipReadRows > 0)
                {
                    Logger.WriteInfo(method, "{0}行を読み飛ばしました。", skipReadRows);
                }
                if (element.HasHeaders && !reader.EndOfData)
                {
                    headers.Add(reader.ReadFields());
                }
                int readRows = 0;
                while (!reader.EndOfData && readRows < ServiceContext.MaxReadRows)
                {
                    readRows++;
                    list.Add(reader.ReadFields());
                }
                Logger.WriteInfo(method, "ファイル名:{0}, 読み込み件数:{1}, 読み飛ばし件数:{2}, ヘッダを含む:{3}", backupedFile.Name, readRows, skipReadRows, element.HasHeaders);
            }
            return result;
        }

        /// <summary>
        /// CSVファイルのデータをローカルテーブルに出力します。
        /// </summary>
        /// <param name="headers">CSV から読み取ったヘッダー行。</param>
        /// <param name="list">LocalDB へ書き込むデータ行。</param>
        /// <param name="element">書き込み先の論理テーブル名を含む設定。</param>
        private void WriteToTable(List<string[]> headers, List<string[]> list, LoadCSVElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            if (list.Count == 0 && headers.Count == 0)
            {
                return;
            }
            if (headers.Count > 0)
            {
                list.Insert(0, headers.First());
            }
            ServiceContext.GetLocalDB().WriteToServer(list, element.ToTable, headers.Count > 0);
            string physicalTableName = ServiceContext.GetLocalDB().GetLocalDBPhysicalTableName(element.ToTable);
            ServiceContext.SharedVariable.SetValue(element.ToTable, physicalTableName);
            Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.ToTable, typeof(string));
        }
    }
}
