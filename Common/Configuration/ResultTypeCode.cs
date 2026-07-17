using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IF.Batch.Common.Configuration
{
    /// <summary>
    /// バッチ実行結果の種類を表す列挙型
    /// </summary>
    public enum ResultTypeCode
    {
        /// <summary>
        /// 正常終了。値 = 0。
        /// </summary>
        [EnumMember(Value = "Success")]
        Success = 0,
        /// <summary>
        /// 異常終了。値 = 1。
        /// </summary>
        [EnumMember(Value = "Error")]
        Error = 1,
        /// <summary>
        /// 警告終了。値 = 2。
        /// </summary>
        [EnumMember(Value = "Warning")]
        Warning = 2,
    }
}
