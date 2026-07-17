using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IF.Batch.DTFX.Elements;

namespace IF.Batch.DTFX.Exceptions
{
    /// <summary>
    /// アプリケーション終了例外
    /// </summary>
    public class AppExitException : Exception
    {
        public AppExitElement Element
        {
            get;
            set;
        }
        public AppExitException(AppExitElement element)
        {
            Element = element;
        }
    }
}
