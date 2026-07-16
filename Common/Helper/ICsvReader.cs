/************************************************************************
* ファイル名:	ICsvReader.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*
*************************************************************************/
using System;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// CSVFile読み込み共通インターフェイス
    /// </summary>
    public interface ICsvReader : IDisposable
    {
        #region プロパティ
        string[] CommentTokens { get; set; }

        string[] Delimiters { get; set; }

        bool EndOfData { get; }

        string ErrorLine { get; }

        long ErrorLineNumber { get; }

        int[] FieldWidths { get; set; }

        bool HasFieldsEnclosedInQuotes { get; set; }

        long LineNumber { get; }

        bool TrimWhiteSpace { get; set; }

        bool IsFieldTypeDelimited { get; }

        bool IsFieldTypeFixed { get; }
        #endregion

        #region メソッド定義
        string[] ReadFields();

        string ReadLine();

        string ReadToEnd();

        void SetDelimiters(params string[] delimiters);

        void SetFieldWidth(int[] fieldWidths);

        void SetFieldTypeDelimited();

        void SetFieldTypeFixed();
        #endregion
    }
}
