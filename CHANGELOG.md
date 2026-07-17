# Changelog

このプロジェクトは [Keep a Changelog](https://keepachangelog.com/) の形式に従います。

## Unreleased

### Added

- DTFX の製品名とプロジェクト構成
- データベースなしで実行できる Quick Start
- 結果コードの優先順位、引数解析、XSD、サンプルを検証するスモークテスト
- GitHub Actions によるビルドと検証のワークフロー
- コントリビューションガイドとセキュリティポリシー
- 構成ファイルおよび XML 要素のリファレンス
- Common ライブラリの API ガイドと、CSV から ZIP を作成する実行可能なサンプル
- MIT License

### Changed

- R7001 から `IF.Batch.DTFX` へプロジェクト名を変更
- 公開ドキュメントを日本語に統一し、実装に合わせて起動方法と設定の説明を更新
- 外部コマンドの標準出力と標準エラーを並行して取得し、終了コードを実行結果へ反映
- `maxreadrows` 設定を CSV 読み込みとファイルベースの `ForEach` に適用
- `If` の式評価エンジンを旧式の Microsoft.JScript から JexlNet へ変更し、JEXL 文法のドキュメントとサンプルを更新
- Git リポジトリに不要な Subversion / AnkhSVN メタデータを削除
- Common のレガシー変更履歴コメントと空の XML ドキュメントを整理し、公開 API の説明を補完
- Executor の生成責務を `IExecutorFactory` に分離し、ネストした制御フローとサービスから差し替え可能に変更
- AppSettings、接続文字列、ジョブ XML の読み込みを `DataTransferContextFactory` に分離し、`DataTransferService` を実行ライフサイクルの調整に限定
- `ITraceLogger` と既存 `TraceLog` 用アダプターを追加し、サービスとコンテキスト構成処理のログ出力を差し替え可能に変更

### Fixed

- `If` の評価結果を保存する `toVariable` 属性が XSD に不足していた問題
- GZIP を使用するサンプル出力ファイルの拡張子
- PostgreSQL Bulk Insert 要素の誤った XSD 参照
- サンプル Bulk Insert の転送元と転送先のデータソース方向
- Error より Warning が優先されていた結果統合
- ディレクトリ作成失敗が無視されていた動作
- PostgreSQL Executor の誤ったエラー要素名
- 外部コマンドの出力バッファによるデッドロックの可能性
- コードコメントとエラーメッセージの誤記
- `x86` ソリューション構成で `IF.Batch.Common` がビルド対象から外れていたマッピング
- スモークテスト出力に JEXL 実行時依存の `System.Memory.dll` が含まれない問題
- CSV 読み込み時に使用されていない `ApplicationExecutor` を生成していた処理
