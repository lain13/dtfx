/************************************************************************
* ファイル名:	DataTransferContext.cs
* 概要: アプリケーション共有コンテキスト。DB接続・トランザクション・共有変数を一元管理する
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*   25.1-001-02     2013/10/07  姜　恵遠    レコード間の区切り文字(改行文字)追加
*	25.1-001-03		2013/10/10	姜　恵遠	一時テーブル物理削除処理削除
*	27.2-001-01		2015/06/23	朴　眞秀	Mantis:0072130対応
*   21.3-001-01     2021/07/05  姜　恵遠    Biz-A Step1.5対応
*
*************************************************************************/
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

        // 21.3-001-01 ADD START
        /// <summary>
        /// BOMなし(UTF-8文字コードのみ)
        /// </summary>
        public bool WithoutBom { get; set; }
        // 21.3-001-01 ADD END

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

        public DataTransferContext()
            : this(new TraceLogger())
        {
        }

        public DataTransferContext(ITraceLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        public void AddConnectionStringSettings(ConnectionStringSettings settings)
        {
            _connectionStrings.AddOrUpdate(settings.Name, settings, (key, current) => settings);
        }

        /// <summary>
        /// 一時DBを取得する
        /// </summary>
        /// <returns></returns>
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
        /// <returns>DB接続</returns>
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
        /// <returns>DB接続</returns>
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
        /// Transactionをコミットする
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
        /// Transactionをロールバックする
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
                    // 一時DBの場合はTransactionを利用しない
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
                    // 27.2-001-01 ADD START
                    break;
                    // 27.2-001-01 ADD END
                }
                catch
                {
                    if (i < 2)
                    {
                        // 2秒待機
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
                    // 27.2-001-01 ADD START
                    break;
                    // 27.2-001-01 ADD END
                }
                catch
                {
                    if (i < 2)
                    {
                        // 2秒待機
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
                        // 2秒待機
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
        /// CommitAllTransactions
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
        /// RollbackAllTransactions
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
        /// DisposeTransactions
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
        /// DisposeConnections
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
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _localDB = null;
            DisposeTransactions(true);
            DisposeConnections();
        }
    }
}
