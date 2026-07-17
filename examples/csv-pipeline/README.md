# CSV Pipeline

データベースを使わずに、次の処理を順番に確認するサンプルです。

1. `input/customers.csv` を `LoadCSV` で共有変数へ読み込む
2. `ForEach` で各行をログへ出力する
3. 元の CSV を `output/customers.zip` へ格納する
4. 成功コード `0` で終了する

リポジトリのルートから実行する場合:

```bat
examples\csv-pipeline\CSV_PIPELINE.BAT
```

正常終了後は `output/CSV_PIPELINE_YYYYMMDD.log` と `output/customers.zip` が生成されます。`backupdirectory` は空にしているため、入力 CSV は移動・削除されず、繰り返し実行できます。

異なる CSV を試す場合は 1 行目をヘッダー、2 行目以降を `ID,NAME,EMAIL` の 3 列にしてください。`CSV_PIPELINE.XML` 内では各列を `${customer[0]}`、`${customer[1]}`、`${customer[2]}` として参照しています。
