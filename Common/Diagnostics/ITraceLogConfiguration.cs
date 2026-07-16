/************************************************************************
* ファイル名:	ITraceLogConfiguration.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*	25.1-001-01		2013/08/02	姜　恵遠	新規作成
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IF.Batch.Common.Diagnostics
{
    /// <summary>
    /// トレースログコンフィグレーションインタフェース
    /// </summary>
    public interface ITraceLogConfiguration
    {
        /// <summary>
        /// トレースログ出力先のテンプレート文字列
        /// </summary>
        string TracePathTemplate { get; }

        /// <summary>
        /// トレースログ出力レベル
        /// </summary>
        SourceLevels TraceSourceLevels { get; }

        /// <summary>
        /// 自動フラッシュ
        /// </summary>
        bool AutoFlush { get; }

        /// <summary>
        /// ログファイルの最大サイズ(byte)を指定する
        /// </summary>
        long MaxSize { get; }

        /// <summary>
        /// トレースログのバッファサイズ
        /// </summary>
        int BufferSize { get; }

        /// <summary>
        /// トレースログのエンコード
        /// </summary>
        System.Text.Encoding Encoding { get; }

        /// <summary>
        /// GZIP圧縮利用可否
        /// </summary>
        bool UseGzip { get; }

        /// <summary>
        /// 既存のファイルに追加
        /// </summary>
        bool Append { get; }

    }
}
