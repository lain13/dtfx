/************************************************************************
* ファイル名:	PostgreSqlDeleteElement.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	23.1-001-01		2023/02/15	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// PostgreSQLデータ削除処理(PostgreSQLDelete)
    /// </summary>
    public class PostgreSqlDeleteElement : XmlElementBase
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
