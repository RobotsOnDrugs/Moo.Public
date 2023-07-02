using System.Collections.Immutable;
using System.IO;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Moo.CustomFakeNotification;

public partial class MainWindowViewModel : ObservableObject
{
	private readonly ImmutableArray<string> remote_tool_names = new[] { "AnyDesk", "TeamViewer", "UltraViewer" }.ToImmutableArray();

	[ObservableProperty] private string header = null!;

	// ReSharper disable InconsistentNaming // CommunityToolkit doesn't like snake_case (I think, need to double check later)
	[ObservableProperty] private string smallHeader = null!;

	[ObservableProperty] private string detection1 = null!;

	[ObservableProperty] private string detection2 = null!;

	[ObservableProperty] private string detection3 = null!;

	[ObservableProperty] private string detection4 = null!;

	[ObservableProperty] private string supportMessage1 = null!;

	[ObservableProperty] private string supportMessage2 = null!;

	[ObservableProperty] private string supportMessage3 = null!;

	[ObservableProperty] private string phoneNumber = null!;

	[ObservableProperty] private string scanMessage = null!;
	// ReSharper enable InconsistentNaming

	public MainWindowViewModel()
	{
		Header = "Hackers detected!";
		SmallHeader = "You Computer In Danger!";
		// Detection1 = "...15 hacker found on network...";
		Detection1 = "";
		Detection2 = "...Tinba virus found...";
		Detection3 = "...koobface virus found...";
		Detection4 = "...Ilegal pornography from WWW.PORNHUB.COM detected...";
		SupportMessage1 = "Hackers and scammers are currently present on your network";
		SupportMessage2 = "For your safety, we must do a mandatory scan";
		SupportMessage3 = "Do not turn off your computer and contact support immediately:";
		PhoneNumber = File.ReadLines("number.txt").First();
		ScanMessage = "Scan initiated, please wait...";
		_ = Dispatcher.UIThread.InvokeAsync(ScanFiles);
	}

	private async Task ScanFiles()
	{
		foreach (string procname in Process.GetProcesses().Select(proc => proc.ProcessName).ToHashSet())
		{
			await Task.Delay(400);
			ScanMessage = $"Scanning: {procname}";
			if (remote_tool_names.Contains(procname))
				Detection1 = procname;
		}
		ScanMessage = "Scan complete";
	}
}