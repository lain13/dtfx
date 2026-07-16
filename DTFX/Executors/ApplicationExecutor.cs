/************************************************************************
* ファイル名:	ApplicationExecutor.cs
* 概要: XML要素を解析して各下位要素の名前と一致するサブ機能を呼び出すメインExecutor
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   24.1-001-01     2024/03/27  姜　恵遠    PostgreSQL Bulk Insert対応
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
using IF.Batch.DTFX.Exceptions;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// 個別実装メイン処理(Application)
    /// XML要素を解析して各下位要素の名前と一致するサブ機能を呼び出す。
    /// </summary>
    public class ApplicationExecutor : ExecutorBase
    {
        /// <summary>
        /// XMLルート要素の子要素を順次解析し、対応するExecutorを生成・実行します。
        /// </summary>
        public override ResultTypeCode Execute(XElement rawElement)
        {
            ResultTypeCode result = ResultTypeCode.Success;
            // XML要素を繰り返し、実行する。
            foreach (XElement element in rawElement.Elements())
            {
                var executor = CreateExecutor(element);
                ResultTypeCode newresult = executor.Execute(element);
                result = MergeResultTypeCode(result, newresult);
            }

            return result;
        }

        /// <summary>
        /// タスク実行クラス作成処理
        /// </summary>
        /// <returns></returns>
        protected virtual ExecutorBase CreateExecutor(XElement element)
        {
            ExecutorBase executor = null;
            // 28.7-001-01 ADD START
            if (element.Name == XSqlElementConstants.ElementName.SqlSelectScalar)
            {
                executor = new SqlSelectScalarExecutor();
            }
            // 28.7-001-01 ADD END
            else if (element.Name == XSqlElementConstants.ElementName.SqlSelect)
            {
                executor = new SqlSelectExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.SqlInsert)
            {
                executor = new SqlInsertExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.SqlUpdate)
            {
                executor = new SqlUpdateExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.SqlDelete)
            {
                executor = new SqlDeleteExecutor();
            }
            // 28.7-001-01 ADD START
            else if (element.Name == XSqlElementConstants.ElementName.OracleSelectScalar)
            {
                executor = new OracleSelectScalarExecutor();
            }
            // 28.7-001-01 ADD END
            else if (element.Name == XSqlElementConstants.ElementName.OracleSelect)
            {
                executor = new OracleSelectExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.OracleInsert)
            {
                executor = new OracleInsertExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.OracleUpdate)
            {
                executor = new OracleUpdateExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.OracleDelete)
            {
                executor = new OracleDeleteExecutor();
            }
            // 28.7-001-01 ADD START
            else if (element.Name == XSqlElementConstants.ElementName.LocalDBSelectScalar)
            {
                executor = new LocalDBSelectScalarExecutor();
            }
            // 28.7-001-01 ADD END
            else if (element.Name == XSqlElementConstants.ElementName.LocalDBSelect)
            {
                executor = new LocalDBSelectExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.LocalDBInsert)
            {
                executor = new LocalDBInsertExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.LocalDBUpdate)
            {
                executor = new LocalDBUpdateExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.LocalDBDelete)
            {
                executor = new LocalDBDeleteExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.If)
            {
                executor = new IfExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.ForEach)
            {
                executor = new ForEachExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.TraceLog)
            {
                executor = new TraceLogExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.AppExit)
            {
                executor = new AppExitExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.LoadCSV)
            {
                executor = new LoadCSVExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.ExecuteCommand)
            {
                executor = new ExecuteCommandExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.SqlServerBulkInsertFromSqlServer)
            {
                executor = new SqlServerBulkInsertFromSqlServerExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.SqlServerBulkInsertFromOracle)
            {
                executor = new SqlServerBulkInsertFromOracleExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.OracleBulkInsertFromOracle)
            {
                executor = new OracleBulkInsertFromOracleExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.OracleBulkInsertFromSqlServer)
            {
                executor = new OracleBulkInsertFromSqlServerExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.PostgreSqlBulkInsertFromSqlServer)
            {
                executor = new PostgreSqlBulkInsertFromSqlServerExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.PostgreSqlBulkInsertFromOracle)
            {
                executor = new PostgreSqlBulkInsertFromOracleExecutor();
            }
            // 21.3-001-01 ADD START
            else if (element.Name == XSqlElementConstants.ElementName.ZipArchive)
            {
                executor = new ZipArchiveExecutor();
            }
            // 21.3-001-01 ADD END
            // 28.7-001-01 ADD START
            else if (element.Name == XSqlElementConstants.ElementName.PostgreSqlSelectScalar)
            {
                executor = new PostgreSqlSelectScalarExecutor();
            }
            // 28.7-001-01 ADD END
            // 21.3-001-01 ADD START
            else if (element.Name == XSqlElementConstants.ElementName.PostgreSqlSelect)
            {
                executor = new PostgreSqlSelectExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.PostgreSqlInsert)
            {
                executor = new PostgreSqlInsertExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.PostgreSqlUpdate)
            {
                executor = new PostgreSqlUpdateExecutor();
            }
            else if (element.Name == XSqlElementConstants.ElementName.PostgreSqlDelete)
            {
                executor = new PostgreSqlDeleteExecutor();
            }
            // 21.3-001-01 ADD END
            else
            {
                throw new AppConfigurationException(XSqlElementConstants.ElementName.Application, "XMLを解析できませんでした。XML要素名=" + element.Name);
            }

            executor.ServiceContext = this.ServiceContext;
            return executor;
        }
    }
}
