using _PosnetDotNetModule;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using posTest.Models;
using PosnetEncryption;
using ThreeDPayment.Helpers;
using _PosnetDotNetTDSOOSModule;
using System.Collections;
using System.Xml;
using posTest.Helpers;

namespace posTest.Controllers
{
    public class HomeController : Controller
    {
        private string sistemHatasi = "Bankayla bağlantı kurulamadı ! Lütfen daha sonra tekrar deneyin.";

        public bool sonuc { get; set; }
        public string hataMesaji { get; set; }
        public string hataKodu { get; set; }

        // Bankadan geri dönen değerler.
        public string code { get; set; }
        public string groupId { get; set; }
        public string transId { get; set; }
        public string referansNo { get; set; }

        public HomeController()
        {
            new SiteLanguage().SetLanguage(null);
        }

        public ActionResult Uyeliksiz()
        {
            OdemeEntities _context = new OdemeEntities();
            var domain = System.Web.HttpContext.Current.Request.Url.Host;
            Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);
            new SiteLanguage().SetLanguage(Request.QueryString.Get("lang"));
            var cookie = Request.Cookies["doviz"];
            string doviz = "";

            if (cookie == null)
            {
                doviz = "tr";
            }
            if (!string.IsNullOrEmpty(Request.QueryString.Get("doviz")))
            {
                doviz = Request.QueryString.Get("doviz");
            }
            HttpCookie langCookie = new HttpCookie("doviz", doviz);
            langCookie.Expires = DateTime.Now.AddYears(1);
            Response.Cookies.Add(langCookie);
            if (secilenFirma.UyeliksizOdeme == true)
            {
                System.Web.HttpContext.Current.Session["Login"] = "bos";

                return RedirectToAction("Index", "Home");
            }
            else
            {
                System.Web.HttpContext.Current.Session["Login"] = "false";

                return RedirectToAction("GirisYap", "Login");
            }
        }

