using Inedo.BuildMaster.Extensibility.BuildImporters;
using Inedo.Documentation;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    [CustomEditor(typeof(TeamCityBuildImporterTemplateEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.TeamCity.TeamCityBuildImporterTemplate,TeamCity")]
    internal sealed class TeamCityBuildImporterTemplate : BuildImporterTemplateBase<TeamCityBuildImporter>
    {
        [Persistent]
        public string ArtifactName { get; set; }
        [Persistent]
        public string BuildConfigurationId { get; set; }
        [Persistent]
        public string BuildConfigurationDisplayName { get; set; }
        [Persistent]
        public bool ArtifactNameLocked { get; set; }
        [Persistent]
        public string BuildNumber { get; set; }
        [Persistent]
        public string BranchName { get; set; }
        [Persistent]
        public bool BranchNameLocked { get; set; }

        public override RichDescription GetDescription()
        {
            var desc = new RichDescription("Import the ");
            desc.AppendContent(new Hilite(this.ArtifactName));
            desc.AppendContent(" artifact from TeamCity build configuration ");
            desc.AppendContent(new Hilite(this.BuildConfigurationDisplayName));
            if (!string.IsNullOrEmpty(this.BuildNumber))
            {
                desc.AppendContent(" using the special build type: ");
                desc.AppendContent(new Hilite(this.BuildNumber));
            }
            else
            {
                desc.AppendContent(" requiring a special build type or specific build number to be selected at build import time.");
            }
            return desc;
        }
    }
}
