using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// MSSQLデータ一括登録処理(SqlServerBulkInsertFromOracle)
    /// </summary>
    public class SqlServerBulkInsertFromOracleElement : XmlElementBase
    {
        /// <summary>
        /// コピー元データソース
        /// </summary>
        public string FromDataSource { get; set; }

        /// <summary>
        /// コピー先データソース
        /// </summary>
        public string ToDataSource { get; set; }

        /// <summary>
        /// コピー先テーブル名
        /// </summary>
        public string ToTable { get; set; }

        /// <summary>
        /// 変数名（実行結果を変数に保存する場合）
        /// </summary>
        public string ToVariable { get; set; }

        /// <summary>
        /// 実行するＳＱＬ文
        /// </summary>
        public string Value { get; set; }
    }
}
