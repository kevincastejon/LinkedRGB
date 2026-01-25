#if ANDROID
using LinkedLamp.Permissions;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
#endif
using LinkedLamp.Models;
using System.Diagnostics;

namespace LinkedLamp.Pages;

public partial class ScanPage : ContentPage
{
    private ProvisioningContext? _ctx;
#if ANDROID
    private const string DeviceNameFilter = "LinkedLamp_Caskev_";
    private readonly LinkedLamp.Services.EspBleProvisioningService _prov;
    private readonly SendConfigPage _sendConfigPage;

    private CancellationTokenSource? _pageCts;
    private CancellationTokenSource? _scanCts;
    private Task? _scanTask;
    private readonly SemaphoreSlim _scanGate = new(1, 1);

    public ScanPage(LinkedLamp.Services.EspBleProvisioningService prov, SendConfigPage sendConfigPage)
    {
        InitializeComponent();
        _prov = prov;
        _sendConfigPage = sendConfigPage;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _pageCts?.Cancel();
        _pageCts?.Dispose();
        _pageCts = new CancellationTokenSource();

        _scanTask = ScanLoopAsync(_pageCts.Token);
    }

    protected override void OnDisappearing()
    {
        _pageCts?.Cancel();
        CancelScan();
        base.OnDisappearing();
    }

    private async Task ScanLoopAsync(CancellationToken pageToken)
    {
        try
        {
            while (!pageToken.IsCancellationRequested)
            {
                var found = await StartScanAsync(pageToken);
                if (found)
                    return;

                await Task.Delay(1000, pageToken);
                Debug.WriteLine(">>> Scan retrying.");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine(">>> Scan cancelled.");
        }
    }

    private async Task<bool> StartScanAsync(CancellationToken pageToken)
    {
        if (_ctx == null)
            return false;

        await _scanGate.WaitAsync(pageToken);
        try
        {
            CancelScan();

            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(pageToken);
            var token = _scanCts.Token;

            var st = await MauiPermissions.RequestAsync<BluetoothScanPermission>();
            if (st != PermissionStatus.Granted)
            {
                SecondaryLabel.Text = "Bluetooth is required to setup your LinkedLamp. Please enable Bluetooth.";
                return false;
            }

            var btEnabled = CrossBluetoothLE.Current.State == BluetoothState.On;
            if (!btEnabled)
                btEnabled = await LinkedLamp.Platforms.Android.BluetoothEnabler.RequestEnableAsync();

            if (!btEnabled)
            {
                SecondaryLabel.Text = "Bluetooth is required to setup your LinkedLamp. Please enable Bluetooth.";
                return false;
            }

            SecondaryLabel.Text = "Detecting...";
            var devices = await _prov.Scan(DeviceNameFilter, token);

            if (devices.Count == 0)
                return false;

            CancelScan();
            _sendConfigPage.SetContext(_ctx, devices.ElementAt(0));
            await Navigation.PushAsync(_sendConfigPage);
            Navigation.RemovePage(this);
            return true;
        }
        finally
        {
            _scanGate.Release();
        }
    }

    private void CancelScan()
    {
        Debug.WriteLine(">>> Cancel scan.");
        try
        {
            _scanCts?.Cancel();
        }
        catch { }
        finally
        {
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }
#endif

    public void SetContext(ProvisioningContext ctx)
    {
        _ctx = ctx;
        SecondaryLabel.Text = "";
    }
}
