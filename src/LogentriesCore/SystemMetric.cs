using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace LogentriesCore.Net
{
    public class SystemMetric
    {
        #region CPU Counters

        private static PerformanceCounter cpuTime = new PerformanceCounter()
        {
            CategoryName = "Processor",
            CounterName = "% Processor Time",
            InstanceName = "_Total"
        };

        private static PerformanceCounter cpuUserTime = new PerformanceCounter()
        {
            CategoryName = "Processor",
            CounterName = "% User Time",
            InstanceName = "_Total"
        };

        private static PerformanceCounter cpuIdleTime = new PerformanceCounter()
        {
            CategoryName = "Processor",
            CounterName = "% Idle Time",
            InstanceName = "_Total"
        };

        #endregion

        #region Memory Counters

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        #endregion

        #region Disk Counters

        private static PerformanceCounter diskReadCounter = new PerformanceCounter()
        {
            CategoryName = "PhysicalDisk",
            CounterName = "Avg. Disk Bytes/Read",
            InstanceName = "_Total"
        };

        private static PerformanceCounter diskWriteCounter = new PerformanceCounter()
        {
            CategoryName = "PhysicalDisk",
            CounterName = "Avg. Disk Bytes/Write",
            InstanceName = "_Total"
        };

        #endregion

        #region Network Counters

        private long networkSentInitial = 0;
        private long networkReceivedInitial = 0;

        private void resetNetworkInformationCounters()
        {
            getNetworkInformation(out networkSentInitial, out networkReceivedInitial);
        }

        private void getNetworkInformation(out long sent, out long received)
        {
            sent = 0;
            received = 0;

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface netint in interfaces)
            {
                sent += netint.GetIPv4Statistics().BytesSent;
                received += netint.GetIPv4Statistics().BytesReceived;
            }
        }

        private void getNetworkInformationDelta(out long sent, out long received)
        {
            long sentCurrent = 0;
            long receivedCurrent = 0;

            getNetworkInformation(out sentCurrent, out receivedCurrent);

            sent = sentCurrent - networkSentInitial;
            received = receivedCurrent - networkReceivedInitial;
        }

        #endregion

        private static string hostName = String.Empty;

        static SystemMetric()
        {
            cpuTime.BeginInit();
            cpuIdleTime.BeginInit();
            cpuUserTime.BeginInit();
            diskReadCounter.BeginInit();
            diskWriteCounter.BeginInit();

            try
            {
                hostName = "HostName=" + System.Environment.MachineName;
            }
            catch (InvalidOperationException)
            {
                // Do not do anything. The host name must be valid.
            }
        }

        private static readonly SystemMetric instance = new SystemMetric();

        private static SystemMetric Instance
        {
            get
            {
                return instance;
            }
        }

        private SystemMetric()
        {
            resetNetworkInformationCounters();
        }

        public static string Metric()
        {
            StringBuilder metric = new StringBuilder();

            #region Get Host Name
            metric.Append(hostName).Append(";");
            #endregion

            #region Get CPU Information
            metric.Append("CPU.system=").Append(cpuTime.NextValue()    .ToString("0.00")).Append("%").Append(";");
            metric.Append("CPU.user=")  .Append(cpuUserTime.NextValue().ToString("0.00")).Append("%").Append(";");
            metric.Append("CPU.idle=")  .Append(cpuIdleTime.NextValue().ToString("0.00")).Append("%").Append(";");
            #endregion

            #region Get Memory Information
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                metric.Append("Mem.total=").Append(memStatus.ullTotalPhys).Append(";");
                ulong memActive = ((memStatus.ullTotalPhys - memStatus.ullAvailPhys) * 100 / memStatus.ullTotalPhys);
                metric.Append("Mem.active=").Append(memActive).Append("%").Append(";");
            }
            #endregion

            #region Get Disk Information
            metric.Append("Disk.write=").Append(diskWriteCounter.NextValue().ToString("0.")).Append(";");
            metric.Append("Disk.read=") .Append(diskReadCounter .NextValue().ToString("0.")).Append(";");
            #endregion

            #region Get Network Sent/Received Information
            long sentDelta = 0;
            long receivedDelta = 0;

            SystemMetric.Instance.getNetworkInformationDelta(out sentDelta, out receivedDelta);

            metric.Append("Net.send=").Append(sentDelta).Append(";");
            metric.Append("Net.received=").Append(receivedDelta);
            #endregion

            return metric.ToString();
        }
    }
}
