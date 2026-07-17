using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// Oracleデータ登録処理(OracleInsert)
    /// </summary>
    public class OracleInsertElement : XmlElementBase
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
        /// トランザクション制御(none/commit/rollback)
        /// </summary>
        public string Transaction { get; set; }

        /// <summary>
        /// 実行するＳＱＬ文
        /// </summary>
        public string Value { get; set; }
    }
}
