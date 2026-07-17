/************************************************************************
* ファイル名:	OracleSelectExecutor.cs
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
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Diagnostics;
using System.Reflection;
using IF.Batch.DTFX.Service;
using IF.Batch.Common.Configuration;
using IF.Batch.DTFX.Elements;
using IF.Batch.Common.Diagnostics;
using IF.Batch.DTFX.Helper;
using IF.Batch.Common.Helper;
using System.Data.Common;
using IF.Batch.DTFX.Exceptions;
using Oracle.ManagedDataAccess.Client;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// Oracleデータ取得処理(OracleSelect)
    /// Oracleサーバーからデータを取得してその結果をファイル又は新たな一時テーブルに出力する。
    /// </summary>
    public class OracleSelectExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            Logger.WriteDebug(method, element.Value);
            using (var command = new OracleCommand(element.Value))
            {
                command.Connection = ServiceContext.GetConnection<OracleConnection>(element.DataSource);
                command.Transaction = ServiceContext.GetTransaction<OracleTransaction>(element.DataSource);
                command.CommandTimeout = ServiceContext.SqlCommandTimeout;
                using (OracleDataReader reader = command.ExecuteReader())
                {
                    if (!string.IsNullOrEmpty(element.ToTable))
                    {
                        WriteToTable(reader, element);
                    }
                    else if (!string.IsNullOrEmpty(element.ToFile))
                    {
                        WriteToCSV(reader, element);
                    }
                    else if (!string.IsNullOrEmpty(element.ToVariable))
                    {
                        WriteToVariable(reader, element);
                    }
                    else
                    {
                        throw new AppConfigurationException(XSqlElementConstants.ElementName.OracleSelect, "XML形式が正しくありません。" + rawElement.ToString());
                    }
                }
            }
            if (XSqlElementConstants.AttributeValue.commit.Equals(element.Transaction, StringComparison.OrdinalIgnoreCase))
            {
                ServiceContext.CommitTransaction(element.DataSource);
                Logger.WriteDebug(method, "コミットしました。データソース名:{0}", element.DataSource);
            }
            else if (XSqlElementConstants.AttributeValue.rollback.Equals(element.Transaction, StringComparison.OrdinalIgnoreCase))
            {
                ServiceContext.RollbackTransaction(element.DataSource);
                Logger.WriteDebug(method, "ロールバックしました。データソース名:{0}", element.DataSource);
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからOracleSelectElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>OracleSelectElement</returns>
        public OracleSelectElement CreateElement(XElement rawElement)
        {
            OracleSelectElement obj = new OracleSelectElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.DataSource = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.dataSource);
            obj.Transaction =GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.transaction);
            obj.ToFile = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toFile);
            // 28.7-001-01 ADD START
            obj.HeaderString = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.headerString);
            obj.TrailerString = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.trailerString);
            // 28.7-001-01 ADD END
            obj.ToTable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toTable);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.Value = GetParsedStringValue(rawElement);
            return obj;
        }

        /// <summary>
        /// DBから取得した結果をローカルテーブルに出力します。
        /// </summary>
        /// <param name="reader">OracleDataReader</param>
        /// <param name="element">OracleSelectElement</param>
        private void WriteToTable(OracleDataReader reader, OracleSelectElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            ServiceContext.GetLocalDB().WriteToServer(reader, element.ToTable);
            string physicalTableName = ServiceContext.GetLocalDB().GetLocalDBPhysicalTableName(element.ToTable);
            ServiceContext.SharedVariable.SetValue(element.ToTable, physicalTableName);
            Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.ToTable, typeof(string));
        }

        /// <summary>
        /// DBから取得した結果をCSVファイルに出力します。
        /// </summary>
        /// <param name="reader">OracleDataReader</param>
        /// <param name="element">OracleSelectElement</param>
        private void WriteToCSV(OracleDataReader reader, OracleSelectElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            string path = GetOutputPathFullName(element.ToFile);
            string[] writedFiles = new string[] { };
            long writedLine = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // 21.3-001-01 MOD START
            string[] headerStrings = null;
            // ヘッダ文字列を指定した場合
            if (!string.IsNullOrEmpty(element.HeaderString))
            {
                headerStrings = new string[] { element.HeaderString };
            }
            // ヘッダを出力フラグを設定した場合
            else if (ServiceContext.WriteHeaders)
            {
                headerStrings = GetFieldNames(reader);
            }

            using (ConcurrentCsvWriter writer = new ConcurrentCsvWriter(
                path,
                ServiceContext.Encoding,
                ServiceContext.AlwaysCreateFile,
                ServiceContext.UseGzip,
                ServiceContext.MaxWriteRows,
                headerStrings))
            {
                try
                {
                    writer.Formatter = Formatter;
            // 21.3-001-01 MOD END

                    while (reader.Read())
                    {
                        string[] rows = new string[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.IsDBNull(i))
                            {
                                rows[i] = null;
                            }
                            else
                            {
                                object value = reader.GetValue(i);
                                if (value is byte[])
                                {
                                    rows[i] = System.Convert.ToBase64String((byte[])value);
                                }
                                else
                                {
                                    rows[i] = value.ToString();
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(ServiceContext.NullText))
                        {
                            ReplaceNullText(rows, ServiceContext.NullText);
                        }
                        writer.WriteLine(rows);
                        writedLine++;
                    }

                    // 28.7-001-01 ADD START
                    // トレーラ文字列を指定した場合
                    if (!string.IsNullOrEmpty(element.TrailerString))
                    {
                        writer.WriteLine(new string[] { element.TrailerString });
                    }
                    // 28.7-001-01 ADD END
                    writedFiles = writer.WritedFiles;
                }
                catch
                {
                    // データ取得中エラーが発生した場合、出力したファイルを削除する。
                    writer.RollbackFile = true;
                    throw;
                }
            }
            sw.Stop();
            if (writedFiles.Length > 0)
            {
                Logger.WriteInfo(method, "ファイルを出力しました。出力件数：{0}, 処理時間：{1}", writedLine, sw.Elapsed.ToString(@"hh\:mm\:ss\.ff"));
                foreach (string writedFile in writedFiles)
                {
                    Logger.WriteInfo(method, "出力先：{0}", writedFile);
                }
            }
        }

        /// <summary>
        /// DBから取得した結果を変数に出力します。
        /// </summary>
        /// <param name="reader">OracleDataReader</param>
        /// <param name="element">OracleSelectElement</param>
        private void WriteToVariable(OracleDataReader reader, OracleSelectElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            SynchronizedCollection<Dictionary<string, object>> list = new SynchronizedCollection<Dictionary<string, object>>();

            while (reader.Read())
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();

                string[] rows = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string key = reader.GetName(i);
                    object value = null;

                    if (!reader.IsDBNull(i))
                    {
                        value = reader.GetValue(i);
                    }
                    dict.Add(key, value);
                }
                list.Add(dict);
            }
            ServiceContext.SharedVariable.SetValue(element.ToVariable, list);
            Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}, 要素数:{2}", element.ToVariable, list.GetType(), list.Count);
        }
    }
}
