//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using CommandLine;

namespace JiraKanbanMetrics
{
    /// <summary>
    /// Command-line options for the "configure" action
    /// </summary>
    [Verb("configure", HelpText = "Creates or updates a configuration file")]
    internal class ConfigureOptions
    {
        [Option("ConfigFile", HelpText = "Configuration file path", Required = true)]
        public string ConfigFile { get; set; }

        [Option("JiraUsername", HelpText = "Jira User name (when not set: ask every time)")]
        public string JiraUsername { get; set; }
        
        [Option("NoStorePassword", HelpText = "Indicates that an an encrypted password should NOT be stored in the configuration file or not. The password will be requested every time")]
        public bool NoStorePassword { get; set; }
        
        [Option("JiraInstanceBaseAddress", HelpText = "Base URL for your Jira instance")]
        public string JiraInstanceBaseAddress { get; set; }

        [Option("BoardId", HelpText = "Jira Kanban Board ID")]
        public int? BoardId { get; set; }
    }
}