/************************************************************************
* ファイル名:	ITaskExecutor.cs
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
    /// <summary>
    /// タスク実行インターフェース
    /// </summary>
    /// <typeparam name="TParameter"></typeparam>
    public interface ITaskExecutor<TParameter>
        where TParameter : class
    {
        ResultTypeCode Execute(TParameter parameter);
    }
}
