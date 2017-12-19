using System.Runtime.InteropServices;
using System.Text;

namespace SlnGen.Build.Tasks.Internal
{
    internal static class NativeMethods
    {
        /// <summary>
        /// Converts the specified path to its long form.
        /// </summary>
        /// <param name="lpszShortPath">The path to be converted.</param>
        /// <param name="lpszLongPath">A pointer to the buffer to receive the long path.</param>
        /// <param name="cchBuffer">The size of the buffer lpszLongPath points to, in TCHARs.</param>
        /// <returns>If the function succeeds, the return value is the length, in TCHARs, of the string copied to lpszLongPath, not including the terminating null character.
        /// 
        /// If the lpBuffer buffer is too small to contain the path, the return value is the size, in TCHARs, of the buffer that is required to hold the path and the terminating null character.
        /// 
        /// If the function fails for any other reason, such as if the file does not exist, the return value is zero.To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int GetLongPathName([MarshalAs(UnmanagedType.LPTStr)] string lpszShortPath, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszLongPath, [MarshalAs(UnmanagedType.U4)] int cchBuffer);
    }
}