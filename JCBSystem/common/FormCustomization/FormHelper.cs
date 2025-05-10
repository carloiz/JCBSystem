using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JCBSystem.common
{
    public static class FormHelper
    {
        // Method para sa pagbukas ng form na may fade-in effect
        public static async void OpenFormWithFade(Form newForm, bool isDialog)
        {
            newForm.Opacity = 0; // Simulan ang bagong form na invisible

            // Ipakita ang form depende kung dialog o hindi
            if (isDialog)
            {
                newForm.Load += async (s, e) => await FadeIn(newForm); // Mag-fade kapag nag-load na
                newForm.ShowDialog();
            }
            else
            {
                newForm.Show();
                await FadeIn(newForm); // Mag-fade agad kapag Show()
            }
        }

        // Method para sa fade-in animation
        private static async Task FadeIn(Form form)
        {
            for (double i = 0; i <= 1; i += 0.1)
            {
                await Task.Delay(20); // Delay para sa smooth transition
                form.Opacity = i;
            }
        }

        // Method para sa pagsara ng form na may fade-out effect
        public static async void CloseFormWithFade(Form formToClose)
        {
            // Fade-out effect
            for (double i = 1; i >= 0; i -= 0.1)
            {
                await Task.Delay(20); // Delay para sa smooth transition
                formToClose.Opacity = i;
            }

            formToClose.Close(); // Isara ang form pagkatapos ng fade-out
        }
    }
}