/************************************************************************
* ファイル名:	NpgsqlBulkCopy.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*   24.1-001-01     2024/03/27  姜　恵遠    PostgreSQL Bulk Insert対応
*
*************************************************************************/
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IF.Batch.DTFX.Helper
{
    /// <summary>
    /// PostgreSQLのNpgsql接続を利用してバイナリインポートで大量データを一括挿入するユーティリティクラス
    /// </summary>
    public class NpgsqlBulkCopy
    {
        /// <summary>
        /// 指定された System.Data.IDataReader のすべての行を DestinationTableName プロパティで指定される宛先テーブルにコピーします。
        /// </summary>
        public String DestinationTableName { get; set; }

        /// <summary>
        /// タイムアウトになる前に操作が完了するまでの秒数。
        /// </summary>
        public int BulkCopyTimeout { get; set; }

        /// <summary>
        ///  Npgsql.NpgsqlConnection インスタンス。 
        /// </summary>
        private NpgsqlConnection Connection { get; set; }

        public NpgsqlBulkCopy(NpgsqlConnection connection)
        {
            this.Connection = connection;
        }

        /// <summary>
        /// 指定された System.Data.IDataReader のすべての行を DestinationTableName プロパティで指定される宛先テーブルにコピーします。
        /// </summary>
        /// <param name="reader">IDataReader</param>
        public void WriteToServer(IDataReader reader)
        {
            try
            {
                if (String.IsNullOrEmpty(DestinationTableName))
                {
                    throw new ArgumentOutOfRangeException("DestinationTableName", "Destination table must be set");
                }
                int colCount = reader.FieldCount;

                NpgsqlDbType[] types = new NpgsqlDbType[colCount];
                int[] lengths = new int[colCount];
                string[] fieldNames = new string[colCount];

                using (var cmd = new NpgsqlCommand("SELECT * FROM " + DestinationTableName + " LIMIT 1", Connection))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.FieldCount != colCount)
                        {
                            throw new ArgumentOutOfRangeException("dataTable", "Column count in Destination Table does not match column count in source table.");
                        }
                        var columns = rdr.GetColumnSchema();
                        for (int i = 0; i < colCount; i++)
                        {
                            types[i] = (NpgsqlDbType)columns[i].NpgsqlDbType;
                            lengths[i] = columns[i].ColumnSize == null ? 0 : (int)columns[i].ColumnSize;
                            fieldNames[i] = columns[i].ColumnName;
                        }
                    }
                }
                var sB = new StringBuilder(fieldNames[0]);
                for (int p = 1; p < colCount; p++)
                {
                    sB.Append(", " + fieldNames[p]);
                }
                using (var writer = Connection.BeginBinaryImport("COPY " + DestinationTableName + " (" + sB.ToString() + ") FROM STDIN (FORMAT BINARY)"))
                {

                    while (reader.Read())
                    {
                        writer.StartRow();

                        for (int i = 0; i < colCount; i++)
                        {
                            if (reader.IsDBNull(i))
                            {
                                writer.WriteNull();
                            }
                            else
                            {
                                switch (types[i])
                                {
                                    case NpgsqlDbType.Bigint:
                                        writer.Write((long)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Bit:
                                        if (lengths[i] > 1)
                                        {
                                            writer.Write((byte[])reader.GetValue(i), types[i]);
                                        }
                                        else
                                        {
                                            writer.Write((byte)reader.GetValue(i), types[i]);
                                        }
                                        break;
                                    case NpgsqlDbType.Boolean:
                                        writer.Write((bool)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Bytea:
                                        writer.Write((byte[])reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Char:
                                        if (reader.GetValue(i) is string)
                                        {
                                            writer.Write((string)reader.GetValue(i), types[i]);
                                        }
                                        else if (reader.GetValue(i) is Guid)
                                        {
                                            var value = reader.GetValue(i).ToString();
                                            writer.Write(value, types[i]);
                                        }


                                        else if (lengths[i] > 1)
                                        {
                                            writer.Write((char[])reader.GetValue(i), types[i]);
                                        }
                                        else
                                        {

                                            var s = ((string)reader.GetValue(i).ToString()).ToCharArray();
                                            writer.Write(s[0], types[i]);
                                        }
                                        break;
                                    case NpgsqlDbType.Time:
                                    case NpgsqlDbType.Timestamp:
                                    case NpgsqlDbType.TimestampTz:
                                    case NpgsqlDbType.Date:
                                        writer.Write((DateTime)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Double:
                                        writer.Write((double)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Integer:
                                        try
                                        {
                                            if (reader.GetValue(i) is int)
                                            {
                                                writer.Write((int)reader.GetValue(i), types[i]);
                                                break;
                                            }
                                            else if (reader.GetValue(i) is string)
                                            {
                                                var swap = Convert.ToInt32(reader.GetValue(i));
                                                writer.Write((int)swap, types[i]);
                                                break;
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            // 型変換に失敗した場合、object型で書き込みを試行する
                                        }

                                        writer.Write((object)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Interval:
                                        writer.Write((TimeSpan)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Numeric:
                                    case NpgsqlDbType.Money:
                                        writer.Write((decimal)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Real:
                                        writer.Write((Single)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Smallint:

                                        try
                                        {
                                            if (reader.GetValue(i) is byte)
                                            {
                                                var swap = Convert.ToInt16(reader.GetValue(i));
                                                writer.Write((short)swap, types[i]);
                                                break;
                                            }
                                            writer.Write((short)reader.GetValue(i), types[i]);
                                        }
                                        catch (Exception)
                                        {
                                            // 型変換に失敗した場合は処理をスキップする
                                        }

                                        break;
                                    case NpgsqlDbType.Varchar:
                                    case NpgsqlDbType.Text:
                                        writer.Write((string)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Uuid:
                                        writer.Write((Guid)reader.GetValue(i), types[i]);
                                        break;
                                    case NpgsqlDbType.Xml:
                                        writer.Write((string)reader.GetValue(i), types[i]);
                                        break;
                                }
                            }

                        }
                    }
                    writer.Complete();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing NpgSqlBulkCopy.WriteToServer().  See inner exception for details", ex);
            }
        }
    }
}
