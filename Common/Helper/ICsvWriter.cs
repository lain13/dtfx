/************************************************************************
* ファイル名:	ICsvWriter.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   25.1-001-02     2013/10/07  姜　恵遠    NewLine⇒RowDelimiterに変更
*
*************************************************************************/
using System;
using System.Collections.Generic;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// CSVFile書き込み共通インターフェイス
    /// </summary>
    public interface ICsvWriter : IDisposable
    {
        #region プロパティ
        long LineNumber { get; }

        string ErrorLine { get; }

        long ErrorLineNumber { get; }

        CsvFormatter Formatter { get; set; }

        bool AlwaysFieldsEncloseInQuotes { get; set; }

        bool TrimWhiteSpace { get; set; }

        string Delimiter { get; set; }

        string RowDelimiter { get; set; }
        #endregion

        #region メソッド定義
        void Write(IEnumerable<object> tokens, bool newLine = false);

        void Write(string[] fields, bool newLine = false);

        void WriteLine(IEnumerable<object> fields);

        void WriteLine(string[] fields);

        void Flush();
        #endregion
    }
}
