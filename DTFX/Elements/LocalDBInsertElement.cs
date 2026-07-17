using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// 一時DBデータ登録処理(LocalDBInsert)
    /// </summary>
    public class LocalDBInsertElement : XmlElementBase
    {
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
