# DTFX サンプル

すべてのサンプルはデータベース接続なしで実行できます。先にリポジトリのルートで Release ビルドを行ってください。

```bat
dotnet build IF.Batch.sln --no-restore -c Release -p:Platform="Any CPU"
```

| サンプル | 確認できる機能 | 実行コマンド |
|---|---|---|
| [quickstart](quickstart/) | ログ、JEXL 式、共有変数、終了コード | `examples\quickstart\QUICKSTART.BAT` |
| [csv-pipeline](csv-pipeline/) | CSV 読み込み、`ForEach`、配列変数、ZIP 作成 | `examples\csv-pipeline\CSV_PIPELINE.BAT` |

各 BAT は自身のディレクトリを作業ディレクトリとして使用するため、どのカレントディレクトリからでも実行できます。生成されたログや `output`、`Backup`、`Err` ディレクトリは Git の管理対象外です。

サンプル XML はスモークテストで [`Application.xsd`](../DTFX/XMLSchema/Application.xsd) に対して検証されます。新しい例を追加した場合は `tests/DTFX.SmokeTests/Program.cs` の検証対象にも追加してください。
