﻿/*
 *   Copyright (c) 2018 CAST
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
using System.Runtime.Serialization;
using System.Linq;
using System.Resources;

namespace CastReporting.Domain
{
    /// <summary>
    /// Represents a snapshot.
    /// </summary>
    [DataContract(Name = "snapshot")]
    public class Snapshot : CRObject
    {
        #region Serialized properties


        /// <summary>
        /// 
        /// </summary>
        [DataMember(Name = "Number")]
        protected int Number { get; set; }


        /// <summary>
        /// Get/Set technologies name.
        /// </summary>
        [DataMember(Name = "annotation")]
        public Annotation Annotation { get; set; }

       
        #endregion  Serialized properties       


        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Module> Modules { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ApplicationResult> BusinessCriteriaResults { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ApplicationResult> TechnicalCriteriaResults { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ApplicationResult> SizingMeasuresResults { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ApplicationResult> QualityDistributionsResults { get; set; }

      

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ApplicationResult> QualityRulesResults { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ApplicationResult> QualityMeasuresResults { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<QIBusinessCriteria> QIBusinessCriterias { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<QIQualityRules> QIQualityRules { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ApplicationResult> CostComplexityResults { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ActionPlan> ActionsPlan { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Name} - {((Annotation != null) ? Annotation.Version : string.Empty)}";
        }

        // ReSharper disable once AssignNullToNotNullAttribute
        public long Id => long.Parse(Href.Split('/').LastOrDefault());

        public string GetId()
        {
            return Href.Split('/').LastOrDefault();
        }
    }
}
