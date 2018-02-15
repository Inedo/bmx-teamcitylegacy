using System;
using System.ComponentModel;
using System.IO;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.Documentation;
using Inedo.Extensions.TeamCity;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [DisplayName("Get TeamCity Artifact")]
    [Description("Gets an artifact from a TeamCity server.")]
    [CustomEditor(typeof(GetArtifactActionEditor))]
    [Tag(Tags.ContinuousIntegration)]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.GetArtifactAction,TeamCity")]
    public sealed class GetArtifactAction : TeamCityActionBase
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string BuildConfigurationId { get; set; }
        [Persistent]
        public string BuildNumber { get; set; }
        [Persistent]
        public string BranchName { get; set; }
        [Persistent]
        public bool ExtractFilesToTargetDirectory { get; set; } = true;

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription("Get TeamCity ", new Hilite(this.ArtifactName), " Artifact "),
                new RichDescription("of build ",
                    AH.ParseInt(this.BuildNumber) != null ? "#" : "",
                    new Hilite(this.BuildNumber),
                    !string.IsNullOrEmpty(this.BranchName) ? " on branch " + this.BranchName : "",
                    " of the configuration \"",
                    this.BuildConfigurationId,
                    "\" and ",
                    this.ExtractFilesToTargetDirectory ? "deploy its contents" : "copy the artifact",
                    " to ",
                    new DirectoryHilite(this.OverriddenTargetDirectory)
                )
            );
        }

        protected override void Execute()
        {
            var configurer = this.GetExtensionConfigurer();

            string relativeUrl = string.Format("repository/download/{0}/{1}/{2}", this.BuildConfigurationId, this.BuildNumber, this.ArtifactName);

            string branchName = this.GetBranchName(configurer);
            if (branchName != null)
            {
                this.LogDebug("Getting artifact using branch: " + branchName);
                relativeUrl += "?branch=" + Uri.EscapeDataString(branchName);
            }

            this.LogDebug("Downloading TeamCity artifact \"{0}\" from {1} to {2}", this.ArtifactName, configurer.BaseUrl + relativeUrl, this.Context.TargetDirectory);

            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();

            string tempFile;
            using (var client = new TeamCityWebClient(configurer))
            {
                tempFile = Path.GetTempFileName();
                client.DownloadFile(relativeUrl, tempFile);
            }

            if (this.ExtractFilesToTargetDirectory)
            {
                this.LogDebug("Transferring artifact to {0} before extracting...", this.Context.TempDirectory);
                string remoteTempPath = fileOps.CombinePath(this.Context.TempDirectory, this.ArtifactName);
                fileOps.WriteFileBytes(
                    remoteTempPath,
                    File.ReadAllBytes(tempFile)
                );

                this.LogDebug("Extracting TeamCity artifact to {0}...", this.Context.TargetDirectory);
                fileOps.ExtractZipFile(remoteTempPath, this.Context.TargetDirectory, true);
            }
            else
            {
                this.LogDebug("Transferring artifact to {0}...", this.Context.TargetDirectory);
                fileOps.WriteFileBytes(
                    fileOps.CombinePath(this.Context.TargetDirectory, this.ArtifactName),
                    File.ReadAllBytes(tempFile)
                );
            }

            this.LogInformation("Artifact retrieved successfully.");
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
