using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScutLogin.Shared
{
    static class ScutStudentLoginErrorHelper
    {
        static public ScutStudentLoginError GetErrorFromHttp(string http)
        {
            var regx82 = "userid error1";
            if (http.IndexOf(regx82) > 0)
            {
                return ScutStudentLoginError.AccountNotExist;
            }

            var regx83 = "userid error3";
            var regx83_0 = "userid error2";
            if (http.IndexOf(regx83) > 0 || http.IndexOf(regx83_0) > 0)
            {
                return ScutStudentLoginError.UserNameOrPasswordIncorrect;
            }

            var regx16 = "Rpost=2;ret='Authentication Fail ErrCode=16'";
            if (http.IndexOf(regx16) > 0)
            {
                return ScutStudentLoginError.TimePeriodProhibited;
            }

            var regx55 = "In use";
            if (http.IndexOf(regx55) > 0)
            {
                return ScutStudentLoginError.UsersMoreThanLimit;
            }

            return ScutStudentLoginError.Unknown;
        }

        static public string GetErrorHelpText(ScutStudentLoginError error)
        {
            switch (error)
            {
                case ScutStudentLoginError.AccountNotExist:
                    return "账户不存在";
                case ScutStudentLoginError.UserNameOrPasswordIncorrect:
                    return "用户名或密码不正确";
                case ScutStudentLoginError.TimePeriodProhibited:
                    return "此时段禁止登录";
                case ScutStudentLoginError.UsersMoreThanLimit:
                    return "用户数量超过限制";
                case ScutStudentLoginError.Unknown:
                default:
                    return "未知错误";
            }
        }

        static public string GetStatusHelpText(ScutStudentClientStatus status)
        {
            switch (status)
            {
                case ScutStudentClientStatus.NeedLogin:
                    return "未登录";
                case ScutStudentClientStatus.LoggedIn:
                    return "已登录";
                case ScutStudentClientStatus.InternetConnected:
                    return "网络已连接";
                case ScutStudentClientStatus.NoConnection:
                    return "网络未连接";
                case ScutStudentClientStatus.Unknown:
                default:
                    return "未知状态";
            }
        }
    }
}
