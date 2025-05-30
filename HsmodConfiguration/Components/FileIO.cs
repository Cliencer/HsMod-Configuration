using System.IO.Compression;

namespace HsmodConfiguration.Components
{
    internal class FileIO
    {
        public static async Task CopyDirectory(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }
            // 使用队列来处理目录
            Queue<(string Source, string Destination)> queue = new();
            queue.Enqueue((sourceDir, destinationDir));

            while (queue.Count > 0)
            {
                (string Source, string Destination) current = queue.Dequeue();

                // 复制当前目录中的所有文件
                string[] files = Directory.GetFiles(current.Source, "*.*", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    string destFile = Path.Combine(current.Destination, Path.GetFileName(file));
                    File.Copy(file, destFile, overwrite: true); // 覆盖目标文件
                }

                // 处理当前目录中的所有子目录（包括空文件夹）
                string[] subDirs = Directory.GetDirectories(current.Source, "*.*", SearchOption.TopDirectoryOnly);
                foreach (string subDir in subDirs)
                {
                    string destSubDir = Path.Combine(current.Destination, Path.GetFileName(subDir));
                    Directory.CreateDirectory(destSubDir); // 创建目标子目录（即使它是空的）
                    queue.Enqueue((subDir, destSubDir)); // 将子目录加入队列
                }
            }
        }
        public async static Task DeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true); // 递归删除目录及其所有内容
                Console.WriteLine($"目录已删除: {directoryPath}");
            }
            else
            {
                Console.WriteLine($"目录不存在: {directoryPath}");
            }
        }
        public async static Task CreateDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"文件夹已创建: {directoryPath}");
            }
            else
            {
                Console.WriteLine($"文件夹已存在: {directoryPath}");
            }
        }
        public async static Task ExtractZipFile(string zipFilePath, string destinationDirectory)
        {
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            ZipFile.ExtractToDirectory(zipFilePath, destinationDirectory);
        }
    }
}
