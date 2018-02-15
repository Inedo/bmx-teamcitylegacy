using System;
using System.ComponentModel;
using System.Xml.Linq;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.Diagnostics;
using Inedo.Extensions.TeamCity;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [DisplayName("TeamCity")]
    [Description("Imports artifacts from a build in TeamCity.")]
    [BuildImporterTemplate(typeof(TeamCityBuildImporterTemplate))]
    [CustomEditor(typeof(TeamCityBuildImporterEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.TeamCityBuildImporter,TeamCity")]
    public sealed class TeamCityBuildImporter : BuildImporterBase, ICustomBuildNumberProvider
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string BuildConfigurationId { get; set; }
        [Persistent]
        public string BuildConfigurationDisplayName { get; set; }
        [Persistent]
        public string BuildNumber { get; set; }
        [Persistent]
        public string BranchName { get; set; }

        string ICustomBuildNumberProvider.BuildNumber => GetActualBuildNumber(this.BuildNumber);

        public new TeamCityConfigurer GetExtensionConfigurer() => (TeamCityConfigurer)base.GetExtensionConfigurer();

        public override void Import(IBuildImporterContext context)
        {
            var configurer = this.GetExtensionConfigurer();
            var importer = new TeamCityArtifactImporter(configurer, this, context)
            {
                ArtifactName = this.ArtifactName,
                BranchName = this.GetBranchName(configurer),
                BuildConfigurationId = this.BuildConfigurationId,
                BuildNumber = this.BuildNumber
            };
            string teamCityBuildNumber = importer.ImportAsync().Result();
            
            this.LogDebug($"TeamCity build number resolved to {teamCityBuildNumber}, creating $TeamCityBuildNumber variable...");

            DB.Variables_CreateOrUpdateVariableDefinition(
                "TeamCityBuildNumber",
                Application_Id: context.ApplicationId,
                Release_Number: context.ReleaseNumber,
                Build_Number: context.BuildNumber,
                Value_Text: teamCityBuildNumber,
                Sensitive_Indicator: false,
                Environment_Id: null,
                ServerRole_Id: null,
                Server_Id: null,
                ApplicationGroup_Id: null,
                Execution_Id: null,
                Promotion_Id: null,
                Deployable_Id: null
            );
        }

        private string GetActualBuildNumber(string buildNumber)
        {
            string apiUrl = this.TryGetPredefinedConstantBuildNumberApiUrl(buildNumber);
            if (apiUrl == null)
            {
                this.LogDebug($"Using explicit build number: {buildNumber}");
                return buildNumber;
            }

            this.LogDebug($"Build number is the predefined constant \"{buildNumber}\", resolving...");

            try
            {
                var configurer = this.GetExtensionConfigurer();
                string branch = this.GetBranchName(configurer);
                if (branch != null)
                    apiUrl += ",branch:" + Uri.EscapeDataString(branch);

                using (var client = new TeamCityWebClient(configurer))
                {
                    string xml = client.DownloadString(apiUrl);
                    var doc = XDocument.Parse(xml);
                    return doc.Element("builds").Element("build").Attribute("number").Value;
                }
            }
            catch (Exception ex)
            {
                this.LogError("Could not parse actual build number from TeamCity. Exception details: " + ex);
                return null;
            }
        }

        private string TryGetPredefinedConstantBuildNumberApiUrl(string buildNumber)
        {
            if (string.Equals(buildNumber, "lastSuccessful", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,status:success,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastPinned", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,pinned:true,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastFinished", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/buildType:{0},running:false,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            return null;
        }

        private string GetBranchName(TeamCityConfigurer configurer)
        {
            if (!string.IsNullOrEmpty(this.BranchName))
                return this.BranchName;

            if (!string.IsNullOrEmpty(configurer.DefaultBranchName))
                return configurer.DefaultBranchName;

            return null;
        }
    }
}