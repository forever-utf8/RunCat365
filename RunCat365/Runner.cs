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
        // 单色检测阈值
        private const float SATURATION_THRESHOLD = 0.15f;  // 饱和度低于此值视为灰度
        private const float HUE_VARIANCE_THRESHOLD = 30f;  // 色相方差低于此值视为单色
        private const int SAMPLE_PIXELS = 500;             // 采样像素数量

        /// <summary>
        /// 检测图像是否为单色（灰度或接近单一颜色）
        /// </summary>
        internal static bool IsMonochrome(Bitmap bitmap)
        {
            var nonTransparentPixels = new List<Color>();
            var random = new Random(42); // 固定种子确保可重复性

            // 采样非透明像素
            int sampleCount = 0;
            int maxAttempts = SAMPLE_PIXELS * 3;
            int attempts = 0;

            while (sampleCount < SAMPLE_PIXELS && attempts < maxAttempts)
            {
                int x = random.Next(bitmap.Width);
                int y = random.Next(bitmap.Height);
                var pixel = bitmap.GetPixel(x, y);

                if (pixel.A > 20) // 忽略几乎透明的像素
                {
                    nonTransparentPixels.Add(pixel);
                    sampleCount++;
                }
                attempts++;
            }

            if (nonTransparentPixels.Count < 10)
                return true; // 几乎透明的图像视为单色

            // 计算平均饱和度
            float totalSaturation = 0;
            var hues = new List<float>();

            foreach (var pixel in nonTransparentPixels)
            {
                float saturation = pixel.GetSaturation();
                totalSaturation += saturation;

                if (saturation > 0.1f) // 只有有颜色的像素才计算色相
                {
                    hues.Add(pixel.GetHue());
                }
            }

            float avgSaturation = totalSaturation / nonTransparentPixels.Count;

            // 如果平均饱和度很低，视为灰度图像
            if (avgSaturation < SATURATION_THRESHOLD)
                return true;

            // 如果有颜色，检查色相是否集中在同一区域
            if (hues.Count > 0)
            {
                // 计算色相的标准差（考虑色相的环形性质）
                float hueVariance = CalculateCircularVariance(hues);
                if (hueVariance < HUE_VARIANCE_THRESHOLD)
                    return true; // 色相非常集中，视为单色
            }

            return false;
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
        /// </summary>
        internal static float GetAverageBrightness(Bitmap bitmap)
        {
            float totalBrightness = 0;
            int count = 0;

            var random = new Random(42);
            for (int i = 0; i < SAMPLE_PIXELS; i++)
            {
                int x = random.Next(bitmap.Width);
                int y = random.Next(bitmap.Height);
                var pixel = bitmap.GetPixel(x, y);

                if (pixel.A > 20)
                {
                    totalBrightness += pixel.GetBrightness();
                    count++;
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
