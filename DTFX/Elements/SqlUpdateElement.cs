/************************************************************************
* ファイル名:	SqlUpdateElement.cs
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
    /// MSSQLデータ更新処理(SqlUpdate)
    /// </summary>
    public class SqlUpdateElement : XmlElementBase
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
