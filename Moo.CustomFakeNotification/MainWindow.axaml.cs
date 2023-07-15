using System.Diagnostics.CodeAnalysis;

using static Vanara.PInvoke.User32;
using Vanara.PInvoke;

using Windows.Win32.UI.WindowsAndMessaging;

using Avalonia.Input;

namespace Moo.CustomFakeNotification;

public partial class MainWindow : Window
{
	private int _alt_key_pressed = 0;
	private readonly Windows.Win32.Foundation.RECT WindowArea = new(0, 0, 700, 450);
	public MainWindow()
	{
		DataContext = new MainWindowViewModel();
		InitializeComponent();
		Activated += MainWindow_Activated;
		PositionChanged += MainWindow_Activated;
		DoubleTapped += MainWindow_Activated;
		LostFocus += MainWindow_Activated;
		GotFocus += MainWindow_Activated;
		PointerLeave += MainWindow_Activated;
		PointerEnter += MainWindow_Activated;
		PointerCaptureLost += MainWindow_Activated;
		Closing += (_, e) => e.Cancel = true;
		KeyUp += ExitWithAlt;
		_ = Dispatcher.UIThread.InvokeAsync(FlashWarning);
#if DEBUG
		// Task.Run(() => { Thread.Sleep(5000); Environment.Exit(0); });
#endif
	}

	private void ExitWithAlt(object? sender, KeyEventArgs e)
	{
		if (e.Key is not Key.LeftAlt or Key.RightAlt)
		{
			_alt_key_pressed = 0;
			return;
		}
		_alt_key_pressed++;
		if (_alt_key_pressed > 2)
		{
			KeyUp -= ExitWithAlt;
			Environment.Exit(0);
		}
	}

	[SuppressMessage("Roslynator", "RCS1163:Unused parameter.", Justification = "EnumMonitorProc signature")]
	private void MainWindow_Activated(object? sender, EventArgs e)
	{
		HWND mwhandle = GetHandleVanara();
		HWND top_window = GetTopWindow(GetDesktopWindow());
		if (top_window != mwhandle)
			Topmost = true;
		WINDOWPLACEMENT wp = new();
		_ = GetWindowPlacement(mwhandle, ref wp);
		_ = ClipCursor(wp.rcNormalPosition);
		const int margin = 60;
		PRECT work_area = new();
		MONITORINFO pmi = new() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>(), rcMonitor = new(1, 1, 1, 1) };
		bool MonitorProc(nint monitor, nint dc, PRECT size, nint _)
		{
			bool got_monitor = GetMonitorInfo(monitor, ref pmi);
			if (pmi.dwFlags == MonitorInfoFlags.MONITORINFOF_PRIMARY)
				work_area = pmi.rcWork;
			return true;
		}
		_ = EnumDisplayMonitors(HDC.NULL, null, MonitorProc, 0);
		string error_message = Marshal.GetLastPInvokeErrorMessage();
		Windows.Win32.Foundation.HWND mwhandle2 = GetHandleCsWin32();
		nint new_ex_style = Windows.Win32.PInvoke.SetWindowLongPtr(mwhandle2, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_TOPMOST));
		_ = SetWindowPos(mwhandle, -1, work_area.right - (WindowArea.Width + margin), work_area.bottom - (WindowArea.Height + margin), WindowArea.Width, WindowArea.Height, SetWindowPosFlags.SWP_SHOWWINDOW);
		WINDOWPLACEMENT pwndpl = new();
		_ = GetWindowPlacement(mwhandle, ref pwndpl);
		HRGN window_region = Gdi32.CreateRoundRectRgn(WindowArea.X, WindowArea.Y, WindowArea.Width, WindowArea.Height, 10, 10);
		_ = SetWindowRgn(mwhandle, window_region, true);
		error_message = Marshal.GetLastPInvokeErrorMessage();
		Topmost = true;
	}

	private async Task FlashWarning()
	{
		while (true)
		{
			Hackers.Foreground = Brushes.Yellow;
			await Task.Delay(500);
			Hackers.Foreground = Brushes.Black;
			await Task.Delay(500);
		}
	}

	private HWND GetHandleVanara() => this.TryGetPlatformHandle();
	private Windows.Win32.Foundation.HWND GetHandleCsWin32() => (Windows.Win32.Foundation.HWND)PlatformImpl!.Handle.Handle;
}
