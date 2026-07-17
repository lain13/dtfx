# XML 要素リファレンス

## 基本構造

ジョブ定義は `<Application>` をルートとし、子要素を上から順に実行します。要素名と属性名は大文字と小文字を区別します。

```xml
<?xml version="1.0" encoding="utf-8"?>
<Application xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
             xsi:noNamespaceSchemaLocation="../../DTFX/XMLSchema/Application.xsd">
  <TraceLog eventType="information">Start</TraceLog>
  <!-- 処理を記述 -->
  <AppExit result="0">Done</AppExit>
</Application>
```

本文と多くの属性では、`${variable}` 形式の共有変数を利用できます。XML 内の `&`、`<`、`>` はエスケープするか、SQL とコマンドの本文を CDATA で囲んでください。

完全な検証規則は [`Application.xsd`](../DTFX/XMLSchema/Application.xsd)、設定と接続文字列は [`configuration.md`](configuration.md) を参照してください。ランタイムは XSD 検証を強制しないため、CI または XML エディターでスキーマ検証を行うことを推奨します。

## Select と SelectScalar

| 対象 | 複数行 Select | 単一値 Select |
|---|---|---|
| SQL Server | `SqlSelect` | `SqlSelectScalar` |
| Oracle | `OracleSelect` | `OracleSelectScalar` |
| PostgreSQL | `PostgreSqlSelect` | `PostgreSqlSelectScalar` |
| LocalDB | `LocalDBSelect` | `LocalDBSelectScalar` |

Select 要素の本文に SELECT 文を記述します。SQL Server、Oracle、PostgreSQL では `dataSource` が必須です。LocalDB は `__TEMPDB__` 接続を使うため `dataSource` を指定しません。

複数行 Select の出力属性は次のとおりです。

| 属性 | 説明 |
|---|---|
| `toFile` | 結果を CSV へ出力。ディレクトリを含まない場合は `outputdirectory` を使用。`usegzip=true` でも `.gz` は自動付与されない |
| `headerString` | CSV の先頭に固定ヘッダー行を出力。`writeheaders=true` より優先 |
| `trailerString` | CSV の末尾に固定トレーラー行を出力 |
| `toTable` | 結果から LocalDB の一時テーブルを作成し、物理テーブル名を同名の共有変数へ保存 |
| `toVariable` | 行を「列名から値への辞書」のリストとして共有変数へ保存 |
| `transaction` | SQL Server、Oracle、PostgreSQL のみ。実行後に `commit`、`rollback`、`none` のいずれかを適用。既定は `none` |

`toTable`、`toFile`、`toVariable` は最低 1 つ必要です。複数を指定した場合は `toTable`、`toFile`、`toVariable` の順に 1 つだけ実行されるため、通常は 1 つだけ指定してください。

```xml
<SqlSelect dataSource="source" toFile="users.csv.gz" transaction="none"><![CDATA[
  SELECT ID, NAME FROM USERS ORDER BY ID
]]></SqlSelect>

<OracleSelect dataSource="oracle_source" toVariable="users"><![CDATA[
  SELECT ID, NAME FROM USERS
]]></OracleSelect>
```

SelectScalar は `toVariable` が必須で、最初の行の最初の列を保存します。結果がデータベースの NULL の場合は null を保存します。

```xml
<PostgreSqlSelectScalar dataSource="pg_source" toVariable="user_count">
  SELECT COUNT(*) FROM users
</PostgreSqlSelectScalar>
```

## Insert、Update、Delete

| 対象 | Insert | Update | Delete |
|---|---|---|---|
| SQL Server | `SqlInsert` | `SqlUpdate` | `SqlDelete` |
| Oracle | `OracleInsert` | `OracleUpdate` | `OracleDelete` |
| PostgreSQL | `PostgreSqlInsert` | `PostgreSqlUpdate` | `PostgreSqlDelete` |
| LocalDB | `LocalDBInsert` | `LocalDBUpdate` | `LocalDBDelete` |

要素の本文に SQL を記述します。外部データベースでは `dataSource` が必須です。任意の `toVariable` には影響行数を保存します。外部データベースの要素では `transaction="commit|rollback|none"` を指定でき、既定は `none` です。

```xml
<SqlUpdate dataSource="target" toVariable="updated" transaction="commit"><![CDATA[
  UPDATE USERS SET EXPORTED = 1 WHERE EXPORTED = 0
]]></SqlUpdate>
```

