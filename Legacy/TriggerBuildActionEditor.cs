using System.Web;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TriggerBuildActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtBuildConfigurationId;
        private ValidatingTextBox txtAdditionalParameters;
        private ValidatingTextBox txtBranchName;
        private CheckBox chkWaitForCompletion;

        public override void BindToForm(ActionBase extension)
        {
            var action = (TriggerBuildAction)extension;

            this.txtBuildConfigurationId.Text = action.BuildConfigurationId;
            this.txtAdditionalParameters.Text = action.AdditionalParameters;
            this.chkWaitForCompletion.Checked = action.WaitForCompletion;
            this.txtBranchName.Text = action.BranchName;
        }

        public override ActionBase CreateFromForm()
        {
            return new TriggerBuildAction
            {
                BuildConfigurationId = this.txtBuildConfigurationId.Text,
                AdditionalParameters = this.txtAdditionalParameters.Text,
                WaitForCompletion = this.chkWaitForCompletion.Checked,
                BranchName = this.txtBranchName.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtBuildConfigurationId = new ValidatingTextBox { Required = true };

            this.txtBranchName = new ValidatingTextBox { DefaultText = "Default" };

            this.txtAdditionalParameters = new ValidatingTextBox();

            this.chkWaitForCompletion = new CheckBox
            {
                Text = "Wait for build to complete",
                Checked = true
            };

            this.Controls.Add(
                new SlimFormField("Build configuration ID:", this.txtBuildConfigurationId)
                {
                    HelpText = new LiteralHtml("This value can be found in a browser address bar when corresponding configuration is browsed within TeamCity. <br /><br />As an example, teamcity.jetbrains.com/viewLog.html?buildId=64797&buildTypeId=<strong>bt343</strong>&tab=...", false)
                },
                new SlimFormField("Branch:", this.txtBranchName)
                {
                    HelpText = "The branch used to get the artifact, typically used in conjunction with predefined constant build numbers."
                },
                new SlimFormField("Additional parameters:", this.txtAdditionalParameters)
                {
                    HelpText = new LiteralHtml("Optionally enter any additional parameters accepted by the TeamCity API in query string format, for example:<br/> " + HttpUtility.HtmlEncode("&name=agent&value=<agentnamevalue>&name=system.name&value=<systemnamevalue>.."), false)
                },
                new SlimFormField("Wait for completion:", this.chkWaitForCompletion)
                {
                    HelpText = "Specify whether BuildMaster should pause the action until the TeamCity build has completed."
                }
            );
        }
    }
}
