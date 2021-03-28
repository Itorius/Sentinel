using System;
using X11;

namespace Sentinel
{
	public enum SearchDirection
	{
		FindParent,
		FindChildren
	}

	public static class Utility
	{
		public static bool FindWindowClient(IntPtr xdo, Window window, SearchDirection direction, out Window client)
		{
			Atom atom = Xlib.XInternAtom(xdo, "WM_STATE", false);
			client = Window.None;

			bool done = false;
			while (!done)
			{
				if (window == Window.None) return false;

				Xlib.XGetWindowProperty(xdo, window, atom, 0, long.MaxValue, false, Atom.None, out _, out _, out var items, out _, out _);

				if (items == 0)
				{
					Xlib.XQueryTree(xdo, window, out _, out var parent, out var children);

					if (direction == SearchDirection.FindParent)
					{
						window = parent;
					}
					else if (direction == SearchDirection.FindChildren)
					{
						done = true;
						foreach (Window child in children)
						{
							bool ret = FindWindowClient(xdo, child, direction, out window);
							if (ret)
							{
								client = window;
								break;
							}
						}

						if (children.Count == 0) return false;
					}
					else
					{
						client = Window.None;
						return false;
					}
				}
				else
				{
					client = window;
					done = true;
				}
			}

			return true;
		}
	}
}