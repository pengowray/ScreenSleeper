using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;

namespace SleepScreenWPF {
    static class Util {

        public static void WriteTextSafe(this TextBox textBox1, string text) {
            // Check if we need to call this method on the UI thread
            if (textBox1.Dispatcher.CheckAccess()) {
                // This means we're on the UI thread, so we can update the textbox directly
                textBox1.Text = text;
            } else {
                // We're not on the UI thread, so we use Dispatcher.Invoke to handle the update
                textBox1.Dispatcher.Invoke(() => WriteTextSafe(textBox1, text));
            }
        }

        public static void AppendTextSafe(this TextBox textBox1, string text) {
            // Check if we need to call this method on the UI thread
            if (textBox1.Dispatcher.CheckAccess()) {
                // This means we're on the UI thread, so we can update the textbox directly
                textBox1.Text += text;
            } else {
                // We're not on the UI thread, so we use Dispatcher.Invoke to handle the update
                textBox1.Dispatcher.Invoke(() => AppendTextSafe(textBox1, text));
            }
        }

        public static void DoSafe(this Control textBox1, Action action) {
            if (textBox1.Dispatcher.CheckAccess()) {
                action();
            } else {
                textBox1.Dispatcher.Invoke(() => DoSafe(textBox1, action));
            }
        }


    }
}
