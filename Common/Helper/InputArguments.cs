using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// キープレフィックスを使用するコマンドライン引数を、大文字小文字を区別せずに解析します。
    /// </summary>
    public class InputArguments
    {
        #region fields & properties
        /// <summary>
        /// キーの既定プレフィックス。
        /// </summary>
        public const string DEFAULT_KEY_LEADING_PATTERN = "-";

        /// <summary>
        /// 解析済みのキーと値。
        /// </summary>
        protected Dictionary<string, string> _parsedArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// キーを識別するプレフィックス。
        /// </summary>
        protected readonly string _keyLeadingPattern;

        /// <summary>
        /// プレフィックスの有無を問わず、指定されたキーの値を取得または設定します。
        /// </summary>
        /// <param name="key">検索または設定するキー。</param>
        /// <returns>キーに対応する値。キーが存在しない場合は <see langword="null"/>。</returns>
        public string this[string key]
        {
            get { return GetValue(key); }
            set
            {
                if (key != null)
                {
                    _parsedArguments[key] = value;
                }
            }
        }
        /// <summary>
        /// キーを識別するプレフィックスを取得します。
        /// </summary>
        public string KeyLeadingPattern
        {
            get { return _keyLeadingPattern; }
        }
        /// <summary>
        /// 解析済みのすべてのキーを取得します。
        /// </summary>
        public string[] AllKeys
        {
            get { return _parsedArguments.Keys.ToArray(); }
        }
        #endregion

        #region public methods
        /// <summary>
        /// 指定されたプレフィックスを使用して引数を解析します。
        /// </summary>
        /// <param name="args">キーと値が交互に並ぶコマンドライン引数。</param>
        /// <param name="keyLeadingPattern">キーのプレフィックス。空の場合は <see cref="DEFAULT_KEY_LEADING_PATTERN"/>。</param>
        public InputArguments(string[] args, string keyLeadingPattern)
        {
            _keyLeadingPattern = !string.IsNullOrEmpty(keyLeadingPattern) ? keyLeadingPattern : DEFAULT_KEY_LEADING_PATTERN;
            if (args != null && args.Length > 0)
            {
                Parse(args);
            }
        }

        /// <summary>
        /// 既定の <c>-</c> プレフィックスを使用して引数を解析します。
        /// </summary>
        /// <param name="args">キーと値が交互に並ぶコマンドライン引数。</param>
        public InputArguments(string[] args)
            : this(args, null)
        {
        }

        /// <summary>
        /// プレフィックスの有無を問わず、指定されたキーが存在するかを確認します。
        /// </summary>
        /// <param name="key">確認するキー。</param>
        /// <returns>キーが存在する場合は <see langword="true"/>。</returns>
        public bool Contains(string key)
        {
            string adjustedKey;
            return ContainsKey(key, out adjustedKey);
        }

        /// <summary>
        /// キーの先頭からプレフィックスを取り除きます。
        /// </summary>
        /// <param name="key">変換するキー。</param>
        /// <returns>プレフィックスを取り除いたキー。プレフィックスがない場合は元の値。</returns>
        public virtual string GetPeeledKey(string key)
        {
            return IsKey(key) ? key.Substring(_keyLeadingPattern.Length) : key;
        }

        /// <summary>
        /// キーの先頭へ、まだ付いていない場合だけプレフィックスを追加します。
        /// </summary>
        /// <param name="key">変換するキー。</param>
        /// <returns>プレフィックス付きのキー。</returns>
        public virtual string GetDecoratedKey(string key)
        {
            return !IsKey(key) ? (_keyLeadingPattern + key) : key;
        }

        /// <summary>
        /// 文字列がプレフィックスで始まるキーかを確認します。
        /// </summary>
        /// <param name="str">確認する文字列。</param>
        /// <returns>キーの場合は <see langword="true"/>。</returns>
        public virtual bool IsKey(string str)
        {
            return !string.IsNullOrEmpty(str) && str.StartsWith(_keyLeadingPattern);
        }

        /// <summary>
        /// プレフィックスを除いたキーと値のコピーを返します。
        /// </summary>
        /// <returns>プレフィックスを持たないキーで構成した新しいディクショナリ。</returns>
        public virtual Dictionary<string, string> GetPeeledArguments()
        {
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            foreach (string key in _parsedArguments.Keys)
            {
                arguments.Add(GetPeeledKey(key), _parsedArguments[key]);
            }
            return arguments;
        }

        #endregion

        #region internal methods
        /// <summary>
        /// コマンドライン引数をキーと値へ分解します。
        /// </summary>
        /// <param name="args">解析する引数。</param>
        protected virtual void Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                {
                    continue;
                }

                string key = null;
                string val = null;

                if (IsKey(args[i]))
                {
                    key = args[i];
                    if (i + 1 < args.Length && !IsKey(args[i + 1]))
                    {
                        val = args[i + 1];
                        i++;
                    }
                }
                else
                {
                    val = args[i];
                }

                if (key == null)
                {
                    key = val;
                    val = null;
                }
                _parsedArguments[key] = val;
            }
        }

        /// <summary>
        /// プレフィックスの有無を吸収してキーの値を取得します。
        /// </summary>
        /// <param name="key">検索するキー。</param>
        /// <returns>キーに対応する値。存在しない場合は <see langword="null"/>。</returns>
        protected virtual string GetValue(string key)
        {
            string adjustedKey;
            if (ContainsKey(key, out adjustedKey))
            {
                return _parsedArguments[adjustedKey];
            }
            return null;
        }

        /// <summary>
        /// プレフィックスの有無を吸収してキーを検索します。
        /// </summary>
        /// <param name="key">検索するキー。</param>
        /// <param name="adjustedKey">見つかったディクショナリ内の実際のキー。</param>
        /// <returns>キーが存在する場合は <see langword="true"/>。</returns>
        protected virtual bool ContainsKey(string key, out string adjustedKey)
        {
            adjustedKey = key;

            if (_parsedArguments.ContainsKey(key))
            {
                return true;
            }

            if (IsKey(key))
            {
                string peeledKey = GetPeeledKey(key);
                if (_parsedArguments.ContainsKey(peeledKey))
                {
                    adjustedKey = peeledKey;
                    return true;
                }
                return false;
            }

            string decoratedKey = GetDecoratedKey(key);
            if (_parsedArguments.ContainsKey(decoratedKey))
            {
                adjustedKey = decoratedKey;
                return true;
            }
            return false;
        }
        #endregion
    }
}
