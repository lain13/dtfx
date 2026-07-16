/************************************************************************
* ファイル名:	CSVFormatException.cs
* 概要:CSVFile読み込み共通クラス
*
* 履歴:
*	バージョン		日付		作成者		内容
*	24.3-001-01		2012/4/17	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// CSVFile読み込み処理例外クラス
    /// </summary>
    public class CSVFormatException : Exception
    {

        /// <summary>
        /// CSVFormatException クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="ex">フォーマットエラー例外</param>
        public CSVFormatException(MalformedLineException ex)
            : base(ex.Message, ex)
        {
        }

        /// <summary>
        /// 呼び出し履歴の直前のフレームの文字列形式を取得します。
        /// </summary>
        public new string StackTrace
        {
            get
            {
                if (InnerException != null)
                {
                    return InnerException.StackTrace;
                }
                else
                {
                    return base.StackTrace;
                }
            }
        }
    }
}
