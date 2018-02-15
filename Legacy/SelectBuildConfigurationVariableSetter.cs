using System;
using Inedo.BuildMaster.Web.Controls.Extensions;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public sealed class SelectBuildConfigurationVariableSetter : SelectBuildConfigurationPicker, IVariableSetter<SelectBuildConfigurationVariable>
    {
        public SelectBuildConfigurationVariableSetter()
        {
        }

        string IVariableSetter.VariableValue
        {
            get
            {
                return this.SelectedValue;
            }
            set
            {
                this.SelectedValue = value;
            }
        }

        void IVariableSetter<SelectBuildConfigurationVariable>.BindToVariable(SelectBuildConfigurationVariable variable, string defaultValue)
        {
            if (variable == null) throw new ArgumentNullException("variable");

            this.FillItems(TeamCityConfigurer.GetConfigurer(variable.ConfigurationProfileName));
            this.SelectedValue = AH.CoalesceString(variable.Value, defaultValue);
        }
    }
}
