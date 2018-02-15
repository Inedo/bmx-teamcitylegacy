using Inedo.BuildMaster.Extensibility.Actions;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public abstract class TeamCityActionBase : AgentBasedActionBase
    {
        protected TeamCityActionBase()
        {
        }

        public sealed override bool IsConfigurerSettingRequired() => true;

        protected new TeamCityConfigurer GetExtensionConfigurer() => (TeamCityConfigurer)base.GetExtensionConfigurer();
    }
}