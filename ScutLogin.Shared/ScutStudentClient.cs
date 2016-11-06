using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ScutLogin.Shared
{
    class ScutStudentClient
    {
        HttpClient httpClient;
        public ScutStudentClient()
        {
            var handler = new HttpClientHandler() { AllowAutoRedirect = false };
            httpClient = new HttpClient(handler);
        }
        private const string authenticationUrlTemplate = "https://s.scut.edu.cn:801/eportal/?c=ACSetting&a={0}&wlanuserip={1}&wlanacip={2}&wlanacname=&redirect=&session=&vlanid=scut-student&port=&iTermType=1&protocol=https:";
        private const string errorCodeUrl = "https://s.scut.edu.cn/errcode";
        private const string testIfNeedLoginUrl = "http://www.baidu.com/";
        private const string testIfLoggedInUrl = "https://s.scut.edu.cn:801/eportal/?c=ACSetting&a=Login";

        public ScutStudentClientStatus Status { get; set; } = ScutStudentClientStatus.Unknown;

        public string WlanAcIp { get; set; }
        public string UserIp { get; set; }
        public string LoginUrl
        {
            get
            {
                if (Status != ScutStudentClientStatus.NeedLogin)
                {
                    throw new InvalidOperationException($"只能在{nameof(ScutStudentClientStatus.NeedLogin)}状态时获取登录地址");
                }
                return string.Format(authenticationUrlTemplate, "Login", UserIp, WlanAcIp);
            }
        }

        public string LogoutUrl
        {
            get
            {
                if (Status != ScutStudentClientStatus.LoggedIn)
                {
                    throw new InvalidOperationException($"只能在{nameof(ScutStudentClientStatus.LoggedIn)}状态时获取注销地址");
                }
                return string.Format(authenticationUrlTemplate, "Logout", UserIp, WlanAcIp);
            }
        }

        public async Task<ScutStudentClientStatus> TryGetStatus()
        {
            await testInternet();
            if (Status==ScutStudentClientStatus.InternetConnected)
            {
                try
                {
                    using (var response = await httpClient.GetAsync(testIfLoggedInUrl))
                    {
                        if (response.StatusCode==HttpStatusCode.Redirect)
                        {
                            Status = ScutStudentClientStatus.LoggedIn;
                            var query = getQueryFromUrl(response.Headers.Location);
                            UserIp = query["wlanuserip"];
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return Status;
        }

        private async Task testInternet()
        {
            try
            {
                Uri redirectUrl;
                using (var response = await httpClient.GetAsync(testIfNeedLoginUrl))
                {
                    if (response.StatusCode == HttpStatusCode.Redirect)
                    {
                        redirectUrl = response.Headers.Location;
                    }
                    else if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Status = ScutStudentClientStatus.InternetConnected;
                        return;
                    }
                    else
                    {
                        Status = ScutStudentClientStatus.Unknown;
                        return;
                    }
                }

                if (redirectUrl.Host != "s.scut.edu.cn")
                {
                    Status = ScutStudentClientStatus.Unknown;
                    return;
                }
                var query = getQueryFromUrl(redirectUrl);
                UserIp = query["source-address"];
                WlanAcIp = query["nasip"];
                Status = ScutStudentClientStatus.NeedLogin;
                return;
            }
            catch (Exception)
            {
                Status = ScutStudentClientStatus.NoConnection;
                return;
            }
        }

        private static Dictionary<string, string> getQueryFromUrl(Uri redirectUrl)
        {
            return redirectUrl.Query.Substring(1).Split('&')
                                            .Select(s => s.Split('='))
                                            .ToDictionary(s => s[0], s => s.Length > 1 ? s[1] : string.Empty);
        }

        public async Task Login(string userName, string password)
        {
            var loginPostContent = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "0MKKey", "123456"},
                    { "DDDDD", userName},
                    { "para", "00" },
                    { "R1", "0" },
                    { "R2", "" },
                    { "R6", "0" },
                    { "upass", password }
                });

            Uri redirect;
            using (var response = await httpClient.PostAsync(LoginUrl, loginPostContent))
            {
                if (response.StatusCode != HttpStatusCode.Redirect)
                {
                    throw new ApplicationException($"登录请求返回意外的状态代码{response.StatusCode}");
                }

                redirect = response.Headers.Location;
            }
            if (redirect.AbsolutePath == "/3.htm")//登录成功
            {
                Status = ScutStudentClientStatus.LoggedIn;
                return;
            }

            if (redirect.AbsolutePath == "/2.htm")//登录错误
            {
                string http;
                using (var response = await httpClient.GetAsync(errorCodeUrl))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new ApplicationException($"获取错误信息时返回意外的状态代码{response.StatusCode}");
                    }
                    http = await response.Content.ReadAsStringAsync();
                }
                throw new ScutStudentLoginException(ScutStudentLoginErrorHelper.GetErrorFromHttp(http));
            }
        }

        public async Task Logout()
        {
            await httpClient.GetAsync(LogoutUrl);
            Status = ScutStudentClientStatus.NeedLogin;
        }
    }

    enum ScutStudentClientStatus { Unknown, NoConnection, InternetConnected, NeedLogin, LoggedIn }
    public enum ScutStudentLoginError { Unknown, AccountNotExist, UserNameOrPasswordIncorrect, TimePeriodProhibited, UsersMoreThanLimit }


    [Serializable]
    public class ScutStudentLoginException : ApplicationException
    {
        public ScutStudentLoginException() { }
        public ScutStudentLoginException(ScutStudentLoginError error) : base(ScutStudentLoginErrorHelper.GetErrorHelpText(error)) { }
        public ScutStudentLoginException(ScutStudentLoginError error, Exception inner) : base(ScutStudentLoginErrorHelper.GetErrorHelpText(error), inner) { }
        protected ScutStudentLoginException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}