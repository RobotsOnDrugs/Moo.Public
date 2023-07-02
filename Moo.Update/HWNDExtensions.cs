namespace Moo.Update;

public static class HWNDExtensions
{
	public static Process GetWindowProcess(this HWND hwnd)
	{
		uint _ = Vanara.PInvoke.User32.GetWindowThreadProcessId((nint)hwnd, out uint id);
		return Process.GetProcessById((int)id);
	}
}
