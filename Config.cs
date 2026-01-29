using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CorelDrawAutoIgnoreError
{
    public class DialogRule
    {
        public string Name { get; set; }
        public List<string> WindowTitleContains { get; set; }
        public List<string> ContentContains { get; set; }
        public string ButtonToClick { get; set; }
    }

    public class Settings
    {
        public int CheckInterval { get; set; } = 100;
        public bool ShowNotifications { get; set; } = false;
    }

    public class Config
    {
        public List<DialogRule> DialogRules { get; set; }
        public Settings Settings { get; set; }

        public static Config Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<Config>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"配置加载失败: {ex.Message}");
            }

            // 返回默认配置
            return GetDefault();
        }

        public static Config GetDefault()
        {
            return new Config
            {
                DialogRules = new List<DialogRule>
                {
                    new DialogRule
                    {
                        Name = "无效的轮廓ID错误",
                        WindowTitleContains = new List<string> { "文件", ".cdr", "CorelDRAW" },
                        ContentContains = new List<string> { "无效的轮廓", "忽略", "重试" },
                        ButtonToClick = "忽略"
                    },
                    new DialogRule
                    {
                        Name = "自动备份文件提示",
                        WindowTitleContains = new List<string> { "CorelDRAW" },
                        ContentContains = new List<string> { "自动备份", "是否要打开" },
                        ButtonToClick = "取消"
                    }
                },
                Settings = new Settings()
            };
        }

        public void Save(string path)
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"配置保存失败: {ex.Message}");
            }
        }
    }
}
