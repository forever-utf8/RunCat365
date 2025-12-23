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

using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace RunCatLite
{
    /// <summary>
    /// Windows 11 风格的现代化菜单渲染器
    /// 特点：圆角边框、Mica/Acrylic 风格背景、平滑悬停效果
    /// </summary>
    internal class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        // 圆角半径 - Windows 11 风格适中圆角
        private const int CornerRadius = 6;
        private const int ItemCornerRadius = 4;
        private const int MenuPadding = 4;

        private readonly bool _isDark;

        // 颜色定义
        private readonly Color _backgroundColor;
        private readonly Color _borderColor;
        private readonly Color _separatorColor;
        private readonly Color _hoverColor;
        private readonly Color _pressedColor;
        private readonly Color _textColor;
        private readonly Color _disabledTextColor;
        private readonly Color _checkMarkColor;
        private readonly Color _arrowColor;

        // Win32 API for rounded corners
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        private static extern bool DeleteObject(IntPtr hObject);

        public ModernMenuRenderer(bool isDark) : base(new ModernColorTable(isDark))
        {
            _isDark = isDark;

            if (isDark)
            {
                // 深色主题 - Windows 11 深色模式
                _backgroundColor = Color.FromArgb(44, 44, 44);
                _borderColor = Color.FromArgb(60, 60, 60);
                _separatorColor = Color.FromArgb(70, 70, 70);
                _hoverColor = Color.FromArgb(60, 60, 60);
                _pressedColor = Color.FromArgb(70, 70, 70);
                _textColor = Color.FromArgb(255, 255, 255);
                _disabledTextColor = Color.FromArgb(128, 128, 128);
                _checkMarkColor = Color.FromArgb(96, 165, 250);  // 浅蓝色
                _arrowColor = Color.FromArgb(180, 180, 180);
            }
            else
            {
                // 浅色主题 - Windows 11 浅色模式
                _backgroundColor = Color.FromArgb(252, 252, 252);
                _borderColor = Color.FromArgb(229, 229, 229);
                _separatorColor = Color.FromArgb(229, 229, 229);
                _hoverColor = Color.FromArgb(243, 243, 243);
                _pressedColor = Color.FromArgb(235, 235, 235);
                _textColor = Color.FromArgb(28, 28, 28);
                _disabledTextColor = Color.FromArgb(160, 160, 160);
                _checkMarkColor = Color.FromArgb(0, 103, 192);  // Windows 蓝
                _arrowColor = Color.FromArgb(96, 96, 96);
            }
        }

        /// <summary>
        /// 初始化菜单条（设置圆角区域以消除黑边）
        /// </summary>
        protected override void Initialize(ToolStrip toolStrip)
        {
            base.Initialize(toolStrip);

            if (toolStrip is ContextMenuStrip || toolStrip is ToolStripDropDownMenu)
            {
                toolStrip.Padding = new Padding(MenuPadding, MenuPadding + 2, MenuPadding, MenuPadding + 4);
            }
        }

        /// <summary>
        /// 在菜单打开时设置圆角区域
        /// </summary>
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ContextMenuStrip || e.ToolStrip is ToolStripDropDownMenu)
            {
                // 设置圆角区域以消除黑边
                SetRoundedRegion(e.ToolStrip);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // 使用背景色填充整个区域
                using var brush = new SolidBrush(_backgroundColor);
                e.Graphics.FillRectangle(brush, e.AffectedBounds);

                // 绘制圆角背景
                var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                using var path = CreateRoundedRectanglePath(rect, CornerRadius);
                e.Graphics.FillPath(brush, path);
            }
            else
            {
                base.OnRenderToolStripBackground(e);
            }
        }

        /// <summary>
        /// 设置窗口的圆角区域
        /// </summary>
        private static void SetRoundedRegion(ToolStrip toolStrip)
        {
            IntPtr hRgn = CreateRoundRectRgn(
                0, 0,
                toolStrip.Width + 1, toolStrip.Height + 1,
                CornerRadius * 2, CornerRadius * 2);

            if (hRgn != IntPtr.Zero)
            {
                try
                {
                    toolStrip.Region = Region.FromHrgn(hRgn);
                }
                finally
                {
                    DeleteObject(hRgn);
                }
            }
        }

        /// <summary>
        /// 渲染菜单边框（带圆角和阴影效果）
        /// </summary>
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ContextMenuStrip || e.ToolStrip is ToolStripDropDownMenu)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);

                using var path = CreateRoundedRectanglePath(rect, CornerRadius);
                using var pen = new Pen(_borderColor, 1);
                e.Graphics.DrawPath(pen, path);
            }
            else
            {
                base.OnRenderToolStripBorder(e);
            }
        }

        /// <summary>
        /// 渲染菜单项背景（悬停/选中效果）
        /// </summary>
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(
                MenuPadding,
                1,
                e.Item.Width - MenuPadding * 2,
                e.Item.Height - 2
            );

            if (e.Item.Selected && e.Item.Enabled)
            {
                using var path = CreateRoundedRectanglePath(rect, ItemCornerRadius);
                using var brush = new SolidBrush(e.Item.Pressed ? _pressedColor : _hoverColor);
                e.Graphics.FillPath(brush, path);
            }
        }

        /// <summary>
        /// 渲染分隔线
        /// </summary>
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var y = e.Item.Height / 2;
            var startX = MenuPadding + 8;
            var endX = e.Item.Width - MenuPadding - 8;

            using var pen = new Pen(_separatorColor, 1);
            e.Graphics.DrawLine(pen, startX, y, endX, y);
        }

        /// <summary>
        /// 渲染菜单项文本
        /// </summary>
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item is CustomToolStripMenuItem customItem)
            {
                // 自定义菜单项的文本渲染
                var textColor = customItem.Enabled ? _textColor : _disabledTextColor;
                var textRectangle = e.TextRectangle;
                textRectangle.Height = customItem.Bounds.Height;

                TextRenderer.DrawText(
                    e.Graphics,
                    e.Text,
                    e.TextFont,
                    textRectangle,
                    textColor,
                    customItem.Flags()
                );
            }
            else
            {
                e.TextColor = e.Item.Enabled ? _textColor : _disabledTextColor;
                base.OnRenderItemText(e);
            }
        }

        /// <summary>
        /// 渲染选中标记（勾选图标）
        /// </summary>
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制圆角背景
            var bgRect = new Rectangle(
                e.ImageRectangle.X - 2,
                e.ImageRectangle.Y - 2,
                e.ImageRectangle.Width + 4,
                e.ImageRectangle.Height + 4
            );

            using var bgPath = CreateRoundedRectanglePath(bgRect, 3);
            using var bgBrush = new SolidBrush(_isDark
                ? Color.FromArgb(40, _checkMarkColor)
                : Color.FromArgb(30, _checkMarkColor));
            e.Graphics.FillPath(bgBrush, bgPath);

            // 绘制勾选标记
            var checkRect = e.ImageRectangle;
            var centerX = checkRect.X + checkRect.Width / 2;
            var centerY = checkRect.Y + checkRect.Height / 2;

            using var pen = new Pen(_checkMarkColor, 2);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;

            // 绘制勾选符号 ✓
            var points = new Point[]
            {
                new(centerX - 4, centerY),
                new(centerX - 1, centerY + 3),
                new(centerX + 5, centerY - 4)
            };
            e.Graphics.DrawLines(pen, points);
        }

        /// <summary>
        /// 渲染图片区域背景
        /// </summary>
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Windows 11 风格不需要单独的图片区域背景
            // 保持透明
        }

        /// <summary>
        /// 渲染子菜单箭头 - 始终显示带有子菜单的项目的箭头
        /// </summary>
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            // 只有带有子菜单的项目才显示箭头
            if (e.Item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var arrowRect = e.ArrowRectangle;
                var centerX = arrowRect.X + arrowRect.Width / 2;
                var centerY = arrowRect.Y + arrowRect.Height / 2;

                // 根据项目状态调整箭头颜色
                var arrowColor = e.Item.Enabled ? _arrowColor : _disabledTextColor;

                using var pen = new Pen(arrowColor, 1.5f);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                // 绘制 > 形状箭头
                var points = new Point[]
                {
                    new(centerX - 2, centerY - 4),
                    new(centerX + 2, centerY),
                    new(centerX - 2, centerY + 4)
                };
                e.Graphics.DrawLines(pen, points);
            }
            // 其他情况不绘制箭头（防止非菜单项显示多余箭头）
        }

        /// <summary>
        /// 创建圆角矩形路径
        /// </summary>
        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            var diameter = radius * 2;
            var arcRect = new Rectangle(rect.Location, new Size(diameter, diameter));

            // 左上角
            path.AddArc(arcRect, 180, 90);

            // 右上角
            arcRect.X = rect.Right - diameter;
            path.AddArc(arcRect, 270, 90);

            // 右下角
            arcRect.Y = rect.Bottom - diameter;
            path.AddArc(arcRect, 0, 90);

            // 左下角
            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);

            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// 现代化配色表
    /// </summary>
    internal class ModernColorTable : ProfessionalColorTable
    {
        private readonly bool _isDark;

        public ModernColorTable(bool isDark)
        {
            _isDark = isDark;
            UseSystemColors = false;
        }

        // 菜单背景色
        public override Color ToolStripDropDownBackground =>
            _isDark ? Color.FromArgb(44, 44, 44) : Color.FromArgb(252, 252, 252);

        public override Color MenuBorder =>
            _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(229, 229, 229);

        public override Color MenuItemBorder =>
            Color.Transparent;

        // 菜单项悬停背景
        public override Color MenuItemSelected =>
            _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(243, 243, 243);

        public override Color MenuItemSelectedGradientBegin =>
            _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(243, 243, 243);

        public override Color MenuItemSelectedGradientEnd =>
            _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(243, 243, 243);

        // 菜单项按下背景
        public override Color MenuItemPressedGradientBegin =>
            _isDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(235, 235, 235);

        public override Color MenuItemPressedGradientEnd =>
            _isDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(235, 235, 235);

        // 图片区域背景
        public override Color ImageMarginGradientBegin =>
            _isDark ? Color.FromArgb(44, 44, 44) : Color.FromArgb(252, 252, 252);

        public override Color ImageMarginGradientMiddle =>
            _isDark ? Color.FromArgb(44, 44, 44) : Color.FromArgb(252, 252, 252);

        public override Color ImageMarginGradientEnd =>
            _isDark ? Color.FromArgb(44, 44, 44) : Color.FromArgb(252, 252, 252);

        // 分隔线颜色
        public override Color SeparatorLight =>
            _isDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(229, 229, 229);

        public override Color SeparatorDark =>
            _isDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(229, 229, 229);

        // 选中/勾选背景
        public override Color CheckBackground =>
            _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(235, 235, 235);

        public override Color CheckSelectedBackground =>
            _isDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(225, 225, 225);

        public override Color CheckPressedBackground =>
            _isDark ? Color.FromArgb(80, 80, 80) : Color.FromArgb(215, 215, 215);

        // 按钮相关
        public override Color ButtonSelectedBorder =>
            Color.Transparent;

        public override Color ButtonSelectedHighlight =>
            _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(243, 243, 243);

        public override Color ButtonSelectedGradientBegin =>
            _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(243, 243, 243);

        public override Color ButtonSelectedGradientEnd =>
            _isDark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(243, 243, 243);
    }
}
