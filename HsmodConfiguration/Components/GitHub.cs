using System.Text.Json;
namespace HsmodConfiguration.Components
{
    public class GitHub
    {
        public static int Downloadprogress = 0;
        public static async Task<(string url, string version)> GetLatestReleaseDownloadUrl(string repositoryOwner, string repositoryName)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = $"http://api.github.com/repos/{repositoryOwner}/{repositoryName}/releases/latest";
                string myApiUrl = $"http://api.github.com/repos/cliencer/{repositoryName}/releases/latest";
				client.DefaultRequestHeaders.Add("User-Agent", "Other");
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
					response = await client.GetAsync(myApiUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("网络错误，无法获取Github资源，请手动下载HsMod.dll并复制插件到游戏安装目录下BepInEx\\plugins文件夹。");
                    }
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JsonDocument document = JsonDocument.Parse(jsonResponse);

                // 获取最新版本的下载链接
                JsonElement root = document.RootElement;
                JsonElement assets = root.GetProperty("assets");
                string version = root.GetProperty("name").GetString();
                foreach (JsonElement asset in assets.EnumerateArray())
                {
                    string browserDownloadUrl = asset.GetProperty("browser_download_url").GetString();
                    if (browserDownloadUrl != null)
                    {
                        return (browserDownloadUrl, version);
                    }
                }
            }
            return ("", "");
        }
        public static async Task DownloadFileWithProgressAsync(string fileUrl, string savePath)
        {
            Downloadprogress = 0;
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                long downloadedBytes = 0;

                using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            // 更新进度条
                            UpdateProgress(downloadedBytes, totalBytes);
                        }
                    }
                }
            }
        }

        // 进度条更新方法
        public static void UpdateProgress(long downloadedBytes, long? totalBytes)
        {
            if (totalBytes.HasValue)
            {
                Downloadprogress = (int)((downloadedBytes * 100) / totalBytes.Value);
            }
            else
            {
                Console.Write($"\r已下载: {downloadedBytes} bytes");
            }
        }
        public class GitHubRelease
        {
            public string Name { get; set; }
            public string DownloadUrl { get; set; }
        }
    }
}
