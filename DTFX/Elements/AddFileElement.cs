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
