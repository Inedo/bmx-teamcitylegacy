using System.ComponentModel;
using System.Threading;
using Inedo.BuildMaster;
using Inedo.Documentation;
using Inedo.Extensions.TeamCity;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [DisplayName("Trigger TeamCity Build")]
    [Description("Triggers a build in TeamCity using the specified build configuration ID.")]
    [CustomEditor(typeof(TriggerBuildActionEditor))]
    [Tag(Tags.ContinuousIntegration)]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.TriggerBuildAction,TeamCity")]
    public sealed class TriggerBuildAction : TeamCityActionBase
    {
        [Persistent]
        public string BuildConfigurationId { get; set; }
        [Persistent]
        public string AdditionalParameters { get; set; }
        [Persistent]
        public bool WaitForCompletion { get; set; }
        [Persistent]
        public string BranchName { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Triggers a build of the configuration \"{0}\" in TeamCity{1}{2}.",
                this.BuildConfigurationId,
                Util.ConcatNE(" with the additional parameters \"", this.AdditionalParameters, "\""),
                !string.IsNullOrEmpty(this.BranchName) ? " using branch " + this.BranchName : ""
            );
        }

        protected override void Execute()
        {
            var configurer = this.GetExtensionConfigurer();
            string branch = this.GetBranchName(configurer);

            var queuer = new TeamCityBuildQueuer(configurer, this)
            {
                BuildConfigurationId = this.BuildConfigurationId,
                AdditionalParameters = this.AdditionalParameters,
                WaitForCompletion = this.WaitForCompletion, 
                BranchName = branch
            };

            queuer.QueueBuildAsync(CancellationToken.None, logProgressToExecutionLog: true).WaitAndUnwrapExceptions();
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
