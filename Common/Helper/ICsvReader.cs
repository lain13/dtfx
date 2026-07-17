using System;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// CSVFile読み込み共通インターフェイス
    /// </summary>
    public interface ICsvReader : IDisposable
    {
        #region プロパティ
        /// <summary>行コメントとして扱うトークンを取得または設定します。</summary>
        string[] CommentTokens { get; set; }

        /// <summary>フィールド区切り文字を取得または設定します。</summary>
        string[] Delimiters { get; set; }

        /// <summary>読み取れるデータが残っていないかを取得します。</summary>
        bool EndOfData { get; }

        /// <summary>直前の解析エラーが発生した行の内容を取得します。</summary>
        string ErrorLine { get; }

        /// <summary>直前の解析エラーが発生した行番号を取得します。</summary>
        long ErrorLineNumber { get; }

        /// <summary>固定長フィールドの幅を取得または設定します。</summary>
        int[] FieldWidths { get; set; }

        /// <summary>引用符で囲まれたフィールドを解析するかを取得または設定します。</summary>
        bool HasFieldsEnclosedInQuotes { get; set; }

        /// <summary>現在の行番号を取得します。</summary>
        long LineNumber { get; }

        /// <summary>フィールドの前後の空白を除去するかを取得または設定します。</summary>
        bool TrimWhiteSpace { get; set; }

        /// <summary>区切り文字形式で解析しているかを取得します。</summary>
        bool IsFieldTypeDelimited { get; }

        /// <summary>固定長形式で解析しているかを取得します。</summary>
        bool IsFieldTypeFixed { get; }
        #endregion

        #region メソッド定義
        /// <summary>現在行のすべてのフィールドを読み取ります。</summary>
        /// <returns>現在行のフィールド。</returns>
        string[] ReadFields();

        /// <summary>現在位置から 1 行を読み取ります。</summary>
        /// <returns>読み取った行。末尾の場合は <see langword="null"/>。</returns>
        string ReadLine();

        /// <summary>現在位置から末尾までを読み取ります。</summary>
        /// <returns>残りの文字列。</returns>
        string ReadToEnd();

        /// <summary>区切り文字を設定し、区切り文字形式へ切り替えます。</summary>
        /// <param name="delimiters">使用する区切り文字。</param>
        void SetDelimiters(params string[] delimiters);

        /// <summary>固定長フィールドの幅を設定します。</summary>
        /// <param name="fieldWidths">各フィールドの幅。</param>
        void SetFieldWidth(int[] fieldWidths);

        /// <summary>区切り文字形式へ切り替えます。</summary>
        void SetFieldTypeDelimited();

        /// <summary>固定長形式へ切り替えます。</summary>
        void SetFieldTypeFixed();
        #endregion
    }
}
