/************************************************************************
* ファイル名:	IService.cs
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
using IF.Batch.Common.Configuration;

namespace IF.Batch.Common.Service
{
    public interface IService : IDisposable 
    {
        /// <summary>
        /// 環境設定を確認します。
        /// </summary>
        bool EnsureServiceConfigurations();

        /// <summary>
        /// 初期化します。
        /// </summary>
        bool InitService();

        /// <summary>
        /// 実行します。
        /// </summary>
        void ExecuteService();
    }
}
