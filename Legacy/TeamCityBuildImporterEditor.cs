using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityBuildImporterEditor : BuildImporterEditorBase<TeamCityBuildImporterTemplate>
    {
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtBranchName;
        private BuildNumberPicker ctlBuildNumber;

        public override BuildImporterBase CreateFromForm()
        {
            return new TeamCityBuildImporter
            {
                ArtifactName = this.txtArtifactName.Text,
                BuildConfigurationId = this.Template.BuildConfigurationId,
                BuildConfigurationDisplayName = this.Template.BuildConfigurationDisplayName,
                BuildNumber = this.ctlBuildNumber.Value,
                BranchName = this.txtBranchName.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtArtifactName = new ValidatingTextBox
            {
                Required = true,
                Enabled = !this.Template.ArtifactNameLocked,
                Text = this.Template.ArtifactName
            };
            this.txtBranchName = new ValidatingTextBox
            {
                Text = this.Template.BranchName,
                Enabled = !this.Template.BranchNameLocked,
                DefaultText = "Default"
            };
            this.ctlBuildNumber = new BuildNumberPicker
            {
                Value = Util.CoalesceStr(this.Template.BuildNumber, "lastSuccessful"),
                Enabled = string.IsNullOrEmpty(this.Template.BuildNumber),
                BuildConfigurationId = this.Template.BuildConfigurationId,
                ConfigurerId = this.Template.ExtensionConfigurerId ?? 0
            };

            this.Controls.Add(
                new SlimFormField("Build configuration:", new Div(this.Template.BuildConfigurationDisplayName)),
                new SlimFormField("TeamCity build number:", new Div(this.ctlBuildNumber)),
                new SlimFormField("Artifact name:", this.txtArtifactName)
                {
                    HelpText = "The name of artifact, for example: \"ideaIC-118.SNAPSHOT.win.zip\"."
                },
                new SlimFormField("Branch:", this.txtBranchName)
                {
                    HelpText = "The branch used to get the artifact, typically used in conjunction with predefined constant build numbers."
                }
            );
        }
    }
}
