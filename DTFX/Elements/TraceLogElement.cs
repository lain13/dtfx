/************************************************************************
* ファイル名:	TraceLogElement.cs
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
    /// メッセージ出力処理(TraceLog)
    /// </summary>
    public class TraceLogElement : XmlElementBase
    {
        /// <summary>
        /// メッセージの種類（情報/警告/エラー/デバック）
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// 変数名（実行結果を変数に保存する場合）
        /// </summary>
        public string ToVariable { get; set; }

        /// <summary>
        /// 出力するメッセージ
        /// </summary>
        public string Value { get; set; }
    }
}
