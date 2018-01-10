﻿using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using System;
using Android.Views;
using Android.Net.Wifi;
using System.Threading.Tasks;

namespace ScutLogin.Droid
{
    [Activity(Label = "宿舍WLAN", MainLauncher = true, Icon = "@drawable/logo", LaunchMode = Android.Content.PM.LaunchMode.SingleTop)]
    public class MainActivity : Activity
    {
        public const string usernamePrefKey = "UserName",
            passwordPrefKey = "Password",
            ifSavePasswordPrefKey = "IfSavePassword",
            ifAutoLoginPrefKey = "IfAutoLogin",
            wlanAcIpPrefKey = "WlanAcIp";

        public const string PrefName = "LoginInfo";
        ISharedPreferences sharedPref;
        Button loginButton;
        Button logoutButton;
        EditText userNameEdit;
        EditText passwordEdit;
        CheckBox autoLoginCheckBox;
        CheckBox savePasswordCheckBox;
        TextView statusText;
        TextView errorText;

        Shared.ScutStudentClient client = new Shared.ScutStudentClient();
        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Initialize();

            sharedPref = this.GetSharedPreferences(PrefName, FileCreationMode.Private);


            userNameEdit.Text = sharedPref.GetString(usernamePrefKey, Shared.DefaultLoginInfo.UserName);
            passwordEdit.Text = sharedPref.GetString(passwordPrefKey, Shared.DefaultLoginInfo.Password);
            savePasswordCheckBox.Checked = sharedPref.GetBoolean(ifSavePasswordPrefKey, false);
            autoLoginCheckBox.Checked = sharedPref.GetBoolean(ifAutoLoginPrefKey, false);

            await GetStatus();

            if (client.Status == Shared.ScutStudentClientStatus.NeedLogin && autoLoginCheckBox.Checked)
            {
                LoginButton_Click(this, new EventArgs());
            }
        }

        private async Task GetStatus()
        {
            WifiManager wifi = (WifiManager)GetSystemService(Context.WifiService);
            if (wifi.ConnectionInfo.SSID.Contains(Shared.ScutStudentClient.wifiSsid))
            {
                errorText.Text = string.Empty;
                await client.TryGetStatus();
            }
            else
            {
                client.ResetStatus();
                errorText.Text = $"尚未连接{Shared.ScutStudentClient.wifiSsid}";
            }
            SyncStatus();
        }

        protected override void OnStop()
        {
            base.OnStop();
            UpdatePref();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MainMenu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.about:
                    OpenAbout();
                    return true;
                case Resource.Id.refresh:
                    RefreshStatus();
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        private async void RefreshStatus()
        {
            statusText.Text = "正在检测";
            loginButton.Enabled = false;
            logoutButton.Enabled = false;
            await GetStatus();
        }

        private void OpenAbout()
        {
            Intent intent = new Intent(this, typeof(AboutActivity));
            StartActivity(intent);
        }

        private void Initialize()
        {
            SetContentView(Resource.Layout.Main);
            this.Title = "scut-student WLAN登录";

            statusText = FindViewById<TextView>(Resource.Id.statusText);
            errorText = FindViewById<TextView>(Resource.Id.errorText);
            loginButton = FindViewById<Button>(Resource.Id.loginButton);
            logoutButton = FindViewById<Button>(Resource.Id.logoutButton);
            userNameEdit = FindViewById<EditText>(Resource.Id.userName);
            passwordEdit = FindViewById<EditText>(Resource.Id.password);
            autoLoginCheckBox = FindViewById<CheckBox>(Resource.Id.autoLogin);
            savePasswordCheckBox = FindViewById<CheckBox>(Resource.Id.savePassword);

            loginButton.Click += LoginButton_Click;
            logoutButton.Click += LogoutButton_Click;
            autoLoginCheckBox.CheckedChange += AutoLoginCheckBox_CheckedChange;
            savePasswordCheckBox.CheckedChange += SavePasswordCheckBox_CheckedChange;
        }

        private void SavePasswordCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (!e.IsChecked)
            {
                autoLoginCheckBox.Checked = false;
            }
        }

        private void AutoLoginCheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                savePasswordCheckBox.Checked = true;
            }
        }

        private async void LogoutButton_Click(object sender, System.EventArgs e)
        {
            statusText.Text = "正在注销";
            errorText.Text = string.Empty;
            logoutButton.Enabled = false;

            try
            {
                if (string.IsNullOrEmpty(client.WlanAcIp))
                {
                    client.WlanAcIp = sharedPref.GetString(wlanAcIpPrefKey, Shared.DefaultLoginInfo.WlanAcIp);

                    if (string.IsNullOrEmpty(client.WlanAcIp))
                    {
                        throw new InvalidOperationException("需要登录信息以注销，请至少成功登录一次");
                    }
                }
                await client.Logout();
            }
            catch (Exception ex)
            {
                errorText.Text = ex.Message;
            }
            SyncStatus();

        }

        private void SyncStatus()
        {
            var status = client.Status;
            statusText.Text = Shared.ScutStudentLoginErrorHelper.GetStatusHelpText(status);
            if (status == Shared.ScutStudentClientStatus.NeedLogin)
            {
                loginButton.Enabled = true;
                logoutButton.Visibility = ViewStates.Gone;
                loginButton.Visibility = ViewStates.Visible;
            }
            else if (status == Shared.ScutStudentClientStatus.LoggedIn)
            {
                logoutButton.Enabled = true;
                loginButton.Visibility = ViewStates.Gone;
                logoutButton.Visibility = ViewStates.Visible;
            }
            else
            {
                loginButton.Enabled = false;
                logoutButton.Visibility = ViewStates.Gone;
                loginButton.Visibility = ViewStates.Visible;
            }
        }

        private async void LoginButton_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(userNameEdit.Text))
            {
                Toast.MakeText(this, "用户名不能为空", ToastLength.Short).Show();
                return;
            }
            if (string.IsNullOrEmpty(passwordEdit.Text))
            {
                Toast.MakeText(this, "密码不能为空", ToastLength.Short).Show();
                return;
            }

            statusText.Text = "正在登录";
            errorText.Text = string.Empty;
            loginButton.Enabled = false;
            try
            {
                await client.Login(userNameEdit.Text, passwordEdit.Text);
                sharedPref.Edit().PutString(wlanAcIpPrefKey, client.WlanAcIp).Apply();
            }
            catch (Exception ex)
            {
                errorText.Text = ex.Message;
            }
            SyncStatus();
        }

        private void UpdatePref()
        {
            var editor = sharedPref.Edit();
            editor.PutString(usernamePrefKey, userNameEdit.Text);
            editor.PutString(passwordPrefKey, savePasswordCheckBox.Checked ? passwordEdit.Text : string.Empty);
            editor.PutBoolean(ifSavePasswordPrefKey, savePasswordCheckBox.Checked);
            editor.PutBoolean(ifAutoLoginPrefKey, autoLoginCheckBox.Checked);
            editor.Apply();
        }
    }
}

