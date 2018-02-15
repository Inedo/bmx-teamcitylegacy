using System;
using System.Linq;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.Extensions.TeamCity;
using Inedo.Serialization;
using Inedo.Web;

[assembly: ExtensionConfigurer(typeof(Inedo.BuildMasterExtensions.TeamCity.TeamCityConfigurer))]

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [CustomEditor(typeof(TeamCityConfigurerEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.TeamCityConfigurer,TeamCity")]
    public sealed class TeamCityConfigurer : ExtensionConfigurerBase, ITeamCityConnectionInfo
    {
        string ITeamCityConnectionInfo.UserName => this.Username; // Rubbish casing

        internal static readonly string ConfigurerName = typeof(TeamCityConfigurer).FullName + "," + typeof(TeamCityConfigurer).Assembly.GetName().Name;

        /// <summary>
        /// Gets the configurer based on the profile name, the default configurer if no name is specified, or null.
        /// </summary>
        /// <param name="profileName">Name of the profile.</param>
        internal static TeamCityConfigurer GetConfigurer(string profileName = null, int? configurerId = null)
        {
            var profiles = DB.ExtensionConfiguration_GetConfigurations(ConfigurerName);

            var configurer = profiles.FirstOrDefault(
                p => p.ExtensionConfiguration_Id == configurerId || string.Equals(profileName, p.Profile_Name, StringComparison.OrdinalIgnoreCase)
            );

            if (configurer == null)
                configurer = profiles.FirstOrDefault(p => p.Default_Indicator.Equals(Domains.YN.Yes));

            if (configurer == null)
                return null;

            return (TeamCityConfigurer)Persistence.DeserializeFromPersistedObjectXml(configurer.Extension_Configuration);
        }

        /// <summary>
        /// Gets or sets the server URL without the form of authentication included in the URL.
        /// </summary>
        [Persistent]
        public string ServerUrl { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [Persistent]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Persistent(Encrypted = true)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the default name of the branch to supply for TeamCity operations.
        /// </summary>
        [Persistent]
        public string DefaultBranchName { get; set; }

        /// <summary>
        /// Gets the base URL used for connections to the TeamCity server that incorporates the authentication mechanism.
        /// </summary>
        public string BaseUrl => $"{this.ServerUrl.TrimEnd('/')}/{(string.IsNullOrEmpty(this.Username) ? "guestAuth" : "httpAuth")}/";
    }
}
