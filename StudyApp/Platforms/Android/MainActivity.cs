using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using AndroidColor = Android.Graphics.Color;

namespace StudyApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ApplySystemBarTheme();
        }

        protected override void OnResume()
        {
            base.OnResume();
            ApplySystemBarTheme();
        }

        private void ApplySystemBarTheme()
        {
            if (Window is null)
            {
                return;
            }

            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.SetStatusBarColor(AndroidColor.ParseColor("#EAF4FF"));
            Window.SetNavigationBarColor(AndroidColor.ParseColor("#F7FBFF"));

            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (controller is null)
            {
                return;
            }

            controller.AppearanceLightStatusBars = true;
            controller.AppearanceLightNavigationBars = true;
        }
    }
}
