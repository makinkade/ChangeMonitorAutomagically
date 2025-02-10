using System;
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
    private object _lockObject = new();

    private Task CurrentChangeMonitorRequest { get; set; } = Task.CompletedTask;

    private CancellationTokenSource CancellationTokenSource { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var deviceNotificationArrivalSubscription = DeviceNotification
            .OnDeviceArrival()
            .Subscribe(UsbDeviceAdded);

        var deviceNotificationRemovalSubscription = DeviceNotification
            .OnDeviceRemoved()
            .Subscribe(UsbDeviceRemoved);
    }

    private void UsbDeviceRemoved(DeviceInterfaceChangeInfo device)
    {
        if (device.Device.FriendlyDeviceName.Contains("Logitech BRIO"))
        {
            ChangeMonitor(17);
        }
    }

    private void UsbDeviceAdded(DeviceInterfaceChangeInfo device)
    {
        if (device.Device.FriendlyDeviceName.Contains("Logitech BRIO"))
        {
            ChangeMonitor(15);
        }
    }

    private void ChangeMonitor(int monitorId)
    {
        lock (_lockObject)
        {
            CancellationTokenSource?.Cancel();
            CurrentChangeMonitorRequest.Wait();
            CancellationTokenSource = new CancellationTokenSource();
            CurrentChangeMonitorRequest = Task.Run(() =>
            {
                for (int i = 0; i < 12; i++)
                {
                    if (CancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    Process.Start(@"C:\ControlMyMonitor\ControlMyMonitor.exe", $@"/SetValue ""\\.\DISPLAY1\Monitor0"" 60 {monitorId}");
                    CancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
                }
            });
        }
    }
}
