# DTFX

**Data Transfer Framework for XML**

DTFX は、XML に定義した処理を上から順に実行し、SQL Server、Oracle、PostgreSQL 間のデータ連携とファイル処理を自動化する Windows 向けコンソール ETL エンジンです。旧プロジェクト名は R7001 で、公開にあわせて DTFX に改称しました。

## 主な機能

- SQL Server、Oracle、PostgreSQL の Select / Insert / Update / Delete
- データベース間の Bulk Insert
- CSV および GZIP の読み書き
- SQL Server の `tempdb` を利用する一時テーブル
- `If`、`ForEach`、`AppExit` による制御フロー
- 外部コマンドの実行、ログ出力、ZIP 作成
- `${variable}` 形式の共有変数展開

## 動作要件

- Windows
- .NET Framework 4.6.2 Developer Pack または Targeting Pack
- Visual Studio、または Build Tools for Visual Studio に含まれる MSBuild
- NuGet CLI

実際のデータベース処理には、対象データベースと接続情報が必要です。Oracle および PostgreSQL のドライバーを含む依存関係は `packages.config` で管理しています。

## ビルド

```bat
NuGet.exe restore IF.Batch.sln
MSBuild.exe IF.Batch.sln /p:Configuration=Release /p:Platform="Any CPU"
```

依存関係を復元済みであれば、次のコマンドでもビルドできます。

```bat
dotnet build IF.Batch.sln --no-restore -c Release -p:Platform="Any CPU"
```

実行ファイルは `DTFX\bin\Release\IF.Batch.DTFX.exe` に生成されます。

## 1 分で試す

Quick Start では、データベース接続を使わずに式の評価、ログ出力、正常終了までの流れを確認できます。リポジトリのルートで次を実行してください。

```bat
examples\quickstart\QUICKSTART.BAT
```

実行ファイルを直接起動する場合は次のとおりです。

```bat
DTFX\bin\Release\IF.Batch.DTFX.exe -appid QUICKSTART -appdirectory examples\quickstart
```

一般的な起動形式は次のとおりです。

```bat
IF.Batch.DTFX.exe -appid <APPID> [-appdirectory <DIRECTORY>]
```

| 引数 | 説明 |
|---|---|
| `appid` | 必須。拡張子を除いたジョブ定義 XML のファイル名 |
| `appdirectory` | `{appid}.xml` と任意の `{appid}.config` を置くディレクトリ |

`appdirectory` を省略すると、まず実行ファイルの構成ファイルにある `appdirectory` が使われます。このリポジトリの既定値は `.\` です。設定も空の場合に限り、実行ファイルのディレクトリへフォールバックします。相対パスはプロセスのカレントディレクトリを基準に解決されるため、運用時は `-appdirectory` を明示することを推奨します。

終了コードは `0` が成功、`1` がエラー、`2` が警告です。`-?` または `-help` を指定した場合と、`-appid` を省略した場合はヘルプを表示して `0` で終了します。

## ジョブ設定

ジョブごとに、同じベース名の XML と任意の config を用意します。

```text
MY_JOB.xml
MY_JOB.config
```

`MY_JOB.config` には接続文字列、入出力ディレクトリ、CSV 形式、ログなどを指定できます。コマンドラインの値はジョブ config より優先され、ジョブ config は実行ファイルの config より優先されます。詳細と安全な設定例は [`docs/configuration.md`](docs/configuration.md) を参照してください。

## XML の例

```xml
<?xml version="1.0" encoding="utf-8"?>
<Application xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
             xsi:noNamespaceSchemaLocation="../../DTFX/XMLSchema/Application.xsd">
  <TraceLog eventType="information">Job started.</TraceLog>
  <SqlSelect dataSource="source" toFile="users.csv.gz"><![CDATA[
    SELECT * FROM USERS
  ]]></SqlSelect>
  <AppExit result="0">Job completed.</AppExit>
</Application>
```

利用できる要素と属性は [`docs/xml-elements.md`](docs/xml-elements.md) にまとめています。完全な要素例は [`SAMPLE_APP.XML`](DTFX/01_%E3%83%90%E3%83%83%E3%83%81%E4%BD%9C%E6%88%90%E3%82%B5%E3%83%B3%E3%83%97%E3%83%AB/SAMPLE_APP.XML)、検証用スキーマは [`Application.xsd`](DTFX/XMLSchema/Application.xsd) を参照してください。完全版サンプルの接続文字列と SQL は、実行環境に合わせて変更する必要があります。

> ジョブ XML はコードと同じ信頼境界で管理してください。`ExecuteCommand` は Windows コマンドを実行し、SQL 要素は指定したデータベース権限で任意の SQL を実行します。

## テスト

Release ビルド後に次を実行します。

```bat
tests\DTFX.SmokeTests\bin\Release\DTFX.SmokeTests.exe
```

スモークテストは、結果コードの優先順位、コマンドライン引数の解析、XSD のコンパイル、および Quick Start と完全版サンプルのスキーマ整合性を検証します。実データベースへの接続テストは含みません。

## プロジェクト構成

```text
Common/                  ログ、CSV、構成、ファイル操作の共通機能
DTFX/                    ETL エンジン、XML 要素、Executor
docs/                    利用方法と設計資料
examples/quickstart/     データベース不要の実行例
tests/DTFX.SmokeTests/   外部テストパッケージ不要のスモークテスト
```

## ドキュメント

- [構成ファイルと設定](docs/configuration.md)
- [XML 要素リファレンス](docs/xml-elements.md)
- [アーキテクチャ](docs/architecture.md)
- [コントリビューションガイド](CONTRIBUTING.md)
- [セキュリティポリシー](SECURITY.md)
- [変更履歴](CHANGELOG.md)

## ライセンス

DTFX is released under the [MIT License](LICENSE).
