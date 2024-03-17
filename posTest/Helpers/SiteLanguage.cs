using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

namespace posTest.Helpers
{
    public class SiteLanguage
    {
        public static List<Language> AvailableLanguage => new List<Language>
        {
            new Language {LangFullName = "tr", LangCultureName = "tr"},
            new Language {LangFullName = "en", LangCultureName = "en"},
            new Language {LangFullName = "fr", LangCultureName = "fr"},
            new Language {LangFullName = "de", LangCultureName = "de"}
        };

        public static bool IsLanguageAvailable(string lang)
        {
            return AvailableLanguage.Where(x => x.LangFullName.Equals(lang)).FirstOrDefault() != null ? true : false;
        }

        public static string GetDefaultLanguage()
        {
            return AvailableLanguage[0].LangCultureName;
        }

        public void SetLanguage(string lang)
        {
            try
            {
                var cookie = HttpContext.Current.Request.Cookies["culture"];
                if (lang == null && cookie==null)
                {
                    lang = GetDefaultLanguage();
                }
                else
                {
                    if (!string.IsNullOrEmpty(lang))
                    {
                        var cultureInfo = new CultureInfo(lang);
                        Thread.CurrentThread.CurrentUICulture = cultureInfo;
                        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureInfo.Name);
                        HttpCookie langCookie = new HttpCookie("culture", lang);
                        langCookie.Expires = DateTime.Now.AddYears(1);
                        HttpContext.Current.Response.Cookies.Add(langCookie);
                    }
                    else
                    {
                        var cultureInfo = new CultureInfo(cookie.Value);
                        Thread.CurrentThread.CurrentUICulture = cultureInfo;
                        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureInfo.Name);
                    }
                }
            }
            catch
            {
                var cultureInfo = new CultureInfo(lang);
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureInfo.Name);
                HttpCookie langCookie = new HttpCookie("culture", lang);
                langCookie.Expires = DateTime.Now.AddYears(1);
                HttpContext.Current.Response.Cookies.Add(langCookie);
            }
        }

        public class Language
        {
            public string LangFullName { get; set; }
            public string LangCultureName { get; set; }
        }
    }
}