// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Text.Json;

namespace RunCatLite
{
    /// <summary>
    /// 便携式配置管理器
    /// 配置文件存储在应用程序目录下的 config.json
    /// </summary>
    internal class PortableSettings
    {
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        private static readonly object _lock = new();
        private static PortableSettings? _instance;

        // 配置项
        public string Runner { get; set; } = "cat";
        public string Theme { get; set; } = "";
        public string FPSMaxLimit { get; set; } = "FPS40";
        public bool FirstLaunch { get; set; } = true;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static PortableSettings Default
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= Load();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        private static PortableSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var settings = JsonSerializer.Deserialize<PortableSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load config: {ex.Message}");
            }

            return new PortableSettings();
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void Save()
        {
            try
            {
                lock (_lock)
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    var json = JsonSerializer.Serialize(this, options);
                    File.WriteAllText(ConfigPath, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void Reload()
        {
            var loaded = Load();
            Runner = loaded.Runner;
            Theme = loaded.Theme;
            FPSMaxLimit = loaded.FPSMaxLimit;
            FirstLaunch = loaded.FirstLaunch;
        }
    }
}
