using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class GetArtifactActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtBuildConfigurationId;
        private ValidatingTextBox txtBuildNumber;
        private ValidatingTextBox txtBranchName;
        private CheckBox chkExtractFilesToTargetDirectory;

        public override bool DisplayTargetDirectory => true;

        public override void BindToForm(ActionBase extension)
        {
            var action = (GetArtifactAction)extension;

            this.txtArtifactName.Text = action.ArtifactName;
            this.txtBuildConfigurationId.Text = action.BuildConfigurationId;
            this.txtBuildNumber.Text = action.BuildNumber;
            this.txtBranchName.Text = action.BranchName;
            this.chkExtractFilesToTargetDirectory.Checked = action.ExtractFilesToTargetDirectory;
        }

        public override ActionBase CreateFromForm()
        {
            return new GetArtifactAction
            {
                ArtifactName = this.txtArtifactName.Text,
                BuildConfigurationId = this.txtBuildConfigurationId.Text,
                BuildNumber = this.txtBuildNumber.Text,
                BranchName = this.txtBranchName.Text,
                ExtractFilesToTargetDirectory = this.chkExtractFilesToTargetDirectory.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtArtifactName = new ValidatingTextBox { Required = true };
            this.txtBuildConfigurationId = new ValidatingTextBox { Required = true };
            this.txtBuildNumber = new ValidatingTextBox { Required = true };
            this.txtBranchName = new ValidatingTextBox { DefaultText = "default" };
            this.chkExtractFilesToTargetDirectory = new CheckBox { Text = "Extract files in artifact to target directory" };

            this.Controls.Add(
                new SlimFormField("Artifact name:", this.txtArtifactName)
                {
                    HelpText = new LiteralHtml("The name of artifact, for example: <br />\"ideaIC-118.SNAPSHOT.win.zip\". This value can also take a form of \"artifactName!archivePath\" for reading archive's content", false)
                },
                new SlimFormField("Build configuration ID:", this.txtBuildConfigurationId)
                {
                    HelpText = new LiteralHtml("This value can be found in a browser address bar when corresponding configuration is browsed within TeamCity. <br /><br />As an example, teamcity.jetbrains.com/viewLog.html?buildId=64797&buildTypeId=<strong>bt343</strong>&tab=...", false)
                },
                new SlimFormField("Build number:", this.txtBuildNumber)
                {
                    HelpText = "The build number or one of predefined constants: \"lastSuccessful\", \"lastPinned\", or \"lastFinished\"."
                },
                new SlimFormField("Branch:", this.txtBranchName)
                {
                    HelpText = "The branch used to get the artifact, typically used in conjunction with predefined constant build numbers."
                },
                new SlimFormField("Additional options:", this.chkExtractFilesToTargetDirectory)
            );
        }
    }
}
