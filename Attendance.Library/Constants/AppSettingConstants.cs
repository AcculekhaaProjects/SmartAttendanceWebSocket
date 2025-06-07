using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.Library.Constants
{
    public static class AppSettingConstants
    {
        public const string jwtAuthenticationEnabled = "AuthConfig:jwtAuthenticationEnabled";
        public const string JwtSecretKey = "JwtConfig:SecretKey";
        public const string JwtExpiryTime = "JwtConfig:ExpiryTime";
        public const string DbDefaultConnectionString = "ConnectionStrings:DefaultConnectionString";
    }
}
