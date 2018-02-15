using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using Inedo.BuildMaster.Artifacts;
using Inedo.BuildMaster.Extensibility;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensions.TeamCity;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityArtifactImporter
    {
        public string BuildConfigurationId { get; set; }
        public string ProjectName { get; set; }
        public string BuildConfigurationName { get; set; }
        public string ArtifactName { get; set; }
        public string BuildNumber { get; set; }
        public string BranchName { get; set; }

        public ITeamCityConnectionInfo ConnectionInfo { get; }
        public ILogger Logger { get; }
        public IGenericBuildMasterContext Context { get; }

        public TeamCityArtifactImporter(ITeamCityConnectionInfo connectionInfo, ILogger logger, IGenericBuildMasterContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.ApplicationId == null)
                throw new InvalidOperationException("context requires a valid application ID");

            this.ConnectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Context = context;
        }

        public async Task<string> ImportAsync()
        {
            this.Logger.LogInformation($"Importing artifact \"{this.ArtifactName}\" from TeamCity...");

            if (this.BuildConfigurationId == null)
            {
                if (this.BuildConfigurationName != null && this.ProjectName != null)
                {
                    await SetBuildConfigurationIdFromName().ConfigureAwait(false);
                }
                else
                {
                    throw new ExecutionFailureException("If BuildConfigurationId is not specified directly, a project name and configuration name are required.");
                }
            }

            if (string.IsNullOrEmpty(this.BuildNumber))
            {
                this.Logger.LogDebug("BuildNumber was not specified, using lastSuccessful...");
                this.BuildNumber = "lastSuccessful";
            }

            string relativeUrl = string.Format("repository/download/{0}/{1}/{2}", this.BuildConfigurationId, this.BuildNumber, this.ArtifactName);

            if (!string.IsNullOrEmpty(this.BranchName))
            {
                this.Logger.LogDebug("Branch name was specified: " + this.BranchName);
                relativeUrl += "?branch=" + Uri.EscapeDataString(this.BranchName);
            }

            this.Logger.LogDebug(string.Format("Importing TeamCity artifact \"{0}\" from {1}...", this.ArtifactName, this.ConnectionInfo.GetApiUrl() + relativeUrl));

            string tempFile = null;
            try
            {
                using (var client = new TeamCityWebClient(this.ConnectionInfo))
                {
                    tempFile = Path.GetTempFileName();
                    this.Logger.LogDebug($"Downloading temp file to \"{tempFile}\"...");
                    try
                    {
                        await client.DownloadFileTaskAsync(relativeUrl, tempFile).ConfigureAwait(false);
                    }
                    catch (WebException wex)
                    {
                        var response = wex.Response as HttpWebResponse;
                        if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                            this.Logger.LogWarning("The TeamCity request returned a 404 - this could mean that the branch name, build number, or build configuration is invalid.");

                        throw;
                    }
                }

                this.Logger.LogInformation("Importing artifact into BuildMaster...");
                using (var file = File.OpenRead(tempFile))
                {
                    await Artifact.CreateArtifactAsync(
                        applicationId: (int)this.Context.ApplicationId,
                        releaseNumber: this.Context.ReleaseNumber,
                        buildNumber: this.Context.BuildNumber,
                        deployableId: this.Context.DeployableId,
                        executionId: this.Context.ExecutionId,
                        artifactName: TrimWhitespaceAndZipExtension(this.ArtifactName),
                        artifactData: file,
                        overwrite: true
                    );
                }
            }
            finally
            {
                if (tempFile != null)
                {
                    this.Logger.LogDebug("Removing temp file...");
                    FileEx.Delete(tempFile);
                }
            }

            this.Logger.LogInformation(this.ArtifactName + " artifact imported.");
            
            return await this.GetActualBuildNumber().ConfigureAwait(false);
        }

        private async Task<string> GetActualBuildNumber()
        {
            this.Logger.LogDebug("Resolving actual build number...");

            string apiUrl = this.TryGetPredefinedConstantBuildNumberApiUrl(this.BuildNumber);
            if (apiUrl == null)
            {
                this.Logger.LogDebug($"Using explicit build number: {this.BuildNumber}");
                return this.BuildNumber;
            }

            this.Logger.LogDebug($"Build number is the predefined constant \"{this.BuildNumber}\", resolving...");

            try
            {
                if (this.BranchName != null)
                    apiUrl += ",branch:" + Uri.EscapeDataString(this.BranchName);

                using (var client = new TeamCityWebClient(this.ConnectionInfo))
                {
                    string xml = await client.DownloadStringTaskAsync(apiUrl).ConfigureAwait(false);
                    var doc = XDocument.Parse(xml);
                    return doc.Element("builds").Element("build").Attribute("number").Value;
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError("Could not parse actual build number from TeamCity. Exception details: " + ex);
                return null;
            }
        }

        private string TryGetPredefinedConstantBuildNumberApiUrl(string buildNumber)
        {
            if (string.Equals(buildNumber, "lastSuccessful", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/?locator=buildType:{0},running:false,status:success,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastPinned", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/?locator=buildType:{0},running:false,pinned:true,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            if (string.Equals(buildNumber, "lastFinished", StringComparison.OrdinalIgnoreCase))
                return string.Format("app/rest/builds/?locator=buildType:{0},running:false,count:1", Uri.EscapeDataString(this.BuildConfigurationId));

            return null;
        }

        private async Task SetBuildConfigurationIdFromName()
        {
            this.Logger.LogDebug("Attempting to resolve build configuration ID from project and name...");
            using (var client = new TeamCityWebClient(this.ConnectionInfo))
            {
                this.Logger.LogDebug("Downloading build types...");
                string result = await client.DownloadStringTaskAsync("app/rest/buildTypes").ConfigureAwait(false);
                var doc = XDocument.Parse(result);
                var buildConfigurations = from e in doc.Element("buildTypes").Elements("buildType")
                                          let buildType = new BuildType(e)
                                          where string.Equals(buildType.BuildConfigurationName, this.BuildConfigurationName, StringComparison.OrdinalIgnoreCase)
                                          let match = new
                                          {
                                              BuildType = buildType,
                                              Index = Array.FindIndex(buildType.ProjectNameParts, p => string.Equals(p, this.ProjectName, StringComparison.OrdinalIgnoreCase))
                                          }
                                          where match.Index > -1 || string.Equals(match.BuildType.ProjectName, this.ProjectName, StringComparison.OrdinalIgnoreCase)
                                          orderby match.Index
                                          select match.BuildType.BuildConfigurationId;
                
                this.BuildConfigurationId = buildConfigurations.FirstOrDefault();
                if (this.BuildConfigurationId == null)
                    throw new ExecutionFailureException($"Build configuration ID could not be found for project \"{this.ProjectName}\" and build configuration \"{this.BuildConfigurationName}\".");

                this.Logger.LogDebug("Build configuration ID resolved to: " + this.BuildConfigurationId);
            }
        }

        private static string TrimWhitespaceAndZipExtension(string artifactName)
        {
            string file = PathEx.GetFileName(artifactName).Trim();
            if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return file.Substring(0, file.Length - ".zip".Length);
            else
                return file;
        }
    }
}
