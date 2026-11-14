using System.Runtime.InteropServices;

namespace Jarvis;

// Win32 calls instead of PerformanceCounter: counter names are localized on non-English Windows.
public sealed class SystemTelemetry
{
    private const double Gb = 1024d * 1024 * 1024;
    private ulong _lastIdle, _lastTotal;

    /// <summary>CPU use since the previous call; null on the first call.</summary>
    public double? CpuPercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) return null;
        var total = kernel + user; // kernel time already includes idle time
        var (idleDelta, totalDelta) = (idle - _lastIdle, total - _lastTotal);
        var first = _lastTotal == 0;
        (_lastIdle, _lastTotal) = (idle, total);
        return first || totalDelta == 0 ? null : 100.0 * (totalDelta - idleDelta) / totalDelta;
    }

    public static (double UsedGb, double TotalGb)? Memory()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status)) return null;
        return ((status.TotalPhys - status.AvailPhys) / Gb, status.TotalPhys / Gb);
    }

    public static double SystemDriveFreeGb() =>
        new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!).AvailableFreeSpace / Gb;

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length, MemoryLoad;
        public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);
}
