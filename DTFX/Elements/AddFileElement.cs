/************************************************************************
* ファイル名:	AddFileElement.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*   21.3-001-01     2021/07/05  姜　恵遠    Biz-A Step1.5対応
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// ZIPファイルファイル追加
    /// </summary>
    public class AddFileElement : XmlElementBase
    {
        /// <summary>
        /// ディレクトリ
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// ファイル名パターン
        /// </summary>
        public string FilenamePattern { get; set; }

        /// <summary>
        /// 圧縮後削除
        /// </summary>
        public bool? DeletedOnArchived { get; set; }
    }
}
