/************************************************************************
* ファイル名:	ForEachElement.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// ループ処理(ForEach)
    /// </summary>
    public class ForEachElement : XmlElementBase
    {
        /// <summary>
        /// 変数名
        /// </summary>
        public string Var { get; set; }

        /// <summary>
        /// 読み込みファイル名
        /// </summary>
        public string FromFile { get; set; }

        /// <summary>
        /// 読み込みテーブル名
        /// </summary>
        public string FromTable { get; set; }

        /// <summary>
        /// 読み込み変数名
        /// </summary>
        public string FromVariable { get; set; }

        /// <summary>
        /// 正常終了時トランザクション制御(none/commit/rollback)
        /// </summary>
        public string Transaction { get; set; }

        /// <summary>
        /// エラー発生時中止可否(true/false)
        /// </summary>
        public bool StopOnError { get; set; }

        /// <summary>
        /// エラー発生時トランザクション制御(none/commit/rollback)
        /// </summary>
        public string TransactionOnError { get; set; }

    }
}
