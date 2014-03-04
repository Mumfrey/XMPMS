// ======================================================================
//  Unreal2 XMP Master Server
//  Copyright (C) 2010-2011  Adam Mummery-Smith
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

//  Copyright Notice:
//  Unreal and the Unreal logo are registered trademarks of Epic
//  Games, Inc. ALL RIGHTS RESERVED.
// ======================================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace XMPMS.Util
{
    /// <summary>
    /// Functions for detecting whether we are in a console session or not
    /// </summary>
    internal static class ConsoleUtilities
    {
        #region Interop
        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private const int STD_OUTPUT_HANDLE = -11;

        private struct COORD
        {
            public short X;
            public short Y;
        }

        private struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        private struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }
        #endregion

        /// <summary>
        /// Try to detect whether the application is running in a console session
        /// </summary>
        /// <returns>True if a console session was detected</returns>
        internal static bool InConsoleSession()
        {
            CONSOLE_SCREEN_BUFFER_INFO csbi;
            return (GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), out csbi));
        }

        /// <summary>
        /// Try to show the console window (if we have one)
        /// </summary>
        internal static void ShowConsoleWindow()
        {
            Console.Title = "Master Server Console";
            IntPtr hWnd = FindWindow(null, "Master Server Console");
            if (hWnd != IntPtr.Zero) ShowWindow(hWnd, SW_SHOW);
        }

        /// <summary>
        /// Try to hide the console window (if we have one)
        /// </summary>
        internal static void HideConsoleWindow()
        {
            Console.Title = "Master Server Console";
            IntPtr hWnd = FindWindow(null, "Master Server Console");
            if (hWnd != IntPtr.Zero) ShowWindow(hWnd, SW_HIDE);
        }
    }
}
