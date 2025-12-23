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

using System.ComponentModel;

namespace RunCatLite
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly CustomToolStripMenuItem runnersMenu;
        private readonly NotifyIcon notifyIcon = new();
        private readonly ContextMenuStrip contextMenuStrip;
        private readonly List<Icon> icons = [];
        private readonly object iconLock = new();
        private int current = 0;

        private readonly Func<string> getRunner;
        private readonly Action<string> setRunner;
        private readonly Func<Theme> getSystemTheme;
        private readonly Func<Theme> getManualTheme;

        internal ContextMenuManager(
            Func<string> getRunner,
            Action<string> setRunner,
            Func<Theme> getSystemTheme,
            Func<Theme> getManualTheme,
            Action<Theme> setManualTheme,
            Func<FPSMaxLimit> getFPSMaxLimit,
            Action<FPSMaxLimit> setFPSMaxLimit,
            Func<bool> getLaunchAtStartup,
            Func<bool, bool> toggleLaunchAtStartup,
            Action openRepository,
            Action onExit
        )
        {
            this.getRunner = getRunner;
            this.setRunner = setRunner;
            this.getSystemTheme = getSystemTheme;
            this.getManualTheme = getManualTheme;

            systemInfoMenu.Text = "-\n-\n-\n-\n-";
            systemInfoMenu.Enabled = false;

            // 角色菜单
            runnersMenu = new CustomToolStripMenuItem("角色");

            var themeMenu = new CustomToolStripMenuItem("外观");
            themeMenu.SetupSubMenusFromEnum<Theme>(
                t => t.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Theme>(
                        parent,
                        sender,
                        (string? s, out Theme t) => Enum.TryParse(s, out t),
                        t => setManualTheme(t)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                t => getManualTheme() == t,
                _ => null
            );

            var fpsMaxLimitMenu = new CustomToolStripMenuItem("最大帧率");
            fpsMaxLimitMenu.SetupSubMenusFromEnum<FPSMaxLimit>(
                f => f.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<FPSMaxLimit>(
                        parent,
                        sender,
                        (string? s, out FPSMaxLimit f) => FPSMaxLimitExtension.TryParse(s, out f),
                        f => setFPSMaxLimit(f)
                    );
                },
                f => getFPSMaxLimit() == f,
                _ => null
            );

            var launchAtStartupMenu = new CustomToolStripMenuItem("自启动")
            {
                Checked = getLaunchAtStartup()
            };
            launchAtStartupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleLaunchAtStartup);

            var settingsMenu = new CustomToolStripMenuItem("设置");
            settingsMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                themeMenu,
                fpsMaxLimitMenu,
                launchAtStartupMenu
            });

            var appVersionMenu = new CustomToolStripMenuItem(
                $"{Application.ProductName} v{Application.ProductVersion}"
            )
            {
                Enabled = false
            };

            var repositoryMenu = new CustomToolStripMenuItem("➡️仓库");
            repositoryMenu.Click += (sender, e) => openRepository();

            var informationMenu = new CustomToolStripMenuItem("关于");
            informationMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                appVersionMenu,
                repositoryMenu
            });

            var exitMenu = new CustomToolStripMenuItem("退出");
            exitMenu.Click += (sender, e) => onExit();

            contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                systemInfoMenu,
                new ToolStripSeparator(),
                runnersMenu,
                new ToolStripSeparator(),
                settingsMenu,
                informationMenu,
                new ToolStripSeparator(),
                exitMenu
            });

            // 应用现代化渲染器
            UpdateMenuRenderer();

            // 初始刷新角色菜单（确保有子菜单项以显示箭头）
            RefreshRunnersMenu();

            // 每次右键打开菜单时刷新角色列表
            contextMenuStrip.Opening += (sender, e) => RefreshRunnersMenu();

            SetIcons(getSystemTheme(), getManualTheme(), getRunner());

            notifyIcon.Text = "-";
            notifyIcon.Icon = icons.Count > 0 ? icons[0] : null;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        /// <summary>
        /// 更新菜单渲染器以适应当前主题
        /// </summary>
        internal void UpdateMenuRenderer()
        {
            var actualTheme = getManualTheme() == Theme.System ? getSystemTheme() : getManualTheme();
            var isDark = actualTheme == Theme.Dark;
            contextMenuStrip.Renderer = new ModernMenuRenderer(isDark);
        }

        /// <summary>
        /// 动态刷新角色菜单
        /// </summary>
        private void RefreshRunnersMenu()
        {
            runnersMenu.DropDownItems.Clear();

            var runners = RunnerManager.GetAvailableRunners();
            var currentRunner = getRunner();
            var themeName = GetThemeName();

            foreach (var runnerName in runners)
            {
                var displayName = RunnerManager.GetDisplayName(runnerName);
                var menuItem = new CustomToolStripMenuItem(displayName)
                {
                    Checked = (runnerName == currentRunner),
                    Tag = runnerName
                };

                // 设置缩略图
                var thumbnail = RunnerManager.GetThumbnail(runnerName, themeName);
                if (thumbnail != null)
                {
                    menuItem.Image = thumbnail;
                }

                menuItem.Click += (sender, e) =>
                {
                    if (sender is ToolStripMenuItem item && item.Tag is string selectedRunner)
                    {
                        // 更新选中状态
                        foreach (ToolStripMenuItem child in runnersMenu.DropDownItems)
                        {
                            child.Checked = false;
                        }
                        item.Checked = true;

                        setRunner(selectedRunner);
                        SetIcons(getSystemTheme(), getManualTheme(), selectedRunner);
                    }
                };

                runnersMenu.DropDownItems.Add(menuItem);
            }

            if (runners.Count == 0)
            {
                var emptyItem = new CustomToolStripMenuItem("(无可用角色)")
                {
                    Enabled = false
                };
                runnersMenu.DropDownItems.Add(emptyItem);
            }
        }

        private string GetThemeName()
        {
            var actualTheme = getManualTheme() == Theme.System ? getSystemTheme() : getManualTheme();
            return actualTheme == Theme.Light ? "light" : "dark";
        }

        private static void HandleMenuItemSelection<T>(
            ToolStripMenuItem parentMenu,
            object? sender,
            CustomTryParseDelegate<T> tryParseMethod,
            Action<T> assignValueAction
        )
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem childItem in parentMenu.DropDownItems)
            {
                childItem.Checked = false;
            }
            item.Checked = true;
            if (tryParseMethod(item.Text, out T parsedValue))
            {
                assignValueAction(parsedValue);
            }
        }

        internal void SetIcons(Theme systemTheme, Theme manualTheme, string runnerName)
        {
            var themeName = (manualTheme == Theme.System ? systemTheme : manualTheme) == Theme.Light ? "light" : "dark";
            var list = RunnerManager.LoadIcons(runnerName, themeName);

            lock (iconLock)
            {
                // 先保存旧图标引用
                var oldIcons = new List<Icon>(icons);
                icons.Clear();
                icons.AddRange(list);
                current = 0;

                // 设置新图标后再释放旧图标
                if (icons.Count > 0)
                {
                    notifyIcon.Icon = icons[0];
                }

                // 现在可以安全地释放旧图标
                foreach (var oldIcon in oldIcons)
                {
                    try { oldIcon.Dispose(); } catch { }
                }
            }
        }

        private static void HandleStartupMenuClick(object? sender, Func<bool, bool> toggleLaunchAtStartup)
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            try
            {
                if (toggleLaunchAtStartup(item.Checked))
                {
                    item.Checked = !item.Checked;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        internal void ShowBalloonTip()
        {
            var message = "程序已启动。" +
                "如果图标没有显示在任务栏，可能被系统隐藏了，" +
                "请手动将其移到任务栏并固定。";
            notifyIcon.ShowBalloonTip(5000, "RunCat-Lite", message, ToolTipIcon.Info);
        }

        internal void AdvanceFrame()
        {
            lock (iconLock)
            {
                if (icons.Count == 0) return;
                if (icons.Count <= current) current = 0;
                notifyIcon.Icon = icons[current];
                current = (current + 1) % icons.Count;
            }
        }

        internal void SetSystemInfoMenuText(string text)
        {
            systemInfoMenu.Text = text;
        }

        internal void SetNotifyIconText(string text)
        {
            notifyIcon.Text = text;
        }

        internal void HideNotifyIcon()
        {
            notifyIcon.Visible = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (iconLock)
                {
                    icons.ForEach(icon => icon.Dispose());
                    icons.Clear();
                }

                if (notifyIcon is not null)
                {
                    notifyIcon.ContextMenuStrip?.Dispose();
                    notifyIcon.Dispose();
                }
            }
        }

        private delegate bool CustomTryParseDelegate<T>(string? value, out T result);
    }
}
