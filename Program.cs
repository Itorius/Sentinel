using System;
using Cairo;
using Gdk;
using Gtk;
using Key = Gdk.Key;
using Rectangle = Gdk.Rectangle;
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
			HotkeyPrintscreen.Pressed += (_, _) => OnPrintscreen();
			HotkeyPrintscreen.Register();

			MainWindow = new Window("Sentinel") { Icon = new Pixbuf("icon.png") };

			MainWindow.DeleteEvent += (_, _) =>
			{
				HotkeyPrintscreen.Unregister();
				Application.Quit();
			};

			MainWindow.Show();

			Application.Run();
		}

		public struct select_area_filter_data
		{
			public Rectangle rect;
			public bool buttonpressed;
			public Widget window;
			public bool aborted;

			public select_area_filter_data(Rectangle rect, bool buttonpressed, Widget window, bool aborted)
			{
				this.rect = rect;
				this.buttonpressed = buttonpressed;
				this.window = window;
				this.aborted = aborted;
			}
		}

		private static select_area_filter_data data;
		private static Rectangle rect;
		private static void OnPrintscreen()
		{
			Widget window = GetSelectWindow();
			data = new select_area_filter_data(Rectangle.Zero, false, window, false);

			window.KeyPressEvent += (o, args) =>
			{
				if (args.Event.Key == Key.Escape)
				{
					data.rect.X = 0;
					data.rect.Y = 0;
					data.rect.Width = 0;
					data.rect.Height = 0;
					data.aborted = true;
					args.RetVal = true;

					Application.Quit();
				}
			};

			void OnButtonPress(object o, ButtonPressEventArgs args)
			{
				args.RetVal = true;

				if (data.buttonpressed) return;

				data.buttonpressed = true;
				data.rect.X = (int)args.Event.XRoot;
				data.rect.Y = (int)args.Event.YRoot;
			}

			window.ButtonPressEvent += OnButtonPress;

			void OnButtonRelease(object o, ButtonReleaseEventArgs args)
			{
				args.RetVal = true;

				if (!data.buttonpressed) return;

				data.rect.Width = (int)Math.Abs(data.rect.X - args.Event.XRoot);
				data.rect.Height = (int)Math.Abs(data.rect.Y - args.Event.YRoot);
				data.rect.X = (int)Math.Min(data.rect.X, args.Event.XRoot);
				data.rect.Y = (int)Math.Min(data.rect.Y, args.Event.YRoot);

				if (data.rect.Width == 0 || data.rect.Height == 0) data.aborted = true;

				Application.Quit();
			}

			window.ButtonReleaseEvent += OnButtonRelease;

			void OnMouseMove(object o, MotionNotifyEventArgs args)
			{
				args.RetVal = true;

				Rectangle draw_rect = new Rectangle();

				if (!data.buttonpressed) return;

				draw_rect.Width = (int)Math.Abs(data.rect.X - args.Event.XRoot);
				draw_rect.Height = (int)Math.Abs(data.rect.Y - args.Event.YRoot);
				draw_rect.X = (int)Math.Min(data.rect.X, args.Event.XRoot);
				draw_rect.Y = (int)Math.Min(data.rect.Y, args.Event.YRoot);

				rect = draw_rect;
				window.QueueDraw();

				if (draw_rect.Width <= 0 || draw_rect.Height <= 0)
				{
					// window.Window.Move(-100, -100);
					// window.Window.Resize(10, 10);
					return;
				}
					
				// const int sensitivity = 1;
				// ((Window)window).Move(sensitivity * (int)Math.Round(draw_rect.X / (double)sensitivity), sensitivity * (int)Math.Round(draw_rect.Y / (double)sensitivity));
				// ((Window)window).Resize(sensitivity * (int)Math.Round(draw_rect.Width / (double)sensitivity), sensitivity * (int)Math.Round(draw_rect.Height / (double)sensitivity));

				/* We (ab)use app-paintable to indicate if we have an RGBA window */
				if (!window.AppPaintable)
				{
					/* Shape the window to make only the outline visible */
					if (draw_rect.Width > 2 && draw_rect.Height > 2)
					{
						RectangleInt region_rect = new RectangleInt
						{
							X = 0,
							Y = 0,
							Width = draw_rect.Width,
							Height = draw_rect.Height
						};

						Cairo.Region region = new(region_rect);
						region_rect.X++;
						region_rect.Y++;
						region_rect.Width -= 2;
						region_rect.Height -= 2;

						region.SubtractRectangle(region_rect);

						window.Window.ShapeCombineRegion(region, 0, 0);
						region.Dispose();
					}
					else
						window.Window.ShapeCombineRegion(null, 0, 0);
				}
			}

			window.MotionNotifyEvent += OnMouseMove;

			Display display = window.Display;
			Cursor cursor = new(display, CursorType.Crosshair);

			Seat seat = display.DefaultSeat;
			GrabStatus result = seat.Grab(window.Window, SeatCapabilities.All, false, cursor, null, null);
			Console.WriteLine(result);

			Application.Run();

			seat.Ungrab();

			window.Dispose();
		}

		private static Widget GetSelectWindow()
		{
			Screen screen = Screen.Default;
			Visual visual = screen.RgbaVisual;
			Widget window = new Window(Gtk.WindowType.Popup);

			if (screen.IsComposited && visual != null)
			{
				window.Visual = visual;
				window.AppPaintable = true;
			}

			window.Drawn += SelectDraw;
			((Window)window).Move(0, 0);
			((Window)window).Resize(screen.Width, screen.Height);

			rect = Rectangle.Zero;
			
			window.Show();
			return window;
		}

		private const string CSSStyle = "rubberband";

		private static void SelectDraw(object sender, DrawnArgs args)
		{
			// Widget widget = (Widget)sender;
			// var style = widget.StyleContext;
			//
			// var imageSurface = new ImageSurface(Format.ARGB32, 5000, 5000);
			// var ctx = new Context(imageSurface);
			//
			// /* do all your drawing here */
			//
			// ctx.Operator = Operator.Source;
			// ctx.SetSourceRGBA(0, 0, 0, 0);
			// ctx.Paint();
			//
			// style.Save();
			// style.AddClass(CSSStyle);
			//
			// style.RenderBackground(ctx, 0, 0, widget.AllocatedWidth, widget.AllocatedHeight);
			// style.RenderFrame(ctx, 0, 0, widget.AllocatedWidth, widget.AllocatedHeight);
			//
			// style.Restore();
			//
			// ctx.Dispose();

			args.RetVal = false;

			Widget widget = (Widget)sender;
			var style = widget.StyleContext;
			var ctx = args.Cr;

			if (widget.AppPaintable)
			{
				ctx.Operator = Operator.Source;
				ctx.SetSourceRGBA(0, 0, 0, 0);
				ctx.Paint();

				style.Save();
				style.AddClass(CSSStyle);

				style.RenderBackground(ctx, rect.X, rect.Y, rect.Width, rect.Height);
				style.RenderFrame(ctx, rect.X, rect.Y, rect.Width, rect.Height);
				
				style.Restore();
			}
		}
	}
}