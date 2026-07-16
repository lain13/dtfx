/************************************************************************
* ファイル名:	IXmlElement.cs
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
    public interface IXmlElement
    {
        /// <summary>
        /// 要素のID
        /// </summary>
        string Id { get; set; }
    }
}
