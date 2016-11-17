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
using System;
using System.Collections.Generic;
using System.Linq;
using CastReporting.Reporting.Atrributes;
using CastReporting.Reporting.Builder.BlockProcessing;
using CastReporting.Reporting.ReportingModel;
using CastReporting.BLL.Computing;
using CastReporting.Domain;
using CastReporting.Reporting.Helper;

namespace CastReporting.Reporting.Block.Text
{
	[Block("APPLICATION_RULE"), Block("APPLICATION_METRIC")]
    class ApplicationRule : TextBlock
    {
        #region METHODS
        protected override string Content(ReportData reportData, Dictionary<string, string> options)
        {
          
            int metricId = options.GetIntOption("ID", 0);
            string _snapshot = options.GetOption("SNAPSHOT", "CURRENT");
           
            if (null != reportData &&
                null != reportData.CurrentSnapshot && metricId != 0)
            {
                if (_snapshot == "PREVIOUS" && reportData.PreviousSnapshot != null)
                {
                    double? result = BusinessCriteriaUtility.GetMetricValue(reportData.PreviousSnapshot, metricId);
                    return result.HasValue ? result.Value.ToString("N2") : Constants.No_Value;
                }
                else
                {
                    double? result = BusinessCriteriaUtility.GetMetricValue(reportData.CurrentSnapshot, metricId);
                    return result.HasValue ? result.Value.ToString("N2") : Constants.No_Value;
                }
            }
            return CastReporting.Domain.Constants.No_Value;
        }
        #endregion METHODS
    }
}
