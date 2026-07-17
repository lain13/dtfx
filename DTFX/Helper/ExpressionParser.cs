using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using IF.Batch.DTFX.Service;

namespace IF.Batch.DTFX.Helper
{
    /// <summary>
    /// 文字列内の <c>${name}</c> 形式を共有変数の値で置換します。
    /// 配列インデックス、辞書キー、公開プロパティを連結して参照でき、解決できない式は元のまま残します。
    /// </summary>
    public class ExpressionParser
    {
        /// <summary>
        /// 「${変数}」形式の文字列が含まれているか確認するための正規表現式
        /// </summary>
        public const string REGEX_EX = @"\${([^}]+)}";

        private SharedVariable _sharedValues;

        /// <summary>
        /// 置換元となる共有変数を指定してパーサーを生成します。
        /// </summary>
        /// <param name="SharedValues">共有変数</param>
        public ExpressionParser(SharedVariable SharedValues)
        {
            this._sharedValues = SharedValues;
        }

        /// <summary>
        /// 文字列を解析します。
        /// </summary>
        /// <param name="origString">解析する文字列</param>
        /// <returns>置換された文字列</returns>
        public string ParseString(string origString)
        {
            MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(origString, REGEX_EX, RegexOptions.IgnoreCase);
            foreach (Match m in mc)
            {
                string parseTarget = m.Groups[1].Value;
                string replaceTarget = m.Value;
                if (_sharedValues.ContainsKey(parseTarget))
                {
                    object obj = _sharedValues.GetValue(parseTarget) ?? string.Empty;
                    origString = origString.Replace(replaceTarget, obj.ToString());
                }
                else
                {
                    origString = ParseString(origString, replaceTarget, parseTarget);
                }
            }
            return origString;
        }

        /// <summary>
        /// 文字列を解析します。
        /// </summary>
        /// <param name="origString">元の文字列</param>
        /// <param name="replaceTarget">置換する文字列</param>
        /// <param name="parseTarget">解析する文字列</param>
        /// <returns>置換された文字列</returns>
        private string ParseString(string origString, string replaceTarget, string parseTarget)
        {
            int dotIndex = parseTarget.IndexOf('.');
            int indexorIndex = parseTarget.IndexOf('[');

            if (dotIndex == -1 && indexorIndex == -1)
            {
                return origString;
            }
            else if (dotIndex == -1)
            {
                string subKey1 = parseTarget.Substring(0, indexorIndex);
                string subKey2 = parseTarget.Substring(indexorIndex);
                if (_sharedValues.ContainsKey(subKey1))
                {
                    object obj = _sharedValues.GetValue(subKey1);
                    string value = ParseStringFromIndexor(obj, subKey2);
                    if (value != null)
                    {
                        return origString.Replace(replaceTarget, value);
                    }
                }
            }
            else if (indexorIndex == -1)
            {
                string subKey1 = parseTarget.Substring(0, dotIndex);
                string subKey2 = parseTarget.Substring(dotIndex);
                if (_sharedValues.ContainsKey(subKey1))
                {
                    object obj = _sharedValues.GetValue(subKey1);
                    string value = ParseStringFromDot(obj, subKey2);
                    if (value != null)
                    {
                        return origString.Replace(replaceTarget, value);
                    }
                }
            }
            else
            {
                if (indexorIndex < dotIndex)
                {
                    string subKey1 = parseTarget.Substring(0, indexorIndex);
                    string subKey2 = parseTarget.Substring(indexorIndex);
                    if (_sharedValues.ContainsKey(subKey1))
                    {
                        object obj = _sharedValues.GetValue(subKey1);
                        string value = ParseStringFromIndexor(obj, subKey2);
                        if (value != null)
                        {
                            return origString.Replace(replaceTarget, value);
                        }
                    }
                    else
                    {
                        return replaceTarget;
                    }
                }
                else
                {
                    int lastIndex1 = parseTarget.LastIndexOf(']');
                    if (lastIndex1 == -1)
                    {
                        return origString;
                    }
                    string subKey1 = parseTarget.Substring(0, indexorIndex);
                    string subKey2 = parseTarget.Substring(indexorIndex + 1, lastIndex1 - indexorIndex - 1);
                    if (subKey2[0] == '"' || subKey2[0] == '\'')
                    {
                        subKey2 = subKey2.Substring(1);
                    }
                    if (subKey2[subKey2.Length - 1] == '"' || subKey2[subKey2.Length - 1] == '\'')
                    {
                        subKey2 = subKey2.Substring(0, subKey2.Length - 1);
                    }

                    if (_sharedValues.ContainsKey(subKey1))
                    {
                        object obj = _sharedValues.GetValue(subKey1);
                        string value = ParseStringFromIndexor(obj, subKey2);
                        if (value != null)
                        {
                            return origString.Replace(replaceTarget, value);
                        }
                    }
                }
            }
            return origString;
        }

