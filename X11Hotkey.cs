using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using X11;

namespace Sentinel
{
	public static class HotkeyManager
	{
		private static IntPtr Display;
		private static Window Root;
		private static Dictionary<uint, Action> hotkeys;

		public static void RegisterHotkey(Gdk.Key key, Action onPressed)
		{
			var keycode = Xlib.XKeysymToKeycode(Display, (KeySym)key);
			hotkeys.Add((uint)keycode, onPressed);
		}

		public static unsafe void Initialize()
		{
			Display = Xlib.XOpenDisplay(null);
			Root = Xlib.XDefaultRootWindow(Display);
			hotkeys = new Dictionary<uint, Action>();

			Xlib.XGrabKeyboard(Display, Root, false, GrabMode.Async, GrabMode.Async, 0);

			Task.Run(() =>
			{
				Xlib.XSelectInput(Display, Root, EventMask.KeyPressMask | EventMask.KeyReleaseMask);
				var e = new XAnyEvent();

				while (true)
				{
					while (Xlib.XPending(Display) != 0)
					{
						Xlib.XNextEvent(Display, &e);
						XKeyEvent xKeyEvent = *(XKeyEvent*)&e;

						if (e.send_event) continue;

						switch ((Event)e.type)
						{
							case Event.KeyPress:
							{
								if (IsPrintscreenPressed())
								{
									foreach (var (key, action) in hotkeys)
									{
										if (key == xKeyEvent.keycode)
										{
											action.Invoke();

											break;
										}
									}
								}

								Xlib.XGetInputFocus(Display, out var focus, out _);

								XKeyEvent keyEvent = new XKeyEvent
								{
									display = Display,
									window = focus,
									root = Root,
									subwindow = 0,
									time = 0,
									x = xKeyEvent.x,
									y = xKeyEvent.y,
									x_root = xKeyEvent.x_root,
									y_root = xKeyEvent.y_root,
									same_screen = xKeyEvent.same_screen,
									send_event = xKeyEvent.send_event,
									type = xKeyEvent.type,
									keycode = xKeyEvent.keycode,
									state = xKeyEvent.state
								};

								Xlib.XSendEvent(Display, focus, true, EventMask.KeyPressMask, ref keyEvent);

								break;
							}
							case Event.KeyRelease:
							{
								Xlib.XGetInputFocus(Display, out var focus, out _);

								XKeyEvent keyEvent = new XKeyEvent
								{
									display = Display,
									window = focus,
									root = Root,
									subwindow = 0,
									time = 0,
									x = xKeyEvent.x,
									y = xKeyEvent.y,
									x_root = xKeyEvent.x_root,
									y_root = xKeyEvent.y_root,
									same_screen = xKeyEvent.same_screen,
									send_event = xKeyEvent.send_event,
									type = xKeyEvent.type,
									keycode = xKeyEvent.keycode,
									state = xKeyEvent.state
								};

								Xlib.XSendEvent(Display, focus, true, EventMask.KeyReleaseMask, ref keyEvent);

								break;
							}
						}
					}
				}
			});
		}

		private static unsafe bool IsPrintscreenPressed()
		{
			var keymap = stackalloc byte[32];

			Xlib.XQueryKeymap(Display, keymap);

			int printscreen = (int)Xlib.XKeysymToKeycode(Display, (KeySym)0xFF61);

			return (keymap[printscreen / 8] & (0x1 << (printscreen % 8))) != 0;
		}

		public static void Cleanup()
		{
			hotkeys.Clear();
			Xlib.XUngrabKeyboard(Display, 0);
		}
	}
}