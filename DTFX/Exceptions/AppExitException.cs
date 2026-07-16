/************************************************************************
* ファイル名:	AppConfigurationException.cs
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
using IF.Batch.DTFX.Elements;

namespace IF.Batch.DTFX.Exceptions
{
    /// <summary>
    /// アプリケーション終了例外
    /// </summary>
    public class AppExitException : Exception
    {
        public AppExitElement Element
        {
            get;
            set;
        }
        public AppExitException(AppExitElement element)
        {
            Element = element;
        }
    }
}
