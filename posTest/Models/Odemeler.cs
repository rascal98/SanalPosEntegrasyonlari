//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace posTest.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Odemeler
    {
        public int Id { get; set; }
        public Nullable<int> FirmaId { get; set; }
        public Nullable<int> UyeId { get; set; }
        public string AdSoyad { get; set; }
        public string FirmaUnvan { get; set; }
        public string Telefon { get; set; }
        public string Email { get; set; }
        public string Tutar { get; set; }
        public Nullable<System.DateTime> Tarih { get; set; }
        public string Hata { get; set; }
        public Nullable<bool> Iade { get; set; }
        public Nullable<bool> Satis { get; set; }
        public Nullable<bool> Onay { get; set; }
        public string Taksit { get; set; }
        public string SiparisNo { get; set; }
    
        public virtual Firmalar Firmalar { get; set; }
    }
}