using System;
using System.Collections.Generic;
using System.Text;

namespace ScutLogin.Shared
{
    static class DefaultLoginInfo
    {
#if DEBUG
        public const string UserName = "";
        public const string Password = "";
        public const string WlanAcIp = "";
#else
        public const string UserName = "";
        public const string Password = "";
        public const string WlanAcIp = "";
#endif
    }
}
