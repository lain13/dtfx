using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.DTFX.Service;
using IF.Batch.DTFX.Helper;
using C = IF.Batch.Common.Configuration.AppConfigConstants;
using System.Configuration;
using System.Xml.Linq;
using System.Data.SqlClient;
using System.Data.Common;
using System.Collections.Concurrent;
using IF.Batch.Common.Service;
using IF.Batch.Common.Diagnostics;
using System.Reflection;
using IF.Batch.DTFX.Exceptions;
using Oracle.ManagedDataAccess.Client;
using Npgsql;

namespace IF.Batch.DTFX.Service
{
    /// <summary>
    /// 1 回のジョブ実行で共有する設定、変数、データベース接続、トランザクションを管理します。
    /// </summary>
    public class DataTransferContext : IServiceContext, IDisposable
    {
        /// <summary>
        /// アプリケーションID
        /// </summary>
        public string Appid { get; set; }

        /// <summary>
        /// アプリケーション名
        /// </summary>
        public string Appname { get; set; }

        /// <summary>
        /// アプリケーションパス
        /// </summary>
        public string Appdirectory { get; set; }

        /// <summary>
        /// 入力・出力ファイル名
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// 入力・出力ファイルの文字コード
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// 区切り記号
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// レコード間の区切り文字(改行文字)
        /// </summary>
        public string RowDelimiter { get; set; }

        /// <summary>
        /// 区切り記号入りファイルに出力する場合に、
        /// 常にフィールドを引用符で囲むかどうかを示します。
        /// </summary>
        public bool AlwaysFieldsEncloseInQuotes { get; set; }

        /// <summary>
        /// フィールド値から前後の空白をトリムするかどうかを示します。
        /// </summary>
        public bool TrimWhiteSpace { get; set; }

        /// <summary>
        /// GZIP圧縮可否
        /// </summary>
        public bool UseGzip { get; set; }

        /// <summary>
        /// 入力フォルダ
        /// </summary>
        public string InputDirectory { get; set; }

        /// <summary>
        /// 出力フォルダ
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// バックアップフォルダ
        /// </summary>
        public string BackupDirectory { get; set; }

        /// <summary>
        /// エラーフォルダ
        /// </summary>
        public string ErrorDirectory { get; set; }

        /// <summary>
        /// ファイル読み飛ばし行数
        /// </summary>
        public long SkipReadRows { get; set; }

        /// <summary>
        /// 最大読み込みレコード数
        /// </summary>
        public long MaxReadRows { get; set; }

        /// <summary>
        /// 最大出力レコード数
        /// </summary>
        public long MaxWriteRows { get; set; }

        /// <summary>
        /// NULL置換テキスト
        /// </summary>
        public string NullText { get; set; }

        /// <summary>
        /// ヘッダ出力可否
        /// </summary>
        public bool WriteHeaders { get; set; }

        /// <summary>
        /// 常にファイルを作成する
        /// </summary>
        public bool AlwaysCreateFile { get; set; }

        /// <summary>
        /// SQLコマンドのタイムアウト
        /// </summary>
        public int SqlCommandTimeout { get; set; }
        /// <summary>
        /// BOMなし(UTF-8文字コードのみ)
        /// </summary>
        public bool WithoutBom { get; set; }
        /// <summary>
        /// ルートXML要素
        /// </summary>
        public XElement RootElement { get; set; }

        /// <summary>
        /// アプリケーション共有変数
        /// </summary>
        public readonly SharedVariable SharedVariable = new SharedVariable();

        private readonly ConcurrentDictionary<string, ConnectionStringSettings> _connectionStrings = new ConcurrentDictionary<string, ConnectionStringSettings>();
        private readonly ConcurrentDictionary<string, DbConnection> _dataSources = new ConcurrentDictionary<string, DbConnection>();
        private readonly ConcurrentDictionary<string, DbTransaction> _transactions = new ConcurrentDictionary<string, DbTransaction>();
        private readonly ITraceLogger _logger;
        private LocalDBHelper _localDB = null;

        /// <summary>
        /// 一時DB接続先
        /// </summary>
        private const string __TEMPDB__ = "__TEMPDB__";

        /// <summary>
        /// 標準ロガーを使用するコンテキストを生成します。
        /// </summary>
        public DataTransferContext()
            : this(new TraceLogger())
        {
        }

