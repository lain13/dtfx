# DTFX へのコントリビューション

Issue と Pull Request を歓迎します。変更はできるだけ小さく、検証可能な単位にしてください。

## 開発環境

1. Windows に .NET Framework 4.6.2 Developer Pack と Visual Studio Build Tools をインストールします。
2. `NuGet.exe restore IF.Batch.sln` でパッケージを復元します。
3. `MSBuild.exe IF.Batch.sln /p:Configuration=Release /p:Platform="Any CPU"` でビルドします。
4. `tests\DTFX.SmokeTests\bin\Release\DTFX.SmokeTests.exe` を実行します。

依存関係を復元済みの場合は、手順 3 で `dotnet build IF.Batch.sln --no-restore -c Release -p:Platform="Any CPU"` も利用できます。

## 変更時の原則

- 既存の XML 要素、属性、設定キーとの互換性を優先してください。
- XML 要素や属性を追加した場合は、`Application.xsd`、[`docs/xml-elements.md`](docs/xml-elements.md)、関連するサンプルを同時に更新してください。
- 設定キーを追加または変更した場合は、[`docs/configuration.md`](docs/configuration.md) も更新してください。
- バグ修正には、可能な範囲でスモークテストを追加してください。
- 実際の接続文字列、パスワード、顧客データ、内部パスをコミットしないでください。
- 公開 API や終了コードが変わる場合は、Pull Request の説明に明記してください。
- SQL Server、Oracle、PostgreSQL のプロバイダーを更新する場合は、対象データベースでの統合テスト結果を記載してください。

Pull Request には、変更理由、検証に使ったコマンド、影響を受けるデータベースまたは XML 要素を記載してください。
