/************************************************************************
* ファイル名:	MSSQLTableCreator.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   25.1-001-02     2013/10/07  姜　恵遠    25年度2期
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Data.Common;

namespace IF.Batch.DTFX.Helper
{
    public class MSSQLTableCreator
    {
        /// <summary>
        /// SQLサーバーのデータ型を取得します。
        /// </summary>
        /// <param name="dbTypeName"></param>
        /// <param name="type"></param>
        /// <param name="columnSize"></param>
        /// <param name="numericPrecision"></param>
        /// <param name="numericScale"></param>
        /// <returns></returns>
        public static string SQLGetType(string dbTypeName, object type, int columnSize, int numericPrecision, int numericScale)
        {
            switch (dbTypeName)
            {
                case "money":
                    return "MONEY";
                default:
                    break;
            }

            switch (type.ToString())
            {
                case "System.Byte[]":
                    return "VARBINARY(MAX)";
                case "System.Boolean":
                    return "BIT";
                case "System.DateTime":
                    return "DATETIME";
                case "System.DateTimeOffset":
                    return "DATETIMEOFFSET";
                case "System.Decimal":
                    if (numericPrecision != -1 && numericScale != -1)
                        return "DECIMAL(" + numericPrecision + "," + numericScale + ")";
                    else return "DECIMAL";
                case "System.Double":
                    return "FLOAT";
                case "System.Single":
                    return "REAL";
                case "System.Int64":
                    return "BIGINT";
                case "System.Int32":
                    return "INT";
                case "System.Int16":
                    return "SMALLINT";
                case "System.String":
                    return "NVARCHAR(" + ((columnSize == -1 || columnSize > 8000) ? "MAX" : columnSize.ToString()) + ")";
                case "System.Byte":
                    return "TINYINT";
                case "System.Guid":
                    return "UNIQUEIDENTIFIER";
                default: return "NVARCHAR(MAX)";
            }
        }

        /// <summary>
        /// Create DDL文を生成します。
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetCreateTableStatement(DbDataReader reader, string tableName)
        {

            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("CREATE TABLE [{0}] (", tableName));
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string dbType = SQLGetType(reader.GetDataTypeName(i), reader.GetFieldType(i), -1, 38, 10);
                builder.Append("[");
                builder.Append(reader.GetName(i));
                builder.Append("]");
                builder.Append(" ");
                builder.Append(dbType);
                builder.Append(", ");
            }

            if (reader.FieldCount > 0)
            {
                builder.Length = builder.Length - 2;
            }
            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// Create DDL文を生成します。
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetCreateTableStatement(string[] columnNames, string tableName)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("CREATE TABLE [{0}] (", tableName));
            for (int i = 0; i < columnNames.Length; i++)
            {
                builder.Append("[");
                builder.Append(columnNames[i]);
                builder.Append("]");
                builder.Append(" ");
                builder.Append("NVARCHAR(MAX)");
                builder.Append(", ");
            }

            if (columnNames.Length > 0)
            {
                builder.Length = builder.Length - 2;
            }
            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// カラム名を生成します。
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="columnCount"></param>
        /// <returns></returns>
        public static string[] CreateColumnNames(string columnName, int columnCount)
        {
            string[] columns = new string[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                columns[i] = string.Format("{0}{1}", columnName, (i + 1));
            }
            return columns;
        }
    }
}
