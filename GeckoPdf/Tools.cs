using System.IO;
using System.Runtime.InteropServices;

namespace GeckoPdf
{
    public static class Tools
    {
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (File.Open(filePath, FileMode.Open)) { }
            }
            catch (IOException e)
            {
                var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                return errorCode == 32 || errorCode == 33;
            }

            return false;
        }
    }
}
