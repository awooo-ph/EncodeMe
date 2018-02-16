using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Preferences;
using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe
{
    [Activity(Label = "@string/app_name", Icon = "@drawable/ic_launcher", MainLauncher = true, NoHistory = true,
        Theme = "@style/Theme.Splash", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : Activity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        { 
            base.OnCreate(savedInstanceState);
            
            Client.Start();

            Student stud = null;// (await Db.GetAll<Student>()).FirstOrDefault();
            if (stud == null)
                StartActivity(new Intent(Application.Context, typeof(StudentIntroActivity)));
            else
            {
                var pref = PreferenceManager.GetDefaultSharedPreferences(this);
                var proc = pref.GetBoolean(Constants.ENROLLMENT_PROCESSING, false);
                if (proc)
                {
                    StartActivity(new Intent(Application.Context, typeof(StatusActivity)));
                }
                else StartActivity(new Intent(Application.Context, typeof(SubjectsActivity)));
                
            }
            Finish();
        }
    }
}