## データベース間 Bulk Insert

| 要素 | 転送元 | 転送先 |
|---|---|---|
| `SqlServerBulkInsertFromSqlServer` | SQL Server | SQL Server |
| `SqlServerBulkInsertFromOracle` | Oracle | SQL Server |
| `OracleBulkInsertFromSqlServer` | SQL Server | Oracle |
| `OracleBulkInsertFromOracle` | Oracle | Oracle |
| `PostgreSqlBulkInsertFromSqlServer` | SQL Server | PostgreSQL |
| `PostgreSqlBulkInsertFromOracle` | Oracle | PostgreSQL |

共通の必須属性は `fromDataSource`、`toDataSource`、`toTable` です。本文の SELECT 結果を、列順のまま転送先テーブルへ書き込みます。転送元と転送先には異なるデータソース名を指定してください。

```xml
<PostgreSqlBulkInsertFromSqlServer
    fromDataSource="mssql_source"
    toDataSource="pg_target"
    toTable="users">
  SELECT ID, NAME FROM USERS
</PostgreSqlBulkInsertFromSqlServer>
```

スキーマ互換性のため `toVariable` 属性も受け付けますが、現在の Bulk Insert Executor は値を保存しません。列名のマッピング機能はないため、SELECT の列数、順序、型を転送先テーブルに合わせてください。

## If

`If` は `test` の式を JEXL (JavaScript Expression Language) として評価します。評価結果を文字列化した値が `true` の場合だけ子要素を実行するため、通常は比較式または論理式で真偽値を返してください。`1` など、文字列化した値が `true` にならない結果では子要素を実行しません。任意の `toVariable` を指定すると、評価結果を文字列として共有変数へ保存します。

| 属性 | 必須 | 説明 |
|---|---:|---|
| `test` | はい | 評価する JEXL 式。共有変数は評価前に `${name}` 形式で展開される |
| `toVariable` | いいえ | `true`、`false`、数値、文字列などの評価結果を保存する共有変数名 |

主に次の式を使用できます。

| 種類 | 構文例 |
|---|---|
| 数値・文字列・真偽値 | `10`, `3.14`, `'READY'`, `true`, `false` |
| 算術演算 | `+`, `-`, `*`, `/`, `%` |
| 比較 | `==`, `!=`, `>`, `>=`, `<`, `<=` |
| 論理演算 | `&&`, `||`, `!` |
| 条件演算 | `condition ? value1 : value2` |

```xml
<If test="${user_count} &gt; 0 &amp;&amp; ${error_count} == 0" toVariable="can_continue">
  <TraceLog eventType="information">Processing can continue: ${can_continue}.</TraceLog>
</If>

<If test="'${job_status}' == 'READY' || '${job_status}' == 'RETRY'">
  <TraceLog eventType="information">The job is ready.</TraceLog>
</If>
```

共有変数を数値として比較する場合は `${count} &gt; 0`、文字列として比較する場合は `'${status}' == 'READY'` のように記述します。XML 属性内では `&&` を `&amp;&amp;`、`<` を `&lt;` とエスケープしてください。`<=` は `&lt;=` と記述します。

JEXL はスクリプトではなく式だけを評価します。従来の JScript 固有の文、オブジェクト生成、.NET API 呼び出しは使用できません。

## ForEach

`ForEach` はファイル、LocalDB テーブル、共有変数のいずれかを列挙し、各項目について子要素を実行します。

| 属性 | 必須 | 説明 |
|---|---:|---|
| `var` | はい | 現在の項目を保存する共有変数名 |
| `fromFile` | 選択 | CSV / GZIP のファイル名または検索パターン |
| `fromTable` | 選択 | LocalDB テーブル名 |
| `fromVariable` | 選択 | 列挙可能な共有変数名 |
| `transaction` | いいえ | 正常または警告時の全トランザクション制御。既定は `commit` |
| `transactionOnError` | いいえ | エラー時の全トランザクション制御。既定は `rollback` |
| `stopOnError` | いいえ | `true` なら最初の例外でループを停止。既定は `false` |

3 つの入力属性のうち、必ず 1 つだけ指定してください。複数指定時の実行優先順位は `fromFile`、`fromTable`、`fromVariable` です。

ファイル入力では、現在行を文字列配列として `${row[0]}`、`${row[1]}` のように参照します。`backupdirectory` が設定されていると、入力ファイルは読み込み前にバックアップ先へ移動されます。

