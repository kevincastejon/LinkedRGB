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
#if ANDROID
    private const string DeviceNameFilter = "LinkedLamp_Caskev_";
    private readonly EspBleProvisioningService _prov;
    private readonly WifiSsidPage _wifiSsidPage;

    private CancellationTokenSource? _scanCts;

    public ScanPage(EspBleProvisioningService prov, WifiSsidPage wifiSsidPage)
    {
        InitializeComponent();
        _prov = prov;
        _wifiSsidPage = wifiSsidPage;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        PermissionStatus st = PermissionStatus.Unknown;
        try
        {
            st = await MauiPermissions.CheckStatusAsync<BluetoothScanPermission>();
        }
        catch (Exception)
        {
            ScanButton.Text = "Start Scan";
            ScanButton.IsEnabled = true;
            SecondaryLabel.Text = "Bluetooth permission error.";
            return;
        }
        if (st != PermissionStatus.Granted)
        {
            bool canAskPermission;
            if (!Preferences.Get("PermissionAsked", false))
            {
                canAskPermission = true;
            }
            else
            {
                try
                {
                    canAskPermission = MauiPermissions.ShouldShowRationale<BluetoothScanPermission>();
                }
                catch (Exception)
                {
                    ScanButton.Text = "Start Scan";
                    ScanButton.IsEnabled = true;
                    SecondaryLabel.Text = "Bluetooth permission error.";
                    return;
                }
            }
            if (canAskPermission)
            {
                ScanButton.IsVisible = true;
                SecondaryLabel.Text = $"";
            }
            else
            {
                ScanButton.Text = "Start Scan";
                ScanButton.IsEnabled = true;
                ScanButton.IsVisible = false;
                SecondaryLabel.Text = $"Please activate permission in the app settings. {st}";
            }
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _prov.StopScanAsync();
    }
    private async void OnScanButtonClicked(object sender, EventArgs e)
    {
        //PermissionStatus st = PermissionStatus.Unknown;
        //try
        //{
        //    st = await MauiPermissions.CheckStatusAsync<BluetoothScanPermission>();
        //}
        //catch (Exception)
        //{
        //    ScanButton.Text = "Start Scan";
        //    ScanButton.IsEnabled = true;
        //    SecondaryLabel.Text = "Bluetooth permission error.";
        //    return;
        //}
        //if (st != PermissionStatus.Granted)
        //{
        //    bool canAskPermission;
        //    if (!Preferences.Get("PermissionAsked", false))
        //    {
        //        canAskPermission = true;
        //    }
        //    else
        //    {
        //        try
        //        {
        //            canAskPermission = MauiPermissions.ShouldShowRationale<BluetoothScanPermission>();
        //        }
        //        catch (Exception)
        //        {
        //            ScanButton.Text = "Start Scan";
        //            ScanButton.IsEnabled = true;
        //            SecondaryLabel.Text = "Bluetooth permission error.";
        //            return;
        //        }
        //    }
        //    Debug.WriteLine(">>> " + canAskPermission);
        //    if (canAskPermission)
        //    {
        //        Preferences.Set("PermissionAsked", true);
        //        try
        //        {
        //            st = await MauiPermissions.RequestAsync<BluetoothScanPermission>();
        //        }
        //        catch (Exception)
        //        {
        //            ScanButton.Text = "Start Scan";
        //            ScanButton.IsEnabled = true;
        //            SecondaryLabel.Text = "Bluetooth permission error.";
        //            return;
        //        }
        //        if (st != PermissionStatus.Granted)
        //        {
        //            ScanButton.Text = "Start Scan";
        //            ScanButton.IsEnabled = true;
        //            SecondaryLabel.Text = $"Bluetooth permission is mandatory. {st}";
        //            bool canAskAgain;
        //            try
        //            {
        //                canAskAgain = MauiPermissions.ShouldShowRationale<BluetoothScanPermission>();
        //            }
        //            catch (Exception)
        //            {
        //                ScanButton.Text = "Start Scan";
        //                ScanButton.IsEnabled = true;
        //                SecondaryLabel.Text = "Bluetooth permission error.";
        //                return;
        //            }
        //            if (!canAskAgain)
        //            {
        //                ScanButton.Text = "Start Scan";
        //                ScanButton.IsEnabled = true;
        //                ScanButton.IsVisible = false;
        //                AppSettingsButton.IsVisible = true;
        //                SecondaryLabel.Text = $"Please activate permission in the app settings. {st}";
        //            }
        //            return;
        //        }
        //    }
        //    else
        //    {
        //        ScanButton.Text = "Start Scan";
        //        ScanButton.IsEnabled = true;
        //        ScanButton.IsVisible = false;
        //        AppSettingsButton.IsVisible = true;
        //        SecondaryLabel.Text = $"Please activate permission in the app settings. {st}";
        //        return;
        //    }
        //}
        //try
        //{
        //    bool btEnabled = CrossBluetoothLE.Current.State == BluetoothState.On;
        //    if (!btEnabled)
        //    {
        //        btEnabled = await Platforms.Android.BluetoothEnabler.RequestEnableAsync();
        //    }
        //    if (!btEnabled)
        //    {
        //        ScanButton.Text = "Start Scan";
        //        ScanButton.IsEnabled = true;
        //        SecondaryLabel.Text = "Please activate Bluetooth.";
        //        return;
        //    }
        //}
        //catch (Exception)
        //{
        //    ScanButton.Text = "Start Scan";
        //    ScanButton.IsEnabled = true;
        //    SecondaryLabel.Text = "Bluetooth activation error.";
        //    throw;
        //}
        //try
        //{
        //    ScanButton.Text = "Scanning...";
        //    ScanButton.IsEnabled = false;
        //    IDevice? device = await _prov.ScanAndFindFirstDeviceAsync(DeviceNameFilter);
        //    if (device == null)
        //    {
        //        ScanButton.Text = "Start Scan";
        //        ScanButton.IsEnabled = true;
        //        SecondaryLabel.Text = "No device found.";
        //    }
        //    else
        //    {
        //        SecondaryLabel.Text = $"Device found {device.Name} {device.Rssi}db.";
        //    }
        //}
        //catch (Exception)
        //{
        //    ScanButton.Text = "Start Scan";
        //    ScanButton.IsEnabled = true;
        //    SecondaryLabel.Text = "Error during scan.";
        //    throw;
        //}
    }
#endif
}
