using System;
using Cairo;
using Gdk;
using Gtk;
using Rectangle = Gdk.Rectangle;
using Window = Gtk.Window;

namespace Sentinel
{
	public static class Overlay
	{
		private static Window SelectionOverlay = null!;
		private static Rectangle? rect;

		public static ulong XID => X11Wrapper.gdk_x11_window_get_xid(SelectionOverlay.Window.Handle);

		public static event Action<ButtonPressEventArgs>? OnButtonPressed;
		public static event Action<MotionNotifyEventArgs>? OnMouseMoved;

		public static void SetRectangle(Rectangle rectangle)
		{
			rect = rectangle;
			SelectionOverlay.QueueDraw();
		}

		public static void Show()
		{
			SelectionOverlay.Show();
		}

		public static void Hide()
		{
			SelectionOverlay.Hide();
			rect = null;
		}

		internal static void Initialize()
		{
			Screen screen = Screen.Default;
			Visual visual = screen.RgbaVisual;

			SelectionOverlay = new Window(Gtk.WindowType.Popup);

			CssProvider styleProvider = new();
			styleProvider.LoadFromPath("theme.css");

			if (screen.IsComposited && visual != null)
			{
				SelectionOverlay.Visual = visual;
				SelectionOverlay.AppPaintable = true;
			}

			SelectionOverlay.Events = EventMask.PointerMotionMask | EventMask.ButtonPressMask;
			SelectionOverlay.MotionNotifyEvent += (o, args) =>
			{
				OnMouseMoved?.Invoke(args);
			};
			SelectionOverlay.StyleContext.AddProvider(styleProvider, StyleProviderPriority.User);
			SelectionOverlay.Name = "Overlay";

			SelectionOverlay.ButtonPressEvent += (o, args) => { OnButtonPressed?.Invoke(args); };

			SelectionOverlay.Drawn += (o, args) =>
			{
				if (rect == null) return;

				args.RetVal = false;

				Widget widget = (Widget)o;
				var style = widget.StyleContext;
				var ctx = args.Cr;

				if (widget.AppPaintable)
				{
					ctx.Operator = Operator.Source;
					ctx.SetSourceRGBA(0, 0, 0, 0);
					ctx.Paint();

					style.Save();
					style.AddClass("rubberband");

					style.RenderBackground(ctx, rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height);
					style.RenderFrame(ctx, rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height);

					style.Restore();
				}
			};
			SelectionOverlay.Move(0, 0);
			SelectionOverlay.Resize(screen.Width, screen.Height);
		}
	}
}