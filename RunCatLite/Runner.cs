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

    /// <summary>
    /// 图像颜色处理器 - 用于检测单色图像并根据主题重新着色
    /// </summary>
    internal static class ImageColorProcessor
    {
        // ============================================================
        // 单色检测阈值（调节这些值可以改变彩色/单色的判定）
        // 当前设置为"极端敏感"，只有真正的灰度图才会被判定为单色
        // ============================================================

        // 饱和度阈值：平均饱和度低于此值视为灰度图像
        // 值越小，越容易识别为彩色；值越大，越容易识别为单色
        // 范围: 0.0 - 1.0
        // 0.02 = 极端敏感，几乎只有纯灰度才是单色
        private const float SATURATION_THRESHOLD = 0.02f;

        // 色相方差阈值：色相方差低于此值视为单色（同一颜色）
        // 值越小，越容易识别为彩色；值越大，越容易识别为单色
        // 范围: 0 - 180
        // 5 = 极端敏感，颜色稍有变化就是彩色
        private const float HUE_VARIANCE_THRESHOLD = 5f;

        // 透明度阈值：Alpha 值高于此值的像素才纳入计算
        // 低于此值的像素视为透明，完全忽略
        private const int ALPHA_THRESHOLD = 30;

        // 彩色像素比例阈值：彩色像素超过此比例即视为彩色图像
        // 值越小，越容易识别为彩色
        // 0.01 = 只要有 1% 的彩色像素就是彩色图像
        private const float COLOR_PIXEL_RATIO_THRESHOLD = 0.01f;

        // 单个像素的饱和度阈值：超过此值视为"有颜色"的像素
        // 0.05 = 非常敏感，略有点颜色就算
        private const float PIXEL_SATURATION_THRESHOLD = 0.05f;
        // ============================================================

        /// <summary>
        /// 检测图像是否为单色（灰度或接近单一颜色）
        /// 全图扫描，透明像素不计入统计
        /// </summary>
        internal static bool IsMonochrome(Bitmap bitmap)
        {
            int totalOpaquePixels = 0;      // 不透明像素总数
            int coloredPixels = 0;          // 有颜色的像素数
            float totalSaturation = 0;      // 总饱和度
            var hues = new List<float>();   // 有颜色像素的色相列表

            // 全图扫描
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    // 跳过透明像素
                    if (pixel.A <= ALPHA_THRESHOLD)
                        continue;

                    totalOpaquePixels++;
                    float saturation = pixel.GetSaturation();
                    totalSaturation += saturation;

                    // 检测是否为有颜色的像素
                    if (saturation > PIXEL_SATURATION_THRESHOLD)
                    {
                        coloredPixels++;
                        hues.Add(pixel.GetHue());
                    }
                }
            }

            // 如果几乎没有不透明像素，视为单色
            if (totalOpaquePixels < 10)
                return true;

            // 方法1: 检查彩色像素比例
            float colorRatio = (float)coloredPixels / totalOpaquePixels;
            if (colorRatio > COLOR_PIXEL_RATIO_THRESHOLD)
            {
                // 有足够多的彩色像素，再检查色相是否分散
                if (hues.Count > 1)
                {
                    float hueVariance = CalculateCircularVariance(hues);
                    // 如果色相分散，肯定是彩色
                    if (hueVariance > HUE_VARIANCE_THRESHOLD)
                        return false; // 彩色图像
                }
                // 色相集中但有颜色 -> 单色（如纯红色图标）
                // 但如果饱和度够高，还是当彩色处理（保留原色）
                float avgSaturation = totalSaturation / totalOpaquePixels;
                if (avgSaturation > 0.3f)
                    return false; // 高饱和度单色，保留原色
            }

            // 方法2: 检查平均饱和度
            float avgSat = totalSaturation / totalOpaquePixels;
            if (avgSat > SATURATION_THRESHOLD)
            {
                // 有饱和度，但彩色像素不多，可能是低饱和度的彩色
                // 再宽松一点，只要有一定饱和度就算彩色
                return false;
            }

            // 真正的灰度图像
            return true;
        }

        /// <summary>
        /// 计算环形数据（如角度、色相）的方差
        /// </summary>
        private static float CalculateCircularVariance(List<float> angles)
        {
            if (angles.Count == 0) return 0;

            double sumSin = 0, sumCos = 0;
            foreach (var angle in angles)
            {
                double radians = angle * Math.PI / 180.0;
                sumSin += Math.Sin(radians);
                sumCos += Math.Cos(radians);
            }

            double meanSin = sumSin / angles.Count;
            double meanCos = sumCos / angles.Count;
            double r = Math.Sqrt(meanSin * meanSin + meanCos * meanCos);

            // 1 - r 是环形方差的一种度量，r 接近 1 表示数据集中
            return (float)((1 - r) * 180); // 转换为度数范围
        }

        /// <summary>
        /// 获取图像的主要亮度（用于确定是深色还是浅色图像）
        /// 全图扫描，透明像素不计入统计
        /// </summary>
        internal static float GetAverageBrightness(Bitmap bitmap)
        {
            float totalBrightness = 0;
            int count = 0;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    if (pixel.A > ALPHA_THRESHOLD)
                    {
                        totalBrightness += pixel.GetBrightness();
                        count++;
                    }
                }
            }

            return count > 0 ? totalBrightness / count : 0.5f;
        }

        /// <summary>
        /// 根据目标主题重新着色单色图像
        /// </summary>
        /// <param name="bitmap">原始位图</param>
        /// <param name="isDarkTheme">是否为暗色主题</param>
        /// <returns>重新着色后的位图（需要调用者释放）</returns>
        internal static Bitmap Recolor(Bitmap bitmap, bool isDarkTheme)
        {
            // 获取原图亮度，决定目标颜色
            float brightness = GetAverageBrightness(bitmap);

            // 目标颜色逻辑：
            // - 暗色主题 → 需要浅色图标（白色/浅灰）
            // - 亮色主题 → 需要深色图标（黑色/深灰）
            Color targetColor;
            if (isDarkTheme)
            {
                // 暗色主题，使用浅色
                targetColor = Color.FromArgb(240, 240, 240);
            }
            else
            {
                // 亮色主题，使用深色
                targetColor = Color.FromArgb(30, 30, 30);
            }

            return RecolorBitmap(bitmap, targetColor);
        }

        /// <summary>
        /// 使用 LockBits 高效地重新着色位图
        /// 保留原始透明度，将所有非透明像素的 RGB 替换为目标颜色
        /// </summary>
        private static unsafe Bitmap RecolorBitmap(Bitmap source, Color targetColor)
        {
            var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

            var sourceData = source.LockBits(
                new Rectangle(0, 0, source.Width, source.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var resultData = result.LockBits(
                new Rectangle(0, 0, result.Width, result.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                byte* srcPtr = (byte*)sourceData.Scan0;
                byte* dstPtr = (byte*)resultData.Scan0;
                int pixelCount = source.Width * source.Height;

                for (int i = 0; i < pixelCount; i++)
                {
                    int offset = i * 4;
                    byte alpha = srcPtr[offset + 3];

                    if (alpha > 0)
                    {
                        // 保留原始 alpha，替换 RGB
                        // 可选：根据原始亮度调整目标 alpha，保留细节
                        byte originalBrightness = (byte)((srcPtr[offset] + srcPtr[offset + 1] + srcPtr[offset + 2]) / 3);

                        // 保留边缘细节：根据原始亮度微调 alpha
                        // 这样可以保留抗锯齿效果
                        dstPtr[offset] = targetColor.B;     // Blue
                        dstPtr[offset + 1] = targetColor.G; // Green
                        dstPtr[offset + 2] = targetColor.R; // Red
                        dstPtr[offset + 3] = alpha;         // Alpha（保留原值）
                    }
                    else
                    {
                        // 完全透明的像素保持不变
                        dstPtr[offset] = 0;
                        dstPtr[offset + 1] = 0;
                        dstPtr[offset + 2] = 0;
                        dstPtr[offset + 3] = 0;
                    }
                }
            }
            finally
            {
                source.UnlockBits(sourceData);
                result.UnlockBits(resultData);
            }

            return result;
        }
    }

    internal static class RunnerManager
    {
        private static readonly string[] SupportedExtensions = [".png", ".gif"];

        private static string GetRunnersPath()
        {
            return AppInfo.RunnersDirectory;
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
        /// 加载动画的所有帧（带主题感知的自动着色）
        /// </summary>
        /// <param name="runnerName">角色名称</param>
        /// <param name="isDarkTheme">是否为暗色主题</param>
        internal static List<Icon> LoadFrames(string runnerName, bool isDarkTheme)
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
                List<Bitmap> frames;

                if (extension == ".gif")
                {
                    frames = LoadGifFramesAsBitmaps(filePath);
                }
                else // .png
                {
                    frames = LoadPngFramesAsBitmaps(filePath);
                }

                if (frames.Count == 0)
                    return icons;

                // 检测第一帧是否为单色图像
                bool isMonochrome = ImageColorProcessor.IsMonochrome(frames[0]);

                foreach (var frame in frames)
                {
                    Bitmap processedFrame = frame;

                    if (isMonochrome)
                    {
                        // 单色图像：根据主题重新着色
                        processedFrame = ImageColorProcessor.Recolor(frame, isDarkTheme);
                        frame.Dispose(); // 释放原始帧
                    }

                    var icon = BitmapToIcon(processedFrame);
                    if (icon != null)
                    {
                        icons.Add(icon);
                    }

                    if (isMonochrome)
                    {
                        processedFrame.Dispose(); // 释放处理后的帧
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading frames from {filePath}: {ex.Message}");
            }

            return icons;
        }

        /// <summary>
        /// 无主题参数的重载（默认使用亮色主题处理）
        /// </summary>
        internal static List<Icon> LoadFrames(string runnerName)
        {
            return LoadFrames(runnerName, isDarkTheme: false);
        }

        /// <summary>
        /// 加载 GIF 动画帧为 Bitmap 列表
        /// </summary>
        private static List<Bitmap> LoadGifFramesAsBitmaps(string filePath)
        {
            var frames = new List<Bitmap>();

            using var image = Image.FromFile(filePath);
            var dimension = new FrameDimension(image.FrameDimensionsList[0]);
            int frameCount = image.GetFrameCount(dimension);

            for (int i = 0; i < frameCount; i++)
            {
                image.SelectActiveFrame(dimension, i);
                frames.Add(new Bitmap(image));
            }

            return frames;
        }

        /// <summary>
        /// 加载 PNG（包括 APNG）帧为 Bitmap 列表
        /// </summary>
        private static List<Bitmap> LoadPngFramesAsBitmaps(string filePath)
        {
            var frames = new List<Bitmap>();

            // 先尝试作为 APNG 解析
            try
            {
                using var fileStream = File.OpenRead(filePath);
                var pngReader = new Png_Reader();
                var apngFrames = pngReader.Open(fileStream).SpltAPng();

                if (apngFrames != null && apngFrames.Count > 1)
                {
                    // 这是一个 APNG，有多帧
                    foreach (var frameData in apngFrames)
                    {
                        using var ms = new MemoryStream(frameData.Value.ToArray());
                        frames.Add(new Bitmap(ms));
                    }
                    return frames;
                }
            }
            catch
            {
                // APNG 解析失败，当作静态 PNG 处理
            }

            // 作为静态 PNG 处理
            try
            {
                frames.Add(new Bitmap(filePath));
            }
            catch
            {
                // 忽略加载错误
            }

            return frames;
        }

        /// <summary>
        /// 获取动画的第一帧作为缩略图（带主题感知）
        /// </summary>
        internal static Bitmap? GetThumbnail(string runnerName, bool isDarkTheme)
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
                Bitmap? firstFrame = null;
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (extension == ".gif")
                {
                    using var image = Image.FromFile(filePath);
                    firstFrame = new Bitmap(image);
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
                            var first = frames.First();
                            using var ms = new MemoryStream(first.Value.ToArray());
                            firstFrame = new Bitmap(ms);
                        }
                    }
                    catch
                    {
                        // APNG 解析失败，当作静态 PNG
                    }

                    if (firstFrame == null)
                    {
                        firstFrame = new Bitmap(filePath);
                    }
                }

                if (firstFrame != null)
                {
                    // 检测是否为单色并根据主题着色
                    if (ImageColorProcessor.IsMonochrome(firstFrame))
                    {
                        var recolored = ImageColorProcessor.Recolor(firstFrame, isDarkTheme);
                        firstFrame.Dispose();
                        return recolored;
                    }
                }

                return firstFrame;
            }
            catch
            {
                // 忽略错误
            }

            return null;
        }

        /// <summary>
        /// 无主题参数的重载
        /// </summary>
        internal static Bitmap? GetThumbnail(string runnerName)
        {
            return GetThumbnail(runnerName, isDarkTheme: false);
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
