using System;
using Cairo;
using Gdk;
using Gtk;
using Key = Gdk.Key;
using Rectangle = Gdk.Rectangle;
using Window = Gtk.Window;

namespace Sentinel.ScreenCapture
{
	public class AreaSelection
	{
		private struct SelectionData
		{
			public Rectangle rect;
			public bool buttonpressed;
			public Widget window;
			public bool aborted;

			public SelectionData(Rectangle rect, bool buttonpressed, Widget window, bool aborted)
			{
				this.rect = rect;
				this.buttonpressed = buttonpressed;
				this.window = window;
				this.aborted = aborted;
			}
		}

		private const string CSSStyle = "rubberband";

		private SelectionData data;
		private Rectangle rect;

		public AreaSelection()
		{
			Widget window = GetSelectWindow();
			data = new SelectionData(Rectangle.Zero, false, window, false);

			window.KeyPressEvent += KeyPressed;
			window.ButtonPressEvent += ButtonPressed;
			window.ButtonReleaseEvent += ButtonReleased;
			window.MotionNotifyEvent += MouseMoved;

			Display display = window.Display;
			Cursor cursor = new(display, CursorType.Crosshair);
			Seat seat = display.DefaultSeat;

			seat.Grab(window.Window, SeatCapabilities.AllPointing, false, cursor, null, null);

			Application.Run();

			seat.Ungrab();

			window.Dispose();
		}

		private void MouseMoved(object o, MotionNotifyEventArgs args)
		{
			args.RetVal = true;

			Rectangle draw_rect = new Rectangle();

			if (!data.buttonpressed) return;

			draw_rect.Width = (int)Math.Abs(data.rect.X - args.Event.XRoot);
			draw_rect.Height = (int)Math.Abs(data.rect.Y - args.Event.YRoot);
			draw_rect.X = (int)Math.Min(data.rect.X, args.Event.XRoot);
			draw_rect.Y = (int)Math.Min(data.rect.Y, args.Event.YRoot);

			rect = draw_rect;
			data.window.QueueDraw();

			if (draw_rect.Width <= 0 || draw_rect.Height <= 0)
				return;

			/* We (ab)use app-paintable to indicate if we have an RGBA window */
			if (!data.window.AppPaintable)
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

					data.window.Window.ShapeCombineRegion(region, 0, 0);
					region.Dispose();
				}
				else
					data.window.Window.ShapeCombineRegion(null, 0, 0);
			}
		}

		private void ButtonReleased(object o, ButtonReleaseEventArgs args)
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

		private void ButtonPressed(object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			if (data.buttonpressed) return;

			data.buttonpressed = true;
			data.rect.X = (int)args.Event.XRoot;
			data.rect.Y = (int)args.Event.YRoot;
		}

		private void KeyPressed(object o, KeyPressEventArgs args)
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
		}

		private Widget GetSelectWindow()
		{
			Screen screen = Screen.Default;
			Visual visual = screen.RgbaVisual;
			Window window = new(Gtk.WindowType.Popup);

			if (screen.IsComposited && visual != null)
			{
				window.Visual = visual;
				window.AppPaintable = true;
			}

			window.Drawn += DrawSelection;
			window.Move(0, 0);
			window.Resize(screen.Width, screen.Height);

			rect = Rectangle.Zero;

			window.Show();
			return window;
		}

		private void DrawSelection(object sender, DrawnArgs args)
		{
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