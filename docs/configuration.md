# 構成ファイルと設定

## ジョブを構成するファイル

`-appid MY_JOB -appdirectory C:\Jobs\MyJob` で起動する場合、DTFX は次のファイルを使用します。

```text
C:\Jobs\MyJob\MY_JOB.xml       必須: ジョブ定義
C:\Jobs\MyJob\MY_JOB.config    任意: ジョブ固有の設定と接続文字列
```

共通の既定値は、実行ファイルと同じ場所に生成される `IF.Batch.DTFX.exe.config` にあります。ソースは [`DTFX/app.config`](../DTFX/app.config) です。

## 設定の優先順位

AppSettings は、優先度の高い順に次の値が使われます。

1. コマンドライン引数
2. `{appid}.config` の `<appSettings>`
3. `IF.Batch.DTFX.exe.config` の `<appSettings>`

`appdirectory` はジョブ config を探す前に確定するため例外です。コマンドラインの `-appdirectory`、実行ファイル config の `appdirectory`、実行ファイルのディレクトリの順で決まり、ジョブ config 内の同名設定では変更できません。

接続文字列は実行ファイル config を読み込んだ後、ジョブ config の `<connectionStrings>` をマージします。同じ名前がある場合はジョブ config が優先されます。設定名とコマンドライン引数は共有変数にも登録され、XML から `${key}` で参照できます。

> 相対パスは `appdirectory` ではなく、プロセスのカレントディレクトリを基準に解決されます。タスクスケジューラなどから起動する場合は、作業ディレクトリと `-appdirectory` を明示してください。

## ジョブ config の例

実際の認証情報は、リポジトリへコミットしない安全な設定ファイルまたはシークレット管理基盤から配布してください。

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <connectionStrings>
    <add name="source"
         providerName="System.Data.SqlClient"
         connectionString="Data Source=SERVER;Initial Catalog=SOURCE_DB;Integrated Security=True;" />
    <add name="target"
         providerName="Npgsql"
         connectionString="Server=SERVER;Port=5432;Database=TARGET_DB;User ID=USER;Password=PASSWORD;" />
  </connectionStrings>
  <appSettings>
    <add key="appname" value="User export" />
    <add key="inputdirectory" value="C:\Jobs\MyJob\Input" />
    <add key="outputdirectory" value="C:\Jobs\MyJob\Output" />
    <add key="backupdirectory" value="C:\Jobs\MyJob\Backup" />
    <add key="errordirectory" value="C:\Jobs\MyJob\Error" />
    <add key="encoding" value="UTF-8" />
    <add key="withoutbom" value="true" />
    <add key="trace.templatepath" value="C:\Jobs\MyJob\Log\MY_JOB_%YYYYMMDD%.log" />
  </appSettings>
