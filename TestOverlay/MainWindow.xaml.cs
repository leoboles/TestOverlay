using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Hooks;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

namespace TestOverlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var hostHandle = Win32Window.FindWindow(null, txtWindowTitle.Text);

            var windowTarget = new Win32Window(hostHandle);

            var mySelf = new OverlayWindow();

            mySelf.Hide();
            mySelf.ResizeMode = ResizeMode.NoResize;
            mySelf.WindowStyle = WindowStyle.None;
            mySelf.Opacity = 0;
            mySelf.Show();

            IntPtr guestHandle = new WindowInteropHelper(mySelf).Handle;
            Win32Window.SetWindowLongPtr(guestHandle, (int)Win32Window.GWL.GWL_STYLE, new IntPtr(Win32Window.GetWindowLong32(guestHandle, (int)Win32Window.GWL.GWL_STYLE) | 0x40000000L));
            if (Win32Window.SetParent(guestHandle, hostHandle).ToInt32() == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Task.Run(() =>
            {
                mySelf.Dispatcher.Invoke(() =>
                {
                    RECT rect;
                    //Win32Window.GetClientRect(hostHandle, out rect);
                    rect = GetWindowScaledSize(windowTarget.hWnd, mySelf);


                    Win32Window.SetWindowPos(
                    new WindowInteropHelper(mySelf).Handle,
                    new IntPtr(-1),
                    0,
                    0,
                    rect.Width - 1,
                    rect.Height - 16, 0x0010 | 0x0004);

                    mySelf.Width = rect.Width;
                    mySelf.Height = rect.Height;

                    mySelf.Opacity = 1;
                });
            });
        }

        public static RECT GetWindowScaledSize(IntPtr windowTargethandle, Window windowToScale)
        {
            RECT rect;
            Win32Window.GetWindowRect(windowTargethandle, out rect);

            if (windowToScale != null)
            {
                Matrix m = PresentationSource.FromVisual(windowToScale).CompositionTarget.TransformToDevice;
                double dx = m.M11;
                double dy = m.M22;

                rect.Width = (int)Math.Round((rect.Width - 42) / dx);
                rect.Height = (int)Math.Round((rect.Height - 40) / dy);
            }
            return rect;
        }
    }
}
