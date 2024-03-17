using posTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using posTest.ViewModels;

namespace posTest.Controllers
{
    public class BayiOdemelerController : Controller
    {
        public BayiOdemelerController()
        {

            OdemeEntities _context = new OdemeEntities();

            if (System.Web.HttpContext.Current.Session["FirmaId"] == null)
            {
                string users = System.Web.HttpContext.Current.User.Identity.Name;
                if (users != "")
                {
                    System.Web.HttpContext.Current.Session["FirmaId"] = _context.FirmaKullaniciBaglama.FirstOrDefault(x => x.KullaniciAdi == users).FirmaId;
                }
            }
        }

        public ActionResult Liste(string start, string end)
        {
            int uyeId = Convert.ToInt32(Session["Login"]);
            OdemeEntities _context = new OdemeEntities();
            var domain = System.Web.HttpContext.Current.Request.Url.Host;
            Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);


            List<Uyeler> uyeler = _context.Uyeler.Where(x => x.FirmaId == secilenFirma.Id).ToList();

            List<Odemeler> odemeListesi = _context.Odemeler.Where(x => x.FirmaId == secilenFirma.Id && x.Onay == true && x.UyeId == uyeId).ToList();
            if (start != null && end != null)
            {
                DateTime baslangic = Convert.ToDateTime(start);
                DateTime bitis = Convert.ToDateTime(end);

                odemeListesi = odemeListesi.Where(x => x.Tarih >= baslangic && x.Tarih <= bitis).ToList();
            }

            OdemeViewModel model = new OdemeViewModel();
            model.uyeListesi = uyeler;
            model.odemeListesi = odemeListesi;

            return View(model);
        }
        [HttpPost]
        public PartialViewResult OdemeDetay(int id)
        {
            int uyeId = Convert.ToInt32(Session["Login"]);
            OdemeEntities _context = new OdemeEntities();
            var domain = System.Web.HttpContext.Current.Request.Url.Host;
            Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

            List<Uyeler> uyeler = _context.Uyeler.Where(x => x.FirmaId == secilenFirma.Id).ToList();

            Odemeler odeme = _context.Odemeler.FirstOrDefault(x => x.FirmaId == secilenFirma.Id && x.Onay == true && x.Id == id);

            OdemeViewModel model = new OdemeViewModel();
            model.Odeme = odeme;
            model.uyeListesi = uyeler;



            return PartialView(model);
        }
        public ActionResult OdemeDetayPrint(int id)
        {

            int uyeId = Convert.ToInt32(Session["Login"]);
            OdemeEntities _context = new OdemeEntities();
            var domain = System.Web.HttpContext.Current.Request.Url.Host;
            Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

            List<Uyeler> uyeler = _context.Uyeler.Where(x => x.FirmaId == secilenFirma.Id).ToList();

            Odemeler odeme = _context.Odemeler.FirstOrDefault(x => x.FirmaId == secilenFirma.Id && x.Onay == true && x.Id == id);

            OdemeViewModel model = new OdemeViewModel();
            model.Odeme = odeme;
            model.uyeListesi = uyeler;



            return View(model);
        }
        public ActionResult Odemeler()
        {
            return View();
        }


        public ActionResult Index()
        {
            if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
            {
                OdemeEntities _context = new OdemeEntities();
                int uyeId = Convert.ToInt32(Session["Login"]);
                var domain = System.Web.HttpContext.Current.Request.Url.Host;
                Firmalar secilen = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);
                List<FirmaBankaBaglama> firmaBankalar = secilen.FirmaBankaBaglama.ToList();
                List<Bankalar> bankaListesi = new List<Bankalar>();
                foreach (var item in firmaBankalar)
                {
                    bankaListesi.Add(item.Bankalar);
                }

                List<Odemeler> odemeListesi = _context.Odemeler.Where(x => x.FirmaId == secilen.Id && x.Onay == true && x.UyeId == uyeId).ToList();

                List<OdemeIstekleri> odemeIstekleriListesi = _context.OdemeIstekleri.Where(x => x.FirmaId == secilen.Id).ToList();

                List<Loglar> loglar = _context.Loglar.Where(x => x.FirmaId == secilen.Id).ToList();

                OdemeViewModel model = new OdemeViewModel();
                model.odemeListesi = odemeListesi;
                model.odemeIstekleriListesi = odemeIstekleriListesi;
                model.Secilen = secilen;
                model.bankaListesi = bankaListesi;
                model.loglarListesi = loglar;

                return View(model);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public PartialViewResult PastaGrafik()
        {
            if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() != "bos")
            {
                OdemeEntities _context = new OdemeEntities();
                int uyeId = Convert.ToInt32(Session["Login"]);
                var domain = System.Web.HttpContext.Current.Request.Url.Host;
                Firmalar secilen = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);
                List<FirmaBankaBaglama> firmaBankalar = secilen.FirmaBankaBaglama.ToList();
                List<Bankalar> bankaListesi = new List<Bankalar>();
                foreach (var item in firmaBankalar)
                {
                    bankaListesi.Add(item.Bankalar);
                }
                List<Odemeler> odemeler = _context.Odemeler.Where(x => x.FirmaId == secilen.Id && x.Onay == true && x.UyeId == uyeId).ToList();
                List<BankaPasta> bankaTutar = new List<BankaPasta>();
                var random = new Random();

                foreach (var item in bankaListesi)
                {
                    double tutar = 0f;
                    if (item.BankaAdi == "Finans Bank")
                        item.BankaAdi = "QNB Finansbank";
                    var bankaOdemeler = odemeler.Where(x => x.Hata == "(" + item.BankaAdi + ")");
                    if (bankaOdemeler.Count() > 0)
                    {
                        foreach (var item2 in bankaOdemeler)
                        {
                            tutar += Convert.ToDouble(item2.Tutar);
                        }
                        odemeler = odemeler.Where(x => x.FirmaId == secilen.Id && x.Onay == true && x.UyeId == uyeId && x.Hata != "(" + item.BankaAdi + ")").ToList();
                    }
                    BankaPasta pasta = new BankaPasta();
                    pasta.Banka = item.BankaAdi;
                    var color = String.Format("#{0:X6}", random.Next(0x1000000));
                    pasta.Renk = color;
                    pasta.Tutar = tutar.ToString();
                    bankaTutar.Add(pasta);


                }


                return PartialView(bankaTutar);
            }
            return PartialView();
        }
    }
    public class BankaPasta
    {
        public string Banka { get; set; }
        public string Tutar { get; set; }

        public string Renk { get; set; }
    }
}