using System.ComponentModel;
using Inedo.BuildMaster.Extensibility.Variables;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [DisplayName("TeamCity Build Configuration")]
    [Description("Allows selection of a Build Configuration from a TeamCity instance.")]
    [CustomSetter(typeof(SelectBuildConfigurationVariableSetter))]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.SelectBuildConfigurationVariable,TeamCity")]
    public sealed class SelectBuildConfigurationVariable : VariableBase
    {
        [Persistent]
        [DisplayName("Configuration Profile")]
        [Category("Configuration")]
        [Description("When set, uses the specified configuration TeamCity profile instead of the default. This can be important when you have multiple TeamCity instances configured within BuildMaster.")]
        public string ConfigurationProfileName { get; set; }

        [Persistent]
        [DisplayName("Project Name Filter")]
        [Category("Project")]
        [Description("When set, filters the selectable build configurations to the specified project.")]
        public string ProjectNameFilter { get; set; }
    }
}
