using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using posTest.Models;


namespace posTest.ViewModels
{
    public class OdemeViewModel
    {
        public List<Uyeler> uyeListesi { get; set; }

        public List<Odemeler> odemeListesi { get; set; }

        public List<OdemeIstekleri> odemeIstekleriListesi { get; set; }

        public Firmalar Secilen { get; set; }

        public List<Bankalar> bankaListesi { get; set; }

        public List<Loglar> loglarListesi { get; set; }

        public Odemeler Odeme { get; set; }
    }
}