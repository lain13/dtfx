/************************************************************************
* ファイル名:	PostgreSqlBulkInsertFromSqlServerElement.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2024/03/27	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// PostgreSqlデータ一括登録処理(PostgreSqlBulkInsertFromSqlServer)
    /// </summary>
    public class PostgreSqlBulkInsertFromSqlServerElement : XmlElementBase
    {
        /// <summary>
        /// コピー元データソース
        /// </summary>
        public string FromDataSource { get; set; }

        /// <summary>
        /// コピー先データソース
        /// </summary>
        public string ToDataSource { get; set; }

        /// <summary>
        /// コピー先テーブル名
        /// </summary>
        public string ToTable { get; set; }

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
