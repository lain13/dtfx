/************************************************************************
* ファイル名:	ForEachExecutor.cs
* 概要: CSVファイル、テーブル、変数からレコードを繰り返し実行するループ処理
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   28.7-001-01     2017/03/08  姜　恵遠    法人CRM-WG対応 STEP2
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
using System.IO;
using IF.Batch.DTFX.Helper;
using System.Collections;
using System.Data;
using IF.Batch.Common.Helper;
using IF.Batch.DTFX.Exceptions;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// ループ処理(ForEach)
    /// CSVファイル又はテーブルのレコード数分繰り返し、下位要素を実行する。
    /// </summary>
    public class ForEachExecutor : ExecutorBase
    {
        /// <summary>
        /// ForEachループを実行します。
        /// データソース(fromFile/fromTable/fromVariable)に応じた繰り返し処理を行い、
        /// エラー時のトランザクション制御も行います。
        /// </summary>
        public override ResultTypeCode Execute(XElement rawElement)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            var element = CreateElement(rawElement);

            try
            {
                if (!string.IsNullOrEmpty(element.FromFile))
                {
                    result = FromFiles(element);
                }
                else if (!string.IsNullOrEmpty(element.FromTable))
                {
                    result = FromTable(element);
                }
                else if (!string.IsNullOrEmpty(element.FromVariable))
                {
                    result = FromVariable(element);
                }
            }
            catch (AppExitException ex)
            {
                ControlTransactions((ResultTypeCode)ex.Element.Result, element);
                throw;
            }
            catch
            {
                // 実行中例外は発生した場合
                ControlTransactions(ResultTypeCode.Error, element);
                throw;
            }

            return result;
        }

        /// <summary>
        /// XElementからForEachElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>ForEachElement</returns>
        public ForEachElement CreateElement(XElement rawElement)
        {
            ForEachElement obj = new ForEachElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.Var = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.var);
            obj.FromFile = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.fromFile);
            obj.FromTable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.fromTable);
            obj.FromVariable = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.fromVariable);
            obj.Transaction = GetRawStringValue(rawElement, XSqlElementConstants.AttributeName.transaction, XSqlElementConstants.AttributeValue.commit);
            obj.StopOnError = GetBooleanValue(rawElement, XSqlElementConstants.AttributeName.stopOnError, false).Value;
            obj.TransactionOnError = GetRawStringValue(rawElement, XSqlElementConstants.AttributeName.transactionOnError, XSqlElementConstants.AttributeValue.rollback);
            return obj;
        }

        /// <summary>
        /// ローカルテーブルからForEachを実行します。
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private ResultTypeCode FromTable(ForEachElement element)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            ApplicationExecutor executor = new ApplicationExecutor();
            executor.ServiceContext = this.ServiceContext;
            DataTable table = ServiceContext.GetLocalDB().SelectAllTable(element.FromTable);
            if (table == null || table.Rows.Count == 0)
            {
                TraceLog.WriteWarning(method, "ローカルテーブル[{0}]にデータが存在しません。", element.FromTable);
                return ResultTypeCode.Success;
            }
            try
            {
                foreach (DataRow rows in table.Rows)
                {
                    try
                    {
                        Dictionary<string, object> dict = new Dictionary<string, object>();
                        for (int i = 0; i < table.Columns.Count; i++)
                        {
                            object row = rows[i];
                            if (row != null && row is string && ((string)row).Contains('\''))
                            {
                                row = ((string)row).Replace(@"'", @"''");
                            }
                            dict.Add(table.Columns[i].ColumnName, row);
                        }
                        ServiceContext.SharedVariable.SetValue(element.Var, dict);
                        TraceLog.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}, 要素数:{2}", element.Var, dict.GetType(), dict.Count);
                        ResultTypeCode newResult = executor.Execute(element.RawElement);
                        result = MergeResultTypeCode(result, newResult);
                    }
                    catch (AppExitException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result = ResultTypeCode.Error;
                        if (element.StopOnError)
                        {
                            throw;
                        }
                        else
                        {
                            TraceLog.WriteError(method, "アプリケーション実行中予期せぬエラーが発生しました。");
                            TraceLog.WriteException(ex);
                        }
                    }
                }
            }
            finally
            {
                ServiceContext.SharedVariable.RemoveValue(element.Var);
            }
            // トランザクション制御
            ControlTransactions(result, element);
            return result;
        }

        /// <summary>
        /// 共有変数からForEachを実行します。
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private ResultTypeCode FromVariable(ForEachElement element)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            if (!ServiceContext.SharedVariable.ContainsKey(element.FromVariable))
            {
                TraceLog.WriteWarning(method, "[{0}]変数が存在しません。", element.FromVariable);
                return result;
            }
            ApplicationExecutor executor = new ApplicationExecutor();
            executor.ServiceContext = this.ServiceContext;
            object obj = ServiceContext.SharedVariable.GetValue(element.FromVariable);
            if (!(obj is IEnumerable))
            {
                TraceLog.WriteWarning(method, "[{0}]変数はForEachに利用できません。", element.FromVariable);
                return ResultTypeCode.Error;
            }
            IEnumerable enumerable = (IEnumerable)obj;
            try
            {
                foreach (object var in enumerable)
                {
                    ServiceContext.SharedVariable.SetValue(element.Var, var);
                    TraceLog.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.Var, var == null ? null : var.GetType());
                    try
                    {
                        result = MergeResultTypeCode(result, executor.Execute(element.RawElement));
                    }
                    catch (AppExitException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result = ResultTypeCode.Error;
                        if (element.StopOnError)
                        {
                            throw;
                        }
                        else
                        {
                            TraceLog.WriteError(method, "アプリケーション実行中予期せぬエラーが発生しました。");
                            TraceLog.WriteException(ex);
                        }
                    }
                }
            }
            finally
            {
                ServiceContext.SharedVariable.RemoveValue(element.Var);
            }
            // トランザクション制御
            ControlTransactions(result, element);
            return result;
        }

        /// <summary>
        /// ファイルからForEachを実行します。
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private ResultTypeCode FromFiles(ForEachElement element)
        {
            // 28.7-001-01 ADD START
            ResultTypeCode allResult = ResultTypeCode.Success;
            // 28.7-001-01 ADD END
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            FileInfo[] files = GetFiles(element.FromFile);
            if (files.Length == 0)
            {
                TraceLog.WriteInfo(method, "対象ファイルが存在しません。{0}", element.FromFile);
                return ResultTypeCode.Success;
            }
            foreach (FileInfo file in files)
            {
                try
                {
                    result = FromFile(element, file);
                }
                catch (FileNotFoundException)
                {
                    // 他のAPで処理されたファイルなので処理を成功終了する。
                    TraceLog.WriteInfo(method, "{0} が見つかりません。", file.FullName);
                    // 28.7-001-01 MOD START
                    // return ResultTypeCode.Success;
                    return allResult;
                    // 28.7-001-01 MOD END
                }

                // 28.7-001-01 ADD START
                if (allResult == ResultTypeCode.Success && (result == ResultTypeCode.Warning || result == ResultTypeCode.Error))
                {
                    allResult = result;
                }
                else if (allResult == ResultTypeCode.Warning && result == ResultTypeCode.Error)
                {
                    allResult = result;
                }
                // 28.7-001-01 ADD END

                // トランザクション制御
                ControlTransactions(result, element);

                if (result == ResultTypeCode.Error && element.StopOnError)
                {
                    return result;
                }
            }
            // 28.7-001-01 MOD START
            // return result;
            return allResult;
            // 28.7-001-01 MOD END
        }

        /// <summary>
        /// ファイルからForEachを実行します。
        /// </summary>
        /// <param name="element"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        private ResultTypeCode FromFile(ForEachElement element, FileInfo file)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            ApplicationExecutor executor = new ApplicationExecutor();
            executor.ServiceContext = this.ServiceContext;
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

            TraceLog.WriteDebug(method, "[{0}]を読み込みます。", backupedFile);
            bool isGzip = FileHelper.IsGzipFile(backupedFile.FullName);
            using (CsvReader reader = new CsvReader(backupedFile.FullName, ServiceContext.Encoding, isGzip))
            {
                reader.Delimiters = new string[] { ServiceContext.Delimiter };
                reader.TrimWhiteSpace = ServiceContext.TrimWhiteSpace;

                ConcurrentCsvWriter errorWriter = null;
                try
                {
                    if (!string.IsNullOrEmpty(ServiceContext.ErrorDirectory))
                    {
                        errorWriter = new ConcurrentCsvWriter(Path.Combine(ServiceContext.ErrorDirectory, backupedFile.Name + ".error"), ServiceContext.Encoding, false);
                        errorWriter.Formatter = Formatter;
                    }
                    // 読み飛ばし件数
                    int skipReadRows = 0;
                    for (skipReadRows = 0; skipReadRows < ServiceContext.SkipReadRows && !reader.EndOfData; skipReadRows++)
                    {
                        reader.ReadFields();
                    }
                    if (skipReadRows > 0)
                    {
                        TraceLog.WriteInfo(method, "{0}行を読み飛ばしました。", skipReadRows);
                    }
                    // 読み込み件数
                    int readRows = 0;
                    while (!reader.EndOfData && readRows < ServiceContext.MaxReadRows)
                    {
                        readRows++;
                        string[] fields = reader.ReadFields();
                        string[] escapedFields = EscapeSingleQuotes(fields);
                        ServiceContext.SharedVariable.SetValue(element.Var, escapedFields);
                        TraceLog.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}, 要素数:{2}", element.Var, escapedFields.GetType(), escapedFields.Length);
                        try
                        {
                            result = MergeResultTypeCode(result, executor.Execute(element.RawElement));
                        }
                        catch (AppExitException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            result = ResultTypeCode.Error;
                            TraceLog.WriteError(method, "{0}行目にエラーが存在します。", readRows + skipReadRows);
                            if (errorWriter != null)
                            {
                                errorWriter.WriteLine(fields);
                            }
                            if (element.StopOnError)
                            {
                                throw;
                            }
                            else
                            {
                                TraceLog.WriteException(ex);
                            }
                        }
                    }
                    TraceLog.WriteInfo(method, "ファイル名:{0}, 読み込み件数:{1}, 読み飛ばし件数:{2}", backupedFile.Name, readRows, skipReadRows);
                }
                finally
                {
                    ServiceContext.SharedVariable.RemoveValue(element.Var);
                    if (errorWriter != null)
                    {
                        errorWriter.Dispose();
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// トランザクションを制御します。
        /// </summary>
        /// <param name="result"></param>
        /// <param name="element"></param>
        private void ControlTransactions(ResultTypeCode result, ForEachElement element)
        {
            string transaction = result != ResultTypeCode.Error ? element.Transaction : element.TransactionOnError;
            if (XSqlElementConstants.AttributeValue.commit.Equals(transaction, StringComparison.OrdinalIgnoreCase))
            {
                ServiceContext.CommitAllTransactions();
            }
            else if (XSqlElementConstants.AttributeValue.rollback.Equals(transaction, StringComparison.OrdinalIgnoreCase))
            {
                ServiceContext.RollbackAllTransactions();
            }
        }
    }
}
