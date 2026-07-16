/************************************************************************
* ファイル名:	MathEvaluator.cs
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

#pragma warning disable 0618
namespace IF.Batch.DTFX.Helper
{
    /// <summary>
    /// Jscriptを実行するクラス
    /// </summary>
    public class JScriptEvaluator : Microsoft.JScript.INeedEngine
    {
        private Microsoft.JScript.Vsa.VsaEngine _vsaEngine;

        /// <summary>
        /// Jscriptを実行してその結果を返却します。
        /// </summary>
        /// <param name="expr">Jscript表現式</param>
        /// <returns>実行結果</returns>
        public virtual string Evaluate(string expr)
        {
            var result = Microsoft.JScript.Eval.JScriptEvaluate(expr, this.GetEngine());
            return Microsoft.JScript.Convert.ToString(result, true);
        }

        /// <summary>
        /// Jscript解析エンジンを返却します。
        /// </summary>
        /// <returns>Jscript解析エンジン</returns>
        public Microsoft.JScript.Vsa.VsaEngine GetEngine()
        {
            _vsaEngine = _vsaEngine ?? Microsoft.JScript.Vsa.VsaEngine.CreateEngineWithType(this.GetType().TypeHandle);
            return _vsaEngine;
        }

        /// <summary>
        /// Jscript解析エンジンを設定します。
        /// <param name="engine">Jscript解析エンジン</param>
        /// </summary>
        public void SetEngine(Microsoft.JScript.Vsa.VsaEngine engine)
        {
            _vsaEngine = engine;
        }
    }
}
#pragma warning restore 0618
