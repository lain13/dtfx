/************************************************************************
* ファイル名:	LocalDBSelectElement.cs
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
    /// 一時DBデータ取得処理(LocalDBSelect)
    /// </summary>
    public class LocalDBSelectElement : XmlElementBase 
    {
        /// <summary>
        /// ファイル出力パス（実行結果をファイルに出力する場合）
        /// </summary>
        public string ToFile { get; set; }

        // 28.7-001-01 ADD START
        /// <summary>
        /// ヘッダ文字列
        /// </summary>
        public string HeaderString { get; set; }

        /// <summary>
        /// トレーラ文字列
        /// </summary>
        public string TrailerString { get; set; }
        // 28.7-001-01 ADD END

        /// <summary>
        /// ローカルテーブル名（実行結果をテーブルに出力する場合）
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
