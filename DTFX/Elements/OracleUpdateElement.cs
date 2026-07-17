using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// Oracleデータ更新処理(OracleUpdate)
    /// </summary>
    public class OracleUpdateElement : XmlElementBase
    {
        /// <summary>
        /// データソース
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// トランザクション制御(commit/rollback)
        /// </summary>
        public string Transaction { get; set; }

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