        /// <summary>
        /// 対象文字列に「.」が含まれている場合、プロパティまたは辞書キーで値を取得します。
        /// </summary>
        /// <param name="obj">プロパティまたは辞書キーを読み取るオブジェクト。</param>
        /// <param name="key">先頭のピリオドを含む参照パス。</param>
        /// <returns>解決した値。参照できない場合は <see langword="null"/>。</returns>
        private string ParseStringFromDot(object obj, string key)
        {
            string subkey1 = null;
            string subkey2 = null;
            object value = null;

            if (key.StartsWith("."))
            {
                key = key.Substring(1);
            }
            int dotIndex = key.IndexOf('.');
            int indexorIndex = key.IndexOf('[');

            if (dotIndex == -1 && indexorIndex == -1)
            {
                subkey1 = key;
                subkey2 = null;
            }
            else if (dotIndex > -1 || dotIndex < indexorIndex)
            {
                subkey1 = key.Substring(0, dotIndex);
                subkey2 = key.Substring(dotIndex);
            }
            else
            {
                subkey1 = key.Substring(0, indexorIndex);
                subkey2 = key.Substring(indexorIndex);
            }

            Type type = obj.GetType();
            PropertyInfo pInfo = type.GetProperty(subkey1);
            if (pInfo != null)
            {
                value = pInfo.GetValue(obj, null);
            }
            else if (obj is IDictionary)
            {
                if (((IDictionary)obj).Contains(subkey1))
                {
                    value = ((IDictionary)obj)[subkey1];
                }
            }

            if (value == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(subkey2))
            {
                return value.ToString();
            }
            if (subkey2.StartsWith("."))
            {
                return ParseStringFromDot(value, subkey2);
            }
            if (subkey2.StartsWith("["))
            {
                return ParseStringFromIndexor(value, subkey2);
            }
            return null;
        }

        /// <summary>
        /// 対象文字列に「[」が含まれている場合、インデクサーまたは辞書キーで値を取得します。
        /// </summary>
        /// <param name="obj">配列、インデクサー、辞書のいずれか。</param>
        /// <param name="key">角括弧を含む参照パス。</param>
        /// <returns>解決した値。参照できない場合は <see langword="null"/>。</returns>
        private string ParseStringFromIndexor(object obj, string key)
        {
            int subIndex1 = key.IndexOf('[');
            int subIndex2 = key.IndexOf(']');
            string subkey1 = key.Substring(subIndex1 + 1, subIndex2 - subIndex1 -1);
            string subkey2 = key.Substring(subIndex2 + 1);
            if (subkey1.Contains('\'') || subkey1.Contains('"'))
            {
                subkey1 = subkey1.Replace("\'", string.Empty);
                subkey1 = subkey1.Replace("\"", string.Empty);
            }
            Type type = obj.GetType();
            int index = -1;
            object value = null;
            if (int.TryParse(subkey1, out index) && index > -1)
            {
                if (obj is Array)
                {
                    Array ary = (Array)obj;
                    if (ary.Length > index)
                    {
                        value = ary.GetValue(index);
                    }
                }
                else if (obj is IDictionary)
                {
                    IDictionary dict = (IDictionary)obj;
                    if(dict.Count > index)
                    {
                        int cnt = 0;
                        foreach (var val in dict.Values)
                        {
                            if (cnt == index)
                            {
                                value = val;
                                break;
                            }
                            cnt++;
                        }
                    }
                }
                else
                {
                    PropertyInfo indexor = type.GetProperty("Item");
                    if (indexor == null)
                    {
                        return null;
                    }
                    try
                    {
                        value = indexor.GetValue(obj, new Object[] { index });
                    }
                    catch (TargetInvocationException)
                    {
                    }
                }
            }
            else if(obj is IDictionary)
            {
                if (((IDictionary)obj).Contains(subkey1))
                {
                    value = ((IDictionary)obj)[subkey1];
                }
            }
            else if (obj is NameValueCollection)
            {
                if (((NameValueCollection)obj).AllKeys.Contains(subkey1))
                {
                    value = ((NameValueCollection)obj)[subkey1];
                }
            }
            if(value == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(subkey2))
            {
                return value.ToString();
            }
            if (subkey2.StartsWith("."))
            {
                return ParseStringFromDot(value, subkey2);
            }
            if (subkey2.StartsWith("["))
            {
                return ParseStringFromIndexor(value, subkey2);
            }
            return null;
        }
    }
}
