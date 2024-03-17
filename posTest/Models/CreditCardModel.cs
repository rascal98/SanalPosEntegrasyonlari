using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace posTest.Models
{
    public class CreditCardModel
    {
        public string HolderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpireMonth { get; set; }
        public string ExpireYear { get; set; }
        public string CV2 { get; set; }

        //
        public string Amount { get; set; }

        public string Taksit { get; set; }

        public string AdSoyad { get; set; }

        public string Firma { get; set; }

        public string Telefon { get; set; }
        
        public string Email { get; set; }

        public string Banka { get; set; }

        public string Hata { get; set; }

        //

        public string renk { get; set; }

        public string logo { get; set; }

        public string firmaAd { get; set; }

        public string adres { get; set; }

        public string telefon2 { get; set; }

        public string webAdres { get; set; }

        public bool uyeGirisi { get; set; }

        public bool iyzico { get; set; }

        public List<Bankalar> secilenBankalar { get; set; }

        public List<FirmaTaksitleri> secilenTaksitler { get; set; }

        public List<Sozlesmeler> sozlesmeler { get; set; }

    }
}