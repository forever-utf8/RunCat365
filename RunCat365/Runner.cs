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

namespace RunCatLite
{
    internal static class RunnerManager
    {
        private static string GetRunnersPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "runners");
        }

        /// <summary>
        /// 扫描 runners 目录获取所有可用的角色列表
        /// </summary>
        internal static List<string> GetAvailableRunners()
        {
            var runnersPath = GetRunnersPath();
            var runners = new List<string>();

            if (!Directory.Exists(runnersPath))
                return runners;

            foreach (var dir in Directory.GetDirectories(runnersPath))
            {
                var runnerName = Path.GetFileName(dir);
                // 检查目录中是否有 .ico 文件
                if (Directory.GetFiles(dir, "*.ico").Length > 0)
                {
                    runners.Add(runnerName);
                }
            }

            return runners;
        }

        /// <summary>
        /// 获取角色的显示名称（首字母大写）
        /// </summary>
        internal static string GetDisplayName(string runnerName)
        {
            if (string.IsNullOrEmpty(runnerName))
                return "";
            return char.ToUpper(runnerName[0]) + runnerName.Substring(1);
        }

        /// <summary>
        /// 加载角色的图标帧列表
        /// 文件命名格式: {themeName}_{i}.ico (例如: light_0.ico, dark_0.ico)
        /// </summary>
        internal static List<Icon> LoadIcons(string runnerName, string themeName)
        {
            var runnerDir = Path.Combine(GetRunnersPath(), runnerName);
            var icons = new List<Icon>();

            if (!Directory.Exists(runnerDir))
                return icons;

            // 加载 {themeName}_{i}.ico 格式的图标
            int i = 0;
            while (true)
            {
                var iconPath = Path.Combine(runnerDir, $"{themeName}_{i}.ico");
                if (File.Exists(iconPath))
                {
                    try
                    {
                        icons.Add(new Icon(iconPath));
                    }
                    catch { }
                    i++;
                }
                else
                {
                    break;
                }
            }

            // 如果没有找到，尝试加载所有 ico（按文件名排序）
            if (icons.Count == 0)
            {
                var allIcos = Directory.GetFiles(runnerDir, "*.ico")
                    .OrderBy(f => f)
                    .ToList();

                foreach (var file in allIcos)
                {
                    try
                    {
                        icons.Add(new Icon(file));
                    }
                    catch { }
                }
            }

            return icons;
        }

        /// <summary>
        /// 获取角色的缩略图
        /// </summary>
        internal static Bitmap? GetThumbnail(string runnerName, string themeName)
        {
            var runnerDir = Path.Combine(GetRunnersPath(), runnerName);
            if (!Directory.Exists(runnerDir))
                return null;

            // 尝试匹配 {themeName}_0.ico
            var iconPath = Path.Combine(runnerDir, $"{themeName}_0.ico");

            // 如果不存在，尝试第一个 ico 文件
            if (!File.Exists(iconPath))
            {
                var allIcos = Directory.GetFiles(runnerDir, "*.ico")
                    .OrderBy(f => f)
                    .ToArray();

                if (allIcos.Length > 0)
                {
                    iconPath = allIcos[0];
                }
                else
                {
                    return null;
                }
            }

            try
            {
                using var icon = new Icon(iconPath);
                return icon.ToBitmap();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 验证角色是否存在
        /// </summary>
        internal static bool IsValidRunner(string runnerName)
        {
            if (string.IsNullOrEmpty(runnerName))
                return false;
            var runnerDir = Path.Combine(GetRunnersPath(), runnerName);
            return Directory.Exists(runnerDir) && Directory.GetFiles(runnerDir, "*.ico").Length > 0;
        }

        /// <summary>
        /// 获取默认角色（第一个可用的）
        /// </summary>
        internal static string GetDefaultRunner()
        {
            var runners = GetAvailableRunners();
            return runners.Count > 0 ? runners[0] : "";
        }
    }
}
