using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Devices;

namespace ChangeMonitorAutomagically;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private const int WorkMonitor = 17;
    private const int HomeMonitor = 15;
    private readonly Lock _lockObject = new();

    private CancellationTokenSource CancellationTokenSource { get; } = new();
    private Task? ChangeMonitorTask { get; set; }

    private bool _onWorkComputer = true;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        CancellationTokenSource.Cancel();
        base.OnClosing(e);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        _ = DeviceNotification
            .OnDeviceArrival()
            .Subscribe(UsbDeviceAdded);

        _ = DeviceNotification
            .OnDeviceRemoved()
            .Subscribe(UsbDeviceRemoved);

        ChangeMonitorTask = Task.Run(() =>
        {
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                Task.Delay(TimeSpan.FromSeconds(5), CancellationTokenSource.Token).Wait();
                ChangeMonitor();
            }
        });
    }

    private void UsbDeviceRemoved(DeviceInterfaceChangeInfo device)
    {
        if (device.Device.FriendlyDeviceName.Contains("Logitech BRIO"))
        {
            _onWorkComputer = false;
            ChangeMonitor();
        }
    }

    private void UsbDeviceAdded(DeviceInterfaceChangeInfo device)
    {
        if (device.Device.FriendlyDeviceName.Contains("Logitech BRIO"))
        {
            _onWorkComputer = true;
            ChangeMonitor();
        }
    }

    private void ChangeMonitor()
    {
        lock (_lockObject)
        {
            if (CancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            var monitorId = _onWorkComputer ? HomeMonitor : WorkMonitor;

            // Process.Start(@"C:\ControlMyMonitor\ControlMyMonitor.exe", $@"/SetValue ""\\.\DISPLAY1\Monitor0"" 60 {monitorId}");

            if (!_onWorkComputer)
            {
                Process.Start(@"C:\ControlMyMonitor\hidapitester.exe",
                    $@"--vidpid 046D:C52B --usage 0x0001 --usagePage 0xFF00 --open --length 7 --send-output 0x10,0x02,0x0c,0x1e,0x01,0x00,0x00");
            }
        }
    }
}
