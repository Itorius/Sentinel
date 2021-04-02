using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gdk;
using X11;
using Event = X11.Event;
using EventMask = X11.EventMask;
using Window = X11.Window;

namespace Sentinel
{
	public static class HotkeyManager
	{
		private static IntPtr Display;
		private static Window Root;
		private static Dictionary<uint, Action> hotkeys;

		public static void RegisterHotkey(Key key, Action onPressed)
		{
			var keycode = Xlib.XKeysymToKeycode(Display, (KeySym)key);
			hotkeys.Add((uint)keycode, onPressed);
		}

		// private static Gtk.Window? w;

		private static bool grabbed;

		public static unsafe void Initialize()
		{
			Display = Xlib.XOpenDisplay(null);
			Root = Xlib.XDefaultRootWindow(Display);
			hotkeys = new Dictionary<uint, Action>();

			// bool b = Xlib.XQueryExtension(Display, "XInputExtension", out _, out _, out _);
			// Console.WriteLine(b);
			//
			// int major = 2, minor = 0;
			// int queryResult = Xlib.XIQueryVersion(Display, ref major, ref minor);
			// Console.WriteLine(queryResult + $"{major}.{minor}");
			//
			// if (queryResult == 0)
			// {
			// 	var mask = stackalloc byte[4];
			//
			// 	Xlib.XISetMask(mask, Xlib.XI_RawKeyPress);
			// 	Xlib.XISetMask(mask, Xlib.XI_RawKeyRelease);
			//
			// 	Xlib.XIEventMask evmask = new Xlib.XIEventMask
			// 	{
			// 		deviceid = Xlib.XIAllDevices,
			// 		mask_len = 4
			// 	};
			//
			// 	evmask.mask = mask;
			//
			// 	Console.WriteLine(Xlib.XISelectEvents(Display, Root, &evmask, 1));
			// }
			//
			// return;
			// Xlib.XGrabKeyboard(Display, Root, false, GrabMode.Async, GrabMode.Async, 0);

			var keycode = Xlib.XKeysymToKeycode(Display, (KeySym)0xFF61);

			KeyButtonMask[] masks = { 0, KeyButtonMask.LockMask, KeyButtonMask.Mod2Mask, KeyButtonMask.LockMask | KeyButtonMask.Mod2Mask };

			foreach (KeyButtonMask mask in masks)
			{
				Xlib.XUngrabKey(Display, keycode, mask, Root);
				Xlib.XGrabKey(Display, keycode, mask, Root, false, GrabMode.Async, GrabMode.Async);
			}

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

						switch ((Event)e.type)
						{
							case Event.KeyPress:
							{
								if (xKeyEvent.keycode == (ulong)keycode)
								{
									if (!grabbed)
									{
										Xlib.XGrabKeyboard(Display, Root, false, GrabMode.Async, GrabMode.Async, 0);
										Console.WriteLine("grabbed keyboard");
										grabbed = true;
									}
								}
								else
								{
									bool any = false;
									foreach (var (key, value) in hotkeys)
									{
										if (xKeyEvent.keycode  == key)
										{
											any = true;
											Console.WriteLine(key);
											value.Invoke();
										}
									}

									if (!any)
									{
										Xlib.XUngrabKeyboard(Display, 0);
										Console.WriteLine("ungrabbed keyboard");

										grabbed = false;
									}
								}

								break;
							}
							case Event.KeyRelease:
							{
								if (Xlib.XEventsQueued(Display, QueueMode.QueuedAfterReading) != 0)
								{
									XAnyEvent nev = new XAnyEvent();
									Xlib.XPeekEvent(Display, &nev);
									XKeyEvent next = *(XKeyEvent*)&nev;

									if (nev.type == (int)Event.KeyPress && next.time == xKeyEvent.time && next.keycode == xKeyEvent.keycode)
									{
										break;
									}
								}

								if (xKeyEvent.keycode == (ulong)keycode)
								{
									Xlib.XUngrabKeyboard(Display, 0);
									Console.WriteLine("ungrabbed keyboard");
									grabbed = false;
								}

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