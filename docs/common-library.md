# IF.Batch.Common リファレンス

`IF.Batch.Common` は DTFX の実行エンジンから、CSV、ファイル、構成、ログ、サービス契約を分離した .NET Framework 4.6.2 の共通ライブラリです。`IF.Batch.DTFX` と `DTFX.SmokeTests` がプロジェクト参照しており、単独利用するアプリケーションも `Common/IF.Batch.Common.csproj` を参照できます。

## ビルド構成

共通ライブラリにはネイティブの x86 コードがないため、ソリューションの両プラットフォームから `Any CPU` としてビルドします。

| ソリューション構成 | `IF.Batch.Common` の実構成 | 出力先 |
|---|---|---|
| `Debug|Any CPU` | `Debug|Any CPU` | `Common/bin/Debug/IF.Batch.Common.dll` |
| `Debug|x86` | `Debug|Any CPU` | `Common/bin/Debug/IF.Batch.Common.dll` |
| `Release|Any CPU` | `Release|Any CPU` | `Common/bin/Release/IF.Batch.Common.dll` |
| `Release|x86` | `Release|Any CPU` | `Common/bin/Release/IF.Batch.Common.dll` |

`x86` の `ActiveCfg` と `Build.0` はどちらも明示されています。そのため `IF.Batch.sln` を `x86` でビルドしても Common が省略されず、x86 の DTFX 実行ファイルから同じ Any CPU DLL を参照します。

```bat
dotnet build IF.Batch.sln --no-restore -c Release -p:Platform=x86
```

## 名前空間と責務

| 名前空間 | 主な型 | 用途 |
|---|---|---|
| `IF.Batch.Common.Configuration` | `AppConfigConstants`, `ResultTypeCode` | 構成キーとジョブ結果コード |
| `IF.Batch.Common.Diagnostics` | `ITraceLogger`, `TraceLogger`, `TraceLog`, `SerilogTraceLogWriter` | 注入可能なログ境界と CSV 形式のファイルログ |
| `IF.Batch.Common.Helper` | `CsvReader`, `CsvWriter`, `ConcurrentCsvWriter` | CSV / GZIP の読み書き |
| `IF.Batch.Common.Helper` | `FileHelper`, `LogicalStringComparer` | 枝番、自然順、シグネチャ、パステンプレート |
| `IF.Batch.Common.Helper` | `ConfigurationManagerHelper`, `InputArguments` | 構成のマージとコマンドライン解析 |
| `IF.Batch.Common.Service` | `IService`, `IServiceContext`, `ITaskExecutor<T>` | DTFX サービス層の契約 |

## CSV の読み書き

次の例は UTF-8 (BOM なし) の CSV を作成し、同じファイルを読み戻します。値に区切り文字、引用符、改行、前後の空白が含まれる場合は `CsvFormatter` が必要なフィールドだけを引用符で囲みます。

```csharp
using System;
using System.IO;
using System.Text;
using IF.Batch.Common.Helper;

Directory.CreateDirectory("Data");
var encoding = new UTF8Encoding(false);
var path = Path.Combine("Data", "customers.csv");

using (var writer = new CsvWriter(path, encoding))
{
    writer.WriteLine(new[] { "ID", "NAME", "NOTE" });
    writer.WriteLine(new[] { "1001", "Alice", "contains, comma" });
}

using (var reader = new CsvReader(path, encoding))
{
    while (!reader.EndOfData)
    {
        string[] fields = reader.ReadFields();
        Console.WriteLine(string.Join(" | ", fields));
    }
}
```

GZIP を使用する場合は reader / writer の `useGzip` を `true` にします。拡張子は自動で付かないため、呼び出し側で `.gz` を指定してください。`ConcurrentCsvWriter` は複数スレッドからの書き込みを直列化し、`maxWriteRows` ごとに `_0`, `_1` のような枝番ファイルへ切り替えます。`headerStrings` を指定すると各ファイルの先頭に同じヘッダーを出力します。

## ログ

`ITraceLogger` はサービスや Executor などのアプリケーションコードへ注入するログ契約です。標準実装の `TraceLogger` は既存の静的 `TraceLog` API へ処理を委譲するため、従来のログ設定と出力形式を維持したままテスト用実装へ差し替えられます。`DataTransferService(ITraceLogger)` に渡したロガーは、コンテキスト、LocalDB ヘルパー、ネストした制御フローを含むすべての Executor へ引き継がれます。

`TraceLog` は最初の書き込み時に AppSettings を読み込みます。`trace.templatepath` が空なら `NullTraceLogWriter`、設定されていれば `SerilogTraceLogWriter` を使用します。静的 API は既存コードとの互換性のため引き続き利用できます。

```csharp
using System.Reflection;
using IF.Batch.Common.Diagnostics;

TraceLog.WriteInfo(MethodBase.GetCurrentMethod(), "Imported {0} rows.", rowCount);
TraceLog.WriteWarning(MethodBase.GetCurrentMethod(), "Input file was empty.");
TraceLog.WriteException(exception, "Customer import failed.");
```

CSV ログは日時、重大度、スレッド ID、メソッド、メッセージの順で出力されます。ログ設定と現在の Serilog 互換範囲は [構成ファイルと設定](configuration.md#ログ設定) を参照してください。

## コマンドライン引数

`InputArguments` はキーを大文字小文字を区別せずに保持し、参照時はプレフィックスの有無をどちらも受け付けます。同じキーが複数回指定された場合は最後の値が残ります。値を伴わないフラグの値は `null` です。

```csharp
var args = new InputArguments(new[] {
    "-appid", "DAILY_IMPORT",
    "-dry-run"
});

string appId = args["appid"];       // DAILY_IMPORT
bool dryRun = args.Contains("-dry-run");
string value = args["dry-run"];    // null
```

## ファイル名とパステンプレート

`FileHelper.NextFileName("report.csv.gz")` は同名ファイルがある場合に `report_0.csv.gz`、`report_1.csv.gz` の順で空いている名前を返します。`FindFiles` と `LastFileName` は Windows の論理文字列比較を使うため、`file2` は `file10` より前に並びます。

ログパスでは `%YYYYMMDD%` と `%HHMMSS%`、さらに `%0` から `%2` までのコマンドライン引数プレースホルダーを使用できます。

```csharp
string path = FileHelper.ResolvePathFromTemplate(
    @"Log\job_%YYYYMMDD%_%HHMMSS%.log",
    DateTime.Now);
```

## 利用上の注意

- CSV reader / writer を必ず `using` で破棄し、GZIP の終端とログのバッファーを確実に書き出してください。
- `ConfigurationManagerHelper.MergeAppSettings` はプロセス全体の `ConfigurationManager.AppSettings` を変更します。アプリケーション起動時に一度だけ使用してください。
- `LogicalStringComparer` は Windows の `shlwapi.dll` を利用するため Windows 専用です。
- `FileHelper.NextFileName` は候補名を返す処理です。複数プロセスから同時作成する場合は `CreateNextFile` を使用し、競合時の再試行も考慮してください。
