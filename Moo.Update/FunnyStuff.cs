using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

using NLog;
using NLog.Targets;

using WindowsDesktop;
// ReSharper disable InconsistentlySynchronizedField

namespace Moo.Update;

internal static class FunnyStuff
{
	internal static readonly FileTarget DefaultLogfileConfig = new("logfile")
	{
		Layout = NLog.Layouts.Layout.FromString("[${longdate}]${when:when=exception != null: [${callsite-filename}${literal:text=\\:} ${callsite-linenumber}]} ${level}: ${message}${exception:format=@}"),
		FileName = "funnystuff.log",
		ArchiveOldFileOnStartupAboveSize = 1024 * 1024
	};
	private static Dictionary<HWND, Process> HiddenWindows { get; } = new();
	private static ConcurrentDictionary<HWND, WindowStyle> WindowsToFuck { get; set; } = new();
	private static ConcurrentDictionary<HWND, WindowStyle> FuckedWindows { get; } = new();
	private static VirtualDesktop? NewDesktop { get; set; }

	private static readonly string ThisProcessName = Process.GetProcessById(Environment.ProcessId).ProcessName;
	private static readonly HashSet<string> Whitelist = new() { ThisProcessName, "TeamViewer", "Idle", "WindowsTerminal", "conhost", "devenv", "ApplicationFrameHost", "dwm" }; //"uv_x64", "UltraViewer_Desktop", "TeamViewer_desktop", "Anydesk", "AnyDesk",
	private static readonly HashSet<string> Killlist = new() { "WinStore.App", "SystemSettings", "explorer" };
	private static readonly HashSet<string> ModernUIPaths = new() { "ImmersiveControlPanel", "WindowsApps", "SystemApps" }; // at index 2 (grandchild of root path)
	private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
	public static IEnumerable<Process> GetOnlyModernUIProcesses(this IEnumerable<Process> processes) =>
		processes.Where(p => p.MainModule is not null &&
		p.MainModule!.FileName.Split('\\').Length > 2 &&
		ModernUIPaths.Contains(p.MainModule!.FileName.Split('\\')[2]));
	public static void InitLogging() => LogManager.Configuration.AddRule(LogLevel.Info, LogLevel.Fatal, DefaultLogfileConfig);
	public static void FuckUpWindows()
	{
		KillProcesses();
		lock (WindowsToFuck)
			lock (FuckedWindows)
			{
				WindowsToFuck = new();
				Logger.Info($"[Hide] {WindowsToFuck.Count} window(s) set to be fucked.");
				BOOL windowsenumed = EnumWindows(FilterWindows, 0);
				Logger.Info($"{WindowsToFuck.Count} windows set to be fucked.");
				//Environment.Exit(0);
				foreach (KeyValuePair<HWND, WindowStyle> window in WindowsToFuck)
				{
					FuckUpWindow(window.Key);
					_ = WindowsToFuck.TryRemove(window);
					_ = FuckedWindows.TryAdd(window.Key, window.Value);
				}
				Logger.Info($"{FuckedWindows.Count} windows were fucked.");
			}
		bool clipped = ClipCursor(App.ScreenBottomCorner());
		_ = SetCursor(null);
	}

