using System;
using System.Text.Json.Nodes;
using JexlNet;

namespace IF.Batch.DTFX.Helper
{
    /// <summary>
    /// JEXL式を評価するクラスです。
    /// </summary>
    public class ExpressionEvaluator
    {
        private readonly Jexl _jexl = new Jexl();

        /// <summary>
        /// 式を評価して結果を文字列で返します。
        /// </summary>
        /// <param name="expression">評価するJEXL式</param>
        /// <returns>評価結果。nullの場合は空文字列を返します。</returns>
        public virtual string Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("Expression must not be empty.", "expression");
            }

            JsonNode result = _jexl.Eval(expression);
            return result == null ? string.Empty : result.ToString();
        }
    }
}
