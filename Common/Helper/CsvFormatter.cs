using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// フィールド値を区切り文字、引用符、空白の設定に従って CSV 文字列へ変換します。
    /// </summary>
    public class CsvFormatter
    {
        /// <summary>
        /// 区切り記号：,(Comma)
        /// </summary>
        private const string DEFAULT_DELIMITER = ",";

        #region プロパティ
        /// <summary>
        /// Writerの区切り記号を指定された値に設定します。
        /// </summary>
        public string Delimiter
        {
            get;
            set;
        }

        /// <summary>
        /// 区切り記号入りファイルに出力する場合に、
        /// 常にフィールドを引用符で囲むかどうかを示します。
        /// </summary>
        /// <value>常にフィールドを引用符で囲む場合は <see langword="true"/>。</value>
        public bool AlwaysFieldsEncloseInQuotes
        {
            get;
            set;
        }

        /// <summary>
        /// フィールド値から前後の空白をトリムするかどうかを示します。
        /// </summary>
        /// <value>フィールド値から前後の空白を除去する場合は <see langword="true"/>。</value>
        public bool TrimWhiteSpace
        {
            get;
            set;
        }

        /// <summary>
        /// レコード間の区切り文字(改行文字)を示します。
        /// </summary>
        public string RowDelimiter
        {
            get;
            set;
        }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// 区切り文字、改行、引用符、空白の処理方法を指定してフォーマッターを生成します。
        /// </summary>
        /// <param name="delimiter">項目セパレータ</param>
        /// <param name="rowDelimiter">改行文字</param>
        /// <param name="alwaysFieldsEncloseInQuotes">true:項目をダブルクォーテーションで囲む</param>
        /// <param name="trimWhiteSpace">フィールドの前後の空白を除去する場合は <see langword="true"/>。</param>
        public CsvFormatter(string delimiter = null, string rowDelimiter = null, bool alwaysFieldsEncloseInQuotes = false, bool trimWhiteSpace = false)
        {
            this.Delimiter = delimiter ?? DEFAULT_DELIMITER;
            this.RowDelimiter = rowDelimiter ?? Environment.NewLine;
            this.AlwaysFieldsEncloseInQuotes = alwaysFieldsEncloseInQuotes;
            this.TrimWhiteSpace = trimWhiteSpace;
        }
        #endregion

        /// <summary>
        /// CSV用の文字列に整形します。
        /// レコード間の区切り文字(改行文字)は末尾には付与されません。
        /// </summary>
        /// <param name="tokens">CSV文字列化対象のトークン</param>
        /// <returns>CSV文字列</returns>
        public StringBuilder ToCsv(string[] tokens)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < tokens.Length; ++i)
            {
                WriteField(builder, tokens[i]);
                if (i != tokens.Length - 1)
                {
                    builder.Append(Delimiter);
                }
            }
            return builder;
        }

        /// <summary>
        /// CSV用の文字列に整形します。
        /// レコード間の区切り文字(改行文字)は末尾には付与されません。
        /// </summary>
        /// <param name="fields">CSV文字列化対象のトークン</param>
        /// <returns>CSV文字列</returns>
        public StringBuilder ToCsv(IEnumerable<object> fields)
        {
            StringBuilder builder = new StringBuilder();
            bool isFirstField = true;
            foreach (object field in fields)
            {
                if (!isFirstField)
                {
                    builder.Append(Delimiter);
                }

                WriteField(builder, field == null ? null : field.ToString());
                isFirstField = false;
            }
            return builder;
        }

        /// <summary>
        /// 文字列を書き込みます。
        /// </summary>
        /// <param name="builder">整形結果を追加する文字列ビルダー。</param>
        /// <param name="field">対象文字列</param>
        protected virtual void WriteField(StringBuilder builder, string field)
        {
            string str = field != null ? TrimWhiteSpace ? field.Trim() : field : string.Empty;

            if (RequiresQuotes(str))
            {
                str = "\"" + str.Replace("\"", "\"\"") + "\"";
            }

            builder.Append(str);
        }

        /// <summary>
        /// フィールドを引用符で囲む必要があるかを返します。
        /// </summary>
        protected virtual bool RequiresQuotes(string field)
        {
            return AlwaysFieldsEncloseInQuotes ||
                field.IndexOf('"') > -1 ||
                (!string.IsNullOrEmpty(Delimiter) && field.Contains(Delimiter)) ||
                field.IndexOf('\r') > -1 ||
                field.IndexOf('\n') > -1 ||
                field.StartsWith(" ") ||
                field.StartsWith("\t") ||
                field.EndsWith(" ") ||
                field.EndsWith("\t");
        }
    }
}
