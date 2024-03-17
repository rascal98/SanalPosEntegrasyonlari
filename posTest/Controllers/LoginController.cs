using posTest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace posTest.Controllers
{
    public class LoginController : Controller
    {
        public LoginController()
        {
            OdemeEntities _context = new OdemeEntities();
            var domain = System.Web.HttpContext.Current.Request.Url.Host;
            Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

            if (secilenFirma.UyelikOdeme == true)
            {
                if (System.Web.HttpContext.Current.Session["Login"] == null)
                {
                    System.Web.HttpContext.Current.Session["Login"] = "false";
                }
            }
            else
            {
                if (secilenFirma.UyeliksizOdeme == true)
                {
                    System.Web.HttpContext.Current.Session["Login"] = "bos";
                }
                else
                {
                    System.Web.HttpContext.Current.Session["Login"] = "false";
                }
            }
        }

        public ActionResult GirisYap()
        {
            if (System.Web.HttpContext.Current.Session["Login"] == null)
            {
                System.Web.HttpContext.Current.Session["Login"] = "false";

                OdemeEntities _context = new OdemeEntities();
                var domain = System.Web.HttpContext.Current.Request.Url.Host;
                Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

                return View(secilenFirma);
            }
            else
            {
                if (System.Web.HttpContext.Current.Session["Login"].ToString() != "false" && System.Web.HttpContext.Current.Session["Login"].ToString() == "bos")
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    OdemeEntities _context = new OdemeEntities();
                    var domain = System.Web.HttpContext.Current.Request.Url.Host;
                    Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

                    return View(secilenFirma);
                }
            }

        }

        [HttpPost]
        public ActionResult GirisYap(string KullaniciAdi, string Sifre)
        {
            OdemeEntities _context = new OdemeEntities();
            var domain = System.Web.HttpContext.Current.Request.Url.Host;
            Firmalar secilenFirma = _context.Firmalar.FirstOrDefault(x => x.Domain == domain);

            if (secilenFirma.Uyeler.Count(x => x.EPosta == KullaniciAdi && x.Sifre == Sifre && x.Durum == true) > 0)
            {
                Uyeler gelen = secilenFirma.Uyeler.FirstOrDefault(x => x.EPosta == KullaniciAdi && x.Sifre == Sifre && x.Durum == true);

                System.Web.HttpContext.Current.Session["Login"] = gelen.Id.ToString();
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View(secilenFirma);
            }
        }

        public ActionResult CikisYap()
        {
            System.Web.HttpContext.Current.Session["Login"] = null;
            return RedirectToAction("GirisYap", "Login");
        }
    }
}