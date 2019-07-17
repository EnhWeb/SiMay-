using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SiMay.Core.Common
{
    public class FileHelper
    {
        public static bool VerifyLongPath(string path)
        {
            return path.Length >= 260 || path.Substring(0, path.LastIndexOf("\\")).Length >= 248;
        }
        public static bool VerifyFileName(string name)
        {
            if (name.IndexOf("|") > -1 ||
                name.IndexOf("/") > -1 ||
                name.IndexOf("\\") > -1 ||
                name.IndexOf("*") > -1 ||
                name.IndexOf("?") > -1 ||
                name.IndexOf("\"") > -1 ||
                name.IndexOf("<") > -1 ||
                name.IndexOf(">") > -1 ||
                name.IndexOf(":") > -1)
            {
                return false;
            }

            return true;
        }

        public static string LengthToFileSize(double len)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            string filesize = String.Format("{0:0.##} {1}", len, sizes[order]);
            return filesize;
        }

        public static void CopyDirectory(string srcdir, string desdir)
        {
            string folderName = srcdir.Substring(srcdir.LastIndexOf("\\") + 1);

            string desfolderdir = desdir + "\\" + folderName;

            if (desdir.LastIndexOf("\\") == (desdir.Length - 1))
            {
                desfolderdir = desdir + folderName;
            }
            string[] filenames = Directory.GetFileSystemEntries(srcdir);

            foreach (string file in filenames)
            {
                if (Directory.Exists(file))
                {
                    string currentdir = desfolderdir + "\\" + file.Substring(file.LastIndexOf("\\") + 1);
                    if (!Directory.Exists(currentdir))
                    {
                        Directory.CreateDirectory(currentdir);
                    }

                    CopyDirectory(file, desfolderdir);
                }
                else
                {
                    string srcfileName = file.Substring(file.LastIndexOf("\\") + 1);

                    srcfileName = desfolderdir + "\\" + srcfileName;

                    if (!Directory.Exists(desfolderdir))
                    {
                        Directory.CreateDirectory(desfolderdir);
                    }
                    File.Copy(file, srcfileName);
                }
            }
        }
    }
}
