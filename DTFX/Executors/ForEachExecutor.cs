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
        private readonly IExecutorFactory _executorFactory;

        public ForEachExecutor()
            : this(new ExecutorFactory())
        {
        }

        public ForEachExecutor(IExecutorFactory executorFactory)
        {
            if (executorFactory == null)
            {
                throw new ArgumentNullException("executorFactory");
            }

            _executorFactory = executorFactory;
        }

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
        /// <param name="element">テーブル名、反復変数名、子要素を含む設定。</param>
        /// <returns>すべての行の集約結果。</returns>
        private ResultTypeCode FromTable(ForEachElement element)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            var executor = _executorFactory.CreateApplicationExecutor(ServiceContext);
            DataTable table = ServiceContext.GetLocalDB().SelectAllTable(element.FromTable);
            if (table == null || table.Rows.Count == 0)
            {
                Logger.WriteWarning(method, "ローカルテーブル[{0}]にデータが存在しません。", element.FromTable);
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
                        Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}, 要素数:{2}", element.Var, dict.GetType(), dict.Count);
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
                            Logger.WriteError(method, "アプリケーション実行中予期せぬエラーが発生しました。");
                            Logger.WriteException(ex);
                        }
                    }
                }
            }
            finally
            {
                ServiceContext.SharedVariable.RemoveValue(element.Var);
            }
            ControlTransactions(result, element);
            return result;
        }

        /// <summary>
        /// 共有変数からForEachを実行します。
        /// </summary>
        /// <param name="element">共有変数名、反復変数名、子要素を含む設定。</param>
        /// <returns>すべての項目の集約結果。</returns>
        private ResultTypeCode FromVariable(ForEachElement element)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            if (!ServiceContext.SharedVariable.ContainsKey(element.FromVariable))
            {
                Logger.WriteWarning(method, "[{0}]変数が存在しません。", element.FromVariable);
                return result;
            }
            var executor = _executorFactory.CreateApplicationExecutor(ServiceContext);
            object obj = ServiceContext.SharedVariable.GetValue(element.FromVariable);
            if (!(obj is IEnumerable))
            {
                Logger.WriteWarning(method, "[{0}]変数はForEachに利用できません。", element.FromVariable);
                return ResultTypeCode.Error;
            }
            IEnumerable enumerable = (IEnumerable)obj;
            try
            {
                foreach (object var in enumerable)
                {
                    ServiceContext.SharedVariable.SetValue(element.Var, var);
                    Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}", element.Var, var == null ? null : var.GetType());
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
                            Logger.WriteError(method, "アプリケーション実行中予期せぬエラーが発生しました。");
                            Logger.WriteException(ex);
                        }
                    }
                }
            }
            finally
            {
                ServiceContext.SharedVariable.RemoveValue(element.Var);
            }
            ControlTransactions(result, element);
            return result;
        }

        /// <summary>
        /// ファイルからForEachを実行します。
        /// </summary>
        /// <param name="element">ファイル検索条件、反復変数名、子要素を含む設定。</param>
        /// <returns>一致したすべてのファイルの集約結果。</returns>
        private ResultTypeCode FromFiles(ForEachElement element)
        {
            ResultTypeCode allResult = ResultTypeCode.Success;
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
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
                    result = FromFile(element, file);
                }
                catch (FileNotFoundException)
                {
                    // 列挙後に別プロセスが取得したファイルは競合ではなく処理済みとして扱います。
                    Logger.WriteInfo(method, "{0} が見つかりません。", file.FullName);
                    return allResult;
                }
                if (allResult == ResultTypeCode.Success && (result == ResultTypeCode.Warning || result == ResultTypeCode.Error))
                {
                    allResult = result;
                }
                else if (allResult == ResultTypeCode.Warning && result == ResultTypeCode.Error)
                {
                    allResult = result;
                }
                ControlTransactions(result, element);

                if (result == ResultTypeCode.Error && element.StopOnError)
                {
                    return result;
                }
            }
            return allResult;
        }

        /// <summary>
        /// ファイルからForEachを実行します。
        /// </summary>
        /// <param name="element">CSV 読み込みと子要素の実行設定。</param>
        /// <param name="file">処理する CSV または GZIP ファイル。</param>
        /// <returns>ファイル内のすべての行の集約結果。</returns>
        private ResultTypeCode FromFile(ForEachElement element, FileInfo file)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            MethodBase method = MethodInfo.GetCurrentMethod();
            var executor = _executorFactory.CreateApplicationExecutor(ServiceContext);
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

                ConcurrentCsvWriter errorWriter = null;
                try
                {
                    if (!string.IsNullOrEmpty(ServiceContext.ErrorDirectory))
                    {
                        errorWriter = new ConcurrentCsvWriter(Path.Combine(ServiceContext.ErrorDirectory, backupedFile.Name + ".error"), ServiceContext.Encoding, false);
                        errorWriter.Formatter = Formatter;
                    }
                    int skipReadRows = 0;
                    for (skipReadRows = 0; skipReadRows < ServiceContext.SkipReadRows && !reader.EndOfData; skipReadRows++)
                    {
                        reader.ReadFields();
                    }
                    if (skipReadRows > 0)
                    {
                        Logger.WriteInfo(method, "{0}行を読み飛ばしました。", skipReadRows);
                    }
                    int readRows = 0;
                    while (!reader.EndOfData && readRows < ServiceContext.MaxReadRows)
                    {
                        readRows++;
                        string[] fields = reader.ReadFields();
                        string[] escapedFields = EscapeSingleQuotes(fields);
                        ServiceContext.SharedVariable.SetValue(element.Var, escapedFields);
                        Logger.WriteDebug(method, "共有変数にデータを保存しました。名前:{0}, 型:{1}, 要素数:{2}", element.Var, escapedFields.GetType(), escapedFields.Length);
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
                            Logger.WriteError(method, "{0}行目にエラーが存在します。", readRows + skipReadRows);
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
                                Logger.WriteException(ex);
                            }
                        }
                    }
                    Logger.WriteInfo(method, "ファイル名:{0}, 読み込み件数:{1}, 読み飛ばし件数:{2}", backupedFile.Name, readRows, skipReadRows);
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
        /// <param name="result">反復処理の集約結果。</param>
        /// <param name="element">成功時とエラー時のトランザクション設定。</param>
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
