﻿/*
 *   Copyright (c) 2016 CAST
 *
 * Licensed under a custom license, Version 1.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License, accessible in the main project
 * source code: Empowerment.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System.Collections.Generic;
using System.Linq;
using CastReporting.Domain;
using CastReporting.Reporting.Atrributes;
using CastReporting.Reporting.Builder.BlockProcessing;
using CastReporting.Reporting.ReportingModel;
using CastReporting.Reporting.Helper;
using CastReporting.BLL.Computing;

namespace CastReporting.Reporting.Block.Text
{

    [Block("RULE_FAILED_CHECKS")]
    public class FailedChecksByRule : TextBlock
    {
        #region METHODS
        public override string Content(ReportData reportData, Dictionary<string, string> options)
        {
            string strRuleId = options.GetOption("RULID", string.Empty);
            string _snapshot = options.GetOption("SNAPSHOT", "CURRENT");

            if (reportData?.CurrentSnapshot == null || strRuleId == string.Empty) return Constants.No_Value;
            Result violations;
            int? failedChecks = null;

            if (_snapshot == "PREVIOUS" && reportData.PreviousSnapshot != null)
            {
                violations = reportData.RuleExplorer.GetRulesViolations(reportData.PreviousSnapshot.Href, strRuleId).FirstOrDefault();
            }
            else
            {
                violations = reportData.RuleExplorer.GetRulesViolations(reportData.CurrentSnapshot.Href, strRuleId).FirstOrDefault();
            }

            if (violations != null && violations.ApplicationResults.Any())
            {
                failedChecks = RulesViolationUtility.GetFailedChecks(violations);
            }

            return (failedChecks != null) ? failedChecks.Value.ToString("N0") : Constants.No_Value;
        }
        #endregion METHODS
    }
}