        /// <summary>
        /// 指定したロガーを使用するコンテキストを生成します。
        /// </summary>
        /// <param name="logger">接続とトランザクションの状態を記録するロガー。</param>
        public DataTransferContext(ITraceLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        /// <summary>
        /// データソース名に対応する接続設定を登録または置換します。
        /// </summary>
        /// <param name="settings">登録する接続文字列設定。</param>
        public void AddConnectionStringSettings(ConnectionStringSettings settings)
        {
            _connectionStrings.AddOrUpdate(settings.Name, settings, (key, current) => settings);
        }

        /// <summary>
        /// 一時DBを取得する
        /// </summary>
        /// <returns>同じジョブ実行中に共有される LocalDB ヘルパー。</returns>
        public LocalDBHelper GetLocalDB()
        {
            if (_localDB == null)
            {
                _localDB = new LocalDBHelper(
                    GetConnection<SqlConnection>(__TEMPDB__),
                    SqlCommandTimeout,
                    _logger);
            }
            return _localDB;
        }

        /// <summary>
        /// DB接続を取得する
        /// </summary>
        /// <typeparam name="T">DbConnection型</typeparam>
        /// <param name="dataSourceName">DataSource名</param>
        /// <returns>指定した型の開いているデータベース接続。</returns>
        public T GetConnection<T>(string dataSourceName)
        {
            if (string.IsNullOrWhiteSpace(dataSourceName))
            {
                throw new AppConfigurationException("データソース名がNULLです。");
            }
            object obj = null;
            if (_dataSources.ContainsKey(dataSourceName))
            {
                obj = _dataSources[dataSourceName];
                return (T)obj;
            }
            if (_connectionStrings.ContainsKey(dataSourceName))
            {
                obj = CreateDataSource(_connectionStrings[dataSourceName]);
                _dataSources[dataSourceName] = (DbConnection)obj;
                return (T)obj;
            }
            throw new AppConfigurationException(string.Format("データベース接続情報が正しくありません。データソース名：{0}", dataSourceName));
        }

        /// <summary>
        /// Transactionを取得する
        /// </summary>
        /// <typeparam name="T">DbTransaction型</typeparam>
        /// <param name="dataSourceName">DataSource名</param>
        /// <returns>開始済みのトランザクション。登録されていない場合は <see langword="null"/>。</returns>
        public T GetTransaction<T>(string dataSourceName)
        {
            object obj = null;
            if (_transactions.ContainsKey(dataSourceName))
            {
                obj = _transactions[dataSourceName];
            }
            return (T)obj;
        }

        /// <summary>
        /// 指定したトランザクションをコミットし、同じ接続で次のトランザクションを開始します。
        /// </summary>
        /// <param name="dataSourceName">DataSource名</param>
        public void CommitTransaction(string dataSourceName)
        {
            if (_transactions.ContainsKey(dataSourceName))
            {
                DbTransaction oldtran = null;
                _transactions.TryGetValue(dataSourceName, out oldtran);
                if (oldtran == null)
                {
                    return;
                }
                oldtran.Commit();
                oldtran.Dispose();
                DbConnection dataSource = _dataSources[dataSourceName];
                DbTransaction newtran = dataSource.BeginTransaction();
                _transactions.TryRemove(dataSourceName, out oldtran);
                _transactions.TryAdd(dataSourceName, newtran);
            }
        }

        /// <summary>
        /// 指定したトランザクションをロールバックし、同じ接続で次のトランザクションを開始します。
        /// </summary>
        /// <param name="dataSourceName">DataSource名</param>
        public void RollbackTransaction(string dataSourceName)
        {
            if (_transactions.ContainsKey(dataSourceName))
            {
                DbTransaction oldtran = null;
                _transactions.TryGetValue(dataSourceName, out oldtran);
                if (oldtran == null)
                {
                    return;
                }
                oldtran.Rollback();
                oldtran.Dispose();
                DbConnection dataSource = _dataSources[dataSourceName];
                DbTransaction newtran = dataSource.BeginTransaction();
                _transactions.TryRemove(dataSourceName, out oldtran);
                _transactions.TryAdd(dataSourceName, newtran);
            }
        }

        /// <summary>
        /// DataSourceを生成する。
        /// </summary>
        /// <param name="settings">ConnectionStringSettings</param>
        /// <returns>DbConnection</returns>
        private DbConnection CreateDataSource(ConnectionStringSettings settings)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            _logger.WriteInfo(method, "データベースに接続します。データソース名:{0}, プロバイダ名:{1}", settings.Name, settings.ProviderName);
            switch (settings.ProviderName)
            {
                case "System.Data.SqlClient":
                    SqlConnection sqlConn = CreateSqlServerConnection(settings);
                    // tempdb の作業テーブルは接続全体で共有するため、LocalDB 接続にはトランザクションを作成しません。
                    if (__TEMPDB__ == settings.Name)
                    {
                        _transactions.TryAdd(settings.Name, null);
                    }
                    else
                    {
                        _transactions.TryAdd(settings.Name, sqlConn.BeginTransaction());
                    }
                    return sqlConn;
                case "System.Data.OracleClient":
                    OracleConnection oraConn = CreateOracleConnection(settings);
                    _transactions.TryAdd(settings.Name, oraConn.BeginTransaction());
                    return oraConn;
                case "Npgsql":
                    NpgsqlConnection pgConn = CreatePostgreSqlConnection(settings);
                    _transactions.TryAdd(settings.Name, pgConn.BeginTransaction());
                    return pgConn;
                default:
                    throw new AppConfigurationException(string.Format("データベース接続情報が正しくありません。データソース名：{0}、プロバイダー名：{1}", settings.Name, settings.ProviderName));
            }
        }

