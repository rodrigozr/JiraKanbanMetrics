using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using JiraKanbanMetrics.Core.Data;

namespace JiraKanbanMetrics.Core
{
    /// <summary>
    /// Responsible for generating Kanban charts
    /// </summary>
    public class KanbanCharts
    {
        public JiraKanbanConfig Config { get; set; }
        public Issue[] Issues { get; set; }
        public DoneIssue[] DoneIssues { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Chart FlowEfficiencyChart { get; set; }
        public Chart LeadTimeHistogramChart { get; set; }
        public Chart WeeklyThroughputHistogramChart { get; set; }
        public Chart WeeklyThroughputChart { get; set; }
        public Chart LeadTimeControlChart { get; set; }
        public Chart CummulativeFlowDiagramChart { get; set; }

        /// <summary>
        /// Create charts for the given configuration and list of issues
        /// </summary>
        /// <param name="config">Jira Kanban configuration</param>
        /// <param name="issues">list of issues</param>
        /// <returns>Kanban charts</returns>
        public static KanbanCharts Create(JiraKanbanConfig config, Issue[] issues)
        {
            var c = new KanbanCharts
            {
                Config = config,
                Issues = issues,
                StartDate = DateTime.Today.AddMonths(config.MonthsToAnalyse * -1),
                EndDate = DateTime.Today.AddDays((int)DateTime.Today.DayOfWeek * -1),
            };

            // Consolidate information about tickets that are done
            c.DoneIssues = issues
                .Where(ticket => ticket.Entered(config.CommitmentStartColumns).HasValue)
                .Where(ticket => ticket.Entered(config.InProgressStartColumns).HasValue)
                .Where(ticket => ticket.Entered(config.DoneColumns).HasValue)
                .Select(ticket => new DoneIssue
                {
                    IssueKey = ticket.IssueKey,
                    IssueType = ticket.IssueType,
                    LeadTime = ticket.LeadTime(config),
                    TouchTime = ticket.TouchTime(config),
                    QueueTime = ticket.QueueTime(config),
                    Columns = ticket.Columns,
                    // ReSharper disable PossibleInvalidOperationException
                    ToDo = ticket.Entered(config.CommitmentStartColumns).Value,
                    InProgress = ticket.Entered(config.InProgressStartColumns).Value,
                    Done = ticket.FirstEntered(config.DoneColumns).Value,
                    // ReSharper restore PossibleInvalidOperationException
                })
                .Where(ticket => ticket.Done >= c.StartDate)
                .Where(ticket => ticket.Done <= c.EndDate)
                .ToArray();

            c.ComputeAllCharts();
            return c;
        }

        private void ComputeAllCharts()
        {
		    ComputeFlowEfficiency();
		    ComputeLeadTimeHistogram();
		    ComputeThroughput();
		    ComputeControlChart();
		    ComputeCfd();
        }

        private void ComputeFlowEfficiency()
        {
            var flowEfficiency = DoneIssues
                .GroupBy(ticket => ticket.Done.YearWeek())
                .Where(g => g.Sum(_ => _.LeadTime) > 0)
                .Select(g => new
                {
                    Week = g.Key,
                    Tickets = g.Count(),
                    AverageLeadTime = (int) Math.Round(g.Average(_ => _.LeadTime), 0),
                    FlowEfficiency = Math.Round(g.Sum(_ => _.TouchTime) / (double) g.Sum(_ => _.LeadTime) * 100D, 1)
                })
                .OrderBy(_ => _.Week)
                .ToList();
            // Chart
            {
                var chart = new Chart();
                var chartArea = new ChartArea();
                chart.ChartAreas.Add(chartArea);
                var series = new Series {ChartType = SeriesChartType.Line};
                series.Points.DataBindXY(flowEfficiency.Select(_ => _.Week).ToArray(),
                    flowEfficiency.Select(_ => _.FlowEfficiency).ToArray());
                chart.Series.Add(series);
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.LabelStyle.Angle = -65;
                chartArea.AxisY.Title = "Flow efficiency %";
                chartArea.AxisY.Maximum = 100;

                var average = flowEfficiency.Count > 0 ? flowEfficiency.Average(_ => _.FlowEfficiency) : 0;
                chartArea.AxisY.StripLines.Add(CreateStripLine(average,
                    Color.Blue));
                FlowEfficiencyChart = chart;
            }
        }

        private void ComputeLeadTimeHistogram()
        {
            var maxLeadTime = DoneIssues.Length > 0 ? DoneIssues.Max(_ => _.LeadTime) : 0;
            var buckets = Enumerable.Range(0, maxLeadTime + 1).Paged(3).Select(_ => new { Start = _.First(), End = _.Last() }).ToList();
            var leadHistogram = buckets.Select(bucket => new
                {
                    LeadTime = $"{bucket.Start}-{bucket.End}",
                    Qty = DoneIssues.Where(ticket => ticket.LeadTime >= bucket.Start && ticket.LeadTime <= bucket.End).Count(ticket => ticket.IssueType != "Defect" && ticket.IssueType != "Issue"),
                    QtyIssues = DoneIssues.Where(ticket => ticket.LeadTime >= bucket.Start && ticket.LeadTime <= bucket.End).Count(ticket => ticket.IssueType == "Defect" || ticket.IssueType == "Issue"),
                })
                .ToList();
            {
                var chart = new Chart();
                var chartArea = new ChartArea();
                chart.ChartAreas.Add(chartArea);
                chart.Legends.Add("legend").Docking = Docking.Bottom;
                var series = new Series {ChartType = SeriesChartType.Column};
                series.Points.DataBindXY(leadHistogram.Select(_ => _.LeadTime).ToArray(), leadHistogram.Select(_ => _.Qty).ToArray());
                series.Legend = "legend";
                series.LegendText = "Tasks";
                chart.Series.Add(series);
                series = new Series {ChartType = SeriesChartType.Column};
                series.Points.DataBindXY(leadHistogram.Select(_ => _.LeadTime).ToArray(), leadHistogram.Select(_ => _.QtyIssues).ToArray());
                series.Legend = "legend";
                series.LegendText = "Defects";
                series.Color = Color.Red;
                chart.Series.Add(series);
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.LabelStyle.Angle = -65;
                chartArea.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                LeadTimeControlChart = chart;
            }

        }

        private void ComputeThroughput()
        {
            var throughput = DoneIssues
                .GroupBy(ticket => ticket.Done.YearWeek())
                .Select(g => new
                {
                    Week = g.Key,
                    Throughput = g.Count(),
                    Value = g.Count(_ => !_.IssueType.IsDefect(Config)),
                    Defects = g.Count(_ => _.IssueType.IsDefect(Config)),
                })
                .Select(g => new
                {
                    g.Week,
                    g.Throughput,
                    g.Value,
                    g.Defects,
                    FailureDemand = Math.Round((g.Defects / (float) g.Throughput) * 100, 1)
                })
                .OrderBy(_ => _.Week)
                .ToList();
            var maxThroughput = throughput.Count > 0 ? throughput.Max(_ => _.Throughput) : 0;
            var buckets = Enumerable.Range(0, maxThroughput + 1).Paged(4)
                .Select(_ => new {Start = _.First(), End = _.Last()}).ToList();
            var throughputHistogram = buckets.Select(bucket => new
                {
                    WeeklyThroughput = $"{bucket.Start}-{bucket.End}",
                    NumberOfWeeks = throughput.Count(_ => _.Throughput >= bucket.Start && _.Throughput <= bucket.End),
                })
                .ToList();
            {
                var chart = new Chart();
                var chartArea = new ChartArea();
                chart.ChartAreas.Add(chartArea);
                var series = new Series {ChartType = SeriesChartType.Column};
                series.Points.DataBindXY(throughputHistogram.Select(_ => _.WeeklyThroughput).ToArray(),
                    throughputHistogram.Select(_ => _.NumberOfWeeks).ToArray());
                chart.Series.Add(series);
                chartArea.AxisY.Title = "Quantity of weeks";
                chartArea.AxisX.Title = "Throughput range";
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.LabelStyle.Angle = -65;
                WeeklyThroughputHistogramChart = chart;
            }
            {
                var chart = new Chart();
                var chartArea = new ChartArea();
                chart.ChartAreas.Add(chartArea);
                chart.Legends.Add("legend").Docking = Docking.Bottom;
                {
                    var series = new Series {ChartType = SeriesChartType.Line};
                    series.Points.DataBindXY(throughput.Select(_ => _.Week).ToArray(),
                        throughput.Select(_ => _.Throughput).ToArray());
                    series.Legend = "legend";
                    series.LegendText = "Throughput (tasks + defects)";
                    series.BorderWidth = 3;
                    chart.Series.Add(series);
                }
                {
                    var series = new Series {ChartType = SeriesChartType.Line};
                    series.Points.DataBindXY(throughput.Select(_ => _.Week).ToArray(),
                        throughput.Select(_ => _.Defects).ToArray());
                    series.Legend = "legend";
                    series.LegendText = "Defects";
                    series.Color = Color.Red;
                    chart.Series.Add(series);
                }
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.LabelStyle.Angle = -65;
                chartArea.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;

                var average = throughput.Count > 0 ? throughput.Average(_ => _.Throughput) : 0;
                chartArea.AxisY.StripLines.Add(CreateStripLine(average, Color.Blue));
                WeeklyThroughputChart = chart;
            }
        }

        private void ComputeControlChart()
        {
            var controlChart = DoneIssues.OrderBy(ticket => ticket.Done)
                .Select(ticket => new
                {
                    ticket.IssueKey,
                    ticket.IssueType,
                    Done = ticket.Done.ToString("yyyy-MM-dd"),
                    ticket.LeadTime
                })
                .ToList();
            {
                var chart = new Chart();
                var chartArea = new ChartArea();
                var start = DoneIssues.Length > 0 ? DoneIssues.Min(_ => _.Done) : StartDate;
                var max = DoneIssues.Length > 0 ? DoneIssues.Max(_ => _.Done) : EndDate;
                var numberOfDays = (int) (max - start).TotalDays + 1;
                var xindex = Enumerable.Range(0, numberOfDays).Select(i => start.AddDays(i).ToString("yyyy-MM-dd"))
                    .Distinct().OrderBy(_ => _).ToList();
                chart.ChartAreas.Add(chartArea);
                chart.Legends.Add("legend").Docking = Docking.Bottom;
                {
                    var tickets = controlChart.Where(_ => !_.IssueType.IsDefect(Config));
                    var series = new Series();
                    series.ChartType = SeriesChartType.Point;
                    foreach (var g in tickets)
                        series.Points.Add(new DataPoint()
                        {
                            AxisLabel = g.Done,
                            XValue = xindex.IndexOf(g.Done),
                            YValues = new double[] {g.LeadTime}
                        });
                    series.CustomProperties = "IsXAxisQuantitative=True";
                    series.Legend = "legend";
                    series.LegendText = "Tasks";
                    series.Color = Color.Blue;
                    chart.Series.Add(series);
                }
                {
                    var tickets = controlChart.Where(_ => _.IssueType.IsDefect(Config));
                    var series = new Series {ChartType = SeriesChartType.Point};
                    foreach (var g in tickets)
                        series.Points.Add(new DataPoint()
                        {
                            AxisLabel = g.Done,
                            XValue = xindex.IndexOf(g.Done),
                            YValues = new double[] {g.LeadTime}
                        });
                    series.CustomProperties = "IsXAxisQuantitative=True";
                    series.Legend = "legend";
                    series.LegendText = "Issues";
                    series.Color = Color.Red;
                    chart.Series.Add(series);
                }


                var avg = controlChart.Count > 0 ? controlChart.Average(_ => _.LeadTime) : 0;
                var p95 = controlChart.Count > 0 ? controlChart.Percentile(95, _ => _.LeadTime) : 0;
                var p90 = controlChart.Count > 0 ? controlChart.Percentile(90, _ => _.LeadTime) : 0;
                chartArea.AxisY.StripLines.Add(CreateStripLine(avg, Color.Blue, $"Average ({avg:0})"));
                chartArea.AxisY.StripLines.Add(CreateStripLine(p95, Color.Violet, $"95th percentile ({p95:0})"));
                chartArea.AxisY.StripLines.Add(CreateStripLine(p90, Color.Brown, $"90th percentile ({p90:0})"));

                chartArea.AxisX.Interval = 2;
                chartArea.AxisX.LabelStyle.Angle = -65;
                chartArea.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                chartArea.AxisY.Title = "Lead time";
                LeadTimeControlChart = chart;
            }
        }

        private void ComputeCfd()
        {
            var cumulativeFlow = Issues
                .Where(ticket => ticket.Entered(Config.CommitmentStartColumns).HasValue)
                .Where(ticket => ticket.Entered(Config.InProgressStartColumns).HasValue)
                .Where(ticket => ticket.Entered(Config.DoneColumns).HasValue)
                .Where(ticket => ticket.Entered(Config.DoneColumns) >= StartDate)
                // ReSharper disable once PossibleInvalidOperationException
                .Select(ticket => ticket.Entered(Config.DoneColumns).Value.GetWeekEndDate())
                .Distinct()
                .OrderBy(_ => _)
                .Select(weekEndDate => CumulativeFlow(weekEndDate, Issues))
                .OrderBy(_ => _["Week"])
                .ToList();
            {
                var floor = cumulativeFlow.Count > 0 ? cumulativeFlow.Min(_ => (int)_["Done"]) : 0;
                var keys = cumulativeFlow.Count > 0 ? cumulativeFlow.First().Keys.Except(new[] { "Week" }).Reverse().ToList() : new List<string>();
                var chart = new Chart();
                var chartArea = new ChartArea();
                chart.ChartAreas.Add(chartArea);
                chart.Legends.Add("legend").Docking = Docking.Bottom;
                foreach (var key in keys)
                {
                    var series = new Series {ChartType = SeriesChartType.StackedArea};
                    series.Points.DataBindXY(cumulativeFlow.Select(_ => _["Week"]).ToArray(),
                        cumulativeFlow.Select(_ => (int)_[key] - (key == "Done" ? floor : 0))
                            .ToArray());
                    series.Legend = "legend";
                    series.LegendText = key;
                    chart.Series.Add(series);
                }
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.LabelStyle.Angle = -65;
                chartArea.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                chartArea.AxisY.Title = "Number of items";
                CummulativeFlowDiagramChart = chart;
            }
        }

        // Key -> Column name
        // Value -> Quantity of tickets for that week & column
        public IDictionary<string, object> CumulativeFlow(DateTime weekEndDate, Issue[] tickets)
        {
            var nextWeekStart = weekEndDate.AddDays(1);
            IDictionary<string, object> flow = new ExpandoObject();
            flow["Week"] = weekEndDate.YearWeek();
            foreach (var col in tickets[0].Columns.Select(_ => _.Name))
            {
                flow[col] = 0;
            }
            foreach (var ticket in tickets)
            {
                if (ticket.Created >= nextWeekStart)
                    continue;
                var col = ticket.Columns
                              .Where(c => c.Transitions.Any(t => t.IsDateWithin(weekEndDate)))
                              .Select(c => c.Name)
                              .FirstOrDefault() ?? Config.BacklogColumnName;
                flow[col] = (int)flow[col] + 1;
            }
            return flow;
        }

        public StripLine CreateStripLine(double value, Color color, string text = "Average")
        {
            return new StripLine
            {
                Interval = 0,
                IntervalOffset = value,
                StripWidth = 0.01,
                BackColor = color,
                Text = text
            };
        }

    }
}