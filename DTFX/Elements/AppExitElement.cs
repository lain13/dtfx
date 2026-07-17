using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.DTFX.Service;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// 終了処理(AppExit)
    /// </summary>
    public class AppExitElement : XmlElementBase
    {
        /// <summary>
        /// 結果返却（0：正常終了、1：異常終了、2：警告終了）
        /// </summary>
        public int Result { get; set; }

        /// <summary>
        /// 出力するメッセージ
        /// </summary>
        public string Value { get; set; }
    }
}
