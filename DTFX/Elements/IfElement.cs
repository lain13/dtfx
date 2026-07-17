using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// 条件処理(If)
    /// </summary>
    public class IfElement : XmlElementBase
    {
        /// <summary>
        /// 条件文(JEXL形式)
        /// </summary>
        public string Test { get; set; }

        /// <summary>
        /// 変数名（実行結果を変数に保存する場合）
        /// </summary>
        public string ToVariable { get; set; }
    }
}
