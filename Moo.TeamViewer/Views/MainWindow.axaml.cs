using Avalonia;
using Avalonia.Controls;

using Moo.TeamViewer.ViewModels;

namespace Moo.TeamViewer.Views;

public partial class MainWindow : Window
{
	public PixelPoint WindowPosition;
	public MainWindow()
	{
		InitializeComponent();
		PixelPoint bottomright = Screens.Primary.Bounds.BottomRight;
		WindowPosition = new(Screens.Primary.Bounds.BottomRight.X - 275, Screens.Primary.Bounds.BottomRight.Y - (215 + 100));
		DataContext = new MainWindowViewModel();
		Position = WindowPosition;
		Closing += (o, e) => e.Cancel = true;
	}
}