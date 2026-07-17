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
- MIT License

### Changed

- R7001 から `IF.Batch.DTFX` へプロジェクト名を変更
- 公開ドキュメントを日本語に統一し、実装に合わせて起動方法と設定の説明を更新
- 外部コマンドの標準出力と標準エラーを並行して取得し、終了コードを実行結果へ反映
- `maxreadrows` 設定を CSV 読み込みとファイルベースの `ForEach` に適用
- Git リポジトリに不要な Subversion / AnkhSVN メタデータを削除

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
