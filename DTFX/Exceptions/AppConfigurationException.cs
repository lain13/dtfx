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
    /// アプリケーション環境設定例外
    /// </summary>
    public class AppConfigurationException : Exception
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        public AppConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        public AppConfigurationException(string elementName, string message)
            : base(string.Format("{0}:{1}", elementName, message))
        {
        }
    }
}
