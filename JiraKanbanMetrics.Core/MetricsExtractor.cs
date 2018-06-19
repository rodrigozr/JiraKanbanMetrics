using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using JiraKanbanMetrics.Core.Data;
using Newtonsoft.Json.Linq;

namespace JiraKanbanMetrics.Core
{
    /// <summary>
    /// Class used to extract metrics from a Jira Kanban Board
    /// </summary>
    /// <example>
    /// <code>
    /// var jiraBoardId = 123456;
    /// using (var extractor = new MetricsExtractor(config)) {
    ///     var data = await extractor.GetBoardDataAsync(jiraBoardId);
    ///     ...
    /// }
    /// </code>
    /// </example>
    public class MetricsExtractor : IDisposable
    {
        private readonly JiraKanbanConfig _config;
        private readonly Action<string> _logCallback;
        private HttpClient _client;

        /// <summary>
        /// Creates a new metrics extractor for the given Kanban configuration
        /// </summary>
        /// <param name="config">configuration to use</param>
        /// <param name="logCallback">optional callback delegate to receive log messages</param>
        public MetricsExtractor(JiraKanbanConfig config, Action<string> logCallback = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.JiraInstanceBaseAddress)) throw new ArgumentNullException(nameof(config.JiraInstanceBaseAddress));
            if (string.IsNullOrWhiteSpace(config.JiraUsername)) throw new ArgumentNullException(nameof(config.JiraUsername));
            if (config.JiraPassword == null || config.JiraPassword.Length == 0) throw new ArgumentNullException(nameof(config.JiraPassword));
            _config = config;
            _logCallback = logCallback;

            _client = new HttpClient
            {
                BaseAddress = new Uri(config.JiraInstanceBaseAddress)
            };

            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.JiraUsername}:{Crypto.GetStr(config.JiraPassword)}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        }

        /// <summary>
        /// Extracts raw issue metrics from the given Jira kanban board
        /// </summary>
        /// <param name="boardId">Jira kanban board ID</param>
        /// <param name="quickFilters">optional quick filter IDs to use</param>
        /// <returns>raw issue metrics, which can later be used with the charting APIs to generate charts</returns>
        public async Task<Issue[]> GetBoardDataAsync(int boardId, int[] quickFilters = null)
        {
            quickFilters = quickFilters ?? new int[0];
            var filters = quickFilters.Length == 0 ? "" : string.Join("&activeQuickFilters=", new[] { "" }.Concat(quickFilters.Select(_ => _.ToString())));
            _logCallback?.Invoke($"Connecting to Jira: {_config.JiraInstanceBaseAddress}");
            var res = await _client.GetAsync($"rest/greenhopper/1.0/xboard/work/allData.json?rapidViewId={boardId}{filters}").ConfigureAwait(false);
            var responseBody = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            res.EnsureSuccessStatusCode();
            var json = JObject.Parse(responseBody);

            var issues = await GetIssues(json).ConfigureAwait(false);
            // Filter ignored types and ignored issues
            issues = issues
                .Where(issue => !_config.IgnoredIssueKeys.Any(key => issue.IssueKey.EqualsIgnoreCase(key)))
                .Where(issue => !_config.IgnoredIssueTypes.Any(type => issue.IssueType.EqualsIgnoreCase(type)));
            return issues.ToArray();
        }

        private static Column[] GetIssueColumns(JObject issue, Dictionary<string, int[]> boardColumns)
        {
            var statusToColumn = boardColumns.SelectMany(kv => kv.Value.Select(id => (id, kv.Key))).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

            var transitions = (
                from history in issue["changelog"]["histories"]
                from item in history["items"]
                where (string)item["field"] == "status"
                let fromId = (int)item["from"]
                let toId = (int)item["to"]
                let fromColumn = statusToColumn.ContainsKey(fromId) ? statusToColumn[fromId] : null
                let toColumn = statusToColumn.ContainsKey(toId) ? statusToColumn[toId] : null
                where fromColumn != toColumn
                select new
                {
                    Timestamp = ((DateTime)history["created"]).ToUniversalTime(),
                    From = fromColumn,
                    To = toColumn,
                }).ToList();

            var columns = (
                from name in boardColumns.Keys
                let allEntered = transitions.Where(_ => _.To == name).OrderBy(_ => _.Timestamp).Select(_ => _.Timestamp).ToList()
                let allExited = transitions.Where(_ => _.From == name).OrderBy(_ => _.Timestamp).Select(_ => _.Timestamp).Concat(new[] { DateTime.MaxValue }).ToList()
                let entered = transitions.Where(_ => _.To == name).OrderByDescending(_ => _.Timestamp).Select(_ => (DateTime?)_.Timestamp).FirstOrDefault()
                let exited = transitions.Where(_ => _.From == name).OrderByDescending(_ => _.Timestamp).Select(_ => (DateTime?)_.Timestamp).FirstOrDefault()
                select new Column
                {
                    Name = name,
                    Entered = entered,
                    Exited = exited,
                    Transitions = allEntered.Select(e => new DateRange
                    {
                        Entered = e,
                        Exited = allExited.First(date => date >= e)
                    }).ToArray()
                }
            ).ToArray();

            return columns;
        }

        private static Dictionary<string, int[]> GetAllColumnsStatusIds(JObject json)
        {
            return json["columnsData"]["columns"]
                .Select(col => new KeyValuePair<string, int[]>((string)col["name"], col["statusIds"].ToObject<int[]>()))
                .ToDictionary(_ => _.Key, _ => _.Value);
        }

        private async Task<IEnumerable<Issue>> GetIssues(JObject json)
        {
            var boardColumns = GetAllColumnsStatusIds(json);

            var issueKeys = json["issuesData"]["issues"].Select(issue => (string)issue["key"]).OrderBy(_ => _).ToList();
            _logCallback?.Invoke($"Analysing {issueKeys.Count} issues...");
            var pageSize = 100;
            var pagedKeys = issueKeys.Paged(pageSize);
            var result = new List<Issue>();
            int retrievedQty = 0;
            foreach (var keys in pagedKeys)
            {
                var keysJql = string.Join(",", keys);
                var res = await _client.GetAsync($"rest/api/2/search?jql=key%20in%20({keysJql})&expand=changelog&fields=issuetype,created&maxResults={pageSize}").ConfigureAwait(false);
                var responseBody = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                res.EnsureSuccessStatusCode();
                var issuesJson = JObject.Parse(responseBody);
                retrievedQty += issuesJson["issues"].Count();
                _logCallback?.Invoke($"Retrieved details for {retrievedQty} issues...");
                result.AddRange(issuesJson["issues"]
                    .Select(issue => new Issue
                    {
                        IssueKey = (string)issue["key"],
                        IssueType = (string)issue["fields"]["issuetype"]["name"],
                        Created = ((DateTime)issue["fields"]["created"]).ToUniversalTime(),
                        Columns = GetIssueColumns((JObject)issue, boardColumns),
                    }));
            }
            _logCallback?.Invoke($"Processing retrieved data...");
            return result;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _client?.Dispose();
            _client = null;
        }
    }
}
