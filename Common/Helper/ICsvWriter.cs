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
        /// <summary>書き込み済みの行数を取得します。</summary>
        long LineNumber { get; }

        /// <summary>直前の書き込みエラーが発生した行の内容を取得します。</summary>
        string ErrorLine { get; }

        /// <summary>直前の書き込みエラーが発生した行番号を取得します。</summary>
        long ErrorLineNumber { get; }

        /// <summary>CSV フォーマッターを取得または設定します。</summary>
        CsvFormatter Formatter { get; set; }

        /// <summary>すべてのフィールドを引用符で囲むかを取得または設定します。</summary>
        bool AlwaysFieldsEncloseInQuotes { get; set; }

        /// <summary>フィールドの前後の空白を除去するかを取得または設定します。</summary>
        bool TrimWhiteSpace { get; set; }

        /// <summary>フィールド区切り文字を取得または設定します。</summary>
        string Delimiter { get; set; }

        /// <summary>行区切り文字を取得または設定します。</summary>
        string RowDelimiter { get; set; }
        #endregion

        #region メソッド定義
        /// <summary>オブジェクトの列を CSV フィールドとして書き込みます。</summary>
        /// <param name="tokens">書き込むフィールド。</param>
        /// <param name="newLine">書き込み後に行を終了する場合は <see langword="true"/>。</param>
        void Write(IEnumerable<object> tokens, bool newLine = false);

        /// <summary>文字列の列を CSV フィールドとして書き込みます。</summary>
        /// <param name="fields">書き込むフィールド。</param>
        /// <param name="newLine">書き込み後に行を終了する場合は <see langword="true"/>。</param>
        void Write(string[] fields, bool newLine = false);

        /// <summary>オブジェクトの列を 1 行の CSV レコードとして書き込みます。</summary>
        /// <param name="fields">書き込むフィールド。</param>
        void WriteLine(IEnumerable<object> fields);

        /// <summary>文字列の列を 1 行の CSV レコードとして書き込みます。</summary>
        /// <param name="fields">書き込むフィールド。</param>
        void WriteLine(string[] fields);

        /// <summary>バッファーの内容を基になるストリームへ書き込みます。</summary>
        void Flush();
        #endregion
    }
}
