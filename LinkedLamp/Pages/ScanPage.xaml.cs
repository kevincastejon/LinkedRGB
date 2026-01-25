#if ANDROID
using LinkedLamp.Permissions;
using LinkedLamp.Services;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Diagnostics;
using System.Threading;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
#endif

using LinkedLamp.Models;

namespace LinkedLamp.Pages;

public partial class ScanPage : ContentPage
{
    private ProvisioningContext? _ctx;

#if ANDROID
    private const string DeviceNameFilter = "LinkedLamp_Caskev_";
    private readonly EspBleProvisioningService _prov;
    private readonly SendConfigPage _sendConfigPage;

    private CancellationTokenSource? _pageCts;
    private Task? _runTask;

    private int _lifecycleStamp;
    private bool _isActive;

    public ScanPage(EspBleProvisioningService prov, SendConfigPage sendConfigPage)
    {
        InitializeComponent();
        _prov = prov;
        _sendConfigPage = sendConfigPage;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _isActive = true;
        Interlocked.Increment(ref _lifecycleStamp);

        _pageCts?.Cancel();
        _pageCts?.Dispose();
        _pageCts = new CancellationTokenSource();

        SecondaryLabel.Text = "Detecting...";
        var stamp = _lifecycleStamp;
        _runTask = RunAsync(stamp, _pageCts.Token);
    }

    protected override void OnDisappearing()
    {
        _isActive = false;
        Interlocked.Increment(ref _lifecycleStamp);

        try { _pageCts?.Cancel(); } catch { }
        _pageCts?.Dispose();
        _pageCts = null;

        _ = Task.Run(() => _prov.CancelAndDisconnectAsync());

        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _isActive = false;
        Interlocked.Increment(ref _lifecycleStamp);

        try { _pageCts?.Cancel(); } catch { }
        _ = Task.Run(() => _prov.CancelAndDisconnectAsync());

        return base.OnBackButtonPressed();
    }

    private bool StillCurrent(int stamp, CancellationToken token)
    {
        return _isActive && stamp == Volatile.Read(ref _lifecycleStamp) && !token.IsCancellationRequested;
    }

    private async Task RunAsync(int stamp, CancellationToken token)
    {
        try
        {
            if (_ctx == null)
                return;

            var st = await MauiPermissions.RequestAsync<BluetoothScanPermission>();
            if (!StillCurrent(stamp, token))
                return;

            if (st != PermissionStatus.Granted)
            {
                SecondaryLabel.Text = "Bluetooth is required to setup your LinkedLamp. Please enable Bluetooth.";
                return;
            }

            var btEnabled = CrossBluetoothLE.Current.State == BluetoothState.On;
            if (!btEnabled)
                btEnabled = await LinkedLamp.Platforms.Android.BluetoothEnabler.RequestEnableAsync();

            if (!StillCurrent(stamp, token))
                return;

            if (!btEnabled)
            {
                SecondaryLabel.Text = "Bluetooth is required to setup your LinkedLamp. Please enable Bluetooth.";
                return;
            }

            while (StillCurrent(stamp, token))
            {
                SecondaryLabel.Text = "Detecting...";
                var devices = await _prov.ScanAsync(DeviceNameFilter, token);

                if (!StillCurrent(stamp, token))
                    return;

                if (devices.Count > 0)
                {
                    var device = devices.First();

                    if (!StillCurrent(stamp, token))
                        return;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (!StillCurrent(stamp, token))
                            return;

                        Debug.WriteLine($">>> Device found: {device.Name}");

                        _sendConfigPage.SetContext(_ctx, device);
                        await Navigation.PushAsync(_sendConfigPage);
                        Navigation.RemovePage(this);
                    });

                    return;
                }

                SecondaryLabel.Text = "No device found. Retrying...";
                await Task.Delay(1000, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            if (_isActive)
                SecondaryLabel.Text = "Scan failed. Please retry.";
        }
    }
#endif

    public void SetContext(ProvisioningContext ctx)
    {
        _ctx = ctx;
        SecondaryLabel.Text = "";
    }
}
