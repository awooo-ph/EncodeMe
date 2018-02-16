using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Webkit;
using Android.Widget;

using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe
{
    [Activity(Label = "@string/app_name", Icon = "@drawable/ic_launcher", Theme = "@style/Theme.FullScreen", NoHistory = true,
        ConfigurationChanges = ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
    public class StudentIntroActivity : Activity
    {
        private ProgressBar _progressBar;
        private EditText _studentId,_password;
        private Button _next;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.StudentIntro);
            
            _progressBar = FindViewById<ProgressBar>(Resource.Id.Progress);
            _studentId = FindViewById<EditText>(Resource.Id.StudentId);
            _next = FindViewById<Button>(Resource.Id.NextButton);
            _password = FindViewById<EditText>(Resource.Id.Password);
            
            _next.Click += NextOnClick;

           // var img = FindViewById<GifImageView>(Resource.Id.gifImageView);
           // var stream = Assets.Open("loading.gif");
          //  byte[] bytes;
          //  using (MemoryStream ms = new MemoryStream())
          //  {
          //      stream.CopyTo(ms);
          //      bytes = ms.ToArray();
          //  }
          //  img.SetBytes(bytes);
         //   img.StartAnimation();
            
        }

        private async void NextOnClick(object sender, EventArgs eventArgs)
        {
            _progressBar.Indeterminate = true;
            _studentId.Enabled = false;
            _next.Enabled = false;
            
            var pref = PreferenceManager.GetDefaultSharedPreferences(this);
            var edit = pref.Edit();
            edit.PutBoolean("submitted", true);
            edit.PutInt("intro", 2);
            edit.PutString("studentId", _studentId.Text);
            edit.Commit();

            var result = await Client.GetStudentInfo(_studentId.Text, _password.Text);

            _progressBar.Indeterminate = false;
            _studentId.Enabled = true;
            _next.Enabled = true;
            _studentId.SelectAll();
            
            if (result.Result == ResultCodes.Success && result.Student != null)
            {
                await Db.DropTable<Student>();
                await Db.Save(result.Student);
                StartActivity(new Intent(Application.Context, typeof(StudentInfo)));
                Finish();
            }
            else
            {
                var dlg = new AlertDialog.Builder(this);
                if (result.Result == ResultCodes.NotFound)
                {
                    dlg.SetTitle("Student ID not found");
                    dlg.SetPositiveButton("Okay", (o, args) =>
                    {
                        
                    });
                }
                else
                {
                    dlg.SetTitle(result.Message);
                    dlg.SetMessage("Please make sure you are connected to the network.");
                    dlg.SetPositiveButton("Retry", (o, args) =>
                    {
                        NextOnClick(sender, eventArgs);
                    });

                    dlg.SetCancelable(true);
                    dlg.SetNegativeButton("Cancel", (o, args) =>
                    {
                        edit = pref.Edit();
                        edit.PutBoolean("submitted", false);
                        edit.PutInt("intro", 1);
                        edit.Commit();

                    });
                }
                
                
                dlg.Show();
            }
        }
        
        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            
            var pref = PreferenceManager.GetDefaultSharedPreferences(this);
            _progressBar.Indeterminate = pref.GetBoolean("submitted",false);
            _studentId.Enabled = !pref.GetBoolean("submitted",false);
            _next.Enabled = !pref.GetBoolean("submitted",false);
           _studentId.Text = pref.GetString("studentId", "");
        }
    }
}