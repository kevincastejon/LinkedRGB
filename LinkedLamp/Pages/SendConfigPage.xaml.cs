#if ANDROID
using Android.Content.Res;
using LinkedLamp.Models;
using Plugin.BLE.Abstractions.Contracts;
#endif

namespace LinkedLamp.Pages;

public partial class SendConfigPage : ContentPage
{
#if ANDROID
    private readonly LinkedLamp.Services.EspBleProvisioningService _prov;
    private ProvisioningContext? _ctx;
    private IDevice? _device;
    private bool _started;
    private CancellationTokenSource? _provCts;
    private readonly IServiceProvider _services;
    public SendConfigPage(LinkedLamp.Services.EspBleProvisioningService prov, IServiceProvider services)
    {
        InitializeComponent();
        _prov = prov;
        _services = services;
    }

    public void SetContext(ProvisioningContext ctx, IDevice device)
    {
        _ctx = ctx;
        _device = device;
        _started = false;
        MainLabel.Text = "Sending configuration...";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        BackWifiButton.IsVisible = false;
        BackHomeButton.IsVisible = false;
        if (_started)
            return;

        _started = true;
        _ = RunProvisioningAsync();
    }

    protected override void OnDisappearing()
    {
        try { _provCts?.Cancel(); } catch { }
        _provCts?.Dispose();
        _provCts = null;
        base.OnDisappearing();
    }

    private async void OnBackWifiClicked(object sender, EventArgs e)
    {
        try { _provCts?.Cancel(); } catch { }
        _provCts?.Dispose();
        _provCts = null;

        var homePage = _services.GetRequiredService<HomePage>();
        var wifiPage = _services.GetRequiredService<WifiSsidPage>();

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var window = this.Window ?? Application.Current!.Windows[0];
            window.Page = new NavigationPage(homePage);
            await window.Page.Navigation.PushAsync(wifiPage, false);
        });
    }

    private async void OnBackHomeClicked(object sender, EventArgs e)
    {
        try { _provCts?.Cancel(); } catch { }
        _provCts?.Dispose();
        _provCts = null;

        var homePage = _services.GetRequiredService<HomePage>();
        var wifiPage = _services.GetRequiredService<WifiSsidPage>();

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var window = this.Window ?? Application.Current!.Windows[0];
            window.Page = new NavigationPage(homePage);
        });
    }

    private async Task RunProvisioningAsync()
    {
        if (_ctx == null || _device == null)
        {
            MainLabel.Text = "Missing provisioning data.";
            return;
        }
        MainLabel.Text = "Sending configuration...";

        _provCts?.Cancel();
        _provCts?.Dispose();
        _provCts = new CancellationTokenSource();

        bool success;
        try
        {
            await _prov.ConnectAndSetup(
                _device,
                _ctx.GroupName,
                _ctx.Ssid,
                _ctx.Password,
                _provCts.Token);

            success = true;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            success = false;
        }
        BackWifiButton.IsVisible = !success;
        BackHomeButton.IsVisible = success;
        MainLabel.Text = success
            ? "Configuration sent successfully."
            : "Configuration failed. Please check WiFi credentials and try again.";
    }
#endif
}
