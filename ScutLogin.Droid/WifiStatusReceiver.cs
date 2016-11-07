using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Net.Wifi;
using Android.Net;

namespace ScutLogin.Droid
{
    [BroadcastReceiver]
    [IntentFilter(new[] { WifiManager.NetworkStateChangedAction })]
    public class WifiStatusReceiver : BroadcastReceiver
    {
        static bool working = false;
        const int failedNotificationId = 0;
        const int succeedNotificationId = 1;

        public override async void OnReceive(Context context, Intent intent)
        {
            var sharedPref = context.GetSharedPreferences(MainActivity.PrefName, FileCreationMode.Private);
            if (!sharedPref.GetBoolean(MainActivity.ifAutoLoginPrefKey, false))
            {
                return;
            }
            if (working)
            {
                return;
            }
            working = true;

            NetworkInfo info = (NetworkInfo)intent.GetParcelableExtra(WifiManager.ExtraNetworkInfo);
            System.Diagnostics.Debug.WriteLine($"{info.GetState()}/{info.GetDetailedState()}");
            if (info.IsConnected)
            {
                WifiInfo wifiInfo = (WifiInfo)intent.GetParcelableExtra(WifiManager.ExtraWifiInfo);
                if (wifiInfo != null && wifiInfo.SSID.Contains(Shared.ScutStudentClient.wifiSsid))
                {
                    Shared.ScutStudentClient client = new Shared.ScutStudentClient();
                    await client.TryGetStatus();
                    if (client.Status == Shared.ScutStudentClientStatus.NeedLogin)
                    {

                        string userName = sharedPref.GetString(MainActivity.usernamePrefKey, string.Empty);
                        string password = sharedPref.GetString(MainActivity.passwordPrefKey, string.Empty);
                        Notification.Builder builder = new Notification.Builder(context);
                        int notificationId;
                        try
                        {
                            await client.Login(userName, password);
                            sharedPref.Edit().PutString(MainActivity.wlanAcIpPrefKey, client.WlanAcIp).Apply();
                            builder.SetContentTitle("自动登录scut-student成功")
                                .SetContentText($"账户：{userName}")
                                .SetSmallIcon(Resource.Drawable.ic_wifi_lock_white_24dp);
                            notificationId = succeedNotificationId;
                        }
                        catch (Exception e)
                        {
                            builder.SetContentTitle("自动登录scut-student失败")
                                .SetContentText(e.Message)
                                .SetSmallIcon(Resource.Drawable.ic_perm_scan_wifi_white_24dp);
                            notificationId = failedNotificationId;
                        }

                        var resultIntent = new Intent(context, typeof(MainActivity));
                        TaskStackBuilder stackBuilder = TaskStackBuilder.Create(context);
                        stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(MainActivity)));
                        stackBuilder.AddNextIntent(resultIntent);
                        PendingIntent resultPendingIntent = stackBuilder.GetPendingIntent(0, PendingIntentFlags.UpdateCurrent);
                        builder.SetContentIntent(resultPendingIntent);
                        Notification notification = builder.Build();
                        NotificationManager notificationManager =
                            context.GetSystemService(Context.NotificationService) as NotificationManager;
                        notificationManager.Notify(notificationId, notification);

                    }
                }
            }
            working = false;
        }
    }
}