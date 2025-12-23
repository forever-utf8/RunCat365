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

using QSoft.Apng;
using System.Drawing.Imaging;

namespace RunCatLite
{
    /// <summary>
    /// 代表一个可用的角色（动画）
    /// </summary>
    internal class RunnerInfo
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }

    internal static class RunnerManager
    {
        private static readonly string[] SupportedExtensions = [".png", ".gif"];

        private static string GetRunnersPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "runners");
        }

        /// <summary>
        /// 扫描 runners 目录获取所有可用的角色列表
        /// 返回文件名（不含扩展名）作为角色标识
        /// </summary>
        internal static List<RunnerInfo> GetAvailableRunners()
        {
            var runnersPath = GetRunnersPath();
            var runners = new List<RunnerInfo>();

            if (!Directory.Exists(runnersPath))
                return runners;

            var files = Directory.GetFiles(runnersPath)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .ToList();

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                runners.Add(new RunnerInfo
                {
                    FileName = fileName,
                    FilePath = file,
                    DisplayName = fileName  // 直接使用文件名作为显示名称
                });
            }

            return runners;
        }

        /// <summary>
        /// 加载动画的所有帧
        /// 支持 APNG、GIF、静态 PNG
        /// </summary>
        internal static List<Icon> LoadFrames(string runnerName)
        {
            var runnersPath = GetRunnersPath();
            var icons = new List<Icon>();

            // 查找匹配的文件
            string? filePath = null;
            foreach (var ext in SupportedExtensions)
            {
                var path = Path.Combine(runnersPath, runnerName + ext);
                if (File.Exists(path))
                {
                    filePath = path;
                    break;
                }
            }

            if (filePath == null)
                return icons;

            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (extension == ".gif")
                {
                    icons = LoadGifFrames(filePath);
                }
                else if (extension == ".png")
                {
                    icons = LoadPngFrames(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading frames from {filePath}: {ex.Message}");
            }

            return icons;
        }

        /// <summary>
        /// 加载 GIF 动画帧
        /// </summary>
        private static List<Icon> LoadGifFrames(string filePath)
        {
            var icons = new List<Icon>();

            using var image = Image.FromFile(filePath);
            var dimension = new FrameDimension(image.FrameDimensionsList[0]);
            int frameCount = image.GetFrameCount(dimension);

            for (int i = 0; i < frameCount; i++)
            {
                image.SelectActiveFrame(dimension, i);
                var frame = new Bitmap(image);
                var icon = BitmapToIcon(frame);
                if (icon != null)
                {
                    icons.Add(icon);
                }
                frame.Dispose();
            }

            return icons;
        }

        /// <summary>
        /// 加载 PNG（包括 APNG）帧
        /// </summary>
        private static List<Icon> LoadPngFrames(string filePath)
        {
            var icons = new List<Icon>();

            // 先尝试作为 APNG 解析
            try
            {
                using var fileStream = File.OpenRead(filePath);
                var pngReader = new Png_Reader();
                var frames = pngReader.Open(fileStream).SpltAPng();

                if (frames != null && frames.Count > 1)
                {
                    // 这是一个 APNG，有多帧
                    foreach (var frameData in frames)
                    {
                        using var ms = new MemoryStream(frameData.Value.ToArray());
                        using var frameBitmap = new Bitmap(ms);
                        var icon = BitmapToIcon(frameBitmap);
                        if (icon != null)
                        {
                            icons.Add(icon);
                        }
                    }
                    return icons;
                }
            }
            catch
            {
                // APNG 解析失败，当作静态 PNG 处理
            }

            // 作为静态 PNG 处理
            try
            {
                using var bitmap = new Bitmap(filePath);
                var icon = BitmapToIcon(bitmap);
                if (icon != null)
                {
                    icons.Add(icon);
                }
            }
            catch
            {
                // 忽略加载错误
            }

            return icons;
        }

        /// <summary>
        /// 获取动画的第一帧作为缩略图
        /// </summary>
        internal static Bitmap? GetThumbnail(string runnerName)
        {
            var runnersPath = GetRunnersPath();

            // 查找匹配的文件
            string? filePath = null;
            foreach (var ext in SupportedExtensions)
            {
                var path = Path.Combine(runnersPath, runnerName + ext);
                if (File.Exists(path))
                {
                    filePath = path;
                    break;
                }
            }

            if (filePath == null)
                return null;

            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (extension == ".gif")
                {
                    using var image = Image.FromFile(filePath);
                    return new Bitmap(image);
                }
                else if (extension == ".png")
                {
                    // 对于 APNG，尝试获取第一帧
                    try
                    {
                        using var fileStream = File.OpenRead(filePath);
                        var pngReader = new Png_Reader();
                        var frames = pngReader.Open(fileStream).SpltAPng();

                        if (frames != null && frames.Count > 0)
                        {
                            var firstFrame = frames.First();
                            using var ms = new MemoryStream(firstFrame.Value.ToArray());
                            return new Bitmap(ms);
                        }
                    }
                    catch
                    {
                        // APNG 解析失败，当作静态 PNG
                    }

                    return new Bitmap(filePath);
                }
            }
            catch
            {
                // 忽略错误
            }

            return null;
        }

        /// <summary>
        /// 验证角色是否存在
        /// </summary>
        internal static bool IsValidRunner(string runnerName)
        {
            if (string.IsNullOrEmpty(runnerName))
                return false;

            var runnersPath = GetRunnersPath();
            return SupportedExtensions.Any(ext =>
                File.Exists(Path.Combine(runnersPath, runnerName + ext)));
        }

        /// <summary>
        /// 获取默认角色（第一个可用的）
        /// </summary>
        internal static string GetDefaultRunner()
        {
            var runners = GetAvailableRunners();
            return runners.Count > 0 ? runners[0].FileName : "";
        }

        /// <summary>
        /// 将 Bitmap 转换为 Icon
        /// </summary>
        private static Icon? BitmapToIcon(Bitmap bitmap)
        {
            try
            {
                // 创建一个新的 Bitmap 副本以确保格式正确
                using var copy = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(copy))
                {
                    g.Clear(Color.Transparent);
                    g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                }

                var hIcon = copy.GetHicon();
                return Icon.FromHandle(hIcon);
            }
            catch
            {
                return null;
            }
        }
    }
}
