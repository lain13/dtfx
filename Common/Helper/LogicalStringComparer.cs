/************************************************************************
* ファイル名:	LogicalStringComparer.cs
* 概要: 文字列を自然順ソートで並び替えるクラス。
* 履歴:
*	バージョン		日付		作成者		内容
*	27.2-001-01		2015/06/23	姜　恵遠	新規作成(Mantis:0072130対応)
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// 大文字小文字を区別せずに、
    /// 文字列内に含まれている数字を考慮して文字列を比較します。
    /// </summary>
    public class LogicalStringComparer :
    System.Collections.IComparer,
    System.Collections.Generic.IComparer<string>
    {
        [System.Runtime.InteropServices.DllImport("shlwapi.dll",
        CharSet = System.Runtime.InteropServices.CharSet.Unicode,
        ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string x, string y);

        /// <summary>
        /// 文字列を比較します。
        /// </summary>
        /// <param name="x">文字列１</param>
        /// <param name="y">文字列２</param>
        /// <returns></returns>
        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }

        /// <summary>
        /// Objectを比較します。
        /// </summary>
        /// <param name="x">object 1</param>
        /// <param name="y">object 2</param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            return this.Compare(x == null ? null : x.ToString(), y == null ? null : y.ToString());
        }
    }
}
