/************************************************************************
* ファイル名:	SqlSelectScalarElement.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*   28.7-001-01     2017/03/08  姜　恵遠    新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// MSSQLデータ取得処理(SqlSelectScalar)
    /// </summary>
    public class SqlSelectScalarElement : XmlElementBase 
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
        /// 実行するＳＱＬ文
        /// </summary>
        public string Value { get; set; }
    }
}
