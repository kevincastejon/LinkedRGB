using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text;

namespace LinkedLamp.Services;

public class EspBleProvisioningService
{
    private static readonly Guid SERVICE_UUID = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
    private static readonly Guid WIFIPROV_UUID = Guid.Parse("6E400002-B5A3-F393-E0A9-E50E24DCCA9E");

    private readonly IAdapter _adapter;
    private IDevice? _connectedDevice;
    private ICharacteristic? _wifiProvChar;
    private string _ssid = "";
    private string _pass = "";
    public EspBleProvisioningService()
    {
        _adapter = CrossBluetoothLE.Current.Adapter;
    }
    public async Task<HashSet<IDevice>> Scan(int durationMs)
    {
        HashSet<IDevice> foundDevices = [];
        void Handler(object? sender, DeviceEventArgs e)
        {
            var name = e.Device.Name;
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!name.StartsWith("LinkedLamp_Caskev_")) return;

            lock (foundDevices)
            {
                if (!foundDevices.Contains(e.Device))
                    foundDevices.Add(e.Device);
            }
        }
        _adapter.DeviceDiscovered += Handler;
        try
        {
            using var cts = new CancellationTokenSource(durationMs);
            await _adapter.StartScanningForDevicesAsync(new Plugin.BLE.Abstractions.ScanFilterOptions() { ServiceUuids = new Guid[] { SERVICE_UUID } });
        }
        catch (OperationCanceledException)
        {
            // expected when duration expires
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _adapter.DeviceDiscovered -= Handler;
        }
        return foundDevices;
    }
    public async Task ConnectAndSetup(IDevice device, string ssid, string pass)
    {
        _ssid = ssid;
        _pass = pass;
        await _adapter.ConnectToDeviceAsync(device);
        var services = await device.GetServicesAsync();
        var provService = services.FirstOrDefault(s => s.Id == SERVICE_UUID) ?? throw new InvalidOperationException("Service not found.");
        ICharacteristic wifiProvChar = await provService.GetCharacteristicAsync(WIFIPROV_UUID) ?? throw new InvalidOperationException("Characteristic not found.");
        _connectedDevice = device;
        _wifiProvChar = wifiProvChar;
        await SendWifiCredentialsAndConnectAsync();
    }
    private async Task DisconnectAsync()
    {
        var dev = _connectedDevice;
        if (dev is null)
            return;

        await _adapter.DisconnectDeviceAsync(dev);
        _connectedDevice = null;
        _wifiProvChar = null;
    }
    private async Task SendWifiCredentialsAndConnectAsync()
    {
        _ssid = _ssid.Trim();
        if (string.IsNullOrWhiteSpace(_ssid))
            throw new ArgumentException("SSID empty.");

        await _wifiProvChar!.WriteAsync(SerializeCredentials("qzdqzd", _pass));
        await DisconnectAsync();
    }
    static private byte[] SerializeCredentials(string ssid, string pass)
    {
        List<byte> data = new();
        byte[] ssidBytes = Encoding.UTF8.GetBytes(ssid);
        byte[] passBytes = Encoding.UTF8.GetBytes(pass);
        data.Add((byte)ssidBytes.Length);
        data.AddRange(ssidBytes);
        data.Add((byte)passBytes.Length);
        data.AddRange(passBytes);
        return [.. data];
    }
}
