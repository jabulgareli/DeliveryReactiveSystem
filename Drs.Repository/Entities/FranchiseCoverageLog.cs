//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Drs.Repository.Entities
{
    using System;
    using System.Collections.Generic;
    
    public partial class FranchiseCoverageLog
    {
        public int FranchiseCoverageLogId { get; set; }
        public int FranchiseId { get; set; }
        public string LastConfig { get; set; }
        public string StoresCoverage { get; set; }
        public string UserIdLog { get; set; }
        public System.DateTime TimestampLog { get; set; }
    
        public virtual FranchiseCoverage FranchiseCoverage { get; set; }
    }
}
