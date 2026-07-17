using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Xml;
using System.IO.Compression;
using IF.Batch.Common.Helper;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// 日付ことサイズごとに自動ファイル解決を行う
    /// スレッドセーフなトレースライタクラス
    /// </summary>
    public class ThreadSafeTraceLogWriter : IDisposable, ITraceLogWriter
    {
        #region トレース出力設定
        /// <summary>
        /// トレースの出力先。トレースが有効な場合のみ出力
        /// </summary>
        private string _tracePathTemplate = @"C:\ApLog\Trace\Audit.Plugin_%YYYYMMDD%.log";
        /// <summary>
        /// トレーススイッチ
        /// </summary>
        private SourceSwitch _switch = new SourceSwitch("SecurityLog", SourceLevels.Information.ToString());
        /// <summary>
        /// 出力バッファストリーム
        /// </summary>
        private TextWriter _stream = null;
        /// <summary>
        /// 実際に出力されるストリーム
        /// </summary>
        private Stream _baseStream = null;
        /// <summary>
        /// ファイルのAutoFlush設定
        /// </summary>
        private bool _autoFlush = false;
        /// <summary>
        /// トレースログのエンコーディング
        /// </summary>
        private static Encoding _encoding = Encoding.Default;
        /// <summary>
        /// トレースログのバッファサイズ
        /// 8K
        /// </summary>
        private static int _bufferSize = 8 * 1024;
        /// <summary>
        /// トレースログ切り替えサイズ
        /// トレースログサイズがこのサイズを超過
        /// していた場合、ログファイルを切り替える。
        /// 10MB
        /// </summary>
        private static long _traceMaxSize = 10 * 1024 * 1024;
        /// <summary>
        /// GZIP圧縮利用可否
        /// </summary>
        private static bool _useGzip = false;
        /// <summary>
        /// 既存のファイルに追加
        /// </summary>
        private static bool _append = false;
        /// <summary>
        /// 最後のトレースログを出力した日時
        /// 日付が変わっていた場合ファイルを切り替える
        /// </summary>
        private DateTime _lastTraceDate = DateTime.MinValue;
        /// <summary>
        /// ログファイルリソース管理用ロック
        /// </summary>
        private ReaderWriterLockSlim _traceLock = new ReaderWriterLockSlim();
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ThreadSafeTraceLogWriter() { }
        #endregion

        #region 初期化処理
        /// <summary>
        /// 初期化処理メソッド
        /// </summary>
        /// <param name="config">トレースのコンフィグレーション</param>
        public virtual void Initialize(ITraceLogConfiguration config)
        {
            _switch.Level = config.TraceSourceLevels;
            _tracePathTemplate = config.TracePathTemplate;
            _autoFlush = config.AutoFlush;
            if (config.MaxSize > 0)
            {
                _traceMaxSize = config.MaxSize;
            }
            if (config.BufferSize > 0)
            {
                _bufferSize = config.BufferSize;
            }
            if (config.Encoding != null)
            {
                _encoding = config.Encoding;
            }
            _useGzip = config.UseGzip;
            _append = config.Append;
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_DomainUnload);
        }
        #endregion

        #region IOの開始終了処理メソッド
        /// <summary>
        /// ストリームをクローズする
        /// </summary>
        public virtual void Close()
        {
            if (_stream != null)
            {
                _stream.Flush();
                _stream.Close();
                _stream = null;
            }
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        public virtual void Dispose()
        {
            Close();
        }

        /// <summary>
        /// ストリームを作成する。
        /// </summary>
        /// <param name="append">既存のファイルに追加する場合は <see langword="true"/>。</param>
        private void PopulateStream(bool append)
        {
            // ファイルパスを求める
            string filepath = FileHelper.ResolvePathFromTemplate(_tracePathTemplate, DateTime.Now);
            filepath = append ? FileHelper.LastFileName(filepath) : FileHelper.NextFileName(filepath);

            // ストリームの構築
            Stream baseStream = OpenNewFileStream(filepath);
            Stream newstream = _useGzip ? new GZipStream(baseStream, CompressionMode.Compress) : baseStream;
            StreamWriter sw = new StreamWriter(newstream, _encoding, _bufferSize);
            sw.AutoFlush = _autoFlush;
            // スレッドセーフなラッパー作成
            _stream = TextWriter.Synchronized(sw);
            _baseStream = baseStream;
        }

        /// <summary>
        /// 新しいファイルストリームの構築
        /// </summary>
        /// <param name="filepath">ファイル名</param>
        /// <returns>ストリーム</returns>
        private Stream OpenNewFileStream(string filepath)
        {
            try
            {
                if (File.Exists(filepath) && new FileInfo(filepath).Length >= _traceMaxSize)
                {
                    filepath = FileHelper.NextFileName(filepath);
                }
                return File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.Read);
            }
            catch
            {
                return File.Open(FileHelper.NextFileName(filepath), FileMode.Append, FileAccess.Write, FileShare.Read);
            }
        }

        /// <summary>
        /// アンロードイベントハンドラ
        /// トレースファイルを閉じる。
        /// </summary>
        /// <param name="sender">イベントを発生させたアプリケーションドメイン。</param>
        /// <param name="e">イベントデータ。</param>
        void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            _traceLock.EnterWriteLock();
            try
            {
                Close();
            }
            finally
            {
                _traceLock.ExitWriteLock();
            }
        }
        #endregion

        #region トレース出力処理本体
        /// <summary>
        /// トレース出力を行う。
        /// </summary>
        /// <param name="level">トレースレベル</param>
        /// <param name="trace">トレース</param>
        public void WriteTrace(TraceEventType level, params string[] trace)
        {
            WriteTraceInternal(level, trace);
        }

        /// <summary>
        /// トレース出力を行う。
        /// </summary>
        /// <param name="level">トレースレベル</param>
        /// <param name="trace">トレース</param>
        [Conditional("TRACE")]
        private void WriteTraceInternal(TraceEventType level, params string[] trace)
        {
            if (!_switch.ShouldTrace(level)) return;

            DateTime dt = DateTime.Now;
            SwitchStream(dt);

            _traceLock.EnterReadLock();
            try
            {
                // 書式指定してメッセージをフォーマット
                string message = FormatTraceLog(level, dt, trace);
                _stream.WriteLine(message);
            }
            finally
            {
                _traceLock.ExitReadLock();
            }
        }

        /// <summary>
        /// トレースイベントをファイルへ書き込む文字列に整形します。
        /// </summary>
        /// <param name="traceEventType">トレースイベントの重大度。</param>
        /// <param name="traceDateTime">イベントが発生した日時。</param>
        /// <param name="traces">整形対象のトレースフィールド。</param>
        /// <returns>ファイルへ書き込む整形済み文字列。</returns>
        protected virtual string FormatTraceLog(TraceEventType traceEventType, DateTime traceDateTime, params string[] traces)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var trace in traces)
            {
                string message = string.Format("{0}:{1}:{2}", traceDateTime.ToString("yyyy/MM/dd HH:mm:ss"), traceEventType.ToString(), trace);
                builder.AppendLine(message);
            }
            return builder.ToString();
        }

        /// <summary>
        /// トレースログを切り替える必要がある場合、
        /// ログファイルの切り替え処理を行う。
        /// </summary>
        /// <param name="dt">最新のトレースログ出力日</param>
        /// <returns>true:切り替え発生</returns>
        private bool SwitchStream(DateTime dt)
        {
            _traceLock.EnterUpgradeableReadLock();
            try
            {
                if (_stream == null)
                {
                    _traceLock.EnterWriteLock();
                    try
                    {
                        // ロック取得後再確認
                        if (_stream == null)
                        {
                            _lastTraceDate = dt;
                            Close();
                            PopulateStream(_append);
                            return true;
                        }
                    }
                    finally
                    {
                        _traceLock.ExitWriteLock();
                    }
                }
                else if (_lastTraceDate.ToString("yyyy/MM/dd") != dt.ToString("yyyy/MM/dd"))
                {
                    _traceLock.EnterWriteLock();
                    try
                    {
                        // ロック取得後再確認
                        if (_lastTraceDate.ToString("yyyy/MM/dd") != dt.ToString("yyyy/MM/dd"))
                        {
                            _lastTraceDate = dt;
                            Close();
                            PopulateStream(false);
                            return true;
                        }
                    }
                    finally
                    {
                        _traceLock.ExitWriteLock();
                    }
                }
                else if (_baseStream.Length > _traceMaxSize)
                {
                    _traceLock.EnterWriteLock();
                    try
                    {
                        //　ロック取得後再確認
                        if (_baseStream.Length > _traceMaxSize)
                        {
                            Close();
                            PopulateStream(false);
                            return true;
                        }
                    }
                    finally
                    {
                        _traceLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _traceLock.ExitUpgradeableReadLock();
            }
            return false;
        }
        #endregion

        #region 例外メッセージ出力用ヘルパーメソッド
        /// <summary>
        /// 可能な限りすべてのプロパティデータの出力と、
        /// InnerExceptionのトレースデータを取得するメソッド
        /// </summary>
        /// <param name="ex">トレースデータを取得する例外インスタンス</param>
        /// <returns>トレース用文字列</returns>
        private static string CreateExceptionTrace(Exception ex)
        {
            string template = "{0}:{1}" + Environment.NewLine;
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format(template, "ExceptionType", ex.GetType().ToString()));
            builder.Append(string.Format(template, "ToString()", ex.ToString()));
            // 全てのパブリックプロパティを取得
            PropertyInfo[] properties = ex.GetType().GetProperties();
            foreach (PropertyInfo p in properties)
            {
                // InnerExceptionは最後
                if (p.Name == "InnerException") continue;

                if (p.GetGetMethod() != null)
                {
                    object o = p.GetValue(ex, null);
                    if (o == null) o = "(null)";
                    builder.Append(string.Format(template, p.Name, o.ToString()));
                    if (o is Array)
                    {
                        Array ar = o as Array;
                        foreach (object item in ar)
                        {
                            o = item;
                            if (o == null) o = "(null)";
                            builder.Append(string.Format(template, p.Name, item.ToString()));
                        }
                    }
                    else if (o is XmlNode)
                    {
                        XmlNode node = o as XmlNode;
                        builder.Append(string.Format(template, p.Name, node.InnerText));
                    }
                }
            }
            if (ex.InnerException != null)
            {
                builder.Append(string.Format(template, "InnerException", ex.InnerException.GetType().ToString()));
                builder.Append(CreateExceptionTrace(ex.InnerException));
            }

            return builder.ToString();
        }
        #endregion
    }
}
