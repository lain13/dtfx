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

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// 一時DBデータ取得処理(LocalDBSelect)
    /// 一時DBからデータを取得してその結果をファイル又は新たな一時テーブルに出力する。
    /// </summary>
    public class LocalDBSelectExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            Logger.WriteDebug(method, element.Value);

            using (var command = new SqlCommand(element.Value))
            {
                command.Connection = ServiceContext.GetLocalDB().Connection;
                command.CommandTimeout = ServiceContext.SqlCommandTimeout;
                using (SqlDataReader reader = command.ExecuteReader())
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
                        throw new AppConfigurationException(XSqlElementConstants.ElementName.LocalDBSelect, "XML形式が正しくありません。" + rawElement.ToString());
                    }
                }
            }
            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからLocalDBSelectElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>LocalDBSelectElement</returns>
        public LocalDBSelectElement CreateElement(XElement rawElement)
        {
            LocalDBSelectElement obj = new LocalDBSelectElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.ToFile = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toFile);
            obj.HeaderString = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.headerString);
            obj.TrailerString = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.trailerString);
            obj.ToTable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toTable);
            obj.ToVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.toVariable);
            obj.Value = GetParsedStringValue(rawElement);
            return obj;
        }

        /// <summary>
        /// DBから取得した結果をローカルテーブルに出力します。
        /// </summary>
        /// <param name="reader">SqlDataReader</param>
        /// <param name="element">LocalDBSelectElement</param>
        private void WriteToTable(SqlDataReader reader, LocalDBSelectElement element)
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
        /// <param name="reader">SqlDataReader</param>
        /// <param name="element">LocalDBSelectElement</param>
        private void WriteToCSV(SqlDataReader reader, LocalDBSelectElement element)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            string path = GetOutputPathFullName(element.ToFile);
            string[] writedFiles = new string[] { };
            long writedLine = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string[] headerStrings = null;
            if (!string.IsNullOrEmpty(element.HeaderString))
            {
                headerStrings = new string[] { element.HeaderString };
            }
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
                    if (!string.IsNullOrEmpty(element.TrailerString))
                    {
                        writer.WriteLine(new string[] { element.TrailerString });
                    }
                    writedFiles = writer.WritedFiles;
                }
                catch
                {
                    // 読み取りに失敗した不完全な CSV は Dispose 時に削除します。
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
        /// <param name="reader">SqlDataReader</param>
        /// <param name="element">LocalDBSelectElement</param>
        private void WriteToVariable(SqlDataReader reader, LocalDBSelectElement element)
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
