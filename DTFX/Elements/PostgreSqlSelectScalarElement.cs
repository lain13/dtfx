using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// PostgreSqlデータ取得処理(PostgreSqlSelectScalar)
    /// </summary>
    public class PostgreSqlSelectScalarElement : XmlElementBase 
    {
        /// <summary>
        /// データソース
        /// </summary>
        public string DataSource { get; set; }

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
