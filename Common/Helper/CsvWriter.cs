using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.IO.Compression;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// CSVFile書き込み共通クラス
    /// </summary>
    public class CsvWriter : ICsvWriter
    {
        /// <summary>
        /// バッファー サイズ：2097152(2MB)
        /// </summary>
        protected const int DEFAULT_BUFFER_SIZE = 2097152;

        /// <summary>
        /// 区切り記号：,(Comma)
        /// </summary>
        protected const string DEFAULT_DELIMITER = ",";

        #region フィールド
        /// <summary>
        /// System.IO.Stream.解析するストリーム。
        /// </summary>
        protected Stream _stream;

        /// <summary>
        /// System.IO.Stream.解析するWriter。
        /// </summary>
        protected StreamWriter _writer;
        #endregion

        #region プロパティ
        /// <summary>
        /// 直前の行の番号を返します。
        /// </summary>
        public long LineNumber
        {
            get;
            protected set;
        }

        /// <summary>
        /// 直前に発生したCSVFormatException 例外の原因となった行を返します。
        /// </summary>
        public string ErrorLine
        {
            get;
            protected set;
        }

        /// <summary>
        /// 直前の CSVFormatException 例外が発生した行の番号を返します。
        /// </summary>
        public long ErrorLineNumber
        {
            get;
            protected set;
        }

        private CsvFormatter _formatter = new CsvFormatter();
        /// <summary>
        /// Csvフォーマッタ
        /// </summary>
        public CsvFormatter Formatter
        {
            get
            {
                return _formatter;
            }
            set
            {
                _formatter = value;
            }
        }
        
        /// <summary>
        /// 区切り記号入りファイルに出力する場合に、
        /// 常にフィールドを引用符で囲むかどうかを示します。
        /// </summary>
        /// <value>常にフィールドを引用符で囲む場合は <see langword="true"/>。</value>
        public bool AlwaysFieldsEncloseInQuotes
        {
            get
            {
                return _formatter.AlwaysFieldsEncloseInQuotes;
            }
            set
            {
                _formatter.AlwaysFieldsEncloseInQuotes = value;
            }
        }

        /// <summary>
        /// フィールド値から前後の空白をトリムするかどうかを示します。
        /// </summary>
        /// <value>フィールド値から前後の空白を除去する場合は <see langword="true"/>。</value>
        public bool TrimWhiteSpace
        {
            get
            {
                return _formatter.TrimWhiteSpace;
            }
            set
            {
                _formatter.TrimWhiteSpace = value;
            }
        }

        /// <summary>
        /// Writerの区切り記号を指定された値に設定します。
        /// </summary>
        public string Delimiter
        {
            get
            {
                return _formatter.Delimiter;
            }
            set
            {
                _formatter.Delimiter = value;
            }
        }

        /// <summary>
        /// レコード間の区切り文字(改行文字)を示します。
        /// </summary>
        public string RowDelimiter
        {
            get
            {
                return _formatter.RowDelimiter;
            }
            set
            {
                _formatter.RowDelimiter = value;
            }
        }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// CSVFileWriter クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="stream">解析するストリーム。</param>
        /// <param name="encoding">
        /// System.Text.Encoding.ファイルからエンコーディングを判断できない場合に使用する文字エンコーディング。
        /// 既定値は System.Text.Encoding.Defaultです。
        /// </param>
        /// <param name="useGzip">Gzipファイルの場合true。既定値は falseです。</param>
        public CsvWriter(Stream stream, Encoding encoding = null, bool useGzip = false)
        {
            Initialize(stream, encoding ?? Encoding.Default, useGzip);
        }

        /// <summary>
        /// CSVFileWriter クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="path">String.解析するファイルの絶対パス。</param>
        /// <param name="encoding">
        /// System.Text.Encoding.ファイルからエンコーディングを判断できない場合に使用する文字エンコーディング。
        /// 既定値は System.Text.Encoding.Defaultです。
        /// </param>
        /// <param name="append">既存のファイルに追加する場合true。既定値は falseです。</param>
        /// <param name="useGzip">Gzipファイルの場合true。既定値は falseです。</param>
        /// <param name="bufferSize">バッファーのサイズを指定します。既定値は 2097152(2MB)です。</param>
        /// <exception cref="System.ArgumentNullException">
        /// path が空の文字列であるか、defaultEncoding が Nothing です。
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        /// ファイルが見つかりませんでした。
        /// </exception>
        public CsvWriter(string path, Encoding encoding = null, bool append = false, bool useGzip = false, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            Initialize(path, encoding ?? Encoding.Default, append, useGzip, bufferSize);
        }
        #endregion

        #region メソッド定義
        /// <summary>
        /// CSVFileWriterを既定値に初期化します。
        /// </summary>
        /// <param name="path">String.解析するファイルの絶対パス。</param>
        /// <param name="encoding">
        /// System.Text.Encoding.ファイルからエンコーディングを判断できない場合に使用する文字エンコーディング。
        /// </param>
        /// <param name="append">既存のファイルに追加する場合true。</param>
        /// <param name="useGzip">Gzipファイルの場合true。</param>
        /// <param name="bufferSize">バッファーのサイズを指定します。</param>
        protected virtual void Initialize(string path, Encoding encoding, bool append, bool useGzip, int bufferSize)
        {
            FileStream fileStream = File.Open(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
            Initialize(new BufferedStream(fileStream, bufferSize), encoding, useGzip);
        }

        /// <summary>
        /// 指定されたストリームへ書き込む CSV ライターを初期化します。
        /// </summary>
        /// <param name="stream">書き込み先ストリーム。</param>
        /// <param name="encoding">出力に使用する文字エンコーディング。</param>
        /// <param name="useGzip">GZIP 圧縮する場合は <see langword="true"/>。</param>
        protected virtual void Initialize(Stream stream, Encoding encoding, bool useGzip)
        {
            if (useGzip)
            {
                this._stream = new GZipStream(stream, CompressionMode.Compress);
            }
            else
            {
                this._stream = stream;
            }
            this._writer = new StreamWriter(this._stream, encoding);
            this.Formatter = new CsvFormatter();
        }

        /// <summary>
        /// 現在行のすべてのフィールドを書き込みます。
        /// </summary>
        /// <param name="fields">現在の行のフィールド値を格納する文字列の配列。</param>
        /// <param name="newLine">書き込み後に行を終了する場合は <see langword="true"/>。</param>
        public virtual void Write(string[] fields, bool newLine = false)
        {
            try
            {
                _writer.Write(Formatter.ToCsv(fields));
                if (newLine)
                {
                    _writer.Write(RowDelimiter);
                    LineNumber++;
                }
            }
            catch
            {
                ErrorLineNumber = LineNumber;
                ErrorLine = string.Join(string.Empty, fields);
                throw;
            }
        }

        /// <summary>
        /// 現在行のすべてのフィールドを書き込みます。
        /// </summary>
        /// <param name="fields">現在の行のフィールド値を格納する文字列の配列。</param>
        public void WriteLine(string[] fields)
        {
            Write(fields, true);
        }

        /// <summary>
        /// 現在行のすべてのフィールドを書き込みます。
        /// </summary>
        /// <param name="tokens">現在の行のフィールド値を格納する文字列の配列。</param>
        /// <param name="newLine">改行可否</param>
        public virtual void Write(IEnumerable<object> tokens, bool newLine = false)
        {
            try
            {
                _writer.Write(Formatter.ToCsv(tokens));
                if (newLine)
                {
                    _writer.Write(RowDelimiter);
                    LineNumber++;
                }
            }
            catch
            {
                ErrorLineNumber = LineNumber;
                ErrorLine = string.Join(string.Empty, tokens);
                throw;
            }
        }

        /// <summary>
        /// 現在行のすべてのフィールドを書き込みます。
        /// </summary>
        /// <param name="fields">現在の行のフィールド値を格納する配列。</param>
        public void WriteLine(IEnumerable<object> fields)
        {
            Write(fields, true);
        }

        #endregion

        #region ストリームのフラッシュとクローズ
        /// <summary>
        /// ストリームをフラッシュする。
        /// </summary>
        public virtual void Flush()
        {
            if (_writer != null)
            {
                _writer.Flush();
            }
        }

        /// <summary>
        /// 現在の CSVFileWriter オブジェクトを閉じます。
        /// </summary>
        public virtual void Close()
        {
            if (_writer != null)
            {
                _writer.Close();
            }
            if (_stream != null)
            {
                _stream.Close();
            }
        }
        #endregion

        #region IDisposable の実装
        /// <summary>
        /// CSVFileWriter オブジェクトによって使用されているリソースを解放します。
        /// </summary>
        public virtual void Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
            }
            if (_stream != null)
            {
                _stream.Dispose();
            }
        }
        #endregion
    }
}
