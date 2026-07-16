/************************************************************************
* ファイル名:	LoadCSVElement.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/10/07	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// CSV読み込み処理(LoadCSV)
    /// </summary>
    public class LoadCSVElement : XmlElementBase
    {
        /// <summary>
        /// 読み込みファイル名
        /// </summary>
        public string FromFile { get; set; }

        /// <summary>
        /// ローカルテーブル名（読み込みファイルを一時テーブルに出力する場合）
        /// </summary>
        public string ToTable { get; set; }

        /// <summary>
        /// 変数名（読み込みファイルを変数に保存する場合）
        /// </summary>
        public string ToVariable { get; set; }

        /// <summary>
        /// ヘッダを含む
        /// </summary>
        public bool HasHeaders { get; set; }
    }
}
