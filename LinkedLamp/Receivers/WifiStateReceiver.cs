#if ANDROID
using Android.Content;
using Android.Net.Wifi;
using System.Diagnostics;
#endif

#if ANDROID
public class WifiStateReceiver : BroadcastReceiver
{
    public event Action<bool>? WifiStateChanged;

    public override void OnReceive(Context context, Intent intent)
    {
        try
        {
            if (intent?.Action != WifiManager.WifiStateChangedAction)
            {
                return;
            }
            int stateInt = intent.GetIntExtra(WifiManager.ExtraWifiState, -1);
            Debug.WriteLine($"[WIFI_RX] OnReceive : {stateInt}");
            bool enabled = (stateInt == 3);

            WifiStateChanged?.Invoke(enabled);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WIFI_RX] OnReceive exception: {ex}");
        }
    }
}
#endif
