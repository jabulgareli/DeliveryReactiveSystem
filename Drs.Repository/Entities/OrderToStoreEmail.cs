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
    
    public partial class OrderToStoreEmail
    {
        public long OrderToStoreEmailId { get; set; }
        public long OrderToStoreId { get; set; }
        public int TriesToSend { get; set; }
        public bool IsSent { get; set; }
        public Nullable<System.DateTime> SendTimestamp { get; set; }
    
        public virtual OrderToStore OrderToStore { get; set; }
    }
}
