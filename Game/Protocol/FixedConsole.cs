using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Game.Protocol
{
	public static class FixedConsole
	{
		public static string ReadLine()
		{
			return Console.ReadLine();
		}
	}
}