</configuration>
```

## データベース接続

| データベース | `providerName` | 主な XML 要素 |
|---|---|---|
| SQL Server | `System.Data.SqlClient` | `Sql*`、`SqlServerBulkInsert*` |
| Oracle | `System.Data.OracleClient` | `Oracle*` |
| PostgreSQL | `Npgsql` | `PostgreSql*` |

`dataSource`、`fromDataSource`、`toDataSource` の値には `<connectionStrings>` の `name` を指定します。接続は要素が最初に必要とした時点で開かれ、失敗時は最大 3 回、2 秒間隔で試行されます。

LocalDB 要素は SQL Server の `tempdb` を利用します。使用する場合は、`__TEMPDB__` という名前で `System.Data.SqlClient` の接続文字列を設定してください。接続先には一時テーブルを作成、読み書き、削除できる権限が必要です。

## 主な AppSettings

「標準値」は [`DTFX/app.config`](../DTFX/app.config) の設定、またはキーがない場合のランタイム既定値です。ジョブ config で必要な値だけを上書きできます。

| キー | 標準値 | 説明 |
|---|---:|---|
| `appname` | `appid` | ログに表示するジョブ名。未指定時は `appid` |
| `appdirectory` | `.\` | ジョブ XML と config の検索先。通常はコマンドラインで指定 |
| `inputdirectory` | `.\Data` | `LoadCSV` とファイルベース `ForEach` の既定入力先 |
| `outputdirectory` | `.\Data` | Select の CSV と ZIP の既定出力先 |
| `backupdirectory` | `.\Backup` | 読み込み前の入力ファイル移動先。空にすると移動しない |
| `errordirectory` | `.\Err` | ファイルベース `ForEach` で失敗した行の出力先 |
| `encoding` | `SJIS` | CSV の文字エンコーディング |
| `withoutbom` | `false` | `encoding` が UTF-8 の場合に BOM を付けない |
| `delimiter` | `,` | CSV の列区切り。`\t` などのエスケープ表記を利用可能 |
| `rowdelimiter` | 空 | CSV の行区切り。空の場合はライターの既定値 |
| `alwaysfieldsencloseinquotes` | `true` | 全フィールドを引用符で囲む |
| `trimwhitespace` | `false` | CSV 読み込み時に前後の空白を除去 |
| `usegzip` | `true` | Select の CSV 出力を GZIP 圧縮する。ファイル名へ `.gz` は自動付与されない |
| `skipreadrows` | `0` | CSV 読み込み時に先頭から読み飛ばす行数 |
| `maxreadrows` | `999999999` | `LoadCSV` とファイルベース `ForEach` の最大読み込み行数。`0` 以下は無制限 |
| `maxwriterows` | `10000` | 1 出力ファイルあたりの最大データ行数。`0` 以下は無制限 |
| `nulltext` | 空 | データベースの NULL を CSV へ出力する際の置換文字列 |
| `writeheaders` | `false` | Select の CSV に列名を出力する |
| `alwayscreatefile` | `true` | Select 結果が 0 件でも出力ファイルを作成する |
| `sqlcommandtimeout` | `7200` | SQL と Bulk Copy のタイムアウト秒数 |

`backupdirectory` が設定されている場合、`LoadCSV` とファイルベースの `ForEach` は対象ファイルをバックアップ先へ移動してから読み込みます。コピーではないため、入力ディレクトリからファイルがなくなることに注意してください。

`backupdirectory` と `errordirectory` は起動時に必要に応じて作成されますが、`inputdirectory` と `outputdirectory` は自動作成されません。運用前に作成してください。

## ログ設定

| キー | 標準値 | 説明 |
|---|---:|---|
| `trace.templatepath` | `.\DTFX_%YYYYMMDD%.log` | ログファイルパス。空の場合はログを出力しない |
| `trace.level` | `Information` | `All`、`Off`、`Critical`、`Error`、`Warning`、`Information`、`Verbose` |
| `trace.autoflush` | `true` | 書き込みごとにフラッシュする |
| `trace.maxsize` | 10 MiB | ローテーションするファイルサイズ（バイト） |
| `trace.encoding` | OS 既定 | ログの文字エンコーディング |
| `trace.buffersize` | 8192 | ログストリームのバッファサイズ |
| `trace.usegzip` | `false` | ローテーションしたログを GZIP 圧縮する |
| `trace.append` | `false` | 既存ファイルへ追記する |

ファイルログの出力エンジンには Serilog を使用します。`trace.templatepath`、`trace.level`、`trace.autoflush`、`trace.maxsize`、`trace.encoding`、`trace.append` と CSV 形式は互換性のため維持され、ファイルサイズを超えた場合は Serilog によりローテーションされます。`trace.buffersize` と `trace.usegzip` は Serilog ファイル sink では使用されません。

`trace.erroreventid`、`trace.eventsource`、`trace.errorevententrytype` も構成できますが、通常のファイルログ設定では上表のキーを使用します。

## 共有変数

すべての AppSettings とコマンドライン引数は、起動時に共有変数へ登録されます。各 Executor が生成した結果も `toVariable` や `var` で共有変数へ保存できます。

```xml
<TraceLog eventType="information">Output: ${outputdirectory}</TraceLog>
<If test="${retry_count} &gt; 0 &amp;&amp; '${job_status}' != 'CANCELLED'">
  <TraceLog eventType="warning">Retry is enabled.</TraceLog>
</If>
```

`If` の `test` は共有変数を展開してから JEXL 式として評価します。数値は `${retry_count} &gt; 0`、文字列は `'${job_status}' != 'CANCELLED'` のように記述します。JEXL の詳細と XML エスケープ規則は [`xml-elements.md`](xml-elements.md#if) を参照してください。

配列、辞書、オブジェクトの値は `${rows[0]}`、`${row['COLUMN']}`、`${item.Property}` のように参照できます。存在しない単純な変数は置換されず、元の `${name}` が残ります。

CSV を共有変数へ読み込み、配列として反復する実行可能な例は [`examples/csv-pipeline`](../examples/csv-pipeline/) を参照してください。
