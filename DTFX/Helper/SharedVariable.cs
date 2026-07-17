using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.DTFX.Service
{
    /// <summary>
    /// ジョブ内の値を大文字小文字を区別するキーで保持し、各操作をロックで同期します。
    /// </summary>
    public class SharedVariable
    {
        private readonly Dictionary<string, object> _globalValues = new Dictionary<string, object>();

        private readonly object _syncObject = new object();

        /// <summary>
        /// 共有変数に値が存在するか確認します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>キーが登録されている場合は <see langword="true"/>。</returns>
        public bool ContainsKey(string key)
        {
            lock (_syncObject)
            {
                if (_globalValues.ContainsKey(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 共有変数を取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>登録されている値。キーがない場合は <see langword="null"/>。</returns>
        public object GetValue(string key)
        {
            lock (_syncObject)
            {
                if (_globalValues.ContainsKey(key))
                {
                    return _globalValues[key];
                }
            }
            return null;
        }

        /// <summary>
        /// 共有変数をstring型で取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="defaultValue">既定値</param>
        /// <returns>値</returns>
        public string GetStringValue(string key, string defaultValue = null)
        {
            object value = GetValue(key);
            if (value == null)
            {
                return defaultValue;
            }
            string stringValue = null;
            if (value is string)
            {
                stringValue = (string)value;
            }
            else
            {
                stringValue = value.ToString();
            }
            if (string.IsNullOrEmpty(stringValue))
            {
                return defaultValue;
            }
            return stringValue;
        }

        /// <summary>
        /// 共有変数をint型で取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="defaultValue">既定値</param>
        /// <returns>値</returns>
        public int? GetIntValue(string key, int? defaultValue = null)
        {
            object value = GetValue(key);
            if (value == null)
            {
                return defaultValue;
            }
            if (value is int)
            {
                return (int)value;
            }
            int intValue = 0;
            if (!int.TryParse(value.ToString().Trim(), out intValue))
            {
                return defaultValue;
            }
            return intValue;
        }

        /// <summary>
        /// 共有変数をlong型で取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="defaultValue">既定値</param>
        /// <returns>値</returns>
        public long? GetLongValue(string key, long? defaultValue = null)
        {
            object value = GetValue(key);
            if (value == null)
            {
                return defaultValue;
            }
            if (value is long)
            {
                return (long)value;
            }
            long longValue = 0;
            if (!long.TryParse(value.ToString().Trim(), out longValue))
            {
                return defaultValue;
            }
            return longValue;
        }


        /// <summary>
        /// 共有変数をbool型で取得します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="defaultValue">既定値</param>
        /// <returns>値</returns>
        public bool? GetBooleanValue(string key, bool? defaultValue = null)
        {
            object value = GetValue(key);
            if (value == null)
            {
                return defaultValue;
            }
            if (value is bool)
            {
                return (bool)value;
            }
            bool boolValue = false;
            string stringValue = value.ToString().Trim();
            if ("0" == stringValue)
            {
                return false;
            }
            if ("1" == stringValue)
            {
                return true;
            }
            if (!bool.TryParse(stringValue, out boolValue))
            {
                return defaultValue;
            }
            return boolValue;
        }

        /// <summary>
        /// 共有変数を追加し、同じキーがある場合は値を置換します。
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        public void SetValue(string key, object value)
        {
            lock (_syncObject)
            {
                if (_globalValues.ContainsKey(key))
                {
                    _globalValues[key] = value;
                }
                else
                {
                    _globalValues.Add(key, value);
                }
            }
        }

        /// <summary>
        /// 共有変数から削除します。
        /// </summary>
        /// <param name="key">削除するキー。</param>
        public void RemoveValue(string key)
        {
            lock (_syncObject)
            {
                if (_globalValues.ContainsKey(key))
                {
                    _globalValues.Remove(key);
                }
            }
        }

        /// <summary>
        /// 共有変数から値を全て削除します。
        /// </summary>
        public void Clear()
        {
            lock (_syncObject)
            {
                _globalValues.Clear();
            }
        }
    }
}
