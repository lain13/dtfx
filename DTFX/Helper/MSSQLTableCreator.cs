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
    /// <summary>
    /// データリーダーまたは CSV 列名から SQL Server 一時テーブルの DDL を生成します。
    /// </summary>
    public class MSSQLTableCreator
    {
        /// <summary>
        /// SQLサーバーのデータ型を取得します。
        /// </summary>
        /// <param name="dbTypeName">プロバイダーが返すデータ型名。</param>
        /// <param name="type">列の CLR 型。</param>
        /// <param name="columnSize">文字列列の最大長。負数の場合は <c>MAX</c>。</param>
        /// <param name="numericPrecision">数値列の精度。</param>
        /// <param name="numericScale">数値列の小数点以下桁数。</param>
        /// <returns>一時テーブルの列定義に使用する SQL Server データ型。</returns>
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
        /// <param name="reader">列名と型情報を提供するデータリーダー。</param>
        /// <param name="tableName">作成するテーブル名。</param>
        /// <returns>データリーダーのスキーマに対応する <c>CREATE TABLE</c> 文。</returns>
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
        /// <param name="columnNames">作成する列名。型はすべて <c>NVARCHAR(MAX)</c> になります。</param>
        /// <param name="tableName">作成するテーブル名。</param>
        /// <returns>指定した列名に対応する <c>CREATE TABLE</c> 文。</returns>
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
        /// <param name="columnName">列名のプレフィックス。</param>
        /// <param name="columnCount">生成する列数。</param>
        /// <returns><paramref name="columnName"/> に 1 から始まる番号を付けた列名の配列。</returns>
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
