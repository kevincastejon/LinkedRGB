using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Diagnostics;
using System.Text;

namespace LinkedLamp.Services;

public sealed class EspBleProvisioningService
{
    private static readonly Guid SERVICE_UUID = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
    private static readonly Guid WIFIPROV_UUID = Guid.Parse("6E400002-B5A3-F393-E0A9-E50E24DCCA9E");
    private static readonly Guid WIFICONF_UUID = Guid.Parse("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");

    private readonly IAdapter _adapter;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CancellationTokenSource? _activeOpCts;
    private List<IDevice> _foundDevices = new();
    private IDevice? _connectedDevice;
    private ICharacteristic? _wifiProvChar;
    private ICharacteristic? _wifiConfChar;

    public EspBleProvisioningService()
    {
        _adapter = CrossBluetoothLE.Current.Adapter;
    }

    public async Task<IDevice?> ScanAndFindFirstDeviceAsync(string? deviceNameStartsWithFilter = null, CancellationToken cancellationToken = default)
    {
        if (_adapter.IsScanning)
        {
            throw new InvalidOperationException("Adapter is already scanning.");
        }
        _foundDevices.Clear();
        _adapter.DeviceDiscovered += OnDeviceDiscovered;
        try
        {
            await _adapter.StartScanningForDevicesAsync(new ScanFilterOptions { ServiceUuids = [SERVICE_UUID] }, (IDevice device) => string.IsNullOrEmpty(deviceNameStartsWithFilter) || device.Name.StartsWith(deviceNameStartsWithFilter), false, cancellationToken);
        }
        finally
        {
            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
        }
        if (_foundDevices.Count == 0)
        {
            return null;
        }
        return _foundDevices.OrderByDescending(d => d.Rssi).FirstOrDefault();
    }

    private void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        _foundDevices.Add(e.Device);
    }

    public async Task ProvisionAsync(IDevice device, string groupName, string ssid, string pass, CancellationToken cancellationToken)
    {
        return;
        //await _gate.WaitAsync(cancellationToken);
        //try
        //{
        //    await CancelAndDisconnectInternalAsync().ConfigureAwait(false);

        //    _activeOpCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        //    var token = _activeOpCts.Token;

        //    var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        //    _discoHandler = (s, e) =>
        //    {
        //        if (_connectedDevice != null && e.Device.Id == _connectedDevice.Id)
        //            tcs.TrySetException(new InvalidOperationException("BLE device disconnected."));
        //    };

        //    _adapter.DeviceDisconnected += _discoHandler;

        //    try
        //    {
        //        await _adapter.ConnectToDeviceAsync(device, default, token).ConfigureAwait(false);

        //        _connectedDevice = device;

        //        var services = await device.GetServicesAsync(token).ConfigureAwait(false);
        //        var provService = services.FirstOrDefault(s => s.Id == SERVICE_UUID)
        //            ?? throw new InvalidOperationException("Service not found.");

        //        _wifiProvChar = await provService.GetCharacteristicAsync(WIFIPROV_UUID, token).ConfigureAwait(false)
        //            ?? throw new InvalidOperationException("Characteristic not found.");

        //        _wifiConfChar = await provService.GetCharacteristicAsync(WIFICONF_UUID, token).ConfigureAwait(false)
        //            ?? throw new InvalidOperationException("Characteristic not found.");

        //        _confHandler = (sender, e) =>
        //        {
        //            try
        //            {
        //                var val = e.Characteristic.Value;
        //                if (val == null || val.Length == 0)
        //                {
        //                    tcs.TrySetException(new InvalidOperationException("Empty BLE response"));
        //                    return;
        //                }

        //                var ok = val[0] == 1;
        //                tcs.TrySetResult(ok);
        //            }
        //            catch (Exception ex)
        //            {
        //                tcs.TrySetException(ex);
        //            }
        //        };

        //        _wifiConfChar.ValueUpdated += _confHandler;
        //        await _wifiConfChar.StartUpdatesAsync(token).ConfigureAwait(false);

        //        var payload = SerializeCredentials(groupName.Trim(), ssid.Trim(), pass.Trim());
        //        await _wifiProvChar.WriteAsync(payload, token).ConfigureAwait(false);

        //        var okResult = await tcs.Task.WaitAsync(token).ConfigureAwait(false);
        //        if (!okResult)
        //            throw new InvalidOperationException("ESP Wifi connection failed");
        //    }
        //    finally
        //    {
        //        await StopUpdatesAndUnsubscribeAsync().ConfigureAwait(false);
        //        await DisconnectInternalAsync().ConfigureAwait(false);

        //        if (_discoHandler != null)
        //        {
        //            try { _adapter.DeviceDisconnected -= _discoHandler; } catch { }
        //            _discoHandler = null;
        //        }

        //        ClearActiveOpCts();
        //    }
        //}
        //finally
        //{
        //    _gate.Release();
        //}
    }
    public async Task StopScanAsync()
    {
        await _adapter.StopScanningForDevicesAsync();
    }
    public async Task CancelAndDisconnectAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await CancelAndDisconnectInternalAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task CancelAndDisconnectInternalAsync()
    {
        //try { _activeOpCts?.Cancel(); } catch { }
        //await StopUpdatesAndUnsubscribeAsync().ConfigureAwait(false);
        //await DisconnectInternalAsync().ConfigureAwait(false);

        //if (_adapter.IsScanning)
        //{
        //    try { await _adapter.StopScanningForDevicesAsync().ConfigureAwait(false); }
        //    catch { }
        //}

        //if (_discoHandler != null)
        //{
        //    try { _adapter.DeviceDisconnected -= _discoHandler; } catch { }
        //    _discoHandler = null;
        //}

        //ClearActiveOpCts();
    }

    private async Task StopUpdatesAndUnsubscribeAsync()
    {
        //if (_wifiConfChar != null && _confHandler != null)
        //{
        //    try { _wifiConfChar.ValueUpdated -= _confHandler; } catch { }
        //    _confHandler = null;

        //    try { await _wifiConfChar.StopUpdatesAsync().ConfigureAwait(false); }
        //    catch { }
        //}
    }

    private async Task DisconnectInternalAsync()
    {
        var dev = _connectedDevice;
        if (dev == null)
        {
            _wifiProvChar = null;
            _wifiConfChar = null;
            return;
        }

        try
        {
            Debug.WriteLine(">>> DisconnectDeviceAsync.");
            await _adapter.DisconnectDeviceAsync(dev).ConfigureAwait(false);
        }
        catch { }
        finally
        {
            _connectedDevice = null;
            _wifiProvChar = null;
            _wifiConfChar = null;
        }
    }

    private void ClearActiveOpCts()
    {
        try { _activeOpCts?.Dispose(); } catch { }
        _activeOpCts = null;
    }

    private static byte[] SerializeCredentials(string groupName, string ssid, string pass)
    {
        var data = new List<byte>();

        var groupNameBytes = Encoding.UTF8.GetBytes(groupName);
        var ssidBytes = Encoding.UTF8.GetBytes(ssid);
        var passBytes = Encoding.UTF8.GetBytes(pass);

        data.Add((byte)groupNameBytes.Length);
        data.AddRange(groupNameBytes);

        data.Add((byte)ssidBytes.Length);
        data.AddRange(ssidBytes);

        data.Add((byte)passBytes.Length);
        data.AddRange(passBytes);

        return data.ToArray();
    }
}
