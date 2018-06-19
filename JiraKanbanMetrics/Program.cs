using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using JiraKanbanMetrics.Core;
using JiraKanbanMetrics.Core.Data;

namespace JiraKanbanMetrics
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<ConfigureOptions, GenerateOptions>(args)
                .MapResult(
                    (ConfigureOptions opts) => Configure(opts),
                    (GenerateOptions opts) => Generate(opts),
                    errs => 1);
        }

        private static int Generate(GenerateOptions opts)
        {
            if (!File.Exists(opts.ConfigFile))
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Configuration file does not exist: {opts.ConfigFile}");
                Console.ForegroundColor = color;
                return 1;
            }
            var config = JiraKanbanConfig.ParseXml(opts.ConfigFile);
            if (opts.BoardId != null)
                config.BoardId = opts.BoardId.Value;
            
            if (config.JiraPassword == null && !string.IsNullOrWhiteSpace(config.JiraUsername))
            {
                Console.WriteLine($"Enter the Jira password for user '{config.JiraUsername}':");
                config.JiraPassword = GetPassword();
            }

            Issue[] issues;
            using (var extractor = new MetricsExtractor(config, Console.WriteLine))
            {
                issues = extractor.GetBoardDataAsync(config.BoardId, config.QuickFilters).Result;
            }

            var charts = KanbanCharts.Create(config, issues);
            Console.WriteLine("Success !!!");
            
            return 0;
        }

        private static int Configure(ConfigureOptions opts)
        {
            var config = File.Exists(opts.ConfigFile)
                ? JiraKanbanConfig.ParseXml(opts.ConfigFile)
                : new JiraKanbanConfig();

            if (opts.JiraUsername != null)
                config.JiraUsername = opts.JiraUsername;
            if (opts.JiraInstanceBaseAddress != null)
                config.JiraInstanceBaseAddress = opts.JiraInstanceBaseAddress;
            if (opts.BoardId != null)
                config.BoardId = opts.BoardId.Value;

            if (!opts.NoStorePassword && !string.IsNullOrWhiteSpace(config.JiraUsername))
            {
                Console.WriteLine($"Enter the Jira password for user '{config.JiraUsername}':");
                config.JiraPassword = GetPassword();
            }

            var xml = config.ToXml();
            using (var f = File.OpenWrite(opts.ConfigFile))
                xml.Save(f);
            
            Console.WriteLine($"Configuration file generated at: {opts.ConfigFile}");
            return 0;
        }

        public static SecureString GetPassword()
        {
            var pwd = new SecureString();
            while (true)
            {
                var i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                    break;
                if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length <= 0) continue;
                    pwd.RemoveAt(pwd.Length - 1);
                    Console.Write("\b \b");
                }
                else if (i.KeyChar != '\0')
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }
            Console.WriteLine();
            return pwd;
        }
    }

    [Verb("configure", HelpText = "Creates or updates a configuration file")]
    class ConfigureOptions
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

    [Verb("generate", HelpText = "Generates metrics")]
    class GenerateOptions
    {
        [Option("ConfigFile", HelpText = "Configuration file path", Required = true)]
        public string ConfigFile { get; set; }

        [Option("BoardId", HelpText = "Jira Kanban Board ID")]
        public int? BoardId { get; set; }
    }
}
