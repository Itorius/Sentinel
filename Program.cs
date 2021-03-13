using Gdk;
using Gtk;
using Sentinel.ScreenCapture;
using Key = Gdk.Key;
using Window = Gtk.Window;

namespace Sentinel
{
	internal class Program
	{
		private static X11Hotkey HotkeyPrintscreen = null!;
		private static Window MainWindow = null!;

		private static void Main(string[] args)
		{
			Application.Init();

			HotkeyPrintscreen = new X11Hotkey(Key.Print);
			HotkeyPrintscreen.Pressed += (_, _) =>
			{
				// AreaSelection selection = new();
				WindowSelection selection = new();
			};
			HotkeyPrintscreen.Register();

			MainWindow = new Window("Sentinel") { Icon = new Pixbuf("icon.png") };
			MainWindow.Resize(200, 200);

			MainWindow.DeleteEvent += (_, _) =>
			{
				HotkeyPrintscreen.Unregister();
				Application.Quit();
			};

			MainWindow.Show();

			Application.Run();
		}
	}
}