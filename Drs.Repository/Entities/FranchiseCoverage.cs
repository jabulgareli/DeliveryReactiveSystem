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
    
    public partial class FranchiseCoverage
    {
        public FranchiseCoverage()
        {
            this.FranchiseCoverageLog = new HashSet<FranchiseCoverageLog>();
        }
    
        public int FranchiseId { get; set; }
        public string LastConfig { get; set; }
        public string StoresCoverage { get; set; }
        public string LastUserId { get; set; }
        public System.DateTime TimestampInsUpd { get; set; }
    
        public virtual Franchise Franchise { get; set; }
        public virtual ICollection<FranchiseCoverageLog> FranchiseCoverageLog { get; set; }
    }
}