        public ActionResult Index(string tutar, string uye)
        {
            new SiteLanguage().SetLanguage(Request.QueryString.Get("lang"));
            var cookie = Request.Cookies["doviz"];
            string doviz = "";
            if (cookie != null)
            {
                if (cookie.Value == null || cookie.Value == "")
                {
                    doviz = "tr";
                }
                else
                {
                    doviz = cookie.Value;
                }
            }
            else
            {
                doviz = "tr";
            }
            if (!string.IsNullOrEmpty(Request.QueryString.Get("doviz")))
            {
                doviz = Request.QueryString.Get("doviz");
            }
            HttpCookie langCookie = new HttpCookie("doviz", doviz);
            langCookie.Expires = DateTime.Now.AddYears(1);
            Response.Cookies.Add(langCookie);
            ViewBag.doviz = doviz;
            if (!String.IsNullOrEmpty(tutar) && !String.IsNullOrEmpty(uye))
            {
                ViewBag.tutar = tutar;
                System.Web.HttpContext.Current.Session["Login"] = uye;
            }

            if (System.Web.HttpContext.Current.Session["Login"] == null)
            {
                System.Web.HttpContext.Current.Session["Login"] = "false";
            }

            if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false")
            {
                //firma bilgileri
                OdemeEntities _context = new OdemeEntities();
                var domain = System.Web.HttpContext.Current.Request.Url.Host;
                Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

                //firmanın bankaları
                List<FirmaBankaBaglama> bankalar = _context.FirmaBankaBaglama.Where(x => x.FirmaId == secilenFirma.Id).ToList();
                List<Bankalar> secilenBankalar = new List<Bankalar>();
                foreach (var item in bankalar)
                {
                    Bankalar banka = _context.Bankalar.FirstOrDefault(x => x.Id == item.BankaId);
                    secilenBankalar.Add(banka);
                }

                //firmanın eklediği taksitler
                List<FirmaTaksitleri> taksitler = _context.FirmaTaksitleri.Where(x => x.FirmaId == secilenFirma.Id && x.Onay == true).ToList();

                //firmanın gizlilik sozlesmeleri
                List<Sozlesmeler> sozlesmeler = _context.Sozlesmeler.Where(x => x.FirmaId == secilenFirma.Id && x.Onay == true).ToList();

                CreditCardModel model = new CreditCardModel();
                model.logo = secilenFirma.Logo;
                model.renk = secilenFirma.FirmaRenk;
                model.firmaAd = secilenFirma.FirmaAdi;
                model.secilenBankalar = secilenBankalar;
                model.secilenTaksitler = taksitler;
                model.sozlesmeler = sozlesmeler;
                model.adres = secilenFirma.Adres;
                model.telefon2 = secilenFirma.Telefon;
                model.webAdres = secilenFirma.SiteAdres;
                model.uyeGirisi = Convert.ToBoolean(secilenFirma.UyelikOdeme);
                if (secilenBankalar.Count(x => x.Id == 29) > 0)
                {
                    model.iyzico = true;
                }
                else
                {
                    model.iyzico = false;
                }

                return View(model);
            }
            else
            {
                return RedirectToAction("GirisYap", "Login");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CreditCardModel model)
        {
            TempData["CreditCard"] = model;
            return RedirectToAction("ThreeDGate");
        }

        public ActionResult ThreeDGate()
        {
            //Indexden tempdata ile object olarak gelen değeri geri CreditCardModel'e cast ediyoruz.
            var cardModel = TempData["CreditCard"] as CreditCardModel;
            if (cardModel == null) return RedirectToAction("Index");

            //firma bilgilerini alma
            OdemeEntities _context = new OdemeEntities();
            var domain = System.Web.HttpContext.Current.Request.Url.Host;
            Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

            TempData["secilenFirma"] = secilenFirma;

            if (secilenFirma != null)
            {
                List<Bankalar> secilenBankaListe = new List<Bankalar>();
                List<FirmaBankaBaglama> liste = secilenFirma.FirmaBankaBaglama.ToList();

                if (liste.Count == 0)
                {
                    return View();
                }

                foreach (var item in liste)
                {
                    Bankalar pos = item.Bankalar;
                    secilenBankaListe.Add(pos);
                }

                Bankalar secilenPos;
                if (secilenBankaListe.Find(x => x.BankaAdi == cardModel.Banka) != null)
                {
                    secilenPos = secilenBankaListe.Find(x => x.BankaAdi == cardModel.Banka);
                }
                else
                {
                    secilenPos = liste.FirstOrDefault(x => x.Gecerli == true).Bankalar;
                }
                //secilenPos = liste.FirstOrDefault(x => x.BankaId == 10).Bankalar;

                List<FirmaParametreleri> firmaparametreleri =
                    liste.FirstOrDefault(x => x.BankaId == secilenPos.Id && x.FirmaId == secilenFirma.Id)
                        .FirmaParametreleri.ToList();

                TempData["firmaParametreleri"] = firmaparametreleri;

                switch (secilenPos.BankaAdi)
                {
                    case "Garanti Bankası":
                        return RedirectToAction("Garanti", "Home");
                    case "Akbank":
                        return RedirectToAction("Akbank", "Home");
                    case "Ziraat Bankası":
                        return RedirectToAction("Ziraat", "Home");

                    case "Yapı Kredi Bankası":
                        return RedirectToAction("YapiKredi", "Home");

                    case "Finans Bank":
                        return RedirectToAction("FinansBank", "Home");

                    case "İş Bankası":
                        return RedirectToAction("IsBankasi", "Home");

                    case "HalkBank":
                        return RedirectToAction("HalkBank", "Home");
                }

                return View();
            }
            else
            {
                return View();
            }
        }

        public ActionResult Ziraat()
        {
            string log = "";
            var firma = TempData["secilenFirma"] as Firmalar;
            try
            {
                var firmaparametreleri = TempData["firmaParametreleri"] as List<FirmaParametreleri>;
                var cardModel = TempData["CreditCard"] as CreditCardModel;
                var secilenFirma = TempData["secilenFirma"] as Firmalar;

                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz("******************");
                dosyayaYaz("Ziraat metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Ziraat metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now;

                string processType = "Auth";//İşlem tipi
                string clientId = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Mağaza Numarası").Deger;//Mağaza Numarası
                string storeKey = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Mağaza Anahtarı").Deger; ;//Mağaza anahtarı
                string storeType = "3d_pay_hosting";//SMS onaylı ödeme modeli 3DPay olarak adlandırılıyor.
                string orderNumber = DateTime.Now.ToString("ddMMyyyyHHmmssff");//Sipariş numarası
                string successUrl = "https://" + secilenFirma.Domain + "/Home/Success?orderid=" + orderNumber;//Başarılı Url
                string unsuccessUrl = "https://" + secilenFirma.Domain + "/Home/UnSuccess?orderid=" + orderNumber;//Hata Url
                string randomKey = ThreeDHelper.CreateRandomValue(10, false, false, true, false);
                string installment;
                string orderAmount;
                if (!string.IsNullOrEmpty(cardModel.Taksit))
                {
                    string[] digits = cardModel.Taksit.Split('_');
                    if (digits[0] == "1")
                    {
                        installment = "";
                    }
                    else
                    {
                        installment = digits[0];//Taksit
                    }
                    orderAmount = digits[1];//Decimal seperator nokta olmalı!
                }
                else
                {
                    installment = "";
                    orderAmount = "1.0";
                }

                string currencyCode = "949"; //TL ISO code | EURO "978" | Dolar "840"
                string languageCode = "tr";// veya "en"
                string cardType = "";

                //Güvenlik amaçlı olarak birleştirip şifreliyoruz. Banka decode edip bilgilerin doğruluğunu kontrol ediyor. Alanların sırasına dikkat etmeliyiz.
                string hashFormat = clientId + orderNumber + orderAmount + successUrl + unsuccessUrl + processType + installment + randomKey + storeKey;

                var paymentCollection = new NameValueCollection();
                cardModel.Amount = orderAmount;
                //Mağaza bilgileri
                paymentCollection.Add("hash", ThreeDHelper.ConvertSHA1(hashFormat));
                paymentCollection.Add("clientid", clientId);
                paymentCollection.Add("storetype", storeType);
                paymentCollection.Add("rnd", randomKey);
                paymentCollection.Add("okUrl", successUrl);
                paymentCollection.Add("failUrl", unsuccessUrl);
                paymentCollection.Add("islemtipi", processType);
                //Ödeme bilgileri
                paymentCollection.Add("currency", currencyCode);
                paymentCollection.Add("lang", languageCode);
                paymentCollection.Add("amount", orderAmount);
                paymentCollection.Add("oid", orderNumber);
                //Kredi kart bilgileri
                paymentCollection.Add("pan", cardModel.CardNumber.Replace("-", ""));
                paymentCollection.Add("cardHolderName", cardModel.HolderName);
                paymentCollection.Add("cv2", cardModel.CV2);
                paymentCollection.Add("Ecom_Payment_Card_ExpDate_Year", cardModel.ExpireYear);
                paymentCollection.Add("Ecom_Payment_Card_ExpDate_Month", cardModel.ExpireMonth);
                paymentCollection.Add("taksit", installment);
                paymentCollection.Add("cartType", cardType);

                dosyayaYaz("Ziraat metodu çalıştı.Parametreler belirlendi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                dosyayaYaz("Ziraat metodu çalıştı.Parametreler belirlendi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);

                //ödemenin kaydedilmesi
                CreditCardModel Obj = cardModel;
                Obj.Hata = "(Ziraat Bankası)";

                OdemeEntities _context = new OdemeEntities();

                Odemeler yeniOdeme = new Odemeler();
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                {
                    yeniOdeme.UyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);

                    string uyeMail = _context.Uyeler.FirstOrDefault(x => x.Id == yeniOdeme.UyeId).EPosta;
                    if (_context.OdemeIstekleri.Count(x => x.EPosta == uyeMail && x.Onay == false) > 0)
                    {
                        OdemeIstekleri gelenOdeme =
                            _context.OdemeIstekleri.FirstOrDefault(x => x.EPosta == uyeMail && x.Onay == false);
                        gelenOdeme.Onay = true;
                        _context.OdemeIstekleri.AddOrUpdate(gelenOdeme);
                        _context.SaveChanges();
                    }
                }
                yeniOdeme.Tarih = DateTime.Now;
                yeniOdeme.Satis = true;
                yeniOdeme.FirmaId = secilenFirma.Id;
                yeniOdeme.AdSoyad = Obj.AdSoyad;
                yeniOdeme.FirmaUnvan = Obj.Firma;
                yeniOdeme.Telefon = Obj.Telefon;
                yeniOdeme.Email = Obj.Email;
                yeniOdeme.Tutar = orderAmount;
                yeniOdeme.Taksit = installment;
                yeniOdeme.Hata = Obj.Hata;
                yeniOdeme.Onay = false;
                yeniOdeme.SiparisNo = orderNumber;

                _context.Odemeler.AddOrUpdate(yeniOdeme);

                dosyayaYaz("Ziraat metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Ziraat metodu sonlandı.Ödeme kaydedildi";
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = secilenFirma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                object paymentForm = ThreeDHelper.PrepareForm("https://sanalpos2.ziraatbank.com.tr/fim/est3Dgate", paymentCollection);
                return View(paymentForm);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("Ziraat metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                log += "Ziraat metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // ";
                OdemeEntities _context = new OdemeEntities();
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                throw;
            }

        }
        public ActionResult Akbank()
        {
            string log = "";
            var firma = TempData["secilenFirma"] as Firmalar;
            try
            {
                var firmaparametreleri = TempData["firmaParametreleri"] as List<FirmaParametreleri>;
                var cardModel = TempData["CreditCard"] as CreditCardModel;
                var secilenFirma = TempData["secilenFirma"] as Firmalar;

                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz("******************");
                dosyayaYaz("Akbank metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += " Akbank metodu çalıştı.Parametreler belirlendi.Firma:" + secilenFirma.Id + " // " + DateTime.Now;

                string processType = "Auth";//İşlem tipi
                string clientId = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Mağaza Numarası").Deger;//Mağaza Numarası
                string storeKey = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Mağaza Anahtarı").Deger; ;//Mağaza anahtarı
                string storeType = "3d_pay_hosting";//SMS onaylı ödeme modeli 3DPay olarak adlandırılıyor.
                string orderNumber = DateTime.Now.ToString("ddMMyyyyHHmmssff");//Sipariş numarası
                string successUrl = "https://" + secilenFirma.Domain + "/Home/Success?orderid=" + orderNumber;//Başarılı Url
                string unsuccessUrl = "https://" + secilenFirma.Domain + "/Home/UnSuccess?orderid=" + orderNumber;//Hata Url
                string randomKey = ThreeDHelper.CreateRandomValue(10, false, false, true, false);
                string installment;
                string orderAmount;
                if (!string.IsNullOrEmpty(cardModel.Taksit))
                {
                    string[] digits = cardModel.Taksit.Split('_');
                    if (digits[0] == "1")
                    {
                        installment = "";
                    }
                    else
                    {
                        installment = digits[0];//Taksit
                    }
                    orderAmount = digits[1];//Decimal seperator nokta olmalı!
                }
                else
                {
                    installment = "";
                    orderAmount = "1.0";
                }

                string currencyCode = "949"; //TL ISO code | EURO "978" | Dolar "840"
                string languageCode = "tr";// veya "en"
                string cardType = "";

                //Güvenlik amaçlı olarak birleştirip şifreliyoruz. Banka decode edip bilgilerin doğruluğunu kontrol ediyor. Alanların sırasına dikkat etmeliyiz.
                string hashFormat = clientId + orderNumber + orderAmount + successUrl + unsuccessUrl + processType + installment + randomKey + storeKey;

                var paymentCollection = new NameValueCollection();
                cardModel.Amount = orderAmount;
                //Mağaza bilgileri
                paymentCollection.Add("hash", ThreeDHelper.ConvertSHA1(hashFormat));
                paymentCollection.Add("clientid", clientId);
                paymentCollection.Add("storetype", storeType);
                paymentCollection.Add("rnd", randomKey);
                paymentCollection.Add("okUrl", successUrl);
                paymentCollection.Add("failUrl", unsuccessUrl);
                paymentCollection.Add("islemtipi", processType);
                //Ödeme bilgileri
                paymentCollection.Add("currency", currencyCode);
                paymentCollection.Add("lang", languageCode);
                paymentCollection.Add("amount", orderAmount);
                paymentCollection.Add("oid", orderNumber);
                //Kredi kart bilgileri
                paymentCollection.Add("pan", cardModel.CardNumber.Replace("-", ""));
                paymentCollection.Add("cardHolderName", cardModel.HolderName);
                paymentCollection.Add("cv2", cardModel.CV2);
                paymentCollection.Add("Ecom_Payment_Card_ExpDate_Year", cardModel.ExpireYear);
                paymentCollection.Add("Ecom_Payment_Card_ExpDate_Month", cardModel.ExpireMonth);
                paymentCollection.Add("taksit", installment);
                paymentCollection.Add("cartType", cardType);

                dosyayaYaz("Akbank metodu çalıştı.Parametreler belirlendi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += " Akbank metodu çalıştı.Parametreler belirlendi.Firma:" + secilenFirma.Id + " // " + DateTime.Now;

                //ödemenin kaydedilmesi
                CreditCardModel Obj = cardModel;
                Obj.Hata = "(Akbank)";

                OdemeEntities _context = new OdemeEntities();

                Odemeler yeniOdeme = new Odemeler();
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                {
                    yeniOdeme.UyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);

                    string uyeMail = _context.Uyeler.FirstOrDefault(x => x.Id == yeniOdeme.UyeId).EPosta;
                    if (_context.OdemeIstekleri.Count(x => x.EPosta == uyeMail && x.Onay == false) > 0)
                    {
                        OdemeIstekleri gelenOdeme =
                            _context.OdemeIstekleri.FirstOrDefault(x => x.EPosta == uyeMail && x.Onay == false);
                        gelenOdeme.Onay = true;
                        _context.OdemeIstekleri.AddOrUpdate(gelenOdeme);
                        _context.SaveChanges();
                    }
                }
                yeniOdeme.Tarih = DateTime.Now;
                yeniOdeme.Satis = true;
                yeniOdeme.FirmaId = secilenFirma.Id;
                yeniOdeme.AdSoyad = Obj.AdSoyad;
                yeniOdeme.FirmaUnvan = Obj.Firma;
                yeniOdeme.Telefon = Obj.Telefon;
                yeniOdeme.Email = Obj.Email;
                yeniOdeme.Tutar = orderAmount;
                yeniOdeme.Taksit = installment;
                yeniOdeme.Hata = Obj.Hata;
                yeniOdeme.Onay = false;
                yeniOdeme.SiparisNo = orderNumber;

                _context.Odemeler.AddOrUpdate(yeniOdeme);

                dosyayaYaz("Akbank metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += " Akbank metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now;
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = secilenFirma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                object paymentForm = ThreeDHelper.PrepareForm("https://www.sanalakpos.com/fim/est3Dgate", paymentCollection);
                return View(paymentForm);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("Akbank metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                log += " Akbank metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now;
                OdemeEntities _context = new OdemeEntities();
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                throw;
            }

        }


        public ActionResult YapiKredi()
        {
            string log = "";
            var firma = TempData["secilenFirma"] as Firmalar;
            try
            {
                var firmaparametreleri = TempData["firmaParametreleri"] as List<FirmaParametreleri>;
                var cardModel = TempData["CreditCard"] as CreditCardModel;
                var secilenFirma = TempData["secilenFirma"] as Firmalar;

                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz("******************");
                dosyayaYaz("Yapıkredi metodu çalıştı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                log += " Yapıkredi metodu çalıştı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Random rnd = new Random();

                string ccno = cardModel.CardNumber.Replace("-", "");
                string expdate = cardModel.ExpireYear.Substring(2, 2) + cardModel.ExpireMonth;
                string cvc = string.Format("{0:000}", cardModel.CV2);
                string orderid = DateTime.Now.ToString("ddMMyyyyHHmmssff") + rnd.Next(1111, 9999);
                string currencycode = "TL";

                string instnumber;
                string amount;
                if (!string.IsNullOrEmpty(cardModel.Taksit))
                {
                    string[] digits = cardModel.Taksit.Split('_');

                    if (digits[0] == "1")
                    {
                        instnumber = "00";
                    }
                    else
                    {
                        if (digits[0].Length == 1)
                        {
                            instnumber = "0" + digits[0];//Taksit
                        }
                        else
                        {
                            instnumber = digits[0];//Taksit
                        }
                    }
                    amount = digits[1];
                    cardModel.Amount = amount;
                    cardModel.Taksit = instnumber;

                    if (amount.Contains(".") == true)
                    {
                        string[] tutardizi = amount.Split('.');
                        if (tutardizi[1].Length == 1)
                        {
                            amount = amount.Replace(".", "");
                            amount = amount + "0";
                        }
                        else
                        {
                            amount = amount.Replace(".", "");
                        }
                    }
                    else
                    {
                        amount = amount + "00";
                    }
                }
                else
                {
                    instnumber = "00";
                    amount = "001";
                    cardModel.Amount = "0.01";
                    cardModel.Taksit = instnumber;
                }

                C_PosnetOOSTDS c = new C_PosnetOOSTDS();
                c.SetPosnetID("1010054855812425");

                c.SetURL("https://posnet.yapikredi.com.tr/PosnetWebService/XML");
                //c.SetURL("https://setmpos.ykb.com/PosnetWebService/XML");
                /* c.SetMid(firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Üye işyeri no (MID)").Deger);
                 c.SetTid(firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger);
                 c.SetKey(firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Key").Deger);*/
                c.SetMid("6700972638");
                c.SetTid("67909080");
                c.SetKey("10,10,10,10,10,10,10,10");

                if (Convert.ToInt32(instnumber) > 0) { c.SetKOICode(instnumber); }

                string xml = "<posnetRequest>" +
                             "<mid>" + firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Üye işyeri no (MID)").Deger + "</mid>" +
"<tid>" + firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger + "</tid>" +
"<oosRequestData>"
+ "<posnetid>" + firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Posnet No").Deger + "</posnetid>"
+ "<XID>" + orderid + "</XID>"
+ "<amount>" + amount + "</amount>"
+ "<currencyCode>TL</currencyCode>" +
"<installment>" + instnumber + "</installment>"
+ "<tranType>Sale</tranType>"
+ "<cardHolderName>ĞğÜüİıŞşÖöÇç</cardHolderName>"
+ "<ccno>" + ccno + "</ccno>"
+ "<expDate>" + expdate + "</expDate>"
+ "<cvc>" + cvc + "</cvc>"
+ "</oosRequestData>" +

                             "</posnetRequest>";
                string encodedXml = HttpUtility.UrlEncode(xml);

                WebRequest SiteyeBaglantiTalebi = HttpWebRequest.Create("https://posnet.yapikredi.com.tr/PosnetWebService/XML?xmldata=" + encodedXml);
                //WebRequest SiteyeBaglantiTalebi = HttpWebRequest.Create("https://setmpos.ykb.com/PosnetWebService/XML?xmldata=" + encodedXml);  
                WebResponse GelenCevap = SiteyeBaglantiTalebi.GetResponse();
                StreamReader CevapOku = new StreamReader(GelenCevap.GetResponseStream());
                string KaynakKodlar = CevapOku.ReadToEnd();
                XmlDocument xmll = new XmlDocument();
                xmll.LoadXml(KaynakKodlar);
                string posnetData = xmll.SelectSingleNode("/posnetResponse/oosRequestDataResponse/data1").InnerText;
                string posnetData2 = xmll.SelectSingleNode("/posnetResponse/oosRequestDataResponse/data2").InnerText;
                string digest = xmll.SelectSingleNode("/posnetResponse/oosRequestDataResponse/sign").InnerText;




                dosyayaYaz("Yapıkredi metodu 1. adım çalıştı.Kaynak Kodlar: " + KaynakKodlar + " FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                log += " Yapıkredi metodu 1.adım çalıştı.Kaynak Kodlar: " + KaynakKodlar + " FirmaId: " + secilenFirma.Id + " // " + DateTime.Now;

                /*bool sonuc = c.CreateTranRequestDatas(cardModel.AdSoyad, amount, currencycode, instnumber, orderid, "Sale", ccno, expdate, cvc);*/
                //bool sonuc = c.CreateTranRequestDatas("ad soyad", "100", currencycode, "0", orderid, "Sale", "4506 3470 4958 3145", "2405", "000"); //Test

                dosyayaYaz("Yapıkredi metodu 1. adım çalıştı.Sonuc: " + sonuc + " FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Yapıkredi metodu 1. adım çalıştı.Sonuc: " + sonuc + " FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;

                string mid = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Üye işyeri no (MID)").Deger;
                string posnetID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Posnet No").Deger;

                dosyayaYaz("Yapıkredi posnet  " + posnetData + " FirmaId:" + secilenFirma.Id + " // " + posnetData2 + "//" + DateTime.Now);
                log += "Yapıkredi posnet  " + posnetData + " FirmaId:" + secilenFirma.Id + " // " + posnetData2 + "//" + DateTime.Now;


                string vftCode = ""; //kampanya kodu
                string merchantReturnURL = "https://" + secilenFirma.Domain + "/Home/YapiKrediSonuc?orderid=" + orderid;
                string lang = "tr";
                string url = "https://posnet.yapikredi.com.tr/3DSWebService/YKBPaymentService";
                //string url = "https://setmpos.ykb.com/3DSWebService/YKBPaymentService";
                string openANewWindow = "1";

                var paymentCollection = new NameValueCollection();

                paymentCollection.Add("mid", mid);
                paymentCollection.Add("posnetID", posnetID);
                paymentCollection.Add("posnetData", posnetData);
                paymentCollection.Add("posnetData2", posnetData2);
                paymentCollection.Add("digest", digest);
                paymentCollection.Add("merchantReturnURL", merchantReturnURL);
                paymentCollection.Add("lang", lang);
                paymentCollection.Add("openANewWindow", openANewWindow);

                dosyayaYaz("Yapıkredi metodu parametreler.posnetData=" + posnetData + ".posnetData2=" + posnetData2 + ".digest=" + digest + ".FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Yapıkredi metodu parametreler.posnetData=" + posnetData + ".posnetData2=" + posnetData2 + ".digest=" + digest + ".FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;

                //ödemenin kaydedilmesi
                CreditCardModel Obj = cardModel;
                Obj.Hata = "(Yapı Kredi Bankası)";

                OdemeEntities _context = new OdemeEntities();

                Odemeler yeniOdeme = new Odemeler();
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                {
                    yeniOdeme.UyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);

                    string uyeMail = _context.Uyeler.FirstOrDefault(x => x.Id == yeniOdeme.UyeId).EPosta;
                    if (_context.OdemeIstekleri.Count(x => x.EPosta == uyeMail && x.Onay == false) > 0)
                    {
                        OdemeIstekleri gelenOdeme =
                            _context.OdemeIstekleri.FirstOrDefault(x => x.EPosta == uyeMail && x.Onay == false);
                        gelenOdeme.Onay = true;
                        _context.OdemeIstekleri.AddOrUpdate(gelenOdeme);
                        _context.SaveChanges();
                    }
                }
                yeniOdeme.Tarih = DateTime.Now;
                yeniOdeme.Satis = true;
                yeniOdeme.FirmaId = secilenFirma.Id;
                yeniOdeme.AdSoyad = Obj.AdSoyad;
                yeniOdeme.FirmaUnvan = Obj.Firma;
                yeniOdeme.Telefon = Obj.Telefon;
                yeniOdeme.Email = Obj.Email;
                yeniOdeme.Tutar = Obj.Amount;
                yeniOdeme.Taksit = Obj.Taksit;
                yeniOdeme.Hata = Obj.Hata;
                yeniOdeme.Onay = false;
                yeniOdeme.SiparisNo = orderid;

                _context.Odemeler.AddOrUpdate(yeniOdeme);

                ////
                //YapiKrediData model = new YapiKrediData();
                //model.xid = orderid;
                //model.amount = amount;
                //model.currency = currencycode;
                //model.orderid = orderid;
                //Session["YapiKrediData"] = model;

                //Session["firmaParametre"] = firmaparametreleri;

                object paymentForm = ThreeDHelper.PrepareForm(url, paymentCollection);
                dosyayaYaz("Yapıkredi metodu sonu ödeme kaydedildi.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Yapıkredi metodu sonu ödeme kaydedildi.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                return View(paymentForm);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("Yapıkredi metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                OdemeEntities _context = new OdemeEntities();
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                throw;
            }
        }

        public ActionResult YapiKrediSonuc(string orderid)
        {
            string log = "";
            var firma = TempData["secilenFirma"] as Firmalar;
            try
            {
                OdemeEntities _context = new OdemeEntities();
                var domain = System.Web.HttpContext.Current.Request.Url.Host;
                Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);
                List<FirmaParametreleri> parametreler = new List<FirmaParametreleri>();

                dosyayaYaz("YapıkrediSonuc metodu çalıştı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                log += " YapıkrediSonuc metodu çalıştı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;

                if (secilenFirma != null)
                {
                    List<Bankalar> secilenBankaListe = new List<Bankalar>();
                    List<FirmaBankaBaglama> liste = secilenFirma.FirmaBankaBaglama.ToList();

                    if (liste.Count == 0)
                    {
                        return View();
                    }

                    foreach (var item in liste)
                    {
                        Bankalar pos = item.Bankalar;
                        secilenBankaListe.Add(pos);
                    }

                    Bankalar secilenPos;
                    if (secilenBankaListe.Find(x => x.BankaAdi == "Yapı Kredi Bankası") != null)
                    {
                        secilenPos = secilenBankaListe.Find(x => x.BankaAdi == "Yapı Kredi Bankası");
                    }
                    else
                    {
                        secilenPos = liste.FirstOrDefault(x => x.Gecerli == true).Bankalar;
                    }

                    parametreler = liste.FirstOrDefault(x => x.BankaId == secilenPos.Id && x.FirmaId == secilenFirma.Id)
                            .FirmaParametreleri.ToList();
                }
                else
                {
                    dosyayaYaz("YapıkrediSonuc metodu 1. else çalıştı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                    log += "YapıkrediSonuc metodu 1. else çalıştı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;
                    OdemeLog odemeLog = new OdemeLog();
                    odemeLog.FirmaId = firma.Id;
                    odemeLog.Tarih = DateTime.Now;
                    odemeLog.Ip = Request.UserHostAddress;
                    odemeLog.OdemeLog1 = log;
                    _context.OdemeLog.Add(odemeLog);
                    _context.SaveChanges();
                    return RedirectToAction("UnSuccess", "Home", new { orderid = orderid, hata2 = "Banka parametreleri bulunamadı." });
                }

                string merchant = Request.Form.Get("merchantPacket");
                string merchantNo = Request.Form.Get("merchantId");
                string bank = Request.Form.Get("bankPacket");
                string sign = Request.Form.Get("sign");
                string amount = Request.Form.Get("amount");
                string xid = Request.Form.Get("xid");

                //amount = Convert.ToString(Convert.ToInt32(Convert.ToDouble(amount) * 100));

                if (String.IsNullOrEmpty(orderid))
                {
                    orderid = xid;
                }

                //3. Adım
                dosyayaYaz("YapıkrediSonuc metodu 3. adım çalıştı.FirmaId:" + secilenFirma.Id + " // " + orderid + " // " + amount + "//" + DateTime.Now);
                log += "YapıkrediSonuc metodu 3. adım çalıştı.FirmaId:" + secilenFirma.Id + " // " + orderid + " // " + amount + "//" + DateTime.Now;

                //string firstHash = HASH(/*parametreler.FirstOrDefault(x => x.ParametreId == "Key").Deger*/"10,10,10,10,10,10,10,10" + ";" + /*parametreler.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger*/"67905468");
                //string MAC = HASH(xid + ";" + amount + ";" + "TL" + ";" + merchantNo + ";" + firstHash);
                string firstHash = HASH(parametreler.FirstOrDefault(x => x.ParametreId == "Key").Deger + ';' + parametreler.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger);
                string MAC = HASH(xid + ';' + amount + ';' + "TL" + ';' + merchantNo + ';' + firstHash);

                //TEST
                //string firstHash = HASH("10,10,10,10,10,10,10,10" + ';' + "67909080");
                //string MAC = HASH(xid + ';' + "1000" + ';' + "TL" + ';' + "6700972638" + ';' + firstHash);

                string xml = "<posnetRequest>" +
                             "<mid>" + parametreler.FirstOrDefault(x => x.ParametreId == "Üye işyeri no (MID)").Deger + "</mid>" +
                             "<tid>" + parametreler.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger + "</tid>" +
                             "<oosResolveMerchantData>" +
                             "<bankData>" + bank + "</bankData>" +
                             "<merchantData>" + merchant + "</merchantData>" +
                             "<sign>" + sign + "</sign>" +
                             "<mac>" + MAC + "</mac>" +
                             "</oosResolveMerchantData>" +
                             "</posnetRequest>";
                string encodedXml = HttpUtility.UrlEncode(xml);

                //WebRequest SiteyeBaglantiTalebi = HttpWebRequest.Create("https://setmpos.ykb.com/PosnetWebService/XML?xmldata=" + encodedXml);
                WebRequest SiteyeBaglantiTalebi = HttpWebRequest.Create("https://posnet.yapikredi.com.tr/PosnetWebService/XML?xmldata=" + encodedXml);
                WebResponse GelenCevap = SiteyeBaglantiTalebi.GetResponse();
                StreamReader CevapOku = new StreamReader(GelenCevap.GetResponseStream());
                string KaynakKodlar = CevapOku.ReadToEnd();
                dosyayaYaz("YapıkrediSonuc metodu 3. adım sonucu çalıştı.Kaynak Kod 3 . adım:" + KaynakKodlar + " // " + DateTime.Now);
                log += "YapıkrediSonuc metodu 3. adım sonucu çalıştı.Kaynak Kod 3 . adım:" + KaynakKodlar + " // " + DateTime.Now;

                int IcerikBaslangicIndex = KaynakKodlar.IndexOf("<mdStatus>") + 10;
                int IcerikBitisIndex = KaynakKodlar.Substring(IcerikBaslangicIndex).IndexOf("</mdStatus>");

                dosyayaYaz("YapıkrediSonuc metodu 3. adım sonucu çalıştı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "YapıkrediSonuc metodu 3. adım sonucu çalıştı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;

                if (IcerikBaslangicIndex < 1 || IcerikBitisIndex < 1)
                {
                    int respIndex = KaynakKodlar.IndexOf("<respText>") + 10;
                    int respBitisIndex = KaynakKodlar.Substring(respIndex).IndexOf("</respText>");
                    string respText = KaynakKodlar.Substring(respIndex, respBitisIndex);
                    dosyayaYaz("YapıkrediSonuc metodu 3. adım sonucu çalıştı.Kaynak:" + KaynakKodlar + " // " + DateTime.Now);
                    log += "YapıkrediSonuc metodu 3. adım sonucu çalıştı.Kaynak:" + KaynakKodlar + " // " + DateTime.Now;

                    dosyayaYaz("YapıkrediSonuc metodu 3. adım IcerikBaslangicIndex boş hatası çalıştı." + xid + "/" + amount + "/" + merchantNo + "/xml=" + xml + " //FirmaId:" + secilenFirma.Id + "////" + respText + " // " + DateTime.Now);
                    log += "YapıkrediSonuc metodu 3. adım IcerikBaslangicIndex boş hatası çalıştı." + xid + "/" + amount + "/" + merchantNo + "/xml=" + xml + " //FirmaId:" + secilenFirma.Id + "////" + respText + " // " + DateTime.Now;
                    OdemeLog odemeLog = new OdemeLog();
                    odemeLog.FirmaId = firma.Id;
                    odemeLog.Tarih = DateTime.Now;
                    odemeLog.Ip = Request.UserHostAddress;
                    odemeLog.OdemeLog1 = log;
                    _context.OdemeLog.Add(odemeLog);
                    _context.SaveChanges();
                    return RedirectToAction("UnSuccess", "Home", new { orderid = orderid, hata2 = "3. adım tamamlanamadı." });
                }

                string md = KaynakKodlar.Substring(IcerikBaslangicIndex, IcerikBitisIndex);

                int IcerikBaslangicIndex2 = KaynakKodlar.IndexOf("<mac>") + 5;
                int IcerikBitisIndex2 = KaynakKodlar.Substring(IcerikBaslangicIndex2).IndexOf("</mac>");
                string mac = KaynakKodlar.Substring(IcerikBaslangicIndex2, IcerikBitisIndex2);

                String MAC2 = HASH(md + ';' + xid + ';' + amount + ';' + "TL" + ';' + merchantNo + ';' +
                //String MAC2 = HASH(md + ';' + xid + ';' + "1000" + ';' + "TL" + ';' + "6700972638" + ';' +
                                   HASH(parametreler.FirstOrDefault(x => x.ParametreId == "Key").Deger + ';' + parametreler.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger));

                dosyayaYaz("YapıkrediSonuc metodu 3. adım bitiş.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "YapıkrediSonuc metodu 3. adım bitiş.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;

                if (MAC2 == mac)
                {
                    //4.Adım
                    dosyayaYaz("YapıkrediSonuc metodu 4. adım başlangıcı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                    log += "YapıkrediSonuc metodu 4. adım başlangıcı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;
                    //String MAC3 = HASH(xid + ';' + "1000" + ';' + "TL" + ';' + merchantNo + ';' + HASH(parametreler.FirstOrDefault(x => x.ParametreId == "Key").Deger + ";" + parametreler.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger));
                    String MAC3 = HASH(xid + ';' + amount + ';' + "TL" + ';' + merchantNo + ';' + HASH(parametreler.FirstOrDefault(x => x.ParametreId == "Key").Deger + ";" + parametreler.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger));

                    string xml2 = "<posnetRequest>" +
                                 "<mid>" + parametreler.FirstOrDefault(x => x.ParametreId == "Üye işyeri no (MID)").Deger + "</mid>" +
                                 "<tid>" + parametreler.FirstOrDefault(x => x.ParametreId == "Terminal no (TID)").Deger + "</tid>" +
                                 "<oosTranData>" +
                                 "<bankData>" + bank + "</bankData>" +
                                 "<wpAmount>" + "0" + "</wpAmount>" +
                                "<mac>" + MAC3 + "</mac>" +
                                 "</oosTranData>" +
                                 "</posnetRequest>";
                    string encodedXml2 = HttpUtility.UrlEncode(xml2);

                    WebRequest SiteyeBaglantiTalebi2 = HttpWebRequest.Create("https://posnet.yapikredi.com.tr/PosnetWebService/XML?xmldata=" + encodedXml2);
                    //WebRequest SiteyeBaglantiTalebi2 = HttpWebRequest.Create("https://setmpos.ykb.com/PosnetWebService/XML?xmldata=" + encodedXml2);
                    WebResponse GelenCevap2 = SiteyeBaglantiTalebi2.GetResponse();
                    StreamReader CevapOku2 = new StreamReader(GelenCevap2.GetResponseStream());
                    string KaynakKodlar2 = CevapOku2.ReadToEnd();

                    dosyayaYaz("YapıkrediSonuc metodu 4. adım sonucu.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                    log += "YapıkrediSonuc metodu 4. adım sonucu.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;

                    int IcerikBaslangicIndex3 = KaynakKodlar2.IndexOf("<approved>") + 10;
                    int IcerikBitisIndex3 = KaynakKodlar2.Substring(IcerikBaslangicIndex3).IndexOf("</approved>");
                    string approved = KaynakKodlar2.Substring(IcerikBaslangicIndex3, IcerikBitisIndex3);
                    if (approved == "1" || approved == "2")
                    {
                        dosyayaYaz("YapıkrediSonuc metodu 4. adım sonucu başarılı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                        log += "YapıkrediSonuc metodu 4. adım sonucu başarılı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;
                        OdemeLog odemeLog = new OdemeLog();
                        odemeLog.FirmaId = firma.Id;
                        odemeLog.Tarih = DateTime.Now;
                        odemeLog.Ip = Request.UserHostAddress;
                        odemeLog.OdemeLog1 = log;
                        _context.OdemeLog.Add(odemeLog);
                        _context.SaveChanges();
                        return RedirectToAction("Success", "Home", new { orderid = orderid });
                    }
                    else
                    {
                        int IcerikBaslangicIndex4 = KaynakKodlar2.IndexOf("<respText>") + 10;
                        int IcerikBitisIndex4 = KaynakKodlar2.Substring(IcerikBaslangicIndex4).IndexOf("</respText>");
                        string respText = KaynakKodlar2.Substring(IcerikBaslangicIndex4, IcerikBitisIndex4);
                        dosyayaYaz("YapıkrediSonuc metodu 4. adım sonucu .xml:" + KaynakKodlar2 + " // " + DateTime.Now);
                        log += "YapıkrediSonuc metodu 4. adım sonucu .xml:" + KaynakKodlar2 + " // " + DateTime.Now;

                        dosyayaYaz("YapıkrediSonuc metodu 4. adım sonucu hatalı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                        log += "YapıkrediSonuc metodu 4. adım sonucu hatalı.FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;
                        OdemeLog odemeLog = new OdemeLog();
                        odemeLog.FirmaId = firma.Id;
                        odemeLog.Tarih = DateTime.Now;
                        odemeLog.Ip = Request.UserHostAddress;
                        odemeLog.OdemeLog1 = log;
                        _context.OdemeLog.Add(odemeLog);
                        _context.SaveChanges();
                        return RedirectToAction("UnSuccess", "Home", new { orderid = orderid, hata2 = respText });
                    }
                }
                else
                {
                    int IcerikBaslangicIndex5 = KaynakKodlar.IndexOf("<respText>") + 10;
                    int IcerikBitisIndex5 = KaynakKodlar.Substring(IcerikBaslangicIndex5).IndexOf("</respText>");
                    string respText = KaynakKodlar.Substring(IcerikBaslangicIndex5, IcerikBitisIndex5);

                    dosyayaYaz("YapıkrediSonuc metodu 4. adım başlamadı.Else çalıştı.Hata=" + md + " / " + respText + " .FirmaId:" + secilenFirma.Id + " // " + DateTime.Now);
                    log += "YapıkrediSonuc metodu 4. adım başlamadı.Else çalıştı.Hata=" + md + " / " + respText + " .FirmaId:" + secilenFirma.Id + " // " + DateTime.Now;
                    OdemeLog odemeLog = new OdemeLog();
                    odemeLog.FirmaId = firma.Id;
                    odemeLog.Tarih = DateTime.Now;
                    odemeLog.Ip = Request.UserHostAddress;
                    odemeLog.OdemeLog1 = log;
                    _context.OdemeLog.Add(odemeLog);
                    _context.SaveChanges();
                    return RedirectToAction("UnSuccess", "Home", new { orderid = orderid, hata2 = md + "/" + respText });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("YapıkrediSonuc metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                log += "YapıkrediSonuc metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now;
                OdemeEntities _context = new OdemeEntities();
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                throw;
            }
        }

        public ActionResult Garanti()
        {
            string log = "";
            var firma = TempData["secilenFirma"] as Firmalar;
            try
            {
                var firmaparametreleri = TempData["firmaParametreleri"] as List<FirmaParametreleri>;
                var cardModel = TempData["CreditCard"] as CreditCardModel;
                var secilenFirma = TempData["secilenFirma"] as Firmalar;

                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz("******************");
                dosyayaYaz("Garanti metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Garanti metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now;

                string instnumber;
                string amount;
                if (!string.IsNullOrEmpty(cardModel.Taksit))
                {
                    string[] digits = cardModel.Taksit.Split('_');
                    if (digits[0] == "1")
                    {
                        instnumber = "";
                    }
                    else
                    {
                        instnumber = digits[0];//Taksit
                    }
                    amount = digits[1];
                    cardModel.Amount = amount;
                    cardModel.Taksit = instnumber;

                    if (amount.Contains(".") == true)
                    {
                        string[] tutardizi = amount.Split('.');
                        if (tutardizi[1].Length == 1)
                        {
                            amount = amount.Replace(".", "");
                            amount = amount + "0";
                        }
                        else
                        {
                            amount = amount.Replace(".", "");
                        }
                    }
                    else
                    {
                        amount = amount + "00";
                    }
                }
                else
                {
                    instnumber = "";
                    amount = "000";
                    cardModel.Amount = "0.00";
                    cardModel.Taksit = instnumber;
                }

                string strMode = "PROD";
                string strApiVersion = "v0.01";
                string strTerminalProvUserID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Sanal Pos Kullanıcısı").Deger;
                string strType = "sales";
                string strAmount = amount; // işlem Tutarı
                string strCurrencyCode = "949";
                string strInstallmentCount = instnumber; //Taksit Sayısı. Boş gönderilirse taksit yapılmaz
                string strTerminalUserID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Sanal Pos Kullanıcısı").Deger;
                string strOrderID = DateTime.Now.ToString("ddMMyyyyHHmmssff");
                string strCustomeripaddress = Request.UserHostAddress;
                string strTerminalID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Terminal Numarası").Deger;
                string _strTerminalID = "0" + strTerminalID; //'Başına 0 eklenerek 9 digite tamamlanmalıdır.
                string strTerminalMerchantID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Üye İşyeri Numarası").Deger; //Üye  işyeri Numarası
                string strStoreKey = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "3D Secure Şifresi").Deger; //3D Secure şifreniz
                string strProvisionPassword = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Sanal Pos Şifresi").Deger; //Sanal Pos Şifresi( PROVAUT kullanıcısının şifresi )
                string strSuccessURL = "https://" + secilenFirma.Domain + "/Home/Success?orderid=" + strOrderID;//Başarılı Url
                string strErrorURL = "https://" + secilenFirma.Domain + "/Home/UnSuccess?orderid=" + strOrderID;//Error Url
                                                                                                                //string strSuccessURL = "http://localhost:52245/Home/Success?orderid=" + strOrderID;//Başarılı Url
                                                                                                                //string strErrorURL = "http://localhost:52245/Home/UnSuccess?orderid=" + strOrderID;//Hata Url
                string SecurityData = GetSHA1(strProvisionPassword + _strTerminalID).ToUpper();
                string HashData = GetSHA1(strTerminalID + strOrderID + strAmount + strSuccessURL + strErrorURL + strType + strInstallmentCount + strStoreKey + SecurityData).ToUpper();

                var paymentCollection1 = new NameValueCollection();
                paymentCollection1.Add("mode", strMode);
                paymentCollection1.Add("apiversion", strApiVersion);
                paymentCollection1.Add("terminalprovuserid", strTerminalProvUserID);
                paymentCollection1.Add("terminaluserid", strTerminalUserID);
                paymentCollection1.Add("terminalmerchantid", strTerminalMerchantID);
                paymentCollection1.Add("txntype", strType);
                paymentCollection1.Add("txnamount", strAmount);
                paymentCollection1.Add("txncurrencycode", strCurrencyCode);
                paymentCollection1.Add("txninstallmentcount", strInstallmentCount);
                paymentCollection1.Add("customeripaddress", strCustomeripaddress);
                paymentCollection1.Add("orderid", strOrderID);
                paymentCollection1.Add("terminalid", strTerminalID);
                paymentCollection1.Add("successurl", strSuccessURL);
                paymentCollection1.Add("errorurl", strErrorURL);
                paymentCollection1.Add("secure3dhash", HashData);
                //Kart bilgileri
                paymentCollection1.Add("secure3dsecuritylevel", "3D_PAY");
                paymentCollection1.Add("cardnumber", cardModel.CardNumber);
                paymentCollection1.Add("cardexpiredatemonth", cardModel.ExpireMonth);
                paymentCollection1.Add("cardexpiredateyear", cardModel.ExpireYear.Substring(2, 2));
                paymentCollection1.Add("cardcvv2", cardModel.CV2);

                OdemeEntities _context = new OdemeEntities();

                if (secilenFirma.Id == 34)
                {
                    if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                    {
                        int uyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);
                        string bayiNum = _context.Uyeler.FirstOrDefault(x => x.Id == uyeId).Adres;

                        paymentCollection1.Add("submerchantid", bayiNum);
                    }

                    if (cardModel.Taksit != "1" && cardModel.Taksit != "")
                    {
                        cardModel.Taksit = cardModel.Taksit + "+2";
                    }

                }

                object paymentForm1 = ThreeDHelper.PrepareForm("https://sanalposprov.garanti.com.tr/servlet/gt3dengine", paymentCollection1);

                dosyayaYaz("Garanti metodu çalıştı.İstek gönderildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Garanti metodu çalıştı.İstek gönderildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now;

                //ödemenin kaydedilmesi
                CreditCardModel Obj = cardModel;
                Obj.Hata = "(Garanti Bankası)";

                Odemeler yeniOdeme = new Odemeler();
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                {
                    yeniOdeme.UyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);

                    string uyeMail = _context.Uyeler.FirstOrDefault(x => x.Id == yeniOdeme.UyeId).EPosta;
                    if (_context.OdemeIstekleri.Count(x => x.EPosta == uyeMail && x.Onay == false) > 0)
                    {
                        OdemeIstekleri gelenOdeme =
                            _context.OdemeIstekleri.FirstOrDefault(x => x.EPosta == uyeMail && x.Onay == false);
                        gelenOdeme.Onay = true;
                        _context.OdemeIstekleri.AddOrUpdate(gelenOdeme);
                        _context.SaveChanges();
                    }
                }
                yeniOdeme.Tarih = DateTime.Now;
                yeniOdeme.Satis = true;
                yeniOdeme.FirmaId = secilenFirma.Id;
                yeniOdeme.AdSoyad = Obj.AdSoyad;
                yeniOdeme.FirmaUnvan = Obj.Firma;
                yeniOdeme.Telefon = Obj.Telefon;
                yeniOdeme.Email = Obj.Email;
                yeniOdeme.Tutar = Obj.Amount;
                yeniOdeme.Taksit = Obj.Taksit;
                yeniOdeme.Hata = Obj.Hata;
                yeniOdeme.Onay = false;
                yeniOdeme.SiparisNo = strOrderID;

                _context.Odemeler.AddOrUpdate(yeniOdeme);

                dosyayaYaz("Garanti metodu çalıştı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Garanti metodu çalıştı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now;
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                return View(paymentForm1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("Garanti metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                log += "Garanti metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now;
                OdemeEntities _context = new OdemeEntities();
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                throw;
            }

        }

        public ActionResult GarantiPay(string tutar, string ad, string telefon, string firma, string eposta)
        {
            try
            {
                Random rnd = new Random();
                OdemeEntities _context = new OdemeEntities();
                var domain = System.Web.HttpContext.Current.Request.Url.Host;
                Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz("******************");
                dosyayaYaz("GarantiPay metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now);

                List<FirmaBankaBaglama> liste = secilenFirma.FirmaBankaBaglama.ToList();
                Bankalar secilenPos = _context.Bankalar.FirstOrDefault(x => x.Id == 9);

                List<FirmaParametreleri> firmaparametreleri = liste.FirstOrDefault(x => x.BankaId == secilenPos.Id && x.FirmaId == secilenFirma.Id).FirmaParametreleri.ToList();

                string amount = tutar.Replace(".", "").Replace(",", "");
                string strMode = "PROD";
                string strApiVersion = "v0.01";
                string strTerminalProvUserID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Sanal Pos Kullanıcısı").Deger;
                string strType = "gpdatarequest";
                string strAmount = amount; // işlem Tutarı
                string strCurrencyCode = "949";
                string strInstallmentCount = ""; //Taksit Sayısı. Boş gönderilirse taksit yapılmaz
                string strTerminalUserID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Sanal Pos Kullanıcısı").Deger;
                string strOrderID = DateTime.Now.ToString("ddMMyyyyHHmmssff");
                string strCustomeripaddress = "192.1.1.1";
                string strTerminalID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Terminal Numarası").Deger;
                string _strTerminalID = "0" + strTerminalID; //'Başına 0 eklenerek 9 digite tamamlanmalıdır.
                string strTerminalMerchantID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Üye İşyeri Numarası").Deger; //Üye  işyeri Numarası
                string strStoreKey = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "3D Secure Şifresi").Deger; //3D Secure şifreniz
                string strProvisionPassword = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Sanal Pos Şifresi").Deger; //Sanal Pos Şifresi( PROVOOS kullanıcısının şifresi )
                string strSuccessURL = "https://" + secilenFirma.Domain + "/Home/Success?orderid=" + strOrderID;//Başarılı Url
                string strErrorURL = "https://" + secilenFirma.Domain + "/Home/UnSuccess?orderid=" + strOrderID;//Error Url
                string strCompanyName = secilenFirma.FirmaAdi;
                string strlang = "tr";
                string strtimestamp = System.DateTime.Now.ToString();
                string SecurityData = GetSHA1(strProvisionPassword + _strTerminalID).ToUpper();
                string HashData = GetSHA1(strTerminalID + strOrderID + strAmount + strSuccessURL + strErrorURL + strType + strInstallmentCount + strStoreKey + SecurityData).ToUpper();

                var paymentCollection1 = new NameValueCollection();
                paymentCollection1.Add("mode", strMode);
                paymentCollection1.Add("apiversion", strApiVersion);
                paymentCollection1.Add("terminalprovuserid", strTerminalProvUserID);
                paymentCollection1.Add("terminaluserid", strTerminalUserID);
                paymentCollection1.Add("terminalmerchantid", strTerminalMerchantID);
                paymentCollection1.Add("txntype", strType);
                paymentCollection1.Add("txnsubtype", "sales");
                paymentCollection1.Add("txnamount", strAmount);
                paymentCollection1.Add("txncurrencycode", strCurrencyCode);
                paymentCollection1.Add("orderid", strOrderID);
                paymentCollection1.Add("terminalid", strTerminalID);
                paymentCollection1.Add("successurl", strSuccessURL);
                paymentCollection1.Add("errorurl", strErrorURL);
                paymentCollection1.Add("customeremailaddress", eposta);
                paymentCollection1.Add("customeripaddress", strCustomeripaddress);
                paymentCollection1.Add("companyname", strCompanyName);
                paymentCollection1.Add("lang", strlang);
                paymentCollection1.Add("txntimestamp", strtimestamp);
                paymentCollection1.Add("refreshtime", "5");
                paymentCollection1.Add("secure3dhash", HashData);
                paymentCollection1.Add("garantipay", "Y");
                //paymentCollection1.Add("txnTimeOutPeriod", strtimestamp);
                //paymentCollection1.Add("bnsuseflag", "N");
                //paymentCollection1.Add("fbbuseflag", "N");
                paymentCollection1.Add("txninstallmentcount", "");
                //paymentCollection1.Add("totallinstallmentcount", "1");
                //paymentCollection1.Add("installmentnumber1", "2");
                //paymentCollection1.Add("installmentamount1", "500");
                paymentCollection1.Add("secure3dsecuritylevel", "CUSTOM_PAY");

                object paymentForm1 = ThreeDHelper.PrepareForm("https://sanalposprov.garanti.com.tr/servlet/gt3dengine", paymentCollection1);

                //ödemenin kaydedilmesi
                CreditCardModel Obj = new CreditCardModel();
                Obj.Hata = "(Garanti Pay)";

                Odemeler yeniOdeme = new Odemeler();
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                {
                    yeniOdeme.UyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);

                    string uyeMail = _context.Uyeler.FirstOrDefault(x => x.Id == yeniOdeme.UyeId).EPosta;
                    if (_context.OdemeIstekleri.Count(x => x.EPosta == uyeMail && x.Onay == false) > 0)
                    {
                        OdemeIstekleri gelenOdeme =
                            _context.OdemeIstekleri.FirstOrDefault(x => x.EPosta == uyeMail && x.Onay == false);
                        gelenOdeme.Onay = true;
                        _context.OdemeIstekleri.AddOrUpdate(gelenOdeme);
                        _context.SaveChanges();
                    }
                }
                yeniOdeme.Tarih = DateTime.Now;
                yeniOdeme.Satis = true;
                yeniOdeme.FirmaId = secilenFirma.Id;
                yeniOdeme.AdSoyad = ad;
                yeniOdeme.FirmaUnvan = firma;
                yeniOdeme.Telefon = telefon;
                yeniOdeme.Email = eposta;
                yeniOdeme.Tutar = tutar.Replace(".", "").Replace(",", ".");
                yeniOdeme.Hata = Obj.Hata;
                yeniOdeme.Onay = false;
                yeniOdeme.SiparisNo = strOrderID;

                _context.Odemeler.AddOrUpdate(yeniOdeme);
                _context.SaveChanges();

                dosyayaYaz("GarantiPay metodu çalıştı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);

                return View(paymentForm1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("GarantiPay metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                throw;
            }
        }

        public ActionResult FinansBank()
        {
            string log = "";
            var firma = TempData["secilenFirma"] as Firmalar;
            try
            {
                var firmaparametreleri = TempData["firmaParametreleri"] as List<FirmaParametreleri>;
                var cardModel = TempData["CreditCard"] as CreditCardModel;
                var secilenFirma = TempData["secilenFirma"] as Firmalar;

                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz("******************");
                dosyayaYaz("FinansBank metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "FinansBank metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now;

                string MbrId = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Kurum Kodu (MbrId)").Deger;//Kurum Kodu 5
                string MerchantID = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Üye İşyeri Numarası (MerchantID)").Deger;//Language_MerchantID 107200000008813
                string MerchantPass = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "3D üye İşyeri Anahtarı (MerchantPass)").Deger;//Language_MerchantPass 46630775
                string UserCode = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Kullanıcı Adı (User Code)").Deger;//Kullanici Kodu kuruogluApi
                string SecureType = "3DPay";//Language_SecureType
                string TxnType = "Auth";//Islem Tipi

                string Currency = "949";//Para Birimi
                var cookie = Request.Cookies["doviz"];
                string doviz = "";

                if (cookie != null)
                {
                    if (string.IsNullOrEmpty(cookie.Value))
                    {
                        if (cookie.Value == "usd")
                        {
                            Currency = "840";
                        }
                        else if (cookie.Value == "eur")
                        {
                            Currency = "978";
                        }
                    }
                }


                string OrderId = DateTime.Now.ToString("ddMMyyyyHHmmssff");//Sipariş numarası
                                                                           //string OkUrl = "http://localhost:52245/Home/Success?orderid=" + OrderId;//Başarılı Url
                                                                           //string FailUrl = "http://localhost:52245/Home/UnSuccess?orderid=" + OrderId;//Hata Url
                string OkUrl = "https://" + secilenFirma.Domain + "/Home/Success?orderid=" + OrderId;//Başarılı Url
                string FailUrl = "https://" + secilenFirma.Domain + "/Home/UnSuccess?orderid=" + OrderId;//Hata Url
                string OrgOrderId = OrderId;//Orijinal Islem Siparis Numarasi
                string Lang = "TR";

                string InstallmentCount;
                string PurchAmount;
                if (!string.IsNullOrEmpty(cardModel.Taksit))
                {
                    string[] digits = cardModel.Taksit.Split('_');
                    if (digits[0] == "1")
                    {
                        InstallmentCount = "0";
                    }
                    else
                    {
                        InstallmentCount = digits[0];//Taksit
                    }
                    PurchAmount = digits[1].Replace(",", "");//Decimal seperator nokta olmalı!
                }
                else
                {
                    InstallmentCount = "0";
                    PurchAmount = "1";
                }

                String rnd = DateTime.Now.Ticks.ToString();
                String str = MbrId + OrderId + PurchAmount + OkUrl + FailUrl + TxnType + InstallmentCount + rnd + MerchantPass;
                System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
                byte[] hashingbytes = sha.ComputeHash(bytes);
                String hash = Convert.ToBase64String(hashingbytes);

                var paymentCollection = new NameValueCollection();

                paymentCollection.Add("MbrId", MbrId);
                paymentCollection.Add("MerchantID", MerchantID);
                paymentCollection.Add("UserCode", UserCode);
                paymentCollection.Add("PurchAmount", PurchAmount);
                paymentCollection.Add("Currency", Currency);
                paymentCollection.Add("OrderId", OrderId);
                paymentCollection.Add("InstallmentCount", InstallmentCount);
                paymentCollection.Add("TxnType", TxnType);
                paymentCollection.Add("SecureType", SecureType);
                paymentCollection.Add("Lang", Lang);
                paymentCollection.Add("OkUrl", OkUrl);
                paymentCollection.Add("FailUrl", FailUrl);
                // paymentCollection.Add("OrgOrderId", OrgOrderId);
                paymentCollection.Add("Rnd", rnd);
                paymentCollection.Add("Hash", hash);

                //Kredi kart bilgileri
                paymentCollection.Add("Pan", cardModel.CardNumber.Replace("-", ""));
                paymentCollection.Add("CardHolderName", cardModel.HolderName);
                paymentCollection.Add("Cvv2", cardModel.CV2);
                paymentCollection.Add("Expiry", cardModel.ExpireMonth + cardModel.ExpireYear.Substring(2, 2));

                //ödemenin kaydedilmesi
                CreditCardModel Obj = cardModel;
                Obj.Hata = "(QNB Finansbank)";

                OdemeEntities _context = new OdemeEntities();

                Odemeler yeniOdeme = new Odemeler();
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                {
                    yeniOdeme.UyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);

                    string uyeMail = _context.Uyeler.FirstOrDefault(x => x.Id == yeniOdeme.UyeId).EPosta;
                    if (_context.OdemeIstekleri.Count(x => x.EPosta == uyeMail && x.Onay == false) > 0)
                    {
                        OdemeIstekleri gelenOdeme =
                            _context.OdemeIstekleri.FirstOrDefault(x => x.EPosta == uyeMail && x.Onay == false);
                        gelenOdeme.Onay = true;
                        _context.OdemeIstekleri.AddOrUpdate(gelenOdeme);
                        _context.SaveChanges();
                    }
                }
                yeniOdeme.Tarih = DateTime.Now;
                yeniOdeme.Satis = true;
                yeniOdeme.FirmaId = secilenFirma.Id;
                yeniOdeme.AdSoyad = Obj.AdSoyad;
                yeniOdeme.FirmaUnvan = Obj.Firma;
                yeniOdeme.Telefon = Obj.Telefon;
                yeniOdeme.Email = Obj.Email;
                yeniOdeme.Tutar = PurchAmount;
                yeniOdeme.Taksit = InstallmentCount;
                if (secilenFirma.Id == 34)
                {
                    if (Convert.ToInt32(PurchAmount) < 30000 && Convert.ToInt32(PurchAmount) > 100)
                    {
                        yeniOdeme.Taksit = InstallmentCount + "+2";
                    }
                }
                if (cookie != null)
                {
                    if (string.IsNullOrEmpty(cookie.Value))
                    {
                        if (cookie.Value == "usd")
                        {
                            yeniOdeme.Tutar = PurchAmount + "$";
                        }
                        else if (cookie.Value == "eur")
                        {
                            yeniOdeme.Tutar = PurchAmount + "€";
                        }
                    }
                }

                yeniOdeme.Hata = Obj.Hata;
                yeniOdeme.Onay = false;
                yeniOdeme.SiparisNo = OrderId;

                _context.Odemeler.AddOrUpdate(yeniOdeme);

                dosyayaYaz("FinansBank metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "FinansBank metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now;
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = secilenFirma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                object paymentForm = ThreeDHelper.PrepareForm("https://vpos.qnbfinansbank.com/Gateway/Default.aspx", paymentCollection);

                return View(paymentForm);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("FinansBank metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                log += "FinansBank metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now;
                OdemeEntities _context = new OdemeEntities();
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                throw;
            }

        }

        public ActionResult IsBankasi()
        {
            string log = "";
            var firma = TempData["secilenFirma"] as Firmalar;
            try
            {
                var firmaparametreleri = TempData["firmaParametreleri"] as List<FirmaParametreleri>;
                var cardModel = TempData["CreditCard"] as CreditCardModel;
                var secilenFirma = TempData["secilenFirma"] as Firmalar;

                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz("******************");
                dosyayaYaz("İş Bankası metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "İş Bankası metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now;

                string amount;
                string installment;
                if (!string.IsNullOrEmpty(cardModel.Taksit))
                {
                    string[] digits = cardModel.Taksit.Split('_');
                    if (digits[0] == "1")
                    {
                        installment = "0";
                    }
                    else
                    {
                        installment = digits[0];//Taksit
                    }
                    amount = digits[1];
                }
                else
                {
                    installment = "0";
                    amount = "1";
                }

                //string clientid = "700655000300"; //test bilgileri
                //string storekey = "TRPS0300"; //test bilgileri
                string clientid = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Mağaza Numarası").Deger;//Mağaza Numarası
                string storekey = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "3D Şifre").Deger;//3D Şifre
                                                                                                            //string clientid = "700668691371";//Mağaza Numarası
                                                                                                            //string storekey = "KRGD3927";//3D Şifre
                                                                                                            //api kullanılmadığından api kullanıcı parametreleri kullanılmadı
                string storetype = "3d_pay_hosting";
                string islemtipi = "Auth";
                string prmamount = amount; // işlem Tutarı
                string currency = "949";
                string oid = DateTime.Now.ToString("ddMMyyyyHHmmssff");//Sipariş numarası
                //string okUrl = "http://localhost:52245/Home/Success?orderid=" + oid;//Başarılı Url
                //string failUrl = "http://localhost:52245/Home/UnSuccess?orderid=" + oid;//Hata Url
                //string callbackurl = "http://localhost:52245/Home/CallBack?orderid=" + oid;//Hata Url
                string okUrl = "https://" + secilenFirma.Domain + "/Home/Success?orderid=" + oid;//Başarılı Url
                string failUrl = "https://" + secilenFirma.Domain + "/Home/UnSuccess?orderid=" + oid;//Hata Url
                string callbackurl = "https://" + secilenFirma.Domain + "/Home/CallBack?orderid=" + oid;//Hata Url
                string lang = "tr";
                string rnd = ThreeDHelper.CreateRandomValue(10, false, false, true, false);
                string plaintext = clientid + oid + prmamount + okUrl + failUrl + islemtipi + installment + rnd + callbackurl + storekey;
                string hash = ThreeDHelper.ConvertSHA1(plaintext);

                var paymentCollection1 = new NameValueCollection();
                paymentCollection1.Add("clientid", clientid);
                paymentCollection1.Add("storetype", storetype);
                paymentCollection1.Add("hash", hash);
                paymentCollection1.Add("islemtipi", islemtipi);
                paymentCollection1.Add("amount", prmamount);
                paymentCollection1.Add("currency", currency);
                paymentCollection1.Add("oid", oid);
                paymentCollection1.Add("okUrl", okUrl);
                paymentCollection1.Add("failUrl", failUrl);
                paymentCollection1.Add("callbackurl", callbackurl);
                paymentCollection1.Add("lang", lang);
                paymentCollection1.Add("rnd", rnd);
                paymentCollection1.Add("taksit", installment);
                //Kart bilgileri
                paymentCollection1.Add("cardHolderName", cardModel.HolderName);
                paymentCollection1.Add("cv2", cardModel.CV2);
                paymentCollection1.Add("pan", cardModel.CardNumber.Replace("-", ""));
                paymentCollection1.Add("Ecom_Payment_Card_ExpDate_Year", cardModel.ExpireYear);
                paymentCollection1.Add("Ecom_Payment_Card_ExpDate_Month", cardModel.ExpireMonth);

                //ödemenin kaydedilmesi
                CreditCardModel Obj = cardModel;
                Obj.Hata = "(İş Bankası)";

                OdemeEntities _context = new OdemeEntities();

                Odemeler yeniOdeme = new Odemeler();
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                {
                    yeniOdeme.UyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);

                    string uyeMail = _context.Uyeler.FirstOrDefault(x => x.Id == yeniOdeme.UyeId).EPosta;
                    if (_context.OdemeIstekleri.Count(x => x.EPosta == uyeMail && x.Onay == false) > 0)
                    {
                        OdemeIstekleri gelenOdeme =
                            _context.OdemeIstekleri.FirstOrDefault(x => x.EPosta == uyeMail && x.Onay == false);
                        gelenOdeme.Onay = true;
                        _context.OdemeIstekleri.AddOrUpdate(gelenOdeme);
                        _context.SaveChanges();
                    }
                }
                yeniOdeme.Tarih = DateTime.Now;
                yeniOdeme.Satis = true;
                yeniOdeme.FirmaId = secilenFirma.Id;
                yeniOdeme.AdSoyad = Obj.AdSoyad;
                yeniOdeme.FirmaUnvan = Obj.Firma;
                yeniOdeme.Telefon = Obj.Telefon;
                yeniOdeme.Email = Obj.Email;
                yeniOdeme.Tutar = prmamount;

                yeniOdeme.Taksit = installment;
                if (secilenFirma.Id == 34)
                {
                    if (Convert.ToInt32(prmamount) > 200)
                    {
                        yeniOdeme.Taksit = installment + "+2";
                    }
                }

                yeniOdeme.Hata = Obj.Hata;
                yeniOdeme.Onay = false;
                yeniOdeme.SiparisNo = oid;

                _context.Odemeler.AddOrUpdate(yeniOdeme);

                dosyayaYaz("İş Bankası metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "İş Bankası metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now;
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                // test adresi
                //object paymentForm1 = ThreeDHelper.PrepareForm("https://entegrasyon.asseco-see.com.tr/fim/est3Dgate", paymentCollection1);
                object paymentForm1 = ThreeDHelper.PrepareForm("https://spos.isbank.com.tr/fim/est3dgate", paymentCollection1);

                return View(paymentForm1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("İş Bankası metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                log += "İş Bankası metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now;
                OdemeEntities _context = new OdemeEntities();
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                throw;
            }
        }

        public ActionResult HalkBank()
        {
            string log = "";
            var firma = TempData["secilenFirma"] as Firmalar;
            try
            {
                var firmaparametreleri = TempData["firmaParametreleri"] as List<FirmaParametreleri>;
                var cardModel = TempData["CreditCard"] as CreditCardModel;
                var secilenFirma = TempData["secilenFirma"] as Firmalar;

                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz(Environment.NewLine);
                dosyayaYaz("******************");
                dosyayaYaz("HalkBank metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "HalkBank metodu çalıştı.Firma:" + secilenFirma.Id + " // " + DateTime.Now;

                string amount;
                string installment;
                if (!string.IsNullOrEmpty(cardModel.Taksit))
                {
                    string[] digits = cardModel.Taksit.Split('_');
                    if (digits[0] == "1")
                    {
                        installment = "0";
                    }
                    else
                    {
                        installment = digits[0];//Taksit
                    }
                    amount = digits[1];
                }
                else
                {
                    installment = "0";
                    amount = "1";
                }

                //string clientid = "500300000"; //test bilgileri
                //string storekey = "123456"; //test bilgileri
                string clientid = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "Mağaza Numarası").Deger;//Mağaza Numarası
                string storekey = firmaparametreleri.FirstOrDefault(x => x.ParametreId == "3D Şifre").Deger;//3D Şifre
                //string clientid = "500284598";//Mağaza Numarası
                //string storekey = "KuruogluApi18";//3D Şifre
                string storetype = "3d_pay_hosting";
                string islemtipi = "Auth";
                string prmamount = amount; // işlem Tutarı
                string currency = "949";
                string oid = DateTime.Now.ToString("ddMMyyyyHHmmssff");//Sipariş numarası
                //string okUrl = "http://localhost:52245/Home/Success?orderid=" + oid;//Başarılı Url
                //string failUrl = "http://localhost:52245/Home/UnSuccess?orderid=" + oid;//Hata Url
                //string callbackurl = "http://localhost:52245/Home/CallBack?orderid=" + oid;//Hata Url
                string okUrl = "https://" + secilenFirma.Domain + "/Home/Success?orderid=" + oid;//Başarılı Url
                string failUrl = "https://" + secilenFirma.Domain + "/Home/UnSuccess?orderid=" + oid;//Hata Url
                string callbackurl = "https://" + secilenFirma.Domain + "/Home/CallBack?orderid=" + oid;//Hata Url
                string lang = "tr";
                string rnd = ThreeDHelper.CreateRandomValue(10, false, false, true, false);
                string plaintext = clientid + oid + prmamount + okUrl + failUrl + islemtipi + installment + rnd + callbackurl + storekey;
                string hash = ThreeDHelper.ConvertSHA1(plaintext);

                var paymentCollection1 = new NameValueCollection();
                paymentCollection1.Add("clientid", clientid);
                paymentCollection1.Add("storetype", storetype);
                paymentCollection1.Add("hash", hash);
                paymentCollection1.Add("islemtipi", islemtipi);
                paymentCollection1.Add("amount", prmamount);
                paymentCollection1.Add("currency", currency);
                paymentCollection1.Add("oid", oid);
                paymentCollection1.Add("okUrl", okUrl);
                paymentCollection1.Add("failUrl", failUrl);
                paymentCollection1.Add("callbackurl", callbackurl);
                paymentCollection1.Add("lang", lang);
                paymentCollection1.Add("rnd", rnd);
                paymentCollection1.Add("taksit", installment);
                //Kart bilgileri
                paymentCollection1.Add("cardHolderName", cardModel.HolderName);
                paymentCollection1.Add("cv2", cardModel.CV2);
                paymentCollection1.Add("pan", cardModel.CardNumber.Replace("-", ""));
                paymentCollection1.Add("Ecom_Payment_Card_ExpDate_Year", cardModel.ExpireYear);
                paymentCollection1.Add("Ecom_Payment_Card_ExpDate_Month", cardModel.ExpireMonth);

                //ödemenin kaydedilmesi
                CreditCardModel Obj = cardModel;
                Obj.Hata = "(HalkBank)";

                OdemeEntities _context = new OdemeEntities();

                Odemeler yeniOdeme = new Odemeler();
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
                {
                    yeniOdeme.UyeId = Convert.ToInt32(System.Web.HttpContext.Current.Session["Login"]);

                    string uyeMail = _context.Uyeler.FirstOrDefault(x => x.Id == yeniOdeme.UyeId).EPosta;
                    if (_context.OdemeIstekleri.Count(x => x.EPosta == uyeMail && x.Onay == false) > 0)
                    {
                        OdemeIstekleri gelenOdeme =
                            _context.OdemeIstekleri.FirstOrDefault(x => x.EPosta == uyeMail && x.Onay == false);
                        gelenOdeme.Onay = true;
                        _context.OdemeIstekleri.AddOrUpdate(gelenOdeme);
                        _context.SaveChanges();
                    }
                }
                yeniOdeme.Tarih = DateTime.Now;
                yeniOdeme.Satis = true;
                yeniOdeme.FirmaId = secilenFirma.Id;
                yeniOdeme.AdSoyad = Obj.AdSoyad;
                yeniOdeme.FirmaUnvan = Obj.Firma;
                yeniOdeme.Telefon = Obj.Telefon;
                yeniOdeme.Email = Obj.Email;
                yeniOdeme.Tutar = prmamount;
                yeniOdeme.Taksit = installment;
                yeniOdeme.Hata = Obj.Hata;
                yeniOdeme.Onay = false;
                yeniOdeme.SiparisNo = oid;

                _context.Odemeler.AddOrUpdate(yeniOdeme);

                dosyayaYaz("Halkbank metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now);
                log += "Halkbank metodu sonlandı.Ödeme kaydedildi.Firma:" + secilenFirma.Id + " // " + DateTime.Now;
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                // test adresi
                //object paymentForm1 = ThreeDHelper.PrepareForm("https://entegrasyon.asseco-see.com.tr/fim/est3Dgate", paymentCollection1);
                object paymentForm1 = ThreeDHelper.PrepareForm("https://sanalpos.halkbank.com.tr/fim/est3dgate", paymentCollection1);

                return View(paymentForm1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                dosyayaYaz("Halkbank metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now);
                log += "Halkbank metodu çalıştı.Hata oluştu." + e.Message + " // " + e.StackTrace + " // " + DateTime.Now;
                OdemeEntities _context = new OdemeEntities();
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = firma.Id;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = log;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
                throw;
            }
        }

        public ActionResult Success(string orderid)
        {
            dosyayaYaz("Success metodu çalıştı.orderid:" + orderid + " // " + DateTime.Now);

            try
            {
                OdemeEntities _context = new OdemeEntities();

                //ödemenin veritabanına kaydedilmesi
                if (String.IsNullOrEmpty(orderid))
                {
                    orderid = Request.Form.Get("orderid");
                }

                if (orderid != null && orderid != "")
                {
                    Odemeler secilenOdeme = _context.Odemeler.FirstOrDefault(x => x.SiparisNo == orderid);

                    if (secilenOdeme != null)
                    {
                        dosyayaYaz("Success metodu çalıştı.Secilen ödeme bulundu.orderid:" + orderid + " // " + DateTime.Now);

                        if (secilenOdeme.Onay == true)
                        {
                            dosyayaYaz("Success metodu çalıştı.Secilen ödeme bulundu.Ödeme zaten onaylanmış.orderid:" + orderid + " // " + DateTime.Now);

                            TempData["CreditCard"] = null;
                            TempData["firmaParametreleri"] = null;
                            Session["YapiKrediData"] = null;
                            Session["ServiceObj"] = null;

                            return View();
                        }

                        secilenOdeme.Onay = true;
                        _context.Odemeler.AddOrUpdate(secilenOdeme);
                        _context.SaveChanges();


                        var domain = System.Web.HttpContext.Current.Request.Url.Host;

                        bool sslDurumu = Convert.ToBoolean(WebConfigurationManager.AppSettings["ssl"]);

                        // firmaya ödeme bilgisi maili gönderimi
                        Firmalar secilen = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);
                        string sunucu = secilen.EmailSunucu;
                        var port = secilen.EmailSunucuPort;
                        string mail = secilen.EmailKullaniciAdi;
                        string sifre = secilen.EmailParola;
                        string mailkutusu = secilen.EPosta; //firmanın mail adresi

                        var fromAddress = new MailAddress(secilen.EmailKullaniciAdi, secilen.FirmaAdi);
                        var toAddress = new MailAddress(mailkutusu);
                        string subject = "Ödeme Bilgisi (" + secilenOdeme.FirmaUnvan + ")";
                        string icerik = String.Format(@"<table class='tg'>"
+ "<thead>      "
+ "  <tr>       "
+ "    <th class='tg-0lax' colspan='2'>Ödeme Bilgisi</th>"
+ "  </tr>      "
+ "</thead>     "
+ "<tbody>      "
+ "  <tr>       "
+ "    <td class='tg-0lax' colspan='2'><span style='font-weight:bold'>" + secilenOdeme.AdSoyad + "</span><br>Yapılmış olan ödeme işlemi bilgileri aşağıda bulunmaktadır.</td>"
+ "  </tr>     "
+ "  <tr>      "
+ "    <td class='tg-0lax' colspan='2'><span style='font-weight:bold'>İşlem Bilgileri</span></td>"
+ "  </tr>    "
+ "  <tr>     "
+ "    <td class='tg-0lax'>İşlem No</td>        "
+ "    <td class='tg-0lax'>" + secilenOdeme.SiparisNo + "</td>       "
+ "  </tr>              "
+ "  <tr>             "
+ "    <td class='tg-0lax' colspan='2'><span style='font-weight:bold'>Ödeme Bilgileri</span></td>"
+ "  </tr>     "
+ "  <tr>      "
+ "    <td class='tg-0lax'>Ödeme Şekli</td>"
+ "    <td class='tg-0lax'>" + secilenOdeme.Hata + "</td>"
+ "  </tr>       "
+ "  <tr>        "
+ "    <td class='tg-0lax'>Ödeme Tutarı</td>"
+ "    <td class='tg-0lax'>" + secilenOdeme.Tutar + "</td>"
+ "  </tr>      "
+ "  <tr>       "
+ "    <td class='tg-0lax'>Taksit Sayısı</td>"
+ "    <td class='tg-0lax'>" + secilenOdeme.Taksit + "</td>"
+ "  </tr>      "
+ "  <tr>       "
+ "    <td class='tg-0lax'>İşlem Tarihi</td>"
+ "    <td class='tg-0lax'>" + secilenOdeme.Tarih + "</td>"
+ "  </tr>      "
+ "<tr> "
+ "    <td class='tg-0lax'>Ödeyen Mail Adresi</td>"
+ "    <td class='tg-0lax'>" + secilenOdeme.Email + "</td>"
+ "  </tr>      "
+ "<tr> "
+ "    <td class='tg-0lax'>Ödeyen Telefon</td>"
+ "    <td class='tg-0lax'>" + secilenOdeme.Telefon + "</td>"
+ "  </tr>      "
+ "</tbody>   "
+ "</table>");
                        //AlternateView icerikHtml = new AlternateView(icerik, "text/html");
                        var smtp = new SmtpClient
                        {
                            Host = sunucu,
                            Port = Convert.ToInt32(port),
                            EnableSsl = sslDurumu,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(mail, sifre)
                        };
                        var message = new MailMessage(fromAddress, toAddress);
                        message.Subject = subject;
                        message.Body = icerik;
                        message.IsBodyHtml = true;
                        /*message.AlternateViews.Add(icerikHtml);

                        message.AlternateViews.Add(getEmbeddedImage(Server.MapPath("~/img/logo.png"),
                                                    Server.MapPath("~/img/secure.png"), secilenOdeme.FirmaUnvan, secilenOdeme.Tutar));*/
                        {
                            smtp.Send(message);
                        }

                        dosyayaYaz("Success metodu çalıştı.Firmaya mail gönderildi.orderid:" + orderid + " // " + DateTime.Now);

                        //müşteriye ödeme bilgili maili gönderimi
                        string mailkutusu2 = secilenOdeme.Email; //ödemeyi yapan kişinin mail adresi

                        var fromAddress2 = new MailAddress(secilen.EmailKullaniciAdi, secilen.FirmaAdi);
                        var toAddress2 = new MailAddress(mailkutusu2);
                        string subject2 = "Ödeme Bilgisi";

                        var smtp2 = new SmtpClient
                        {
                            Host = sunucu,
                            Port = Convert.ToInt32(port),
                            EnableSsl = sslDurumu,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(mail, sifre)
                        };
                        var message2 = new MailMessage(fromAddress2, toAddress2);
                        message2.Subject = subject2;
                        message2.IsBodyHtml = true;
                        message2.AlternateViews.Add(getEmbeddedImage2(Server.MapPath("~/img/logo.png"),
                            Server.MapPath("~/img/secure.png"), secilen.FirmaAdi, secilenOdeme.Tutar, secilen.Adres,
                        secilen.Telefon));
                        {
                            smtp2.Send(message2);
                        }

                        dosyayaYaz("Success metodu çalıştı.Müşteriye mail gönderildi.orderid:" + orderid + " // " + DateTime.Now);
                    }
                    else
                    {
                        dosyayaYaz("Success metodu çalıştı.Secilen ödeme bulunamadı.orderid:" + orderid + " // " + DateTime.Now);
                    }

                }
                else
                {
                    dosyayaYaz("Success metodu çalıştı.OrderId bulunamadı.orderid:" + orderid + " // " + DateTime.Now);
                }


                //12.12.2019 güncelleme(Başarılı ödemenin birden fazla kez veritabanına kayıt edilmesini önlemek için)
                TempData["CreditCard"] = null;
                TempData["firmaParametreleri"] = null;
                Session["YapiKrediData"] = null;
                Session["ServiceObj"] = null;

            }
            catch (Exception e)
            {
                //12.12.2019 güncelleme(Başarılı ödemenin birden fazla kez veritabanına kayıt edilmesini önlemek için)
                TempData["CreditCard"] = null;
                TempData["firmaParametreleri"] = null;
                Session["YapiKrediData"] = null;
                Session["ServiceObj"] = null;

                dosyayaYaz("Success metodu çalıştı.Catch çalıştıHata= " + e.Message + " .orderid:" + orderid + " // " + DateTime.Now);

                return View();
            }

            return View();
        }

        /// <summary>
        /// 3d secure hataları
        /// </summary>
        /// <param name="orderid"></param>
        /// <param name="hata2"></param>
        /// <returns></returns>
        public ActionResult UnSuccess(string orderid, string hata2)
        {
            dosyayaYaz("UnSuccess metodu çalıştı.orderid:" + orderid + " // " + DateTime.Now);

            OdemeEntities _context = new OdemeEntities();
            Odemeler gelenOdeme = _context.Odemeler.FirstOrDefault(x => x.SiparisNo == orderid);

            string hata = hata2;
            try
            {
                if (String.IsNullOrEmpty(orderid))
                {
                    orderid = Request.Form.Get("orderid");
                    dosyayaYaz("UnSuccess metodu çalıştı.Orderid boş geldi.Request ile alındı.orderid:" + orderid + " // " + DateTime.Now);
                }


                if (gelenOdeme.Onay == true)
                {
                    dosyayaYaz("UnSuccess metodu çalıştı.Secilen ödeme bulundu.Ödeme daha önceden onaylanmış.orderid:" + orderid + " // " + DateTime.Now);

                    TempData["CreditCard"] = null;
                    TempData["firmaParametreleri"] = null;
                    Session["YapiKrediData"] = null;
                    Session["ServiceObj"] = null;
                    OdemeLog odemeLog1 = new OdemeLog();
                    odemeLog1.FirmaId = gelenOdeme.FirmaId;
                    odemeLog1.Tarih = DateTime.Now;
                    odemeLog1.Ip = Request.UserHostAddress;
                    odemeLog1.OdemeLog1 = "UnSuccess metodu çalıştı.Secilen ödeme bulundu.Ödeme daha önceden onaylanmış.orderid:" + orderid + " // " + DateTime.Now;
                    _context.OdemeLog.Add(odemeLog1);
                    _context.SaveChanges();
                    return View();
                }


                string strMDStatus;
                string strMDStatusText = null;

                if (Request.Form.Get("mdstatus") != null)
                {
                    strMDStatus = Request.Form.Get("mdstatus");
                    if (Request.Form.Get("mderrormessage") != null)
                    {
                        strMDStatusText = Request.Form.Get("mderrormessage") + " - " + Request.Form.Get("hostmsg");
                    }
                    else if (Request.Form.Get("mdErrorMsg") != null)
                    {
                        strMDStatusText = Request.Form.Get("mdErrorMsg");
                    }
                    else if (Request.Form.Get("ErrorMsg") != null || Request.Form.Get("ErrMsg") != null)
                    {
                        strMDStatusText = Request.Form.Get("ErrorMsg") + " - " + Request.Form.Get("ErrMsg");
                    }

                }
                else if (Request.Form.Get("3DStatus") != null)
                {
                    strMDStatus = Request.Form.Get("3DStatus");
                    strMDStatusText = Request.Form.Get("ErrMsg") + " - " + Request.Form.Get("ProcReturnCode");
                    orderid = Request.Form.Get("OrderId");
                }
                else
                {
                    strMDStatus = "";
                }

                if (strMDStatus.Equals("1"))
                {
                    strMDStatusText = strMDStatusText + "-" + "Tam Doğrulama";
                }
                else if (strMDStatus.Equals("2"))
                {
                    strMDStatusText = strMDStatusText + "-" + "Kart Sahibi veya bankası sisteme kayıtlı değil";
                }
                else if (strMDStatus.Equals("3"))
                {
                    strMDStatusText = strMDStatusText + "-" + "Kartın bankası sisteme kayıtlı değil";
                }
                else if (strMDStatus.Equals("4"))
                {
                    strMDStatusText = strMDStatusText + "-" + "Kartın bankası sisteme kayıtlı değil";
                }
                else if (strMDStatus.Equals("5"))
                {
                    strMDStatusText = strMDStatusText + "-" + "Doğrulama yapılamıyor";
                }
                else if (strMDStatus.Equals("6"))
                {
                    strMDStatusText = strMDStatusText + "-" + "3-D Secure Hatası";
                }
                else if (strMDStatus.Equals("7"))
                {
                    strMDStatusText = strMDStatusText + "-" + "Sistem Hatası";
                }
                else if (strMDStatus.Equals("8"))
                {
                    strMDStatusText = strMDStatusText + "-" + "Bilinmeyen Kart No";
                }
                else if (strMDStatus.Equals("0"))
                {
                    strMDStatusText = strMDStatusText + "-" + "Doğrulama Başarısız, 3-D Secure imzası geçersiz.";
                }

                hata = "(" + strMDStatus + ")" + strMDStatusText + " - " + hata;

                dosyayaYaz("UnSuccess metodu çalıştı.Hata: " + hata + ".orderid:" + orderid + " // " + DateTime.Now);
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = gelenOdeme.FirmaId;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = "UnSuccess metodu çalıştı.Hata: " + hata + ".orderid:" + orderid + " // " + DateTime.Now;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                hata = e.Message + " - " + hata;
                dosyayaYaz("UnSuccess metodu çalıştı.Catch çalıştı.mesaj: " + e.Message + ".orderid:" + orderid + " // " + DateTime.Now);
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = gelenOdeme.FirmaId;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = "UnSuccess metodu çalıştı.Catch çalıştı.mesaj: " + e.Message + ".orderid:" + orderid + " // " + DateTime.Now;
                _context.OdemeLog.Add(odemeLog);
                _context.SaveChanges();

            }

            var domain = System.Web.HttpContext.Current.Request.Url.Host;

            //ödemenin veritabanına kaydedilmesi
            Odemeler secilenOdeme = _context.Odemeler.FirstOrDefault(x => x.SiparisNo == orderid);

            if (secilenOdeme != null)
            {
                dosyayaYaz("UnSuccess metodu çalıştı.Secilen ödeme bulundu.orderid:" + orderid + " // " + DateTime.Now);
                secilenOdeme.Hata = hata + " - " + secilenOdeme.Hata;
                OdemeLog odemeLog = new OdemeLog();
                odemeLog.FirmaId = gelenOdeme.FirmaId;
                odemeLog.Tarih = DateTime.Now;
                odemeLog.Ip = Request.UserHostAddress;
                odemeLog.OdemeLog1 = "UnSuccess metodu çalıştı.Secilen ödeme bulundu.orderid:" + orderid + " // " + DateTime.Now;
                _context.OdemeLog.Add(odemeLog);
                _context.Odemeler.AddOrUpdate(secilenOdeme);
                _context.SaveChanges();
            }

            ViewBag.sonuc = hata;
            return View();
        }

        private AlternateView getEmbeddedImage(String filePath, String filePath2, string firmaAdi, string tutar)
        {
            LinkedResource res = new LinkedResource(filePath);
            res.ContentId = Guid.NewGuid().ToString();

            LinkedResource res2 = new LinkedResource(filePath2);
            res2.ContentId = Guid.NewGuid().ToString();
            string htmlBody = @"<html lang=""en"">
	<head>	
		<meta content=""text/html; charset=utf-8"" http-equiv=""Content-Type"">
	
	</head>
	<body>
<table style=""width:500px; font-family: Arial, sans-serif; margin: auto; background-color: #f7f7f7;"">
	<tr>
		<th><img src=""cid:" + res.ContentId + @""" style=""padding-top: 25px; max-width:220px;""></th>
	</tr>
	<tr>
		<th style=""background-color:white; margin: 25px; display: block;"">
			<p style=""font-size: 16px; font-weight: normal; font-family: Arial, sans-serif;"">
				Merhaba
	" + firmaAdi + @"	firmasından " + tutar + @" TL tutarında ödeme alınmıştır.
			</p>
			<p style=""margin: 30px 0px;""> </p>
		</th>
	</tr>
	<tr>
		<th>
			<img src=""cid:" + res2.ContentId + @""" style=""width: 250px; padding-bottom: 25px;"">
		</th>
	</tr>
</table>   
	</body>
</html>
";
            AlternateView alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
            alternateView.LinkedResources.Add(res);
            alternateView.LinkedResources.Add(res2);
            return alternateView;
        }

        private AlternateView getEmbeddedImage2(String filePath, String filePath2, string firmaAdi, string tutar, string adres, string telefon)
        {
            LinkedResource res = new LinkedResource(filePath);
            res.ContentId = Guid.NewGuid().ToString();

            LinkedResource res2 = new LinkedResource(filePath2);
            res2.ContentId = Guid.NewGuid().ToString();
            string htmlBody = @"<html lang=""en"">
	<head>	
		<meta content=""text/html; charset=utf-8"" http-equiv=""Content-Type"">
	
	</head>
	<body>
<table style=""width:500px; font-family: Arial, sans-serif; margin: auto; background-color: #f7f7f7;"">
	<tr>
		<th><img src=""cid:" + res.ContentId + @""" style=""padding-top: 25px; max-width:220px;""></th>
	</tr>
	<tr>
		<th style=""background-color:white; margin: 25px; display: block;"">
			<p style=""font-size: 16px; font-weight: normal; font-family: Arial, sans-serif;"">
				Merhaba
	Firmamıza  " + tutar + @" TL tutarında yapılan ödemeniz alınmıştır.Teşekkür ederiz.
			</p>
			<p style=""margin: 30px 0px;""> </p>
		</th>
	</tr>
	<tr>
		<th>
			<img src=""cid:" + res2.ContentId + @""" style=""width: 250px; padding-bottom: 25px;"">
		</th>
	</tr>
</table>   
<table style=""width:500px; margin: auto; border:solid 1px #f7f7f7; margin-top: 10px;"">
	<tr>
		<th style=""font-size: 12px; font-weight: normal; font-family: Arial, sans-serif; color: gray;"">
			<p>" + firmaAdi + @"</p>
			<p>" + adres + @"</p>
			<p>Telefon: " + telefon + @" </p>			
		</thr
	</tr>
</table>	
	</body>
</html>
";
            AlternateView alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
            alternateView.LinkedResources.Add(res);
            alternateView.LinkedResources.Add(res2);
            return alternateView;
        }

        private string HASH(string originalString)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(originalString));
                return Convert.ToBase64String(bytes);
            }
        }

        string GetSHA1(string SHA1Data)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            string HashedPassword = SHA1Data;
            byte[] hashbytes = Encoding.GetEncoding("ISO-8859-9").GetBytes(HashedPassword);
            byte[] inputbytes = sha.ComputeHash(hashbytes);
            return GetHexaDecimal(inputbytes);
        }

        public string GetHexaDecimal(byte[] bytes)
        {
            StringBuilder s = new StringBuilder(); int length = bytes.Length; for (int n = 0; n <= length - 1; n++) { s.Append(String.Format("{0,2:x}", bytes[n]).Replace(" ", "0")); }
            return s.ToString();
        }

        public string KartKontrol(int kartNo)
        {
            string data;
            OdemeEntities _context = new OdemeEntities();
            KartBinNumaralari secilen = _context.KartBinNumaralari.FirstOrDefault(x => x.Id == kartNo);
            if (secilen != null)
            {
                data = secilen.bankaAdi;
            }
            else
            {
                data = "";
            }
            return data;
        }

        public void deneme()
        {
            bool sslDurumu = false;
            OdemeEntities _context = new OdemeEntities();
            var domain = System.Web.HttpContext.Current.Request.Url.Host;
            //firmaya ödeme bilgisi maili gönderimi
            Firmalar secilen = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);
            string sunucu = secilen.EmailSunucu;
            var port = secilen.EmailSunucuPort;
            string mail = secilen.EmailKullaniciAdi;
            string sifre = secilen.EmailParola;
            string mailkutusu = secilen.EPosta; //firmanın mail adresi

            var fromAddress = new MailAddress(secilen.EmailKullaniciAdi, secilen.FirmaAdi);
            var toAddress = new MailAddress(mailkutusu);
            string subject = "Ödeme Bilgisi";

            var smtp = new SmtpClient
            {
                Host = sunucu,
                Port = Convert.ToInt32(port),
                EnableSsl = sslDurumu,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(mail, sifre)
            };
            var message = new MailMessage(fromAddress, toAddress);
            message.Subject = subject;
            message.IsBodyHtml = true;
            message.AlternateViews.Add(getEmbeddedImage(Server.MapPath("~/img/logo.png"),
                Server.MapPath("~/img/secure.png"), "ttr", "deneme"));
            {
                smtp.Send(message);
            }

        }

        private void dosyayaYaz(string mesaj)
        {
            string dosya_yolu = Server.MapPath("~/Dosyalar/Islemler_" + DateTime.Now.ToString("dd.MM.yyy") + ".txt");
            //İşlem yapacağımız dosyanın yolunu belirtiyoruz.

            FileStream fs = new FileStream(dosya_yolu, FileMode.Append, FileAccess.Write);
            //Bir file stream nesnesi oluşturuyoruz. 1.parametre dosya yolunu,
            //2.parametre dosya varsa açılacağını yoksa oluşturulacağını belirtir,
            //3.parametre dosyaya erişimin veri yazmak için olacağını gösterir.

            StreamWriter sw = new StreamWriter(fs);
            //Yazma işlemi için bir StreamWriter nesnesi oluşturduk.

            sw.WriteLine(mesaj);
            //Dosyaya ekleyeceğimiz iki satırlık yazıyı WriteLine() metodu ile yazacağız.

            sw.Flush();
            //Veriyi tampon bölgeden dosyaya aktardık.
            sw.Close();
            fs.Close();
            //İşimiz bitince kullandığımız nesneleri iade ettik.
        }

        public void CallBack(string orderid)
        {
            dosyayaYaz("CallBack metodu çalıştı.orderid:" + orderid + " // " + DateTime.Now);

            string hata;

            if (String.IsNullOrEmpty(orderid))
            {
                orderid = Request.Form.Get("orderid");
                dosyayaYaz("CallBack metodu çalıştı.Orderid boş geldi.Request ile alındı.orderid:" + orderid + " // " + DateTime.Now);
            }

            OdemeEntities _context = new OdemeEntities();
            Odemeler gelenOdeme = _context.Odemeler.FirstOrDefault(x => x.SiparisNo == orderid);

            if (gelenOdeme.Onay == true)
            {
                dosyayaYaz("CallBack metodu çalıştı.Secilen ödeme bulundu.Ödeme daha önceden onaylanmış.orderid:" + orderid + " // " + DateTime.Now);

                TempData["CreditCard"] = null;
                TempData["firmaParametreleri"] = null;
                Session["YapiKrediData"] = null;
                Session["ServiceObj"] = null;

                return;
            }


            string strMDStatus;
            string strMDStatusText = null;

            if (Request.Form.Get("mdstatus") != null)
            {
                strMDStatus = Request.Form.Get("mdstatus");
                if (Request.Form.Get("mderrormessage") != null)
                {
                    strMDStatusText = Request.Form.Get("mderrormessage") + " - " + Request.Form.Get("hostmsg");
                }
                else if (Request.Form.Get("mdErrorMsg") != null)
                {
                    strMDStatusText = Request.Form.Get("mdErrorMsg");
                }
                else if (Request.Form.Get("ErrorMsg") != null || Request.Form.Get("ErrMsg") != null)
                {
                    strMDStatusText = Request.Form.Get("ErrorMsg") + " - " + Request.Form.Get("ErrMsg");
                }

            }
            else if (Request.Form.Get("3DStatus") != null)
            {
                strMDStatus = Request.Form.Get("3DStatus");
                strMDStatusText = Request.Form.Get("ErrMsg") + " - " + Request.Form.Get("ProcReturnCode");
                orderid = Request.Form.Get("OrderId");
            }
            else
            {
                strMDStatus = "";
            }

            hata = "(" + strMDStatus + ")" + strMDStatusText;
            dosyayaYaz("CallBack metodu çalıştı.Hata: " + hata + ".orderid:" + orderid + " // " + DateTime.Now);
        }
    }
}