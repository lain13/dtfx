/************************************************************************
* ファイル名:	ZipArchiveExecutor.cs
* 概要: 
* 履歴:
*	バージョン		日付		作成者		内容
*   21.3-001-01     2021/07/05  姜　恵遠    Biz-A Step1.5対応
*
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Diagnostics;
using System.Reflection;
using IF.Batch.DTFX.Service;
using IF.Batch.Common.Configuration;
using IF.Batch.DTFX.Elements;
using IF.Batch.Common.Diagnostics;
using Ionic.Zip;
using System.IO;
using IF.Batch.Common.Helper;
using IF.Batch.DTFX.Exceptions;

namespace IF.Batch.DTFX.Executors
{
    /// <summary>
    /// メッセージ出力処理(TraceLog)
    /// ログファイルにログメッセージを出力する。
    /// </summary>
    public class ZipArchiveExecutor : ExecutorBase
    {
        public override ResultTypeCode Execute(XElement rawElement)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            var element = CreateElement(rawElement);
            if (string.IsNullOrWhiteSpace(element.Filename))
            {
                Logger.WriteError(method, "圧縮ファイル名が正しく有りません。");
                return ResultTypeCode.Error;
            }
            string zipfileFullPath = GetOutputPathFullName(element.Filename, element.Overwrite);

            Logger.WriteDebug(method, "圧縮ファイルを保存します。名前:{0}, パスワード:{1}、上書き：{2}", zipfileFullPath, string.IsNullOrEmpty(element.Password) ? "無し" : "有り", element.Overwrite);
            if (element.Overwrite == true && File.Exists(zipfileFullPath))
            {
                Logger.WriteDebug(method, "圧縮ファイルが存在しますので削除します。名前:{0}", zipfileFullPath);
                File.Delete(zipfileFullPath);
            }
            using (ZipFile zip = new ZipFile())
            {
                if (!string.IsNullOrWhiteSpace(element.Password))
                {
                    zip.Password = element.Password;
                    zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                }
                
                foreach (var addFile in element.AddFileElements)
                {
                    foreach (var f in this.GetFiles(addFile.FilenamePattern))
                    {
                        Logger.WriteDebug(method, "ZIPファイルにファイルを追加します。ファイルのパス:{0}", f.FullName);
                        zip.AddFile(f.FullName, ".");
                    }
                }

                zip.Save(zipfileFullPath);
                Logger.WriteInfo(method, "圧縮ファイルを保存しました。名前:{0}, パスワード:{1}", zipfileFullPath, string.IsNullOrEmpty(element.Password) ? "無し" : "有り");
            }
            foreach (var addFile in element.AddFileElements)
            {
                if (addFile.DeletedOnArchived == true)
                {
                    foreach (var f in this.GetFiles(addFile.FilenamePattern))
                    {
                        Logger.WriteDebug(method, "ファイルを削除します。ファイルのパス:{0}", f.FullName);
                        f.Delete();
                    }
                }
            }

            return ResultTypeCode.Success;
        }

        /// <summary>
        /// XElementからZipArchiveElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>ZipArchiveElement</returns>
        public ZipArchiveElement CreateElement(XElement rawElement)
        {
            ZipArchiveElement obj = new ZipArchiveElement();
            obj.RawElement = rawElement;
            obj.Id = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.id);
            obj.Filename = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.filename);
            obj.Password = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.password);
            obj.Overwrite = GetBooleanValue(rawElement, XSqlElementConstants.AttributeName.overwrite);

            foreach (XElement element in rawElement.Elements())
            {
                if (element.Name != XSqlElementConstants.ElementName.AddFile)
                {
                    throw new AppConfigurationException(XSqlElementConstants.ElementName.ZipArchive, "XMLを解析できませんでした。XML要素名=" + element.Name);
                }
                var addFile = CreateAddFileElement(element);
                obj.AddFileElements.Add(addFile);
            }
            return obj;
        }

        /// <summary>
        /// XElementからAddFileElementを生成します。
        /// </summary>
        /// <param name="rawElement">XElement</param>
        /// <returns>ZipArchiveElement</returns>
        protected AddFileElement CreateAddFileElement(XElement rawElement)
        {
            AddFileElement element = new AddFileElement();
            element.FilenamePattern = GetParsedStringValue(rawElement, XSqlElementConstants.AttributeName.filenamePattern);
            element.DeletedOnArchived = GetBooleanValue(rawElement, XSqlElementConstants.AttributeName.deletedOnArchived);
            return element;
        }

        /// <summary>
        /// パターンと一致するファイルを返却します。
        /// </summary>
        /// <param name="searchPattern">検索パターン</param>
        /// <returns>ファイルリスト</returns>
        protected override FileInfo[] GetFiles(string searchPattern)
        {
            MethodBase method = MethodInfo.GetCurrentMethod();
            Logger.WriteDebug(method, "ファイルを検索します。検索パターン:{0}", searchPattern);
            if (File.Exists(searchPattern))
            {
                return new FileInfo[1] { new FileInfo(searchPattern) };
            }
            string path = null;
            string pattern = null;
            FileHelper.TryExtractPathAndPattern(searchPattern, out path, out pattern);
            if (string.IsNullOrEmpty(path))
            {
                Logger.WriteError(method, "対象ファイルのパスが正しくありません。{0}", searchPattern);
                return new FileInfo[0];
            }
            if (!Directory.Exists(path))
            {
                Logger.WriteError(method, "対象ファイルのパスが正しくありません。{0}", searchPattern);
                return new FileInfo[0];
            }

            return FileHelper.FindFiles(path, pattern);
        }
    }
}
