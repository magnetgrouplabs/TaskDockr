using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TaskDockr.Models;
using System;
using System.Threading;

namespace TaskDockr.Services
{
    public interface IMemoryOptimizationService
    {
        Task<bool> OptimizeMemoryUsageAsync();
        Task<bool> ClearIconCacheAsync();
        Task<bool> CompactHeapAsync();
        Task<bool> ReduceWorkingSetAsync();
        Task<bool> FreeUnusedResourcesAsync();
        MemoryUsageInfo GetMemoryUsageInfo();
        Task<bool> SetMemoryLimitAsync(long limitInBytes);
        Task<bool> MonitorMemoryUsageAsync();
    }

    public class MemoryOptimizationService : IMemoryOptimizationService
    {
        private readonly IIconService _iconService;
        private readonly IConfigurationService _configService;
        private Timer _monitoringTimer;
        private bool _isMonitoring;
        private long _memoryLimit;

        public MemoryOptimizationService(IIconService iconService, IConfigurationService configService)
        {
            _iconService = iconService;
            _configService = configService;
            _memoryLimit = 100 * 1024 * 1024; // 100MB default limit
        }

        public async Task<bool> OptimizeMemoryUsageAsync()
        {
            try
            {
                var success = true;
                
                // Clear icon cache
                success &= await ClearIconCacheAsync();
                
                // Compact heap
                success &= await CompactHeapAsync();
                
                // Reduce working set
                success &= await ReduceWorkingSetAsync();
                
                // Free unused resources
                success &= await FreeUnusedResourcesAsync();
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Memory optimization failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ClearIconCacheAsync()
        {
            try
            {
                await _iconService.ClearIconCacheAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clear icon cache: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CompactHeapAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Force garbage collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    
                    // Compact Large Object Heap if supported
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect();
                    }
                    
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> ReduceWorkingSetAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = Process.GetCurrentProcess();
                    
                    // Use Windows API to trim working set
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        SetProcessWorkingSetSize(process.Handle, -1, -1);
                    }
                    
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> FreeUnusedResourcesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Clear various caches and temporary data
                    
                    // Empty clipboard (if we have access)
                    try
                    {
                        // Clipboard.Clear(); // WPF API, not available in WinUI 3
                    }
                    catch
                    {
                        // Ignore clipboard errors
                    }
                    
                    // Clear temporary collections
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public MemoryUsageInfo GetMemoryUsageInfo()
        {
            var process = Process.GetCurrentProcess();
            
            return new MemoryUsageInfo
            {
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                PeakWorkingSet = process.PeakWorkingSet64,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                GCMemory = GC.GetTotalMemory(false),
                IconCacheSize = _iconService.GetCacheSize(),
                Timestamp = DateTime.Now
            };
        }

        public async Task<bool> SetMemoryLimitAsync(long limitInBytes)
        {
            _memoryLimit = limitInBytes;
            return await MonitorMemoryUsageAsync();
        }

        public async Task<bool> MonitorMemoryUsageAsync()
        {
            if (_isMonitoring) return true;
            
            _isMonitoring = true;
            _monitoringTimer = new Timer(async _ =>
            {
                var usageInfo = GetMemoryUsageInfo();
                
                if (usageInfo.WorkingSet > _memoryLimit)
                {
                    await OptimizeMemoryUsageAsync();
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            
            return true;
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);
    }

    public class MemoryUsageInfo
    {
        public long WorkingSet { get; set; }
        public long PrivateMemory { get; set; }
        public long VirtualMemory { get; set; }
        public long PeakWorkingSet { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public long GCMemory { get; set; }
        public int IconCacheSize { get; set; }
        public DateTime Timestamp { get; set; }
    }
}