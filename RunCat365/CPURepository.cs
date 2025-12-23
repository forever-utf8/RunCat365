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

using System.Runtime.InteropServices;

namespace RunCatLite
{
    struct CPUInfo
    {
        internal float Total { get; set; }
        internal float User { get; set; }
        internal float Kernel { get; set; }
        internal float Idle { get; set; }
    }

    internal static class CPUInfoExtension
    {
        internal static string GetDescription(this CPUInfo cpuInfo)
        {
            return $"CPU: {cpuInfo.Total:f1}%";
        }

        internal static List<string> GenerateIndicator(this CPUInfo cpuInfo)
        {
            var resultLines = new List<string>
            {
                $"CPU: {cpuInfo.Total:f1}%",
                $"   ├─ 用户: {cpuInfo.User:f1}%",
                $"   ├─ 内核: {cpuInfo.Kernel:f1}%",
                $"   └─ 空闲: {cpuInfo.Idle:f1}%"
            };
            return resultLines;
        }
    }

    internal class CPURepository
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION
        {
            public long IdleTime;
            public long KernelTime;
            public long UserTime;
            public long Reserved1;
            public long Reserved2;
            public int Reserved3;
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQuerySystemInformation(
            int SystemInformationClass,
            IntPtr SystemInformation,
            int SystemInformationLength,
            out int ReturnLength);

        private const int SystemProcessorPerformanceInformation = 8;

        private readonly int processorCount;
        private long[] prevIdleTimes;
        private long[] prevKernelTimes;
        private long[] prevUserTimes;
        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;

        internal CPURepository()
        {
            processorCount = Environment.ProcessorCount;
            prevIdleTimes = new long[processorCount];
            prevKernelTimes = new long[processorCount];
            prevUserTimes = new long[processorCount];

            // Initialize with first sample
            var info = GetProcessorInfo();
            if (info != null)
            {
                for (int i = 0; i < processorCount; i++)
                {
                    prevIdleTimes[i] = info[i].IdleTime;
                    prevKernelTimes[i] = info[i].KernelTime;
                    prevUserTimes[i] = info[i].UserTime;
                }
            }
        }

        private SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[]? GetProcessorInfo()
        {
            int structSize = Marshal.SizeOf<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>();
            int bufferSize = structSize * processorCount;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            try
            {
                int status = NtQuerySystemInformation(
                    SystemProcessorPerformanceInformation,
                    buffer,
                    bufferSize,
                    out _);

                if (status != 0) return null;

                var result = new SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[processorCount];
                for (int i = 0; i < processorCount; i++)
                {
                    IntPtr ptr = IntPtr.Add(buffer, i * structSize);
                    result[i] = Marshal.PtrToStructure<SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>(ptr);
                }
                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        internal void Update()
        {
            var info = GetProcessorInfo();
            if (info == null) return;

            long totalIdle = 0, totalKernel = 0, totalUser = 0;
            long deltaIdle = 0, deltaKernel = 0, deltaUser = 0;

            for (int i = 0; i < processorCount; i++)
            {
                deltaIdle += info[i].IdleTime - prevIdleTimes[i];
                deltaKernel += info[i].KernelTime - prevKernelTimes[i];
                deltaUser += info[i].UserTime - prevUserTimes[i];

                prevIdleTimes[i] = info[i].IdleTime;
                prevKernelTimes[i] = info[i].KernelTime;
                prevUserTimes[i] = info[i].UserTime;
            }

            // Kernel time includes idle time
            long totalTime = deltaKernel + deltaUser;
            if (totalTime == 0) return;

            long activeKernel = deltaKernel - deltaIdle;

            float idlePercent = (float)deltaIdle / totalTime * 100f;
            float kernelPercent = (float)activeKernel / totalTime * 100f;
            float userPercent = (float)deltaUser / totalTime * 100f;
            float totalPercent = 100f - idlePercent;

            var cpuInfo = new CPUInfo
            {
                Total = Math.Min(100, Math.Max(0, totalPercent)),
                User = Math.Min(100, Math.Max(0, userPercent)),
                Kernel = Math.Min(100, Math.Max(0, kernelPercent)),
                Idle = Math.Min(100, Math.Max(0, idlePercent)),
            };

            cpuInfoList.Add(cpuInfo);
            if (CPU_INFO_LIST_LIMIT_SIZE < cpuInfoList.Count)
            {
                cpuInfoList.RemoveAt(0);
            }
        }

        internal CPUInfo Get()
        {
            if (cpuInfoList.Count == 0) return new CPUInfo();

            return new CPUInfo
            {
                Total = cpuInfoList.Average(x => x.Total),
                User = cpuInfoList.Average(x => x.User),
                Kernel = cpuInfoList.Average(x => x.Kernel),
                Idle = cpuInfoList.Average(x => x.Idle)
            };
        }

        internal void Close()
        {
            // No resources to close with P/Invoke approach
        }
    }
}
