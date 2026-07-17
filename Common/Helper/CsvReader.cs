using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using System.IO.Compression;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// CSVFile読み込み共通クラス
    /// </summary>
    public class CsvReader : ICsvReader
    {
        #region 定数
        /// <summary>
        /// バッファー サイズの規定値：2097152(2MB)
        /// </summary>
        public const int DEFAULT_BUFFER_SIZE = 2097152;

        /// <summary>
        /// 区切り記号の規定値：,(Comma)
        /// </summary>
        public const string DEFAULT_DELIMITER = ",";
        #endregion

        #region フィールド
        /// <summary>
        /// TextFieldParser クラス
        /// </summary>
        private TextFieldParser _parser;

        /// <summary>
        /// System.IO.Stream.解析するストリーム。
        /// </summary>
        private Stream _stream;

        /// <summary>
        /// 解析する文字コード
        /// </summary>
        private Encoding _encoding;

        /// <summary>
        /// バッファー サイズ
        /// </summary>
        private int _bufferSize;

        /// <summary>
        /// Gzip利用可否
        /// </summary>
        private bool _useGzip;
        #endregion

        #region プロパティ
        /// <summary>
        /// コメントトークンを定義します。コメントトークン
        /// である文字列を先頭の行に置くと、その行がコメント
        /// であり、パーサーによって無視されることを示します。
        /// </summary>
        public string[] CommentTokens
        {
            get
            {
                return _parser.CommentTokens;
            }
            set
            {
                _parser.CommentTokens = value;
            }
        }

        /// <summary>
        /// テキストファイルの区切り記号を定義します。
        /// </summary>
        public string[] Delimiters
        {
            get
            {
                return _parser.Delimiters;
            }
            set
            {
                _parser.Delimiters = value;
            }
        }

        /// <summary>
        /// 現在のカーソル位置とファイルの終端との間に、
        /// 空行またはコメント行以外のデータが存在しない場合、True を返します。
        /// </summary>
        /// <value>読み取るデータが残っていない場合は <see langword="true"/>。</value>
        public bool EndOfData
        {
            get
            {
                return _parser.EndOfData;
            }
        }

        /// <summary>
        /// 直前に発生したCSVFormatException 例外の原因となった行を返します。
        /// </summary>
        public string ErrorLine
        {
            get
            {
                return _parser.ErrorLine;
            }
        }

        /// <summary>
        /// 直前の CSVFormatException 例外が発生した行の番号を返します。
        /// </summary>
        public long ErrorLineNumber
        {
            get
            {
                return _parser.ErrorLineNumber;
            }
        }

        /// <summary>
        /// 解析するテキスト ファイルの各列の幅を表します。
        /// </summary>
        public int[] FieldWidths
        {
            get
            {
                return _parser.FieldWidths;
            }
            set
            {
                _parser.FieldWidths = value;
            }
        }

        /// <summary>
        /// 区切り記号入りファイルを解析する場合に、
        /// フィールドが引用符で囲まれているかどうかを示します。
        /// </summary>
        /// <value>現在のフィールドが引用符で囲まれている場合は <see langword="true"/>。</value>
        public bool HasFieldsEnclosedInQuotes
        {
            get
            {
                return _parser.HasFieldsEnclosedInQuotes;
            }
            set
            {
                _parser.HasFieldsEnclosedInQuotes = value;
            }
        }

        /// <summary>
        /// 現在の行番号を返します。ストリームから取り出す文字がなくなった
        /// 場合は-1を返します。
        /// </summary>
        public long LineNumber
        {
            get
            {
                return _parser.LineNumber;
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
                return _parser.TrimWhiteSpace;
            }
            set
            {
                _parser.TrimWhiteSpace = value;
            }
        }

        /// <summary>
        /// フィールドが区切られているか表すフラグ
        /// </summary>
        public bool IsFieldTypeDelimited
        {
            get
            {
                return _parser.TextFieldType == FieldType.Delimited;
            }
        }

        /// <summary>
        /// フィールドが固定長か表すフラグ
        /// </summary>
        public bool IsFieldTypeFixed
        {
            get
            {
                return _parser.TextFieldType == FieldType.FixedWidth;
            }
        }

        /// <summary>
        /// 解析する文字コード
        /// </summary>
        public System.Text.Encoding Encoding
        {
            get
            {
                return _encoding;
            }
        }

        /// <summary>
        /// バッファー サイズ
        /// </summary>
        public int BufferSize
        {
            get
            {
                return _bufferSize;
            }
        }

        /// <summary>
        /// Gzip利用可否
        /// </summary>
        public bool UseGzip
        {
            get
            {
                return _useGzip;
            }
        }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// CSVFileReader クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="stream">解析するストリーム。</param>
        /// <param name="encoding">
        /// System.Text.Encoding.ファイルからエンコーディングを判断できない場合に使用する文字エンコーディング。
        /// 既定値は System.Text.Encoding.Defaultです。
        /// </param>
        /// <param name="useGzip">Gzipファイルの場合true。既定値は falseです。</param>
        public CsvReader(Stream stream, Encoding encoding = null, bool useGzip = false)
        {
            Initialize(stream, encoding ?? Encoding.Default, useGzip);
        }

        /// <summary>
        /// CSVFileReader クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="path">String.解析するファイルの絶対パス。</param>
        /// <param name="encoding">
        /// System.Text.Encoding.ファイルからエンコーディングを判断できない場合に使用する文字エンコーディング。
        /// 既定値は System.Text.Encoding.Defaultです。
        /// </param>
        /// <param name="useGzip">Gzipファイルの場合true。既定値は falseです。</param>
        /// <param name="bufferSize">バッファー サイズ。既定値は DEFAULT_BUFFER_SIZEです。</param>
        /// <exception cref="System.ArgumentNullException">
        /// path が空の文字列であるか、defaultEncoding が Nothing です。
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        /// ファイルが見つかりませんでした。
        /// </exception>
        public CsvReader(string path, Encoding encoding = null, bool useGzip = false, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            Initialize(path, encoding ?? Encoding.Default, useGzip, bufferSize);
        }
        #endregion

        #region メソッド定義
        /// <summary>
        /// CSVFileReaderを既定値に初期化します。
        /// </summary>
        protected virtual void Initialize(string path, Encoding encoding, bool useGzip, int bufferSize)
        {
            _bufferSize = bufferSize;
            Initialize(new BufferedStream(File.OpenRead(path), bufferSize), encoding, useGzip);
        }

        /// <summary>
        /// CSVFileReaderを既定値に初期化します。
        /// </summary>
        protected virtual void Initialize(Stream stream, Encoding encoding, bool useGzip)
        {
            _encoding = encoding;
            _useGzip = useGzip;
            if (useGzip)
            {
                _stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            else
            {
                _stream = stream;
            }
            _parser = new TextFieldParser(_stream, encoding);
            SetDelimiters(DEFAULT_DELIMITER);
            HasFieldsEnclosedInQuotes = true;
            TrimWhiteSpace = false;
        }

        /// <summary>
        /// 現在行のすべてのフィールドを読み込んで文字列の配列として返し、
        /// 次のデータが格納されている行にカーソルを進めます。
        /// </summary>
        /// <returns>現在の行のフィールド値を格納する文字列の配列。</returns>
        /// <exception cref="CSVFormatException">指定された形式を使ってフィールドを解析できません。</exception>
        public string[] ReadFields()
        {
            try
            {
                return _parser.ReadFields();
            }
            catch (MalformedLineException ex)
            {
                throw new CSVFormatException(ex);
            }
        }

        /// <summary>
        /// 1行文字列として読み出します。
        /// </summary>
        /// <returns>現在位置から読み取った 1 行。ストリームの末尾では <see langword="null"/>。</returns>
        public string ReadLine()
        {
            return _parser.ReadLine();
        }

        /// <summary>
        /// 現在のカーソル位置からStreamから全ての文字列を取り出します。
        /// </summary>
        /// <returns>現在位置からストリームの末尾までの文字列。</returns>
        public string ReadToEnd()
        {
            return _parser.ReadToEnd();
        }

        /// <summary>
        /// リーダーの区切り記号を指定された値に設定し、フィールドの種類を Delimited に設定します。
        /// </summary>
        /// <param name="delimiters">String 型の配列。</param>
        /// <exception cref="System.ArgumentException">区切り記号の長さが 0 です。</exception>
        public void SetDelimiters(params string[] delimiters)
        {
            _parser.SetDelimiters(delimiters);
        }

        /// <summary>
        /// 固定長フィールドの場合のフィールド幅を設定する。
        /// </summary>
        /// <param name="fieldWidths">各固定長フィールドの幅。最後の要素には可変長を示す負数を指定できます。</param>
        public void SetFieldWidth(int[] fieldWidths)
        {
            _parser.SetFieldWidths(fieldWidths);
        }

        /// <summary>
        /// フィールドタイプを区切り文字に変更する
        /// </summary>
        public void SetFieldTypeDelimited()
        {
            _parser.TextFieldType = FieldType.Delimited;
        }

        /// <summary>
        /// フィールドタイプを固定幅に変更する
        /// </summary>
        public void SetFieldTypeFixed()
        {
            _parser.TextFieldType = FieldType.FixedWidth;
        }

        #endregion

        #region IDisposableの実装
        /// <summary>
        /// 現在の CSVFileReader オブジェクトを閉じます。
        /// </summary>
        public void Close()
        {
            if (_parser != null)
            {
                _parser.Close();
            }
            if (_stream != null)
            {
                _stream.Close();
            }
        }

        /// <summary>
        /// CSVFileReader オブジェクトによって使用されているリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            Close();
            if (_parser != null)
            {
                _parser.Dispose();
            }
            if (_stream != null)
            {
                _stream.Dispose();
            }
        }
        #endregion
    }
}
