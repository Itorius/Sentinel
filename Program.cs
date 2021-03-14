using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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

			Overlay.Initialize();

			HotkeyPrintscreen = new X11Hotkey(Key.Print);
			HotkeyPrintscreen.Pressed += (_, _) =>
			{
				// AreaSelection selection = new();
				WindowSelection selection = new();
				selection.Callback += rectangle =>
				{
					Task.Run(() =>
					{
						Thread.Sleep(200);
						
						// var root = Display.Default.DefaultScreen.RootWindow;
						// Pixbuf pixbuf = new Pixbuf(root, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
						// pixbuf.Save("test.png", "png");
						
						

						// Atom atom = Atom.Intern("CLIPBOARD", false);
						// Clipboard clipboard = Clipboard.Get(atom);
						// clipboard.Image = pixbuf;
						// clipboard.Store();
					});
				};
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