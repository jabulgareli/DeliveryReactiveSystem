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
    
    public partial class FranchiseStoreOffLine
    {
        public int FranchiseStoreOffLineId { get; set; }
        public int FranchiseStoreId { get; set; }
        public System.DateTime DateTimeStart { get; set; }
        public int Duration { get; set; }
        public System.DateTime DateTimeEnd { get; set; }
        public string UserInsId { get; set; }
        public string UserUpdId { get; set; }
        public bool IsUndefinedOfflineTime { get; set; }
        public bool IsObsolete { get; set; }
    
        public virtual AspNetUsers AspNetUsers { get; set; }
        public virtual AspNetUsers AspNetUsers1 { get; set; }
        public virtual FranchiseStore FranchiseStore { get; set; }
    }
}
