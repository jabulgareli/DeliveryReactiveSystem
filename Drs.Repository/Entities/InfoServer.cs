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
    
    public partial class InfoServer
    {
        public int InfoServerId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public Nullable<int> InfoCallCenterId { get; set; }
    
        public virtual InfoCallCenter InfoCallCenter { get; set; }
    }
}