        /// <summary>
        /// SQLサーバーのデータソースを生成する。
        /// </summary>
        /// <param name="settings">ConnectionStringSettings</param>
        /// <returns>SqlConnection</returns>
        private SqlConnection CreateSqlServerConnection(ConnectionStringSettings settings)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            SqlConnection sqlConn = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    sqlConn = new SqlConnection();
                    sqlConn.ConnectionString = settings.ConnectionString;
                    sqlConn.Open();
                    break;
                }
                catch
                {
                    if (i < 2)
                    {
                        _logger.WriteWarning(method, "データベース接続に失敗しました。データソース名:{0}, プロバイダ名:{1}, 接続回数:{2}", settings.Name, settings.ProviderName, i);
                        System.Threading.Thread.Sleep(2000);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return sqlConn;
        }

        /// <summary>
        /// Oracleサーバーのデータソースを生成する。
        /// </summary>
        /// <param name="settings">ConnectionStringSettings</param>
        /// <returns>OracleConnection</returns>
        private OracleConnection CreateOracleConnection(ConnectionStringSettings settings)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            OracleConnection oraConn = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    oraConn = new OracleConnection();
                    oraConn.ConnectionString = settings.ConnectionString;
                    oraConn.Open();
                    break;
                }
                catch
                {
                    if (i < 2)
                    {
                        _logger.WriteWarning(method, "データベース接続に失敗しました。データソース名:{0}, プロバイダ名:{1}, 接続回数:{2}", settings.Name, settings.ProviderName, i);
                        System.Threading.Thread.Sleep(2000);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return oraConn;
        }

        /// <summary>
        /// PostgreSqlサーバーのデータソースを生成する。
        /// </summary>
        /// <param name="settings">ConnectionStringSettings</param>
        /// <returns>NpgsqlConnection</returns>
        private NpgsqlConnection CreatePostgreSqlConnection(ConnectionStringSettings settings)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            NpgsqlConnection sqlConn = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    sqlConn = new NpgsqlConnection();
                    sqlConn.ConnectionString = settings.ConnectionString;
                    sqlConn.Open();
                    break;
                }
                catch
                {
                    if (i < 2)
                    {
                        _logger.WriteWarning(method, "データベース接続に失敗しました。データソース名:{0}, プロバイダ名:{1}, 接続回数:{2}", settings.Name, settings.ProviderName, i);
                        System.Threading.Thread.Sleep(2000);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return sqlConn;
        }

        /// <summary>
        /// 登録されているすべてのトランザクションをコミットし、各接続で次のトランザクションを開始します。
        /// </summary>
        public void CommitAllTransactions()
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var keys = _transactions.Keys.ToArray();
            foreach (string key in keys)
            {
                CommitTransaction(key);
            }
            _logger.WriteDebug(method, "コミットしました。");
        }

        /// <summary>
        /// 登録されているすべてのトランザクションをロールバックし、各接続で次のトランザクションを開始します。
        /// </summary>
        public void RollbackAllTransactions()
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var keys = _transactions.Keys.ToArray();
            foreach (string key in keys)
            {
                RollbackTransaction(key);
            }
            _logger.WriteDebug(method, "ロールバックしました。");
        }

        /// <summary>
        /// 登録されているトランザクションを確定して破棄します。新しいトランザクションは開始しません。
        /// </summary>
        /// <param name="commit">コミット可否</param>
        public void DisposeTransactions(bool commit = true)
        {
            var keys = _transactions.Keys.ToArray();
            foreach (string key in keys)
            {
                DbTransaction tran = null;
                _transactions.TryGetValue(key, out tran);
                if (tran != null)
                {
                    if (commit)
                    {
                        tran.Commit();
                    }
                    else
                    {
                        tran.Rollback();
                    }
                    tran.Dispose();
                }
                _transactions.TryRemove(key, out tran);
            }
        }

        /// <summary>
        /// 開いているすべてのデータベース接続を破棄します。
        /// </summary>
        public void DisposeConnections()
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var keys = _dataSources.Keys.ToArray();
            foreach (string key in keys)
            {
                DbConnection dataSource = null;
                _dataSources.TryRemove(key, out dataSource);
                if (dataSource != null)
                {
                    _logger.WriteInfo(method, "データベース接続を解除します。データソース名:{0}", key);
                    dataSource.Dispose();
                }
            }
        }

        /// <summary>
        /// 未確定のトランザクションをコミットしてから接続を破棄します。
        /// </summary>
        public void Dispose()
        {
            _localDB = null;
            DisposeTransactions(true);
            DisposeConnections();
        }
    }
}
