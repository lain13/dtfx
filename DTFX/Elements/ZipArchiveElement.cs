/************************************************************************
* ファイル名:	ZipArchiveElement.cs
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
    /// ZIPファイル圧縮処理
    /// </summary>
    public class ZipArchiveElement : XmlElementBase
    {
        /// <summary>
        /// 圧縮ファイル名
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// 圧縮ファイルパスワード
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// ファイル上書き
        /// </summary>
        public bool? Overwrite { get; set; }

        private readonly List<AddFileElement> _addFileElements = new List<AddFileElement>();

        /// <summary>
        /// ファイル追加
        /// </summary>
        public List<AddFileElement> AddFileElements
        {
            get
            {
                return _addFileElements;
            }
            set
            {
                _addFileElements.Clear();
                foreach(var element in value)
                {
                    _addFileElements.Add(element);
                }
            }
        }
    }
}
