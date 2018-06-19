//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using CommandLine;

namespace JiraKanbanMetrics
{
    /// <summary>
    /// Command-line options for the "generate" action
    /// </summary>
    [Verb("generate", HelpText = "Generates metrics")]
    internal class GenerateOptions
    {
        [Option("ConfigFile", HelpText = "Configuration file path", Required = true)]
        public string ConfigFile { get; set; }

        [Option("BoardId", HelpText = "Jira Kanban Board ID (when set, overrides the one in the configuration file)")]
        public int? BoardId { get; set; }

        [Option("NoCache", HelpText = "Disables local disk caching of Jira data")]
        public bool NoCache { get; set; }

        [Option("CacheHours", Default = 2, HelpText = "Number of hours to keep cached data on disk")]
        public int CacheHours { get; set; }

        [Option("ChartsWidth", Default = 1240, HelpText = "Width of the charts, in pixels")]
        public int ChartsWidth { get; set; }

        [Option("ChartsHeight", Default = 780, HelpText = "Height of the charts, in pixels")]
        public int ChartsHeight { get; set; }
    }
}