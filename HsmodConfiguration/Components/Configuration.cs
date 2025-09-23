using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
namespace HsmodConfiguration.Components
{
    public class Configuration
    {
        public static bool firstOpen = true;
        private static HttpClient Http = new HttpClient();
        private static readonly Regex ConfigRegex = new Regex(@"^\s*(?<key>[^=\s]+)\s*=\s*(?<value>.*)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex MetadataRegex = new Regex(@"^##?\s*(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
        public static string url = "localhost";
        public static string port = "58744";
        public static int pid;
        public static bool login;
        public static Dictionary<string, CfgData>? hsmodcfg = new Dictionary<string, CfgData>();
        public static Dictionary<string, Dictionary<string, string>>? skins = new Dictionary<string, Dictionary<string, string>>();
        public static bool isSkinsLoad = false;
        public static bool changed = false;
        public static string GamePath = "";
        public static string? pluginVersion = null;
        public static bool isInstalled = true;
        public static async Task getAlive()
        {

            try
            {
                // 发送GET请求
                HttpResponseMessage response = await Http.GetAsync($"http://{url}:{port}/alive");
                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();

                // 解析JSON数据
                var jsonData = JsonSerializer.Deserialize<AliveData>(jsonResponse);

                if (jsonData?.pid != null)
                {
                    pid = jsonData.pid;
                    if (jsonData.login == "True")
                    {
                        login = true;
                    }
                    else
                    {
                        login = false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                login = false;
                pid = 0;
            }
        }
        public static async Task PickFileAsync()
        {
            var customFileType = new FilePickerFileType(
    new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, new[] { ".exe" } }, // Windows 平台限制为 .exe 文件
        { DevicePlatform.iOS, new[] { "public.executable" } }, // iOS 平台
        { DevicePlatform.Android, new[] { "application/x-ms-dos-executable" } }, // Android 平台
        { DevicePlatform.macOS, new[] { "public.executable" } }, // macOS 平台
        { DevicePlatform.Tizen, new[] { "*/*" } } // Tizen 平台（通用类型）
    });
            var options = new PickOptions
            {
                PickerTitle = "请选择 Hearthstone.exe",
                FileTypes = customFileType
            };

            try
            {
                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    if (result.FileName == "Hearthstone.exe")
                    {
                        GamePath = result.FullPath;
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常
            }
        }

        public static async Task getPluginVersion()
        {
            try
            {
                if (GamePath != "" & File.Exists(GamePath))
                {
                    string dllDir = $@"{System.IO.Path.GetDirectoryName(GamePath)}\BepInEx\plugins\HsMod.dll";
                    if (File.Exists(dllDir))
                    {
                        pluginVersion = FileVersionInfo.GetVersionInfo(dllDir).FileVersion ?? "Unknown";
                    }
                    else
                    {
                        pluginVersion = null;
                    }

                }
            }
            catch (Exception ex) { }
        }
        public static async Task getGamePath()
        {
            try
            {
                using (RegistryKey baseKey = Registry.LocalMachine)
                {
                    using (RegistryKey subKey = baseKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Hearthstone"))
                    {
                        if (subKey != null)
                        {
                            var saveSubkeyNames = subKey.GetSubKeyNames();
                            object path = subKey.GetValue("DisplayIcon"); // 获取默认值
                            if (path is string && path.ToString().EndsWith("Hearthstone.exe"))
                            {
                                GamePath = path.ToString();
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描注册表失败: {ex.Message}");
            }
        }
        public static async Task save()
        {
            try
            {
                if (hsmodcfg == null) { return; }
                foreach (CfgData val in hsmodcfg.Values)
                {
                    if (val.changed)
                    {

                        var requestData = new RequestData
                        {
                            key = val.key,
                            value = val.value
                        };
                        string json = JsonSerializer.Serialize(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await Http.PostAsync($"http://{url}:{port}/config", content);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseBody);


                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
        }
        public static async Task getSkinsData()
        {
            try
            {
                skins = new Dictionary<string, Dictionary<string, string>>();
                // 发送GET请求
                HttpResponseMessage response = await Http.GetAsync($"http://{url}:{port}/skins.log");
                response.EnsureSuccessStatusCode();
                string Response = await response.Content.ReadAsStringAsync();
                string[] lines = Response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0)
                {
                    Configuration.isSkinsLoad = false;
                }
                else
                {
                    Configuration.isSkinsLoad = true;
                }
                List<string> kindlist = ["硬币", "卡背", "对战面板", "酒馆战斗面板", "酒馆击杀特效", "鲍勃", "宠物", "对手宠物"];
                foreach (string k in kindlist)
                {
                    skins.Add(k, new Dictionary<string, string>());
                    skins[k].Add("-1", "不做修改");
                    if (k == "宠物" || k == "对手宠物")
                    {
                        skins[k].Add("0", "隐藏");
                    }
                }
                string kind = "";
                foreach (string line in lines)
                {
                    if (line.StartsWith("#"))
                    {
                        switch (ExtractTextBetween(line, "获取到", "如下："))
                        {
                            case "硬币皮肤":
                                kind = "硬币";
                                break;
                            case "卡背信息":
                                kind = "卡背";
                                break;
                            case "游戏面板信息":
                                kind = "对战面板";
                                break;
                            case "酒馆战斗面板":
                                kind = "酒馆战斗面板";
                                break;
                            case "酒馆终结特效":
                                kind = "酒馆击杀特效";
                                break;
                            case "英雄皮肤（包括酒馆）":
                                kind = "皮肤";
                                break;
                            case "宠物皮肤":
                                kind = "宠物";
                                break;
                        }
                        continue;
                    }
                    if (kind == "皮肤")
                    {
                        var word = line.Split("\t");
                        var skinkind = "";
                        switch (word[2])
                        {
                            case "BATTLEGROUNDS_HERO":
                                skinkind = "酒馆英雄";
                                break;
                            case "BATTLEGROUNDS_GUIDE":
                                skinkind = "鲍勃";
                                break;
                            default:
                                skinkind = "对战英雄";
                                break;
                        }
                        if (!skins.ContainsKey(skinkind))
                        {
                            skins.Add(skinkind, new Dictionary<string, string>());
                            skins[skinkind].Add("-1", "不做修改");
                        }

                        skins[skinkind].Add(word[0], word[1]);
                    }
                    else if (kind == "宠物")
                    {
                        var word = line.Split("\t");
                        string[] kinds = ["宠物", "对手宠物"];
                        foreach (string k in kinds)
                        {
                            skins[k].Add(word[0], word[1]);
                        }

                    }
                    else
                    {
                        var word = line.Split("\t");
                        if (!skins.ContainsKey(kind))
                        {
                            skins.Add(kind, new Dictionary<string, string>());
                            skins[kind].Add("-1", "不做修改");
                        }

                        skins[kind].Add(word[0], word[1]);
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
        }
        public static async Task getHsmodCfg()
        {

            try
            {
                // 发送GET请求
                HttpResponseMessage response = await Http.GetAsync($"http://{url}:{port}/hsmod.cfg");
                response.EnsureSuccessStatusCode();

                string cfgResponse = await response.Content.ReadAsStringAsync();

                string[] blocks = cfgResponse.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);
                var data = new Dictionary<string, CfgData>();
                foreach (string block in blocks)
                {
                    var matches = ConfigRegex.Matches(block);
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            string key = match.Groups["key"].Value.Trim();
                            string value = match.Groups["value"].Value.Trim();
                            var metadataMatches = MetadataRegex.Matches(block);
                            string type = "";
                            string defaultValue = "";
                            string ps = "";
                            string acceptValue = "";
                            string acceptableRange = "";
                            foreach (Match metadataMatch in metadataMatches)
                            {
                                string comment = metadataMatch.Groups[1].Value.Trim();

                                if (comment.StartsWith("Setting type:"))
                                {
                                    type = comment.Substring("Setting type:".Length).Trim();
                                }
                                else if (comment.StartsWith("Default value:"))
                                {
                                    defaultValue = comment.Substring("Default value:".Length).Trim();
                                }
                                else if (comment.StartsWith("Acceptable values:"))
                                {
                                    acceptValue = comment.Substring("Acceptable values:".Length).Trim();
                                }
                                else if (comment.StartsWith("Acceptable value range:"))
                                {
                                    acceptableRange = comment.Substring("Acceptable value range:".Length).Trim();
                                }
                                else
                                {
                                    ps = comment;
                                }
                            }
                            data[key] = new CfgData(key, type, value, defaultValue, ps, acceptValue, acceptableRange);

                        }
                    }
                }
                hsmodcfg = data;
                changed = false;
            }

            catch (Exception e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                login = false;
                pid = 0;
            }
        }
        public static string ExtractTextBetween(string input, string startMarker, string endMarker)
        {
            // 构造正则表达式
            string pattern = $"{Regex.Escape(startMarker)}(.*?){Regex.Escape(endMarker)}";

            // 匹配字符串
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                // 返回匹配到的内容
                return match.Groups[1].Value.Trim();
            }
            else
            {
                // 如果没有匹配到，返回空字符串
                return string.Empty;
            }
        }

        private Dictionary<string, string> ParseConfig(string cfg)
        {
            var config = new Dictionary<string, string>();
            var matches = ConfigRegex.Matches(cfg);
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string key = match.Groups["key"].Value.Trim();
                    string value = match.Groups["value"].Value.Trim();
                    config[key] = value;
                }
            }
            return config;
        }
        // 定义一个类来映射JSON结构
        public class AliveData
        {
            public int pid { get; set; }
            public required string login { get; set; }
        }
        public class RequestData
        {
            public string? key { get; set; }
            public string? value { get; set; }
        }
    }
    public class CfgData
    {
        public string key { get; set; }
        public Type type { get; set; }
        public string value;
        public string stringValue
        {
            get { return this.value; }
            set
            {
                this.value = value;
                Configuration.changed = true;
                this.changed = true;
            }
        }
        public int intValue
        {
            get
            {
                return int.Parse(this.value);
            }
            set
            {
                this.value = value.ToString();
                Configuration.changed = true;
                this.changed = true;
            }
        }
        public bool boolValue
        {
            get
            {
                return Convert.ToBoolean(this.value);
            }
            set
            {
                this.value = value.ToString();
                Configuration.changed = true;
                this.changed = true;
            }
        }
        public string tranValue
        {
            get
            {
                return Configuration.skins[this.key][this.value];
            }
            set
            {
                this.value = Configuration.skins[this.key].FirstOrDefault(q => q.Value == value.ToString()).Key;
                Configuration.changed = true;
                this.changed = true;
            }

        }
        public bool changed = false;
        public string defaultValue { get; set; }
        public string ps { get; set; }
        public List<string>? acceptValue { get; set; }
        public List<int>? acceptableRange { get; set; }

        public CfgData(string key, string type, string value, string defaultValue, string ps, string acceptValue, string acceptableRange)
        {
            this.key = key;
            if (type == "Boolean")
            {
                this.type = typeof(bool);
                this.acceptValue = null;
                this.acceptableRange = null;
            }
            else if (type == "Int32")
            {
                this.type = typeof(int);
                this.acceptValue = null;
                this.acceptableRange = System.Text.RegularExpressions.Regex.Matches(acceptableRange, @"-?\d+")
        .Cast<System.Text.RegularExpressions.Match>()
        .Select(match => int.Parse(match.Value))
        .ToList();
            }
            else
            {
                this.type = typeof(string);
                this.acceptValue = acceptValue.Split(',')
                             .Select(word => word.Trim()) // 去除多余的空格
                             .ToList();
                this.acceptableRange = null;
            }
            this.value = value;
            this.defaultValue = defaultValue;
            this.ps = ps;
            this.stringValue = value;
            this.changed = false;
        }

        public T getValue<T>()
        {
            Type t = typeof(T);
            if (t == typeof(int))
            {
                return (T)(object)int.Parse(value);
            }
            else if (t == typeof(string))
            {
                return (T)(object)value;
            }
            else if (t == typeof(bool))
            {
                return (T)(object)Convert.ToBoolean(value);
            }
            return default(T);
        }
    }
}
