using System;
using Gdk;
using Gtk;
using Key = Gdk.Key;

namespace Sentinel.ScreenCapture
{
	public class AreaSelection
	{
		private struct SelectionData
		{
			public Rectangle rect;
			public bool buttonPressed;
			public bool aborted;

			public SelectionData(Rectangle rect, bool buttonPressed, bool aborted)
			{
				this.rect = rect;
				this.buttonPressed = buttonPressed;
				this.aborted = aborted;
			}
		}

		private const string CSSStyle = "rubberband";

		private SelectionData data;
		private Rectangle rect;
		public event Action<Rectangle>? Callback;

		public AreaSelection()
		{
			data = new SelectionData(Rectangle.Zero, false, false);

			Overlay.Show();

			Overlay.OnButtonPressed += ButtonPressed;
			Overlay.OnKeyPressed += KeyPressed;
			Overlay.OnButtonReleased += ButtonReleased;
			Overlay.OnMouseMoved += MouseMoved;
		}

		private void MouseMoved(MotionNotifyEventArgs args)
		{
			args.RetVal = true;

			Rectangle draw_rect = new Rectangle();

			if (!data.buttonPressed) return;

			draw_rect.Width = (int)Math.Abs(data.rect.X - args.Event.XRoot);
			draw_rect.Height = (int)Math.Abs(data.rect.Y - args.Event.YRoot);
			draw_rect.X = (int)Math.Min(data.rect.X, args.Event.XRoot);
			draw_rect.Y = (int)Math.Min(data.rect.Y, args.Event.YRoot);

			rect = draw_rect;
			Overlay.SetRectangle(rect);
		}

		private void ButtonReleased(ButtonReleaseEventArgs args)
		{
			if (!data.buttonPressed) return;

			data.rect.Width = (int)Math.Abs(data.rect.X - args.Event.XRoot);
			data.rect.Height = (int)Math.Abs(data.rect.Y - args.Event.YRoot);
			data.rect.X = (int)Math.Min(data.rect.X, args.Event.XRoot);
			data.rect.Y = (int)Math.Min(data.rect.Y, args.Event.YRoot);

			if (data.rect.Width == 0 || data.rect.Height == 0) data.aborted = true;

			Overlay.Hide();

			if (!data.aborted) Callback?.Invoke(data.rect);
		}

		private void ButtonPressed(ButtonPressEventArgs args)
		{
			if (data.buttonPressed) return;

			data.buttonPressed = true;
			data.rect.X = (int)args.Event.XRoot;
			data.rect.Y = (int)args.Event.YRoot;
		}

		private void KeyPressed(KeyPressEventArgs args)
		{
			if (args.Event.Key == Key.Escape)
			{
				data.rect.X = 0;
				data.rect.Y = 0;
				data.rect.Width = 0;
				data.rect.Height = 0;
				data.aborted = true;

				Overlay.Hide();

				if (!data.aborted) Callback?.Invoke(data.rect);
			}
		}
	}
}