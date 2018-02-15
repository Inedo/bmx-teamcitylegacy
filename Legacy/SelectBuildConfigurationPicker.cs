using System;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    public class SelectBuildConfigurationPicker : SelectList
    {
        internal Action ExternalInit;

        public SelectBuildConfigurationPicker()
        {
            this.IsIdRequired = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.ExternalInit?.Invoke();
        }

        internal void FillItems(TeamCityConfigurer configurer)
        {
            if (configurer == null)
                return;

            using (var client = new WebClient())
            {
                client.BaseAddress = configurer.BaseUrl;
                if (!string.IsNullOrEmpty(configurer.Username))
                    client.Credentials = new NetworkCredential(configurer.Username, configurer.Password);

                this.Items.AddRange(XDocument
                    .Parse(client.DownloadString("app/rest/buildTypes"))
                    .Element("buildTypes")
                    .Elements("buildType")
                    .Select(e => new
                    {
                        Id = (string)e.Attribute("id"),
                        Project = (string)e.Attribute("projectName"),
                        Name = (string)e.Attribute("name")
                    })
                    .Select(bt => new SelectListItem(bt.Project + ": " + bt.Name, bt.Id))
                    .ToArray());
            }
        }

    }
}