```xml
<ForEach var="row" fromFile="users*.csv" stopOnError="true">
  <TraceLog eventType="information">ID=${row[0]}, NAME=${row[1]}</TraceLog>
</ForEach>
```

Select の `toVariable` を列挙する場合、各行は辞書なので `${row['ID']}` の形式で列を参照できます。

## LoadCSV

`LoadCSV` は CSV または GZIP ファイルを読み込み、共有変数または LocalDB テーブルへ保存します。

| 属性 | 必須 | 説明 |
|---|---:|---|
| `fromFile` | はい | ファイル名または検索パターン。パスがなければ `inputdirectory` を使用 |
| `toVariable` | 選択 | 行を文字列配列のリストとして保存 |
| `toTable` | 選択 | LocalDB テーブルへ保存 |
| `hasHeaders` | いいえ | 先頭行を列名として扱う。既定は `false` |

`toTable` と `toVariable` のどちらかを指定してください。両方を指定した場合は `toTable` が優先されます。`skipreadrows` はヘッダーを読む前に適用され、`maxreadrows` はデータ行数を制限します。`backupdirectory` が設定されている場合は、読み込み前に入力ファイルを移動します。

```xml
<LoadCSV fromFile="users.csv" hasHeaders="true" toTable="users_work" />
```

`toVariable` と `ForEach@fromVariable` を組み合わせたデータベース不要の例は [`examples/csv-pipeline`](../examples/csv-pipeline/) で実行できます。

## TraceLog と AppExit

`TraceLog` の本文をログへ出力します。

| 属性 | 説明 |
|---|---|
| `eventType` | `information`、`error`、`warning`、`verbose`、`off`。明示指定を推奨 |
| `toVariable` | 展開後のメッセージを共有変数へ保存 |

XSD 上の `eventType` 既定値は `information` ですが、ランタイムは XSD の既定値を適用しないため、ログを確実に出力するには属性を明示してください。

`AppExit` は以降の処理を中止し、`result="0|1|2"` をプロセスの終了コードとして返します。本文があれば情報ログへ出力します。既定の結果は `0` です。

```xml
<TraceLog eventType="warning">No input file.</TraceLog>
<AppExit result="2">Completed with warnings.</AppExit>
```

## ExecuteCommand

`ExecuteCommand` は本文を `cmd /c` で実行します。終了コード `0` は成功、それ以外は DTFX のエラー結果になります。

| 属性 | 説明 |
|---|---|
| `toVariable` | 標準出力を共有変数へ保存 |
| `traceLog` | コマンド、終了コード、標準出力、標準エラーのログレベル。値は `TraceLog` の `eventType` と同じで、既定は `off` |

外部コマンドの終了コードは常に共有変数 `${exitcode}` にも保存されます。

```xml
<ExecuteCommand toVariable="output" traceLog="information"><![CDATA[
  where.exe robocopy
]]></ExecuteCommand>
```

この要素は任意の Windows コマンドを実行できます。ジョブ XML の編集権限を厳しく制限し、値に信頼できない入力を連結しないでください。

## ZipArchive と AddFile

`ZipArchive` は、子の `AddFile` に一致したファイルを ZIP にまとめます。

| 要素・属性 | 必須 | 説明 |
|---|---:|---|
| `ZipArchive@filename` | はい | ZIP の出力ファイル名。パスがなければ `outputdirectory` を使用 |
| `ZipArchive@password` | いいえ | WinZip AES-256 のパスワード |
| `ZipArchive@overwrite` | いいえ | `true` なら既存 ZIP を上書き。既定は `false` |
| `AddFile@filenamePattern` | はい | 追加するファイルのパスまたは検索パターン |
| `AddFile@deletedOnArchived` | いいえ | `true` なら ZIP 保存後に元ファイルを削除。既定は `false` |

`filenamePattern` にはディレクトリを含むパスを指定してください。この要素では `inputdirectory` へのフォールバックは行いません。

```xml
<ZipArchive filename="users.zip" overwrite="true">
  <AddFile filenamePattern="${outputdirectory}\users*.csv.gz"
           deletedOnArchived="false" />
</ZipArchive>
```

パスワード付き ZIP は、秘密情報の安全な保管手段の代替にはなりません。`deletedOnArchived="true"` は元ファイルを削除するため、検索パターンを十分に限定してください。
