using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.AccessControl;
using System.IO;
using System.Security.Principal;

namespace IF.Batch.Common.Helper
{
    /// <summary>
    /// DTFX で使用するファイルとパスの操作を提供します。
    /// </summary>
    public class FileHelper
    {
        /// <summary>
        /// パスで指定されたディレクトリが存在しなければ
        /// 作成します。
        /// </summary>
        /// <param name="dirpath">ディレクトリのパス</param>
        /// <returns>作成した場合はtrue</returns>
        public static bool CreateDirectoryIfNotExists(string dirpath)
        {
            if (string.IsNullOrWhiteSpace(dirpath))
            {
                return false;
            }

            if (!Directory.Exists(dirpath))
            {
                Directory.CreateDirectory(dirpath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// ファイル名を次のルールで求める。
        /// (1) filepath に該当するファイルが存在しない場合は
        /// そのファイル名を求めるファイル名とする。
        /// (2) filepath に該当するファイルが存在する場合は、
        /// 枝番が付与された最後のファイル名返却する。
        /// </summary>
        /// <param name="filepath">ファイルのパス</param>
        /// <returns>ファイルがなければ <paramref name="filepath"/>。存在する場合は自然順で最後の一致ファイル。</returns>
        public static string LastFileName(string filepath)
        {
            // ファイルが存在しなければそのまま使用する
            if (!File.Exists(filepath))
            {
                return filepath;
            }

            string pathname = Path.GetDirectoryName(filepath);
            string basename = Path.GetFileNameWithoutExtension(filepath);
            string ext = Path.GetExtension(filepath);
            if (".gz".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                ext = Path.GetExtension(basename) + ext;
                basename = Path.GetFileNameWithoutExtension(basename);
            }
            string searchPattern = string.Format("{0}*{1}", basename, ext);

            return (from file in new DirectoryInfo(pathname).GetFiles(searchPattern)
                    select file.FullName).OrderByDescending(filename => filename, new LogicalStringComparer()).First();

        }

        /// <summary>
        /// ファイル名を次のルールで求める。
        /// (1) filepath に該当するファイルが存在しない場合は
        /// そのファイル名を求めるファイル名とする。
        /// (2) filepath に該当するファイルが存在する場合は、
        /// 枝番が付与されたファイル名を作成する。
        /// </summary>
        /// <param name="filepath">ファイルのパス</param>
        /// <returns>既存ファイルと重複しないパス。必要な場合は <c>_0</c> から始まる枝番を付けます。</returns>
        public static string NextFileName(string filepath)
        {
            string dir = Path.GetDirectoryName(filepath);
            FileHelper.CreateDirectoryIfNotExists(dir);

            // ファイルが存在しなければそのまま使用する
            if (!File.Exists(filepath))
            {
                return filepath;
            }

            // 新しいファイルを作成する
            string basename = Path.GetFileNameWithoutExtension(filepath);
            string ext = Path.GetExtension(filepath);
            if (".gz".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                ext = Path.GetExtension(basename) + ext;
                basename = Path.GetFileNameWithoutExtension(basename);
            }
            string newpath = null;
            int i = 0;
            do
            {
                string file = string.Format("{0}_{1}{2}", basename, i, ext);
                newpath = Path.Combine(dir, file);
                ++i;
            } while (File.Exists(newpath));

            return newpath;
        }

        /// <summary>
        /// ファイル名を次のルールで求める。
        /// (1) filepath に該当するファイルが存在しない場合は
        /// そのファイル名を求めるファイル名とする。
        /// (2) filepath に該当するファイルが存在する場合は、
        /// 枝番が付与されたファイル名を作成する。
        /// </summary>
        /// <param name="filepath">ファイルのパス</param>
        /// <returns>重複しない名前で作成した、書き込み可能なファイルストリーム。</returns>
        public static FileStream CreateNextFile(string filepath)
        {
            if (filepath != NextFileName(filepath))
            {
                System.Threading.Thread.Sleep(new Random().Next(100));
            }
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    return File.Open(NextFileName(filepath), FileMode.Create, FileAccess.Write, FileShare.Read);
                }
                catch (IOException)
                {
                    if (i == 3)
                    {
                        throw;
                    }
                    System.Threading.Thread.Sleep(new Random().Next(100));
                }
            }
            throw new IOException("ファイルを作成できませんでした。");
        }

        /// <summary>
        /// ファイルを移動します。
        /// 移動先に同じファイル名が存在する場合、ファイル名の後に枝番を付与して移動します。
        /// </summary>
        /// <param name="sourceFilePath">移動元ファイル</param>
        /// <param name="destDirectoryName">移動先フォルダ</param>
        /// <param name="destFileName">移動先ファイル名</param>
        /// <param name="outFilePath">異動されたファイル名</param>
        /// <returns>成功可否</returns>
        public static bool NextFileMove(string sourceFilePath, string destDirectoryName, string destFileName, out string outFilePath)
        {
            outFilePath = null;
            if (!File.Exists(sourceFilePath))
            {
                return false;
            }
            if (string.IsNullOrEmpty(destFileName))
            {
                destFileName = Path.GetFileName(sourceFilePath);
            }

            if (!Directory.Exists(destDirectoryName))
            {
                try
                {
                    Directory.CreateDirectory(destDirectoryName);
                }
                catch
                {
                    return false;
                }
            }
            if (!HasWriteAccessToPath(destDirectoryName))
            {
                return false;
            }
            if (sourceFilePath == Path.Combine(destDirectoryName, destFileName))
            {
                return true;
            }

            string tmpFilePath = string.Format("{0}.{1}tmp", sourceFilePath, new Random().Next(10));
            try
            {
                if (File.Exists(tmpFilePath))
                {
                    File.Delete(tmpFilePath);
                }
                File.Move(sourceFilePath, tmpFilePath);
            }
            catch
            {
                return false;
            }
            outFilePath = NextFileName(Path.Combine(destDirectoryName, destFileName));
            try
            {
                File.Move(tmpFilePath, outFilePath);
            }
            catch
            {
                outFilePath = null;
                // 元に戻す
                File.Move(tmpFilePath, sourceFilePath);
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 対象ディレクトリに書き込みが可能か確認します。
        /// </summary>
        /// <param name="filePath">対象ディレクトリ</param>
        /// <returns>可能の場合はtrue</returns>
        public static bool HasWriteAccessToPath(string filePath)
        {
            try
            {
                System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(filePath);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// ファイルがZIPファイルか確認します。
        /// </summary>
        /// <param name="filepath">ファイルのパス</param>
        /// <returns>確認結果</returns>
        public static bool IsZipFile(string filepath)
        {
            return FileStartWith(filepath, 80, 75, 3, 4);
        }

        /// <summary>
        /// ファイルがGZIPファイルか確認します。
        /// </summary>
        /// <param name="filepath">ファイルのパス</param>
        /// <returns>確認結果</returns>
        public static bool IsGzipFile(string filepath)
        {
            return FileStartWith(filepath, 31, 139, 8);
        }

        /// <summary>
        /// ファイルの先頭バイトを比較します。
        /// </summary>
        /// <param name="filepath">ファイルのパス</param>
        /// <param name="expected">比較バイトの配列</param>
        /// <returns>比較結果</returns>
        public static bool FileStartWith(string filepath, params byte[] expected)
        {
            using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (stream.Length < expected.Length)
                {
                    return false;
                }

                byte[] sig = new byte[expected.Length];
                stream.Read(sig, 0, expected.Length);
                return expected.SequenceEqual(sig);
            }
        }

        /// <summary>
        /// パスとファイル検索パターンを分離します。
        /// 例)"C:\pathname\*.xml"⇒"C:\pathname\","*.xml"
        /// </summary>
        /// <param name="fullpathPattern">解析するパターン</param>
        /// <param name="path">パス</param>
        /// <param name="pattern">ファイル名パターン</param>
        public static void TryExtractPathAndPattern(string fullpathPattern, out string path, out string pattern)
        {
            path = string.Empty;
            pattern = string.Empty;
            if (string.IsNullOrEmpty(fullpathPattern))
            {
                return;
            }

            if (!fullpathPattern.Contains('\\'))
            {
                pattern = fullpathPattern;
            }
            else
            {
                int lastindex = fullpathPattern.LastIndexOf('\\');
                path = fullpathPattern.Substring(0, lastindex + 1);
                pattern = fullpathPattern.Substring(lastindex + 1);
            }
        }

        /// <summary>
        /// 特定のフォルダーからファイルを検索する。
        /// </summary>
        /// <param name="path">フォルダー</param>
        /// <param name="searchPattern">ファイル検索パターン</param>
        /// <returns>ファイル名の自然順で並べた一致ファイル。</returns>
        public static FileInfo[] FindFiles(string path, string searchPattern)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles(searchPattern);
            var result = files.OrderBy(file => file.Name, new LogicalStringComparer());
            return result.ToArray();
        }

        /// <summary>
        /// 現在実行中EXEファイルが格納されているPathを返却します。
        /// </summary>
        /// <returns>fullpath</returns>
        public static string GetExecuteDirectory()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            else
            {
                return Path.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// 出力ファイルのパスをトレース用のテンプレート
        /// から求める。日付プレースホルダーはdtの日付
        /// に設定される。
        /// </summary>
        /// <param name="pathTemplate">パスのテンプレート</param>
        /// <param name="dt">テンプレートの日付プレースホルダーの値</param>
        /// <returns>日付とコマンドライン引数のプレースホルダーを置換したパス。</returns>
        public static string ResolvePathFromTemplate(string pathTemplate, DateTime dt)
        {
            if (pathTemplate.Contains("%YYYYMMDD%"))
            {
                pathTemplate = pathTemplate.Replace("%YYYYMMDD%", dt.ToString("yyyyMMdd"));
            }
            if (pathTemplate.Contains("%HHMMSS%"))
            {
                pathTemplate = pathTemplate.Replace("%HHMMSS%", dt.ToString("HHmmss"));
            }
            if (pathTemplate.Contains("%"))
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 0 && pathTemplate.Contains("%0"))
                {
                    pathTemplate = pathTemplate.Replace("%0", args[0]);
                }
                if (args.Length > 1 && pathTemplate.Contains("%1"))
                {
                    pathTemplate = pathTemplate.Replace("%1", args[1]);
                }
                if (args.Length > 2 && pathTemplate.Contains("%2"))
                {
                    pathTemplate = pathTemplate.Replace("%2", args[2]);
                }
            }
            return pathTemplate;
        }
    }
}
