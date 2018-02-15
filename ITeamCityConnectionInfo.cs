using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.Extensions.TeamCity
{
    internal interface ITeamCityConnectionInfo
    {
        string ServerUrl { get; }
        string UserName { get; }
        string Password { get; }
    }

    internal static class ITeamCityConnectionInfoExtensions
    {
        public static string GetApiUrl(this ITeamCityConnectionInfo connectionInfo)
        {
            return $"{connectionInfo.ServerUrl.TrimEnd('/')}/{(string.IsNullOrEmpty(connectionInfo.UserName) ? "guestAuth" : "httpAuth")}/";
        }
    }
}
