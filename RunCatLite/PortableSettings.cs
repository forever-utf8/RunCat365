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

using System.Reflection;
using System.Text.Json;

namespace RunCatLite
{
    /// <summary>
    /// 应用元数据和路径管理
    /// </summary>
    internal static class AppInfo
    {
        /// <summary>
        /// 应用名称
        /// </summary>
        public const string Name = "RunCat-Lite";

        /// <summary>
        /// 获取应用版本
        /// </summary>
        public static string Version
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            }
        }

        /// <summary>
        /// 是否为 Portable 模式
        /// 编译时通过 MSBuild 属性 PortableMode 设置
        /// </summary>
#if PORTABLE_MODE
        public static bool IsPortable => true;
#else
        public static bool IsPortable => false;
#endif

        /// <summary>
        /// 获取数据目录路径
        /// Portable 模式: 程序运行目录
        /// 非 Portable 模式: %APPDATA%/RunCat-Lite/{Version}
        /// </summary>
        public static string DataDirectory
        {
            get
            {
                if (IsPortable)
                {
                    return AppContext.BaseDirectory;
                }
                else
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var appDir = Path.Combine(appData, Name, Version);

                    // 确保目录存在
                    if (!Directory.Exists(appDir))
                    {
                        Directory.CreateDirectory(appDir);
                    }

                    return appDir;
                }
            }
        }

        /// <summary>
        /// 获取 Runners 目录路径
        /// Runners 始终在程序运行目录下
        /// </summary>
        public static string RunnersDirectory => Path.Combine(AppContext.BaseDirectory, "runners");
    }

    /// <summary>
    /// 配置管理器
    /// 根据 Portable 模式决定配置文件的存储位置：
    /// - Portable 模式：程序目录下的 config.json
    /// - 非 Portable 模式：%APPDATA%/RunCat-Lite/{Version}/config.json
    /// </summary>
    internal class PortableSettings
    {
        private static string ConfigPath => Path.Combine(AppInfo.DataDirectory, "config.json");
        private static readonly object _lock = new();
        private static PortableSettings? _instance;

        // 配置项
        public string Runner { get; set; } = "";  // 空字符串表示由程序自动选择第一个
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
                var configPath = ConfigPath;
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
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
                    var configPath = ConfigPath;

                    // 确保目录存在
                    var dir = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(configPath, json);
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
            FPSMaxLimit = loaded.FPSMaxLimit;
            FirstLaunch = loaded.FirstLaunch;
        }
    }
}
