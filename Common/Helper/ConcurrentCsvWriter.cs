/************************************************************************
* ファイル名:	ConcurrentCsvWriter.cs
* 概要:CSVFile書き込み共通クラス
*
* 履歴:
*	バージョン		日付		作成者		内容
*	24.1-001-01		2013/08/02	姜　恵遠	新規作成
*   25.1-001-02     2013/10/07  姜　恵遠    NewLine⇒RowDelimiterに変更
*   21.3-001-01     2021/07/05  姜　恵遠    Biz-A Step1.5対応
*
*************************************************************************/
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
    public class ConcurrentCsvWriter : ICsvWriter
    {

        #region フィールド
        protected CsvWriter _writer;
        protected CsvFormatter _formatter;
        private long _maxWriteRows = long.MaxValue;
        private string _path = null;
        private Encoding _encoding = null;
        private bool _useGzip = false;
        private bool _append = false;
        private bool _alwaysCreateFile = false;
        // 21.3-001-01 ADD START
        private string[] _headerStrings = null;
        // 21.3-001-01 ADD END
        private List<string> _writedFiles = new List<string>();
        private object _syncObject = new object();
        private bool _rollbackFile = false;
        #endregion

        #region プロパティ
        /// <summary>
        /// 直前の行の番号を返します。
        /// </summary>
        public long LineNumber
        {
            get
            {
                return _writer == null ? 0L : _writer.LineNumber;
            }
        }

        /// <summary>
        /// 直前に発生したCSVFormatException 例外の原因となった行を返します。
        /// </summary>
        public string ErrorLine
        {
            get
            {
                return _writer == null ? null : _writer.ErrorLine;
            }
        }

        /// <summary>
        /// 直前の CSVFormatException 例外が発生した行の番号を返します。
        /// </summary>
        public long ErrorLineNumber
        {
            get
            {
                return _writer == null ? 0L : _writer.ErrorLineNumber;
            }
        }

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
                if (_writer != null)
                {
                    _writer.Formatter = value;
                }
            }
        }

        // 21.3-001-01 ADD START
        /// <summary>
        /// ヘッダ文字列
        /// </summary>
        public virtual string[] HeaderStrings
        {
            get
            {
                return _headerStrings;
            }
            set
            {
                _headerStrings = value;
            }
        }
        
        /// <summary>
        /// ヘッダ文字列が存在するかどうか
        /// </summary>
        public virtual bool HasHeaderStrings
        {
        	get
        	{
        		return _headerStrings != null && _headerStrings.Length != 0;
        	}
        }
        // 21.3-001-01 ADD END
        
        /// <summary>
        /// 区切り記号入りファイルに出力する場合に、
        /// 常にフィールドを引用符で囲むかどうかを示します。
        /// </summary>
        /// <returns>常にフィールドを引用符で囲む場合は True。それ以外の場合は False。</returns>
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
        /// <returns>フィールド値から前後の空白をトリムする場合は True。それ以外の場合は False。</returns>
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
        /// 改行文字を示します。
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

        /// <summary>
        /// 出力されたファイルのパスを返却します。
        /// </summary>
        public string[] WritedFiles
        {
            get
            {
                return _writedFiles.ToArray();
            }
        }

        /// <summary>
        /// 出力されたファイルを全て削除するか示します。
        /// </summary>
        public bool RollbackFile
        {
            get
            {
                return _rollbackFile;
            }
            set
            {
                _rollbackFile = value;
            }
        }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// CSVFileWriter クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="path">String.解析するファイルの絶対パス。</param>
        /// <param name="encoding">
        /// System.Text.Encoding.ファイルからエンコーディングを判断できない場合に使用する文字エンコーディング。
        /// 既定値は System.Text.Encoding.Defaultです。
        /// </param>
        /// <param name="alwaysCreateFile">常にファイルを作成する場合true。既定値は falseです。</param>
        /// <param name="useGzip">Gzipファイルの場合true。既定値は falseです。</param>
        /// <param name="maxWriteRows">最大出力レコード。この値を超えると新しいファイルに出力します。0以下の場合は無制限です。</param>
        /// <param name="headerStrings">ヘッダ文字列の配列</param>
        /// <exception cref="System.ArgumentNullException">
        /// path が空の文字列であるか、defaultEncoding が Nothing です。
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        /// ファイルが見つかりませんでした。
        /// </exception>
        public ConcurrentCsvWriter(string path, Encoding encoding = null, bool alwaysCreateFile = false, bool useGzip = false, long maxWriteRows = 0, string[] headerStrings = null)
        {
            this._path = path;
            this._encoding = encoding ?? Encoding.Default;
            this._alwaysCreateFile = alwaysCreateFile;
            this._useGzip = useGzip;
            this._maxWriteRows = maxWriteRows < 1 ? long.MaxValue : maxWriteRows;
            this._headerStrings = headerStrings;
        }
        #endregion

        #region メソッド定義
        /// <summary>
        /// 最大出力
        /// </summary>
        protected virtual void WriteCheck()
        {
            if (_writer == null)
            {
                _writer = new CsvWriter(this._path, this._encoding, this._append, this._useGzip);
                if (_formatter != null)
                {
                    _writer.Formatter = _formatter;
                }
                else
                {
                    _formatter = _writer.Formatter;
                }
                // ヘッダを出力
                if (this.HasHeaderStrings)
                {
                    _writer.WriteLine(this._headerStrings);
                }
                _writedFiles.Add(this._path);
            }
            else if ((this.HasHeaderStrings && this._maxWriteRows + 1 <= _writer.LineNumber) || (!this.HasHeaderStrings && this._maxWriteRows <= _writer.LineNumber))
            {
                _writer.Flush();
                _writer.Dispose();
                string newpath = FileHelper.NextFileName(this._path);
                _writer = new CsvWriter(newpath, this._encoding, this._append, this._useGzip);
                if (_formatter != null)
                {
                    _writer.Formatter = _formatter;
                }
                else
                {
                    _formatter = _writer.Formatter;
                }
                // ヘッダを出力
                if (this.HasHeaderStrings)
                {
                    _writer.WriteLine(this._headerStrings);
                }
                _writedFiles.Add(newpath);
            }
        }

        /// <summary>
        /// 現在行のすべてのフィールドを書き込みます。
        /// </summary>
        /// <param name="fields">現在の行のフィールド値を格納する文字列の配列。</param>
        public virtual void Write(string[] fields, bool newLine = false)
        {
            lock (_syncObject)
            {
                WriteCheck();
                _writer.Write(fields, newLine);
            }
        }

        /// <summary>
        /// 現在行のすべてのフィールドを書き込みます。
        /// </summary>
        /// <param name="tokens">現在の行のフィールド値を格納する文字列の配列。</param>
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
            lock (_syncObject)
            {
                WriteCheck();
                _writer.Write(tokens, newLine);
            }
        }

        /// <summary>
        /// 現在行のすべてのフィールドを書き込みます。
        /// </summary>
        /// <param name="tokens">現在の行のフィールド値を格納する配列。</param>
        public void WriteLine(IEnumerable<object> fields)
        {
            Write(fields);
        }

        #endregion

        #region ストリームのフラッシュとクローズ
        /// <summary>
        /// ストリームをフラッシュする。
        /// </summary>
        public virtual void Flush()
        {
            lock (_syncObject)
            {
                if (_writer != null)
                {
                    _writer.Flush();
                }
            }
        }

        /// <summary>
        /// 現在の CSVFileWriter オブジェクトを閉じます。
        /// </summary>
        public virtual void Close()
        {
            lock (_syncObject)
            {
                if (_writer != null)
                {
                    _writer.Close();
                }
            }
        }
        #endregion

        #region IDisposable の実装
        /// <summary>
        /// CSVFileWriter オブジェクトによって使用されているリソースを解放します。
        /// </summary>
        public virtual void Dispose()
        {
            lock (_syncObject)
            {
                // 常にファイルを作成する場合
                if (!this._rollbackFile && this._alwaysCreateFile)
                {
                    WriteCheck();
                }
                if (_writer != null)
                {
                    _writer.Dispose();
                    _writer = null;
                }
                if (_rollbackFile)
                {
                    DeleteAllWritedFiles();
                }
            }
        }
        #endregion

        /// <summary>
        /// 出力されたファイルを全て削除します。
        /// </summary>
        private void DeleteAllWritedFiles()
        {
            foreach (string file in WritedFiles)
            {
                if(File.Exists(file))
                {
                    File.Delete(file);
                    _writedFiles.Remove(file);
                }
            }
        }
    }
}
