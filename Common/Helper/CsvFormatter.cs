/************************************************************************
* ファイル名:	CsvFormatter.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   25.1-001-02     2013/10/07  姜　恵遠    NewLine⇒RowDelimiterに変更
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.Common.Helper
{
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
        /// <returns>常にフィールドを引用符で囲む場合は True。それ以外の場合は False。</returns>
        public bool AlwaysFieldsEncloseInQuotes
        {
            get;
            set;
        }

        /// <summary>
        /// フィールド値から前後の空白をトリムするかどうかを示します。
        /// </summary>
        /// <returns>フィールド値から前後の空白をトリムする場合は True。それ以外の場合は False。</returns>
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
        /// 項目セパレータと項目をダブルクォーテーションで囲むかを指定する
        /// コンストラクタ
        /// </summary>
        /// <param name="delimiter">項目セパレータ</param>
        /// <param name="rowDelimiter">改行文字</param>
        /// <param name="alwaysFieldsEncloseInQuotes">true:項目をダブルクォーテーションで囲む</param>
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
            int lastColIndex = fields.Count() - 1;
            int i = 0;
            foreach (object field in fields)
            {
                WriteField(builder, field == null ? null : field.ToString());
                // カンマを書き込む
                if (lastColIndex > i)
                {
                    builder.Append(Delimiter);
                }
                i++;
            }
            return builder;
        }

        /// <summary>
        /// 文字列を書き込みます。
        /// </summary>
        /// <param name="field">対象文字列</param>
        protected virtual void WriteField(StringBuilder builder, string field)
        {
            string str = field != null ? TrimWhiteSpace ? field.Trim() : field : string.Empty;

            // 「"」で囲む必要があるか調べる
            if (AlwaysFieldsEncloseInQuotes ||
                str.IndexOf('"') > -1 ||
                str.IndexOf(',') > -1 ||
                str.IndexOf('\r') > -1 ||
                str.IndexOf('\n') > -1 ||
                str.StartsWith(" ") ||
                str.StartsWith("\t") ||
                str.EndsWith(" ") ||
                str.EndsWith("\t"))
            {
                if (str.IndexOf('"') > -1)
                {
                    // 「"」を「""」とする
                    str = "\"" + str.Replace("\"", "\"\"") + "\"";
                }
                else
                {
                    str = "\"" + str + "\"";
                }
            }

            // フィールドを書き込む
            builder.Append(str);
        }
    }
}
