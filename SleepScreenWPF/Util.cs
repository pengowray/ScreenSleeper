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

        // maxLines of 0 or less keeps everything; otherwise the oldest lines are dropped.
        public static void AppendTextSafe(this TextBox textBox1, string text, int maxLines = 0) {
            // Check if we need to call this method on the UI thread
            if (textBox1.Dispatcher.CheckAccess()) {
                // This means we're on the UI thread, so we can update the textbox directly
                textBox1.Text += text;
                if (maxLines > 0) {
                    textBox1.Text = TrimToLastLines(textBox1.Text, maxLines);
                }
            } else {
                // We're not on the UI thread, so we use Dispatcher.Invoke to handle the update
                textBox1.Dispatcher.Invoke(() => AppendTextSafe(textBox1, text, maxLines));
            }
        }

        // Keeps the last maxLines lines of text, dropping whole lines off the front.
        private static string TrimToLastLines(string text, int maxLines) {
            int lines = 0;
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\n') {
                    lines++;
                }
            }

            int toDrop = lines - maxLines;
            if (toDrop <= 0) {
                return text;
            }

            int cut = -1;
            for (int i = 0; i < toDrop; i++) {
                cut = text.IndexOf('\n', cut + 1);
                if (cut < 0) {
                    return text;
                }
            }

            return text.Substring(cut + 1);
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
