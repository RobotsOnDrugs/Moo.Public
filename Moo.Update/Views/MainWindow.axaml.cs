#define FAST

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Avalonia.Input;
using Avalonia.Threading;

using NLog;
using NLog.Targets;

using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

using Vanara.Extensions;
using Vanara.Security.AccessControl;

#if RELEASE
using Windows.Win32.System.Shutdown;
#endif

namespace Moo.Update.Views;
[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
public partial class MainWindow : Window
{
	private int _alt_key_pressed = 0;
	private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
	internal bool AggressiveWindowHiding => WindowOptions.AggressiveWindowHiding;
	private MainWindowOptions WindowOptions { get; init; }
	private Action UndoWindowHide { get; init; }

	internal static readonly FileTarget DefaultLogfileConfig = new("logfile")
	{
		Layout = NLog.Layouts.Layout.FromString("[${longdate}]${when:when=exception != null: [${callsite-filename}${literal:text=\\:} ${callsite-linenumber}]} ${level}: ${message}${exception:format=@}"),
		FileName = "mainwindow.log",
		ArchiveOldFileOnStartupAboveSize = 1024 * 1024
	};

	public MainWindow()
	{
		LogManager.Configuration.AddRule(LogLevel.Info, LogLevel.Fatal, DefaultLogfileConfig);
		Process cur_proc = Process.GetCurrentProcess();
		try
		{
			cur_proc.EnablePrivilege(SystemPrivilege.Shutdown);
		}
		catch (Exception e)
		{
			logger.Info(e);
			LogManager.Flush();
			LogManager.Shutdown();
			throw;
		}
		try
		{
			// ReSharper disable once InconsistentNaming
			string JSONOptions = File.ReadAllText("WindowSettings.json");
			MainWindowOptions mwo = JsonSerializer.Deserialize<MainWindowOptions>(JSONOptions);
			WindowOptions = new()
			{
				AggressiveWindowHiding = mwo.AggressiveWindowHiding,
				UpdateSpeedPre90IntervalMax = mwo.UpdateSpeedPre90IntervalMax,
				UpdateSpeedPre90Min = mwo.UpdateSpeedPre90Min,
				UpdateSpeedPre90Max = mwo.UpdateSpeedPre90Max,
				UpdateSpeedPost90BackwardMax = mwo.UpdateSpeedPost90BackwardMax,
				UpdateSpeedPost90Min = mwo.UpdateSpeedPost90Min,
				UpdateSpeedPost90Max = mwo.UpdateSpeedPost90Max,
			};
		}
		catch (Exception ex)
		{
			logger.Error(ex);
			logger.Error("[MainWindow] Using configuration defaults.");
		}
		UndoWindowHide = WindowOptions.AggressiveWindowHiding ? FunnyStuff.UnfuckWindows : FunnyStuff.UnhideWindows;
		KeyUp += ExitWithAlt;
#if RELEASE
		LostFocus += (_, _) => Persist();
		PointerCaptureLost += (_, _) => Persist();
		Activated += (_, _) => Persist();
		Closing += (_, e) => e.Cancel = true;
		Closed += (_, _) => Activate();
#endif
		InitializeComponent();
		Dispatcher.UIThread.InvokeAsync(MakeProgress);
		logger.Info("Main window spawned.");
	}

	private void ExitWithAlt(object? sender, KeyEventArgs e) => ExitWithAltInternal(e);
	private void ExitWithAltInternal(KeyEventArgs e)
	{
		if (e.Key is not Key.LeftAlt or Key.RightAlt)
		{
			_alt_key_pressed = 0;
			return;
		}
		_alt_key_pressed++;
		if (_alt_key_pressed <= 2) return;
		KeyUp -= ExitWithAlt;
		try
		{
			logger.Info("[MainWindow] Undoing window damage.");
			UndoWindowHide();
		}
		catch (Exception ex) { logger.Error(ex); }
		LogManager.Flush();
		LogManager.Shutdown();
		Environment.Exit(0);
	}
	private async Task MakeProgress()
	{
		int delay_multiplier = 60;
#if FAST
		delay_multiplier = 1;
#endif
		int progress_percentage = 0;
		AvaloniaProgressRing.ProgressRing progress_ring = this.FindControl<AvaloniaProgressRing.ProgressRing>("ProgressRing")!;
		TextBlock stage_tb = this.FindControl<TextBlock>("Stage")!;
		TextBlock turnoff_tb = this.FindControl<TextBlock>("TurnOff")!;
		TextBlock progress_tb = this.FindControl<TextBlock>("ProgressIndicator")!;
		stage_tb.Text = "Please wait updates are installing";
		turnoff_tb.Text = "Do not turn off your computer";
		Random rand = new();
		async Task ProgressScript()
		{
			progress_tb.Text = "0% complete";
			while (progress_percentage < 90)
			{
#if RELEASE
				await Task.Delay(rand.Next(WindowOptions.UpdateSpeedPre90Min * delay_multiplier * 1000, WindowOptions.UpdateSpeedPre90Max * delay_multiplier * 1000));
#else
				await Task.Delay(1 * 100);
#endif
				int progress_increment = rand.Next(1, WindowOptions.UpdateSpeedPre90IntervalMax);
				int max_progress = progress_percentage + progress_increment;
				while (progress_percentage < max_progress)
				{
					await Task.Delay(rand.Next(100, 500));
					progress_percentage++;
					progress_tb.Text = $"{progress_percentage}% complete";
				}
			}
			while (true)
			{
#if RELEASE
				await Task.Delay(rand.Next(WindowOptions.UpdateSpeedPost90Min * delay_multiplier * 1000, WindowOptions.UpdateSpeedPost90Max * delay_multiplier * 1000));
#else
				await Task.Delay(1 * 1000);
#endif
				if (progress_percentage == 99)
				{
					if (rand.Next(10) > 5)
						break;
					progress_percentage -= rand.Next(1, WindowOptions.UpdateSpeedPost90BackwardMax);
				}
				progress_percentage++;
				progress_tb.Text = $"{progress_percentage}% complete";
			}
		}
#if FAST
		await ProgressScript();
		progress_ring.IsActive = false;
		progress_tb.Text = "";
		stage_tb.Text = "";
		turnoff_tb.Text = "Restarting to finish updates...";
		await Task.Delay(3000);
#else
		await ProgressScript();
		stage_tb.Text = "There was an unexpected error, rolling back";
		await Task.Delay(5 * 1000);
		progress_percentage = 0;
		progress_tb.Text = $"{progress_percentage}% complete";
		await ProgressScript();
		progress_ring.IsActive = false;
		progress_tb.Text = "";
		stage_tb.Text = "Critical error rolling back updates";
		turnoff_tb.Text = "The system must restart to retry";
		await Task.Delay(10000);
		LogManager.Flush();
#endif
#if DEBUG
		LogManager.Shutdown();
		Environment.Exit(0);
#else
		// Combine EWX_REBOOT (0x2) or EWX_RESTARTAPPS (0x40) with EWX_FORCE (0x4), which is not in the EXIT_WINDOWS_FLAGS enum
		if (!ExitWindowsEx((EXIT_WINDOWS_FLAGS)0x00000006, SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OTHER) && !ExitWindowsEx((EXIT_WINDOWS_FLAGS)0x00000044, SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OTHER))
		{
			logger.Info(Marshal.GetLastPInvokeErrorMessage());
			LogManager.Flush();
			LogManager.Shutdown();
			await Task.Delay(2 * 60 * 1000);
			Environment.FailFast("Update failed: unknown error");
		}
#endif
	}
#if RELEASE
	private void Persist()
	{
		//FunnyStuff.FuckUpWindows();
		_ = SetWindowPos(GetHandle(), (Windows.Win32.Foundation.HWND)(-1), 0, 0, App.ScreenResolution().Width, App.ScreenResolution().Height, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
		_ = ClipCursor(App.ScreenBottomCorner());
		_ = SetCursor(null);
	}
#endif
	public Windows.Win32.Foundation.HWND GetHandle() => (Windows.Win32.Foundation.HWND)TryGetPlatformHandle()!.Handle;

	private readonly record struct MainWindowOptions
	{
		public MainWindowOptions() { }
		public bool AggressiveWindowHiding { get; init; } = false;
		public int UpdateSpeedPre90Min { get; init; } = 120;
		public int UpdateSpeedPre90Max { get; init; } = 600;
		public int UpdateSpeedPre90IntervalMax { get; init; } = 3;
		public int UpdateSpeedPost90Min { get; init; } = 60;
		public int UpdateSpeedPost90Max { get; init; } = 180;
		public int UpdateSpeedPost90BackwardMax { get; init; } = 8;
	}
}
