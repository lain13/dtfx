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
