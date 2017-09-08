﻿using System.Collections.Generic;
using System.Linq;
using CastReporting.BLL.Computing;
using CastReporting.Reporting.Atrributes;
using CastReporting.Reporting.Builder.BlockProcessing;
using CastReporting.Reporting.ReportingModel;
using CastReporting.Domain;
using CastReporting.Reporting.Helper;
using CastReporting.Reporting.Languages;

namespace CastReporting.Reporting.Block.Table
{
    [Block("ACTION_PLAN_VIOLATIONS")]
    public class ActionPlanViolations : TableBlock
    {
        public override TableDefinition Content(ReportData reportData, Dictionary<string, string> options)
        {
            List<string> rowData = new List<string>();

            bool shortName = options.GetOption("NAME", "FULL") == "SHORT";
            int nbLimitTop = options.GetOption("COUNT") == "ALL" ? -1 : options.GetIntOption("COUNT", 10);
            string filter = options.GetOption("FILTER", "ALL");

            rowData.Add(Labels.RuleName);
            rowData.Add(Labels.ObjectName);
            rowData.Add(Labels.Comment);
            rowData.Add(Labels.Priority);
            rowData.Add(Labels.Status);
            rowData.Add(Labels.LastUpdated);

            IEnumerable<Violation> results = reportData.SnapshotExplorer.GetViolationsInActionPlan(reportData.CurrentSnapshot.Href, nbLimitTop);

            switch (filter)
            {
                case "ADDED":
                    results = results.Where(_ => _.RemedialAction.Status.Equals("added"));
                    break;
                case "PENDING":
                    results = results.Where(_ => _.RemedialAction.Status.Equals("pending"));
                    break;
                case "SOLVED":
                    results = results.Where(_ => _.RemedialAction.Status.Equals("solved"));
                    break;
            }

            var _violations = results as IList<Violation> ?? results.ToList();
            foreach (Violation _violation in _violations)
            {
                rowData.Add(_violation.RulePattern.Name ?? Constants.No_Value);
                rowData.Add(shortName ? _violation.Component.ShortName : _violation.Component.Name ?? Constants.No_Value);
                rowData.Add(_violation.RemedialAction.Comment ?? Constants.No_Value);
                rowData.Add(_violation.RemedialAction.Priority ?? Constants.No_Value);
                rowData.Add(_violation.RemedialAction.Status ?? Constants.No_Value);
                rowData.Add(_violation.RemedialAction.Dates.Updated.DateSnapShot?.ToString(Labels.FORMAT_LONG_DATE) ?? Constants.No_Value);
            }

            var table = new TableDefinition
            {
                HasRowHeaders = false,
                HasColumnHeaders = true,
                NbRows = _violations.Count() + 1,
                NbColumns = 6,
                Data = rowData
            };

            return table;

        }
    }
}