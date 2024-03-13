using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SleepScreenWPF {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }


        private void TurnMonitorOff() {
            // Obtain the window handle for this Window instance
            WindowInteropHelper helper = new WindowInteropHelper(this);
            IntPtr windowHandle = helper.Handle;

            // sleep monitor
            SleepLib.MonitorOff(windowHandle);
        }

        private void TurnMonitorOn() {
            SleepLib.MonitorOn();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            TurnMonitorOff();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            LockScreenLib.LockScreen();
        }
    }
}