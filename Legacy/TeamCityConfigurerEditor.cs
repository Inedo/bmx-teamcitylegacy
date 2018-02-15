using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class TeamCityConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtServerUrl;
        private ValidatingTextBox txtUsername;
        private ValidatingTextBox txtDefaultBranchName;
        private PasswordTextBox txtPassword;

        public override void InitializeDefaultValues()
        {
            this.BindToForm(new TeamCityConfigurer());
        }
        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            var configurer = (TeamCityConfigurer)extension;

            this.txtServerUrl.Text = configurer.ServerUrl;
            this.txtDefaultBranchName.Text = configurer.DefaultBranchName;
            if (!string.IsNullOrEmpty(configurer.Username))
            {
                this.txtUsername.Text = configurer.Username;
                this.txtPassword.Text = configurer.Password;
            }
        }
        public override ExtensionConfigurerBase CreateFromForm()
        {
            var configurer = new TeamCityConfigurer()
            {
                ServerUrl = this.txtServerUrl.Text,
                DefaultBranchName = this.txtDefaultBranchName.Text
            };
            if (!string.IsNullOrEmpty(this.txtUsername.Text))
            {
                configurer.Username = this.txtUsername.Text;
                configurer.Password = this.txtPassword.Text;
            }

            return configurer;
        }

        protected override void CreateChildControls()
        {
            this.txtServerUrl = new ValidatingTextBox { Required = true };
            this.txtUsername = new ValidatingTextBox();
            this.txtPassword = new PasswordTextBox();
            this.txtDefaultBranchName = new ValidatingTextBox { DefaultText = "TeamCity-defined" };

            this.Controls.Add(
                new SlimFormField("TeamCity server URL:", this.txtServerUrl)
                {
                    HelpText = "Enter the URL of the TeamCity server, typically: http://teamcityserver"
                },
                new SlimFormField("User name:", this.txtUsername)
                {
                    HelpText = "If you wish to connect to the TeamCity server with HTTP Authentication, please enter the credentials. Leaving the username field blank will connect using guest authentication."
                },
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("Default branch:", this.txtDefaultBranchName)
                {
                    HelpText = "To override the default branch used by TeamCity for Get Artifact operations, specify a branch name here. "
                    + "The branch may also be overridden in individual deployment actions."
                }
            );
        }
    }
}
