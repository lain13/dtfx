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

## コメントとドキュメント

- ソースコメントは、コードから読み取れない理由、互換性制約、並行処理、データ消失を防ぐための動作を説明してください。処理をそのまま日本語にしたコメントは追加しません。
- 公開型と公開メンバーには、呼び出し側が必要とする契約を XML ドキュメントコメントで記述します。既定値、副作用、所有権、例外時の扱いを優先し、空の `param` や `returns` は残しません。
- ファイル名、作成者、日付、変更番号を並べた履歴ヘッダーや `ADD/MOD START/END` は追加しません。変更履歴は Git と [`CHANGELOG.md`](CHANGELOG.md) で管理し、削除したコードはコメントアウトせず Git から参照します。
- XML 要素と属性の利用方法は [`docs/xml-elements.md`](docs/xml-elements.md)、設定キーは [`docs/configuration.md`](docs/configuration.md)、実行ライフサイクルと設計判断は [`docs/architecture.md`](docs/architecture.md)、共通ライブラリの公開 API は [`docs/common-library.md`](docs/common-library.md) を更新してください。
- コメントだけを変更した場合も Release ビルドとスモークテストを実行し、XML ドキュメントのタグとリンクが壊れていないことを確認してください。
