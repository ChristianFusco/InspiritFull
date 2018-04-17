using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Timers;
using static Android.Support.V7.Widget.RecyclerView;
using Xamarin.Forms;
using inspirit5;

namespace inspirit.Droid
{
    namespace HelloXamarinFormsWorld.Android
    {
        [Activity(Label = "Inspirit", MainLauncher = true,
            ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
        public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
        {
            protected override void OnCreate(Bundle bundle)
            {
                base.OnCreate(bundle);
                Xamarin.Forms.Forms.Init(this, bundle);
                App app = new App();
                LoadApplication(app);
            }
        }
    }
}