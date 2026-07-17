using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.DTFX.Elements;

namespace IF.Batch.DTFX.Exceptions
{
    /// <summary>
    /// アプリケーション環境設定例外
    /// </summary>
    public class AppConfigurationException : Exception
    {
        /// <summary>
        /// 指定したメッセージで構成例外を生成します。
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        public AppConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// XML 要素名を先頭に付けたメッセージで構成例外を生成します。
        /// </summary>
        /// <param name="elementName">構成エラーが発生した XML 要素名。</param>
        /// <param name="message">エラーメッセージ</param>
        public AppConfigurationException(string elementName, string message)
            : base(string.Format("{0}:{1}", elementName, message))
        {
        }
    }
}
