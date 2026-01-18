#if ANDROID
using Android;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using LinkedLamp.Permissions;
using LinkedLamp.Services;
using Microsoft.Maui.ApplicationModel;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
#endif

namespace LinkedLamp.Pages;

#if ANDROID
public partial class BleScanPage : ContentPage
{
    public class DeviceRow : IEquatable<DeviceRow>
    {
        public string Name { get; set; } = "";
        public IDevice Device { get; set; } = default!;

        public bool Equals(DeviceRow? other)
        {
            return other != null && Device.Name == other.Device.Name;
        }
    }
    private readonly EspBleProvisioningService _prov;
    private ObservableCollection<DeviceRow> _devices = new();
    private WifiManager? _wifiManager;
    private WifiStateReceiver? _wifiReceiver;
    public BleScanPage(EspBleProvisioningService prov)
    {
        InitializeComponent();
        _wifiManager = null;
        _prov = prov;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DevicesView.ItemsSource = _devices;
        var context = Android.App.Application.Context;
        _wifiManager = (WifiManager?)context.GetSystemService(Context.WifiService);
        if (_wifiManager == null)
        {
            MainLabel.Text = "Problem with WiF.i... Please restart the application.";
            return;
        }
        _wifiReceiver = new WifiStateReceiver();
        var filter = new IntentFilter(WifiManager.WifiStateChangedAction);
        Android.App.Application.Context.RegisterReceiver(_wifiReceiver, filter);

        if (!_wifiManager.IsWifiEnabled)
        {
            OnWifiDisabled();
        }
        else
        {
            OnWifiEnabled();
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_wifiReceiver != null)
        {
            _wifiReceiver.WifiStateChanged -= OnWifiStateChanged;
            Android.App.Application.Context.UnregisterReceiver(_wifiReceiver);
            _wifiReceiver = null;
            _wifiManager = null;
        }
    }
    private void OnWifiStateChanged(bool enabled)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (enabled)
            {
                OnWifiEnabled();
            }
            else
            {
                OnWifiDisabled();
            }
        });
    }
    private void OnWifiDisabled()
    {
        _wifiReceiver?.WifiStateChanged += OnWifiStateChanged;
        MainLabel.Text = "Please activate the Wifi.";
        OpenWifi.IsVisible = true;
        SsidPicker.IsVisible = false;
    }
    public async void OnWifiEnabled()
    {
        _wifiReceiver?.WifiStateChanged -= OnWifiStateChanged;
        MainLabel.Text = "Select the wifi network you want your LinkedLamp to connect to.";
        OpenWifi.IsVisible = false;
        SsidPicker.IsVisible = true;
        SsidPicker.IsEnabled = false;
        var ssids = await GetNearbySsidsAsync();
        SsidPicker.ItemsSource = ssids;
        SsidPicker.IsEnabled = true;
    }

    public void OnSsidSelected(object? sender, EventArgs e)
    {
        if (!PassEntry.IsVisible)
        {
            MainLabel.Text = "Enter the wifi password if the wifi is protected and start scanning to detect and setup your LinkedLamp.";
            PassEntry.IsVisible = true;
            SetupArduino.IsVisible = true;
        }
    }
    private async void EnsureBluetoothEnabled(object sender, EventArgs e)
    {
        MainLabel.Text = "Bluetooth activation.";
        PassEntry.IsVisible = false;
        SsidPicker.IsVisible = false;
        SetupArduino.IsEnabled = false;
        bool btEnabled = CrossBluetoothLE.Current.State == BluetoothState.On;
        if (!btEnabled)
        {
            btEnabled = await LinkedLamp.Platforms.Android.BluetoothEnabler.RequestEnableAsync();
        }
        if (!btEnabled)
        {
            MainLabel.Text = "Bluetooth is required to setup your LinkedLamp. Please enable Bluetooth and retry.";
            SetupArduino.IsEnabled = true;
        }
        else
        {
            StartProvisioning();
        }
    }
    public async void StartProvisioning()
    {
        MainLabel.Text = "Detecting LinkedLamp devices around.";
        SecondaryLabel.IsVisible = false;
        SetupArduino.Text = "Scanning...";
        _devices.Clear();
        HashSet<IDevice> devices = await _prov.Scan(4);
        foreach (var device in devices)
        {
            _devices.Add(new DeviceRow() { Name = device.Name, Device = device });
        }
        SecondaryLabel.Text = _devices.Count == 0 ? "No device detected. Ensure your LinkedLamp is turned on and near of your android device." : "Select a LinkedLamp device to setup it";
        SecondaryLabel.IsVisible = true;
        DevicesView.IsVisible = true;
        SetupArduino.Text = "Scan";
        SetupArduino.IsEnabled = true;
    }
    public async void OnDeviceSelected(object? sender, SelectionChangedEventArgs e)
    {
        SetupArduino.IsEnabled = false;
        DevicesView.IsEnabled = false;
        try
        {
            await _prov.ConnectAndSetup(
            ((DeviceRow)DevicesView.SelectedItem).Device,
            (string)SsidPicker.SelectedItem,
            PassEntry.Text ?? ""
        );
        }
        catch (Exception)
        {
            MainLabel.Text = "Process failed. You can retry.";
            SsidPicker.IsEnabled = true;
            PassEntry.IsEnabled = true;
            DevicesView.IsEnabled = true;
            return;
        }
        MainLabel.Text = "Process completed. You can now setup another LinkedLamp.";
        SetupArduino.IsEnabled = true;
        DevicesView.IsEnabled = true;
    }
    public void OpenWifiSettings(object sender, EventArgs e)
    {
        var intent = new Intent(Settings.ActionWifiSettings);
        intent.AddFlags(ActivityFlags.NewTask);
        Android.App.Application.Context.StartActivity(intent);
    }
    public async Task<List<string>> GetNearbySsidsAsync()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            await MauiPermissions.RequestAsync<MauiPermissions.NearbyWifiDevices>();
        else
            await MauiPermissions.RequestAsync<MauiPermissions.LocationWhenInUse>();

        var results = _wifiManager.ScanResults;
        if (results == null)
            return new();

        return results
            .Where(r => !string.IsNullOrWhiteSpace(r.Ssid))
            .GroupBy(r => r.Ssid)                 // un SSID peut apparaître plusieurs fois
            .Select(g => g.OrderByDescending(r => r.Level).First())
            .OrderByDescending(r => r.Level)      // RSSI fort en premier
            .Select(r => r.Ssid)
            .ToList();
    }

}
#endif
