using System.Windows;
using System.Globalization;
using System.Threading;   

namespace CeaIndexer
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {


            string savedLang = CeaIndexer.Properties.Settings.Default.AppLanguage;
            CultureInfo culture;

            if (!string.IsNullOrWhiteSpace(savedLang))
            {
                culture = new CultureInfo(savedLang);
            }

            else
            {
                // Použít jazyk systému
                string systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                if (systemLanguage == "cs")
                {
                    culture = new CultureInfo("cs-CZ");
                }
                else
                {
                    culture = new CultureInfo("en-US");
                }
            }


            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            base.OnStartup(e);

        }
    }
}
