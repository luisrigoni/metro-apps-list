using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MetroAppsList.Win32
{
    static class NativeMethods
    {
        [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

        static internal string ExtractStringFromPRIFile(string pathToPRI, string resourceKey)
        {
            //string sWin8ManifestString = @"@{C:\Program Files\WindowsApps\Microsoft.Camera_6.2.8514.0_x64__8wekyb3d8bbwe\resources.pri? ms-resource://Microsoft.Camera/resources/manifestDisplayName}";
            string sWin8ManifestString = string.Format("@{{{0}? {1}}}", pathToPRI, resourceKey);
            var outBuff = new StringBuilder(1024);
            int result = SHLoadIndirectString(sWin8ManifestString, outBuff, outBuff.Capacity, IntPtr.Zero);
            return outBuff.ToString();
        }
    }
}
