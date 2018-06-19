//
// Copyright (c) 2018 Rodrigo Zechin Rosauro
//
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;
using CommandLine;
using JiraKanbanMetrics.Core;
using JiraKanbanMetrics.Core.Data;
using Newtonsoft.Json;

namespace JiraKanbanMetrics
{
    /// <summary>
    /// Main program entry-point
    /// </summary>
    internal class Program
    {
        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<ConfigureOptions, GenerateOptions>(args)
                .MapResult(
                    (ConfigureOptions opts) => Configure(opts),
                    (GenerateOptions opts) => Generate(opts),
                    errs => 1);
        }

        /// <summary>
        /// Executes the "generate" action
        /// </summary>
        /// <param name="opts">command-line options</param>
        /// <returns>exit code</returns>
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
            
            var issues = GetIssues(opts, config);

            var charts = KanbanCharts.Create(config, issues);

            SaveChart(opts, charts.FlowEfficiencyChart, "FlowEfficiencyChart.png");
            SaveChart(opts, charts.LeadTimeHistogramChart, "LeadTimeHistogramChart.png");
            SaveChart(opts, charts.WeeklyThroughputHistogramChart, "WeeklyThroughputHistogramChart.png");
            SaveChart(opts, charts.WeeklyThroughputChart, "WeeklyThroughputChart.png");
            SaveChart(opts, charts.LeadTimeControlChart, "LeadTimeControlChart.png");
            SaveChart(opts, charts.CumulativeFlowDiagramChart, "CumulativeFlowDiagramChart.png");
            
            Console.WriteLine("Success !!!");
            
            return 0;
        }

        private static void SaveChart(GenerateOptions opts, Chart chart, string filename)
        {
            chart.Width = opts.ChartsWidth;
            chart.Height = opts.ChartsHeight;
            chart.SaveImage(filename, ChartImageFormat.Png);
        }

        /// <summary>
        /// Executes the "configure" action
        /// </summary>
        /// <param name="opts">command-line options</param>
        /// <returns>exit code</returns>
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

        /// <summary>
        /// Retrieves issue metrics from the local cache or from Jira
        /// </summary>
        /// <param name="opts">command-line options</param>
        /// <param name="config">Kanban configuration</param>
        /// <returns>list of issue metrics</returns>
        private static Issue[] GetIssues(GenerateOptions opts, JiraKanbanConfig config)
        {
            var cacheFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)  ?? "./", ".cache");
            var version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            var cache = new Cache();
            var quickFilters = config.QuickFilters ?? new int[0];
            Issue[] issues = null;
            if (!opts.NoCache && File.Exists(cacheFile))
            {
                // Attempt to retrieve data from the cache
                try
                {
                    cache = JsonConvert.DeserializeObject<Cache>(File.ReadAllText(cacheFile, new UTF8Encoding(false)));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error reading cache: " + e.Message);
                }
                if (cache.Version != version)
                    cache = new Cache();
                
                // Delete old cached items
                var timeLimit = DateTime.UtcNow.AddHours(opts.CacheHours * -1);
                cache.Items.RemoveAll(c => c.TimestampUtc < timeLimit);

                // Try to find a matching cached item
                issues = cache.Items
                    .Where(c => c.BoardId == config.BoardId)
                    .Where(c => c.QuickFilters.SequenceEqual(quickFilters))
                    .Select(c => c.Issues)
                    .FirstOrDefault();
            }

            if (issues != null)
            {
                Console.WriteLine($"Retrieved data from local cache ({issues.Length} issues)");
            }
            else
            {
                // Data is not cached, retrieve it from Jira
                if (config.JiraPassword == null && !string.IsNullOrWhiteSpace(config.JiraUsername))
                {
                    Console.WriteLine($"Enter the Jira password for user '{config.JiraUsername}':");
                    config.JiraPassword = GetPassword();
                }
                using (var extractor = new MetricsExtractor(config, Console.WriteLine))
                {
                    issues = extractor.GetBoardDataAsync(config.BoardId, quickFilters).Result;
                }
                
                // Add to the cache
                cache.Items.Add(new CacheItem
                {
                    BoardId = config.BoardId,
                    QuickFilters = quickFilters,
                    Issues = issues,
                });
            }

            // Save the new cache
            try
            {
                if (!opts.NoCache)
                {
                    using (var f = File.OpenWrite(cacheFile))
                    {
                        var data = new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(cache));
                        f.Write(data, 0, data.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error writing disk cache: " + e.Message);
            }
            return issues;
        }


        /// <summary>
        /// Securely reads a password from the standard input
        /// </summary>
        /// <returns>the secure string</returns>
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
}
