using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SleepScreenWPF {
    internal class LockScreenLib {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool LockWorkStation();

        public static void LockScreen() {
            LockWorkStation();
        }
    }
}
