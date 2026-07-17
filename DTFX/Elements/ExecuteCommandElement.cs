using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// メッセージ出力処理(TraceLog)
    /// </summary>
    public class ExecuteCommandElement : XmlElementBase
    {
        /// <summary>
        /// 変数名（実行結果を変数に保存する場合）
        /// </summary>
        public string ToVariable { get; set; }

        /// <summary>
        /// メッセージの出力（情報/警告/エラー/デバック/なし）
        /// </summary>
        public string TraceLog { get; set; }

        /// <summary>
        /// 出力するメッセージ
        /// </summary>
        public string Value { get; set; }
    }
}
