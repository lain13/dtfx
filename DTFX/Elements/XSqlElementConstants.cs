/************************************************************************
* ファイル名:	XSqlElementConstants.cs
* 概要: 
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

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// XML要素の定数クラス
    /// </summary>
    public static class XSqlElementConstants
    {
        /// <summary>
        /// 要素の名前を定義した定数です。
        /// </summary>
        public static class ElementName
        {
            // 28.7-001-01 ADD START
            public const string SqlSelectScalar = "SqlSelectScalar";
            // 28.7-001-01 ADD END
            public const string SqlSelect = "SqlSelect";
            public const string SqlInsert = "SqlInsert";
            public const string SqlUpdate = "SqlUpdate";
            public const string SqlDelete = "SqlDelete";
            // 28.7-001-01 ADD START
            public const string OracleSelectScalar = "OracleSelectScalar";
            // 28.7-001-01 ADD END
            public const string OracleSelect = "OracleSelect";
            public const string OracleInsert = "OracleInsert";
            public const string OracleUpdate = "OracleUpdate";
            public const string OracleDelete = "OracleDelete";
            // 28.7-001-01 ADD START
            public const string LocalDBSelectScalar = "LocalDBSelectScalar";
            // 28.7-001-01 ADD END
            public const string LocalDBSelect = "LocalDBSelect";
            public const string LocalDBInsert = "LocalDBInsert";
            public const string LocalDBUpdate = "LocalDBUpdate";
            public const string LocalDBDelete = "LocalDBDelete";
            public const string PostgreSqlSelectScalar = "PostgreSqlSelectScalar";
            public const string PostgreSqlSelect = "PostgreSqlSelect";
            public const string PostgreSqlInsert = "PostgreSqlInsert";
            public const string PostgreSqlUpdate = "PostgreSqlUpdate";
            public const string PostgreSqlDelete = "PostgreSqlDelete";

            public const string If = "If";
            public const string ForEach = "ForEach";
            public const string TraceLog = "TraceLog";
            public const string AppExit = "AppExit";
            public const string Application = "Application";
            public const string LoadCSV = "LoadCSV";
            public const string ExecuteCommand = "ExecuteCommand";
            public const string SqlServerBulkInsertFromSqlServer = "SqlServerBulkInsertFromSqlServer";
            public const string SqlServerBulkInsertFromOracle = "SqlServerBulkInsertFromOracle";
            public const string OracleBulkInsertFromOracle = "OracleBulkInsertFromOracle";
            public const string OracleBulkInsertFromSqlServer = "OracleBulkInsertFromSqlServer";
            public const string PostgreSqlBulkInsertFromOracle = "PostgreSqlBulkInsertFromOracle";
            public const string PostgreSqlBulkInsertFromSqlServer = "PostgreSqlBulkInsertFromSqlServer";
            // 21.3-001-01 ADD START
            public const string ZipArchive = "ZipArchive";
            public const string AddFile = "AddFile";
            // 21.3-001-01 ADD END
        }

        /// <summary>
        /// 要素の属性名を定義した定数です。
        /// </summary>
        public static class AttributeName
        {
            public const string id = "id";
            public const string toTable = "toTable";
            public const string toFile = "toFile";
            // 28.7-001-01 ADD START
            public const string headerString = "headerString";
            public const string trailerString = "trailerString";
            // 28.7-001-01 ADD END
            public const string toDataSource = "toDataSource";
            public const string fromFile = "fromFile";
            public const string fromVariable = "fromVariable";
            public const string fromTable = "fromTable";
            public const string fromDataSource = "fromDataSource";
            public const string stopOnError = "stopOnError";
            public const string dataSource = "dataSource";
            public const string test = "test";
            public const string var = "var";
            public const string toVariable = "toVariable";
            public const string eventType = "eventType";
            public const string traceLog = "traceLog";
            public const string result = "result";
            public const string transaction = "transaction";
            public const string transactionOnError = "transactionOnError";
            public const string hasHeaders = "hasHeaders";
            // 21.3-001-01 ADD START
            public const string filename = "filename";
            public const string password = "password";
            public const string overwrite = "overwrite";
            public const string filenamePattern = "filenamePattern";
            public const string deletedOnArchived = "deletedOnArchived";
            // 21.3-001-01 ADD END
        }

        /// <summary>
        /// 属性の値を定義した定数です。
        /// </summary>
        public static class AttributeValue
        {
            public const string commit = "commit";
            public const string rollback = "rollback";
            public const string error = "error";
            public const string information = "information";
            public const string warning = "warning";
            public const string verbose = "verbose";
            public const string none = "none";
            public const string off = "off";
        }
    }
}
