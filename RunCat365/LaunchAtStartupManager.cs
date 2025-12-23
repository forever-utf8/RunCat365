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

using Microsoft.Win32;

namespace RunCatLite
{
    internal class LaunchAtStartupManager
    {
        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public bool GetStartup()
        {
            using var rKey = Registry.CurrentUser.OpenSubKey(StartupRegistryKey);
            if (rKey is null) return false;
            var value = rKey.GetValue(Application.ProductName) is not null;
            rKey.Close();
            return value;
        }

        public bool SetStartup(bool enabled)
        {
            var productName = Application.ProductName;
            if (productName is null) return false;

            using var rKey = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (rKey is null) return false;

            if (enabled)
            {
                rKey.DeleteValue(productName, false);
            }
            else
            {
                var fileName = Environment.ProcessPath;
                if (fileName is not null)
                {
                    rKey.SetValue(productName, fileName);
                }
            }
            rKey.Close();
            return true;
        }
    }
}
