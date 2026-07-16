/************************************************************************
* ファイル名:	XSqlElementBase.cs
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
using System.Xml.Linq;

namespace IF.Batch.DTFX.Elements
{
    /// <summary>
    /// XML要素のデータモデルの基底クラス
    /// </summary>
    public abstract class XmlElementBase : IXmlElement
    {
        /// <summary>
        /// 要素のXElement
        /// </summary>
        public XElement RawElement { get; set; }

        /// <summary>
        /// 要素のID
        /// </summary>
        public string Id { get; set; }
    }
}