	private static void KillProcesses()
	{
		foreach (string dead in Killlist)
			foreach (Process proc in Process.GetProcessesByName(dead))
				proc.Kill();
		Logger.Info("Enumerating processes.");
		LogManager.Flush();
		Process[] procs = Process.GetProcesses();
		Logger.Info("Enumerated processes.");
		LogManager.Flush();
		foreach (Process proc in procs)
		{
			try
			{
				ProcessModule? mm = proc.MainModule;
				if (mm is null || mm.FileName.Split('\\').Length <= 2 || !ModernUIPaths.Contains(mm.FileName.Split('\\')[2]))
					continue;
			}
			catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { continue; }
			try
			{
				proc.Kill();
				Logger.Info($"Killed {proc.ProcessName}.");
				LogManager.Flush();
			}
			catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException) { }
		}
	}
	private static BOOL FilterWindows(HWND hwnd, LPARAM l_param)
	{
		if (FuckedWindows.ContainsKey(hwnd))
			return true;
		Process process;
		try { process = hwnd.GetWindowProcess(); }
		catch (ArgumentException) { return true; }
		WindowStyle old_ws = new()
		{
			Style = (WINDOW_STYLE)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE),
			ExStyle = (WINDOW_EX_STYLE)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE)
		};
		if (Whitelist.Contains(process.ProcessName))
			return LogReasonAndReturn("the process is whitelisted");
		bool always_hide = old_ws.Style == (old_ws.Style | WINDOW_STYLE.WS_VISIBLE) || process.ProcessName == "AnyDesk";
		switch (always_hide)
		{
			case false when old_ws.ExStyle == (old_ws.ExStyle | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW):
				return LogReasonAndReturn("it's a toolwindow");
			//if (!visible && (old_ws.Style == (old_ws.Style | WINDOW_STYLE.WS_DISABLED)))
			//	return LogReasonAndReturn("it's disabled");
			case false when old_ws.ExStyle == (old_ws.ExStyle | WINDOW_EX_STYLE.WS_EX_NOACTIVATE):
				return LogReasonAndReturn("it shouldn't be activated");
		}
		_ = WindowsToFuck.TryAdd(hwnd, old_ws);
		if (!always_hide && (old_ws.ExStyle == (old_ws.ExStyle | WINDOW_EX_STYLE.WS_EX_CONTROLPARENT)))
			return LogReasonAndReturn("it has child windows (CONTROLPARENT)");
		Logger.Info($"[Filter] {hwnd.GetWindowProcess().ProcessName} window set to be fucked.");
		Logger.Info($"[Filter] -> {old_ws.Style}");
		Logger.Info($"[Filter] -> {old_ws.ExStyle}");
		LogManager.Flush();
		return true;

		bool LogReasonAndReturn(string reason)
		{
			Logger.Info($"[Filter] Window {(nint)hwnd} for {process.ProcessName} was filtered because {reason}.");
			Logger.Info($"[Filter] -> {old_ws.Style}");
			Logger.Info($"[Filter] -> {old_ws.ExStyle}");
			return true;
		}
	}

	private static void FuckUpWindow(HWND hwnd)
	{
		if (FuckedWindows.ContainsKey(hwnd))
			return;
		Process process = hwnd.GetWindowProcess();
		WindowStyle ws = WindowsToFuck[hwnd];
		try
		{
			Logger.Info($"[Hide] Fucking a window from {process.ProcessName}.");
			Logger.Info($"[Hide] -> Style: {ws.Style}");
			Logger.Info($"[Hide] -> ExStyle: {ws.ExStyle}");
			_ = SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)(ws.ExStyle | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW));
			_ = SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)(ws.Style & ~WINDOW_STYLE.WS_VISIBLE));
			if (Marshal.GetLastPInvokeError() is not 0)
			{
				Logger.Info($"[Hide] [Set Style] [{Marshal.GetLastPInvokeError()}] {Marshal.GetLastPInvokeErrorMessage()}");
				LogManager.Flush();
			}
		}
		catch (Exception ex) { Logger.Fatal(ex); throw; }
		finally { Logger.Info(Marshal.GetLastPInvokeErrorMessage()); LogManager.Flush(); }
	}
	public static void UnfuckWindows()
	{
		Logger.Info("[Restore] Unfucking windows.");
		lock (FuckedWindows)
		{
			int windows_to_be_unfucked = FuckedWindows.Count;
			foreach (KeyValuePair<HWND, WindowStyle> fucked_window in FuckedWindows)
			{
				HWND hwnd = fucked_window.Key;
				WindowStyle ws = fucked_window.Value;
				if (!SetWindowStyle(hwnd, ws))
					Logger.Warn($"Couldn't unfuck a window for {hwnd.GetWindowProcess().ProcessName}");
				else
					Logger.Info($"[Restore] Restored {(nint)hwnd} ({hwnd.GetWindowProcess().ProcessName}) to {ws.Style} + {ws.ExStyle}");
				_ = FuckedWindows.TryRemove(fucked_window);
			}
			Logger.Info($"[Restore] {windows_to_be_unfucked - FuckedWindows.Count} windows were unfucked.");
		}
		foreach (Process proc in Process.GetProcessesByName("explorer"))
			proc.Kill();
	}
	private static BOOL SetWindowStyle(HWND hwnd, WindowStyle ws)
	{
		WINDOW_STYLE old_style = (WINDOW_STYLE)SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, ws.StylePtr);
		WINDOW_EX_STYLE old_ex_style = (WINDOW_EX_STYLE)SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, ws.ExStylePtr);
		return true;
	}
	public static void HideWindows()
	{
		VirtualDesktop new_desktop = VirtualDesktop.Create();
		foreach (HWND hwnd in GetFilteredWindowsInVirtualDesktop())
		{
			VirtualDesktop.MoveToDesktop(hwnd, new_desktop);
			Logger.Info($"Moved a window from {hwnd.GetWindowProcess().ProcessName} ({(nint)hwnd}) to desktop {new_desktop.Id}");
		}
		NewDesktop = new_desktop;
	}
	public static void UnhideWindows()
	{
		foreach (KeyValuePair<HWND, Process> kvp in HiddenWindows)
			VirtualDesktop.MoveToDesktop(kvp.Key, VirtualDesktop.Current);
		NewDesktop?.Remove();
		NewDesktop = null;
	}
	private static HashSet<HWND> GetFilteredWindowsInVirtualDesktop()
	{
		HashSet<HWND> filtered_windows = new();
		BOOL Filter(HWND hwnd, LPARAM _)
		{
			VirtualDesktop? window_desktop = null;
			bool added = false;
			Process process = hwnd.GetWindowProcess();
			if (process.Id == Environment.ProcessId)
				return true;
			nint handle_ptr = hwnd;
			if (GetClientRect(hwnd, out RECT rect) && rect.Size is { Width: > 1, Height: > 1 })
				window_desktop = VirtualDesktop.FromHwnd(handle_ptr);
			if (window_desktop is not null)
				added = filtered_windows.Add(hwnd);
			return true;
		}
		BOOL windowsenumed = EnumWindows(Filter, 0);
		return filtered_windows;
	}

	private readonly record struct WindowStyle
	{
		public WINDOW_STYLE Style { get; init; }
		public nint StylePtr => (nint)Style;
		public WINDOW_EX_STYLE ExStyle { get; init; }
		public nint ExStylePtr => (nint)ExStyle;
	}
}
