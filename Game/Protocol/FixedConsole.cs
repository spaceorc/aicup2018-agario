using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Game.Protocol
{
	public static class FixedConsole
	{
#if !__MonoCS__
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(int nStdHandle);
		private static readonly StreamReader inputStreamReader;

		static FixedConsole()
		{
			var safeFileHandle = new SafeFileHandle(GetStdHandle(-10), false);
			if (safeFileHandle.IsInvalid)
				throw new Exception("Invalid console input handle");
			var inputStream = new FileStream(safeFileHandle, FileAccess.Read);
			inputStreamReader = new StreamReader(inputStream);
		}
#endif

		public static string ReadLine()
		{
#if __MonoCS__
			return Console.ReadLine();
#else
			return inputStreamReader.ReadLine();
#endif
		}
	}
}