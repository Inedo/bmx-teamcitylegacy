using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.BuildMaster.Web.Controls.Extensions.BuildImporters;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityBuildImporterTemplateEditor : BuildImporterTemplateEditorBase
    {
        private ValidatingTextBox txtArtifactName;
        private ValidatingTextBox txtBranchName;
        private CheckBox chkArtifactNameLocked;
        private CheckBox chkBranchNameLocked;
        private SelectBuildConfigurationPicker ddlBuildConfigurationId;
        private SelectList ddlBuildNumber;

        public TeamCityBuildImporterTemplateEditor()
        {
            this.ValidateBeforeSave += TeamCityBuildImporterTemplateEditor_ValidateBeforeSave;
        }

        private void TeamCityBuildImporterTemplateEditor_ValidateBeforeSave(object sender, ValidationEventArgs<BuildImporterTemplateBase> e)
        {
            var template = (TeamCityBuildImporterTemplate)e.Extension;
            if (string.IsNullOrWhiteSpace(template.BuildConfigurationId))
            {
                e.Message = "A build configuration is required";
                e.ValidLevel = ValidationLevel.Error;
            }
        }

        public override void BindToForm(BuildImporterTemplateBase extension)
        {
            var template = (TeamCityBuildImporterTemplate)extension;

            this.txtArtifactName.Text = template.ArtifactName;
            this.ddlBuildConfigurationId.SelectedValue = template.BuildConfigurationId;
            this.chkArtifactNameLocked.Checked = !template.ArtifactNameLocked;
            this.ddlBuildNumber.SelectedValue = template.BuildNumber;
            this.txtBranchName.Text = template.BranchName;
            this.chkBranchNameLocked.Checked = !template.BranchNameLocked;
        }

        public override BuildImporterTemplateBase CreateFromForm()
        {
            var selected = this.ddlBuildConfigurationId.Items.FirstOrDefault(i => i.Selected);

            return new TeamCityBuildImporterTemplate
            {
                ArtifactName = this.txtArtifactName.Text,
                ArtifactNameLocked = !this.chkArtifactNameLocked.Checked,
                BuildConfigurationId = this.ddlBuildConfigurationId.SelectedValue,
                BuildConfigurationDisplayName = selected?.Text ?? string.Empty,
                BuildNumber = this.ddlBuildNumber.SelectedValue,
                BranchName = this.txtBranchName.Text,
                BranchNameLocked = !this.chkBranchNameLocked.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtArtifactName = new ValidatingTextBox { Required = true };
            this.txtBranchName = new ValidatingTextBox() { DefaultText = "Default" };
            this.chkArtifactNameLocked = new CheckBox { Text = "Allow selection at build time" };
            this.chkBranchNameLocked = new CheckBox { Text = "Allow selection at build time" };

            this.ddlBuildConfigurationId = new SelectBuildConfigurationPicker() { ID = "ddlBuildConfigurationId" };
            this.ddlBuildConfigurationId.ExternalInit = 
                () =>
                {
                    int? configurerId = this.TryGetConfigurerId();
                    this.ddlBuildConfigurationId.FillItems(TeamCityConfigurer.GetConfigurer(configurerId: configurerId));
                };

            this.ddlBuildNumber = new SelectList();
            this.ddlBuildNumber.Items.Add(new SelectListItem("Select at build import time", ""));
            this.ddlBuildNumber.Items.Add(new SelectListItem("Always use last successful build", "lastSuccessful"));
            this.ddlBuildNumber.Items.Add(new SelectListItem("Always use last finished build", "lastFinished"));
            this.ddlBuildNumber.Items.Add(new SelectListItem("Always use last pinned build", "lastPinned"));

            this.Controls.Add(
                new SlimFormField("Build configuration:", this.ddlBuildConfigurationId),
                new SlimFormField("TeamCity build number:", this.ddlBuildNumber),
                new SlimFormField("Artifact name:", new Div(this.txtArtifactName), new Div(this.chkArtifactNameLocked))
                {
                    HelpText = "The name of artifact, for example: \"ideaIC-118.SNAPSHOT.win.zip\"."
                },
                new SlimFormField("Branch name:", new Div(this.txtBranchName), new Div(this.chkBranchNameLocked))
                {
                    HelpText = "The branch used to get the artifact, typically used in conjunction with predefined constant build numbers."
                }
            );
        }

        /// <summary>
        /// This is a hack to find the selected configurer ID since it is not exposed via the SDK at the moment...
        /// </summary>
        private int? TryGetConfigurerId()
        {
            try
            {
                var ddlExtensionConfigurer = this.Page.FindControl("ddlExtensionConfigurer") as DropDownList;
                if (ddlExtensionConfigurer != null)
                    return AH.ParseInt(ddlExtensionConfigurer.SelectedValue);
            }
            catch
            {
            }
            return null;
        }
    }
}
