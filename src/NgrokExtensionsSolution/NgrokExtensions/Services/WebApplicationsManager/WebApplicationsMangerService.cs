using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using NgrokExtensions.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Task = System.Threading.Tasks.Task;

namespace NgrokExtensions.Services
{
    public class WebApplicationsMangerService : IWebApplicationsManagerService, SWebApplicationsManagerService
    {
        private IAsyncServiceProvider serviceProvider;

        private const string SubdomainSettingName = "ngrok.subdomain";

        private readonly Regex NumberPattern = new Regex(@"\d+");

        private readonly HashSet<string> PortPropertyNames = new HashSet<string>
        {
            "WebApplication.DevelopmentServerPort",
            "WebApplication.IISUrl",
            "WebApplication.CurrentDebugUrl",
            "WebApplication.NonSecureUrl",
            "WebApplication.BrowseURL",
            "NodejsPort", // Node.js project
            "FileName",   // Azure functions if ends with '.funproj'
            "ProjectUrl"
        };

        public WebApplicationsMangerService(IAsyncServiceProvider provider)
        {
            serviceProvider = provider;
        }

        public IDictionary<string, WebApplication> Projects { get; set; }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await serviceProvider.GetServiceAsync(typeof(DTE)) as DTE;
                
            var solution = dte?.Solution;
            var projects = (solution == null) ? null : ProcessProjects(solution.Projects.Cast<Project>());
            Load(projects);
        }

        private IEnumerable<Project> ProcessProjects(IEnumerable<Project> projects)
        {
            var newProjectsList = new List<Project>();
            foreach (var p in projects)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (p.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    newProjectsList.AddRange(ProcessProjects(GetSolutionFolderProjects(p)));
                }
                else
                {
                    newProjectsList.Add(p);
                }
            }

            return newProjectsList;
        }

        private void Load(IEnumerable<Project> projects)
        {
            this.Projects = new Dictionary<string, WebApplication>();

            if (projects == null) return;

            foreach (Project project in projects)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (project.Properties == null) continue; // Project not loaded yet

                foreach (Property prop in project.Properties)
                {
                    //DebugWriteProp(prop);
                    if (!PortPropertyNames.Contains(prop.Name)) continue;

                    var webApp = new WebApplication();

                    if (prop.Name == "FileName")
                    {
                        if (prop.Value.ToString().EndsWith(".funproj"))
                        {
                            // Azure Functions app - use port 7071
                            webApp.PortNumber = 7071;
                            LoadOptionsFromAppSettingsJson(project, webApp);
                        }
                        else
                        {
                            continue;  // FileName property not relevant otherwise
                        }
                    }
                    else
                    {
                        var match = NumberPattern.Match(prop.Value.ToString());
                        if (!match.Success) continue;
                        webApp.PortNumber = int.Parse(match.Value);
                        if (IsAspNetCoreProject(prop.Name))
                        {
                            LoadOptionsFromAppSettingsJson(project, webApp);
                        }
                        else
                        {
                            LoadOptionsFromWebConfig(project, webApp);
                        }
                    }

                    this.Projects.Add(project.Name, webApp);
                    break;
                }
            }
        }

        private IEnumerable<Project> GetSolutionFolderProjects(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return project.ProjectItems.Cast<ProjectItem>()
                .Select(item =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return item.SubProject;
                })
                .Where(subProject => subProject != null)
                .ToList();
        }

        private bool IsAspNetCoreProject(string propName)
        {
            return propName == "ProjectUrl";
        }

        private void LoadOptionsFromWebConfig(Project project, WebApplication webApp)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.Name.ToLower() != "web.config") continue;

                var path = item.FileNames[0];
                var webConfig = XDocument.Load(path);
                var appSettings = webConfig.Descendants("appSettings").FirstOrDefault();
                webApp.SubDomain = appSettings?.Descendants("add")
                    .FirstOrDefault(x => x.Attribute("key")?.Value == SubdomainSettingName)
                    ?.Attribute("value")?.Value;
                break;
            }
        }

        private void LoadOptionsFromAppSettingsJson(Project project, WebApplication webApp)
        {
            // Read the settings from the project's appsettings.json first
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.Name.ToLower() != "appsettings.json") continue;

                ReadOptionsFromJsonFile(item.FileNames[0], webApp);
            }

            // Override any additional settings from the secrets.json file if it exists

            var userSecretsId = project.Properties.OfType<Property>()
                .FirstOrDefault(x => {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return x.Name.Equals("UserSecretsId", StringComparison.OrdinalIgnoreCase);
                }
                )?.Value as String;

            if (string.IsNullOrEmpty(userSecretsId)) return;

            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var secretsFile = Path.Combine(appdata, "Microsoft", "UserSecrets", userSecretsId, "secrets.json");

            ReadOptionsFromJsonFile(secretsFile, webApp);
        }

        private void ReadOptionsFromJsonFile(string path, WebApplication webApp)
        {
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var appSettings = JsonConvert.DeserializeAnonymousType(json,
                new { IsEncrypted = false, Values = new Dictionary<string, string>() });

            if (appSettings.Values != null && appSettings.Values.TryGetValue(SubdomainSettingName, out var subdomain))
            {
                webApp.SubDomain = subdomain;
            }
        }
    }
}
