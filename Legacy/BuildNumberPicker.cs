using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using Inedo.BuildMaster;
using Inedo.Web.ClientResources;
using Inedo.Web.Controls;
using Inedo.Web.Handlers;
using Newtonsoft.Json;

namespace Inedo.BuildMasterExtensions.TeamCity
{
    internal sealed class BuildNumberPicker : HiddenField
    {
        public BuildNumberPicker()
        {
            this.Enabled = true;
        }

        public bool Enabled { get; set; }
        public string BuildConfigurationId { get; set; }
        public Control ControlIdWithBuildConfigurationId { get; set; }
        public int ConfigurerId { get; set; }

        protected override void OnPreRender(EventArgs e)
        {
            this.EnsureID();
            base.OnPreRender(e);

            this.IncludeClientResourceInPage(
                new JavascriptResource("/extension-resources/TeamCity/BuildNumberPicker.js", InedoLibCR.select2)
            );
        }

        protected override void Render(HtmlTextWriter writer)
        {
            base.Render(writer);

            if (!this.Enabled)
            {
                writer.WriteEncodedText(this.Value);
                return;
            }

            writer.Write("<script type=\"text/javascript\">$(function(){");

            writer.Write("BmTeamCityBuildNumberPicker(");
            JsonSerializer.CreateDefault().Serialize(
                writer,
                new
                {
                    ajaxUrl = Ajax.GetUrl(new Func<string, int, object>(BuildNumberPicker.GetBuildNumbers)),
                    hiddenFieldSelector = "#" + this.ClientID,
                    buildConfigSelector = this.ControlIdWithBuildConfigurationId != null ? "#" + this.ControlIdWithBuildConfigurationId.ClientID : null,
                    buildConfigId = this.BuildConfigurationId,
                    configurerId = this.ConfigurerId
                }
            );
            writer.Write(");");

            writer.Write("});</script>");
        }

        [AjaxMethod]
        private static object GetBuildNumbers(string buildConfigurationId, int configurerId)
        {
            TeamCityConfigurer configurer;
            if (configurerId == 0)
                configurer = TeamCityConfigurer.GetConfigurer();
            else
                configurer = (TeamCityConfigurer)Util.ExtensionConfigurers.GetExtensionConfigurer(configurerId);

            using (var client = new WebClient())
            {
                client.BaseAddress = configurer.BaseUrl;
                if (!string.IsNullOrEmpty(configurer.Username))
                    client.Credentials = new NetworkCredential(configurer.Username, configurer.Password);

                string buildHistoryUrl = string.Format("app/rest/buildTypes/id:{0}/builds", Uri.EscapeDataString(buildConfigurationId));

                var list = new List<object>();

                list.Add(new { text = "Last successful build", id = "lastSuccessful" });
                list.Add(new { text = "Last pinned build", id = "lastPinned" });
                list.Add(new { text = "Last finished build", id = "lastFinished" });
                list.AddRange(XDocument
                    .Parse(client.DownloadString(buildHistoryUrl))
                    .Element("builds")
                    .Elements("build")
                    .Select(e => new
                    {
                        id = (string)e.Attribute("number"),
                        text = (string)e.Attribute("number")
                    }));

                return list;
            }
        }

    }
}
