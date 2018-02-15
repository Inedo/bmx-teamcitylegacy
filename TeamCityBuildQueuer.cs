using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Inedo.Diagnostics;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.TeamCity
{
    internal sealed class TeamCityBuildQueuer
    {
        private int progressPercent;
        private string progressMessage;

        public string BuildConfigurationId { get; set; }
        public string ProjectName { get; set; }
        public string BuildConfigurationName { get; set; }
        public string BranchName { get; set; }
        public bool WaitForCompletion { get; set; }
        public string AdditionalParameters { get; set; }

        public ITeamCityConnectionInfo ConnectionInfo { get; }
        public ILogger Logger { get; }

        public TeamCityBuildQueuer(ITeamCityConnectionInfo connectionInfo, ILogger logger)
        {
            this.ConnectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public OperationProgress GetProgress()
        {
            return new OperationProgress(this.progressPercent, this.progressMessage);
        }

        public async Task QueueBuildAsync(CancellationToken cancellationToken, bool logProgressToExecutionLog)
        {
            this.Logger.LogInformation($"Queueing build in TeamCity...");

            if (this.BuildConfigurationName != null && this.ProjectName != null && this.BuildConfigurationId == null)
            {
                await SetBuildConfigurationIdFromName().ConfigureAwait(false);
            }

            using (var client = new TeamCityWebClient(this.ConnectionInfo))
            {
                this.Logger.LogDebug($"Triggering build configuration {this.BuildConfigurationId}...");
                if (this.BranchName != null)
                    this.Logger.LogDebug("Using branch: " + this.BranchName);

                var xdoc = new XDocument(
                    new XElement("build",
                        new XAttribute("branchName", this.BranchName ?? ""),
                        new XElement("buildType", new XAttribute("id", this.BuildConfigurationId))
                    )
                );
                string response = await client.UploadStringTaskAsync("app/rest/buildQueue", xdoc.ToString(SaveOptions.DisableFormatting)).ConfigureAwait(false);
                var status = new TeamCityBuildStatus(response);

                this.Logger.LogInformation($"Build of {this.BuildConfigurationId} was triggered successfully.");                

                if (!this.WaitForCompletion)
                    return;

                this.Logger.LogInformation("Waiting for build to complete...");

                while (!status.Finished)
                {
                    string getBuildStatusResponse = await client.DownloadStringTaskAsync(status.Href).ConfigureAwait(false);
                    status = new TeamCityBuildStatus(getBuildStatusResponse);

                    this.progressPercent = status.PercentageComplete;
                    this.progressMessage = $"Building {status.ProjectName} Build #{status.Number} ({status.PercentageComplete}% Complete)";

                    if (logProgressToExecutionLog)
                        this.Logger.LogInformation(this.progressMessage);

                    await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (status.Success)
                {
                    this.Logger.LogInformation($"{status.ProjectName} build #{status.Number} successful. TeamCity reports: {status.StatusText}");
                }
                else
                {
                    this.Logger.LogError($"{status.ProjectName} build #{status.Number} failed or encountered an error. TeamCity reports: {status.StatusText}");
                }
            }
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

        private sealed class TeamCityBuildStatus
        {
            public string Id { get; }
            public string Number { get; }
            public string Status { get; }
            public string State { get; }
            public string WebUrl { get; }
            public string Href { get; }
            public string WaitReason { get; }
            public string StatusText { get; }
            public string ProjectName { get; }
            public int PercentageComplete { get; }

            public bool Success => string.Equals(this.Status, "success", StringComparison.OrdinalIgnoreCase);
            public bool Finished => string.Equals(this.State, "finished", StringComparison.OrdinalIgnoreCase);

            public TeamCityBuildStatus(string getBuildStatusResponse)
            {
                var xdoc = XDocument.Parse(getBuildStatusResponse);
                this.Id = (string)xdoc.Root.Attribute("id");
                this.Number = (string)xdoc.Root.Attribute("number");
                this.Status = (string)xdoc.Root.Attribute("status");
                this.State = (string)xdoc.Root.Attribute("state");
                this.WebUrl = (string)xdoc.Root.Attribute("webUrl");
                this.Href = string.Join("/", xdoc.Root.Attribute("href").Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1));
                this.WaitReason = (string)xdoc.Root.Element("waitReason") ?? "(none)";
                this.StatusText = (string)xdoc.Root.Element("statusText") ?? "(none)";
                this.ProjectName = (string)xdoc.Root.Element("buildType")?.Attribute("projectName");
                this.PercentageComplete = this.Finished ? 100 : ((int?)xdoc.Root.Attribute("percentageComplete") ?? 0);
            }
        }
    }

    internal sealed class BuildType
    {
        public BuildType(XElement e)
        {
            this.BuildConfigurationId = (string)e.Attribute("id");
            this.BuildConfigurationName = (string)e.Attribute("name");
            this.ProjectName = (string)e.Attribute("projectName");
            this.ProjectNameParts = this.ProjectName.Split(new[] { " :: " }, StringSplitOptions.None);
        }
        public string BuildConfigurationId { get; }
        public string BuildConfigurationName { get; }
        public string ProjectName { get; }
        public string[] ProjectNameParts { get; }
    }
}
