using System;
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
		private static Window MainWindow = null!;

		private static void Main(string[] args)
		{
			Application.Init();

			Overlay.Initialize();
			Configuration.Initialize();

			SetupHotkeys();

			CssProvider styleProvider = new();
			styleProvider.LoadFromPath("main.css");

			MainWindow = new Window("Sentinel") { Icon = new Pixbuf("icon.png") };
			MainWindow.Resize(1280, 720);
			MainWindow.StyleContext.AddProvider(styleProvider, StyleProviderPriority.User);

			MainWindow.DeleteEvent += (_, _) =>
			{
				HotkeyManager.Cleanup();
				Application.Quit();
			};


			// var entry = new Entry
			// {
			// 	Text = "ifaISFHJSU",
			// 	IsEditable = false,
			// 	CanFocus = false
			// };
			//
			// var button = new Button
			// {
			// 	Label = "..."
			// };
			//
			// var dialog = new FileChooserDialog("Select save foler", MainWindow, FileChooserAction.SelectFolder, "Ok", ResponseType.Ok, null);
			//
			// button.ButtonReleaseEvent += (o, eventArgs) =>
			// {
			// 	if (dialog.Run() == (int)ResponseType.Ok)
			// 	{
			// 		entry.Text = dialog.Filename;
			// 	}
			//
			// 	dialog.Destroy();
			// };
			//
			// var box = new Box(Orientation.Horizontal, 0)
			// {
			// 	entry, button
			// };
			//
			// VBox vbox = new VBox();
			// vbox.Add(new Button("Doot"));
			// vbox.Add(new Button("Dooter"));
			// vbox.Add(new Button("Dootest"));
			//
			// vbox.Vexpand = false;
			//
			// HBox hbox = new HBox();
			// hbox.Add(vbox);
			// hbox.Add(box);

			var grid = new Grid();

			var paned = new Paned(Orientation.Horizontal);
			paned.Valign = paned.Halign = Align.Fill;
			paned.Expand = true;

			// var box = new Box(Orientation.Horizontal, 0);
			// box.Hexpand = box.HexpandSet = false;
			// box.Valign = box.Halign = Align.Fill;

			var listbox = new ListBox();

			// var viewport = new Viewport();

			// var listbox = new ListBox();
			// viewport.Add(listbox);

			var widget = new Label("Doot") { Selectable = false, Ypad = 8, Xpad = 8, Xalign = 0f };


			var image = new Image(new Pixbuf("icon.png", 24, 24));

			HBox hbox = new HBox(false, 0);
			hbox.PackStart(image, false, false, 0);
			hbox.PackStart(widget, true, true, 0);

			var listBoxRow = new ListBoxRow { Selectable = false };
			listBoxRow.Add(hbox);

			listbox.Add(listBoxRow);
			listbox.Add(new Label("Dooter") { Selectable = false, Ypad = 8, Xpad = 8, Xalign = 0f });
			listbox.Add(new Label("Dootest") { Selectable = false, Ypad = 8, Xpad = 8, Xalign = 0f });

			grid.Add(paned);
			paned.Add(listbox);
			paned.Add(new Grid());
			// box.Add(sidebar);

			// MainWindow.Add(hbox);
			MainWindow.Add(grid);
			MainWindow.ShowAll();

			Application.Run();
		}

		private static void SetupHotkeys()
		{
			HotkeyManager.Initialize();

			HotkeyManager.RegisterHotkey(Key.W, () =>
			{
				Application.Invoke(delegate
				{
					WindowSelection selection = new();
					selection.Callback += (window, rectangle) =>
					{
						window = window.Toplevel;
						var pixbuf = new Pixbuf(window, 0, 0, window.Width, window.Height);

						pixbuf.Save($"{Configuration.SavePath}{DateTime.Now.ToString(Configuration.TimeFormat)}.png", "png");

						Atom atom = Atom.Intern("CLIPBOARD", false);
						Clipboard clipboard = Clipboard.Get(atom);
						clipboard.Image = pixbuf;
						clipboard.Store();
					};
				});
			});

			HotkeyManager.RegisterHotkey(Key.A, () =>
			{
				Application.Invoke(delegate
				{
					AreaSelection selection = new();
					selection.Callback += rectangle =>
					{
						Task.Delay(200).ContinueWith(_ =>
						{
							var window = Display.Default.DefaultScreen.RootWindow;
							var pixbuf = new Pixbuf(window, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

							pixbuf.Save($"{Configuration.SavePath}{DateTime.Now.ToString(Configuration.TimeFormat)}.png", "png");

							Atom atom = Atom.Intern("CLIPBOARD", false);
							Clipboard clipboard = Clipboard.Get(atom);
							clipboard.Image = pixbuf;
							clipboard.Store();
						}, TaskScheduler.FromCurrentSynchronizationContext());
					};
				});
			});
		}
	}
}