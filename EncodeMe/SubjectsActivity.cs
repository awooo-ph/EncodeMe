using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Views;
using Android.Widget;
using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe
{
    [Activity(Label = "@string/app_name", Icon = "@drawable/ic_launcher", Theme = "@style/Theme")]
    class SubjectsActivity : Activity
    {
        private ListView _subjectsView;
        private Button _submitButton;
        private Button _addButton;
        private ProgressBar _progress;
        private Student _student;
        private ISharedPreferences _pref;
        
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.Subjects);

            _pref = PreferenceManager.GetDefaultSharedPreferences(this);

            _subjectsView = FindViewById<ListView>(Resource.Id.SubjectsListView);
            _submitButton = FindViewById<Button>(Resource.Id.SubmitButton);
            _addButton = FindViewById<Button>(Resource.Id.AddSubjectButton);
            _progress = FindViewById<ProgressBar>(Resource.Id.Progress);

            _progress.Visibility = ViewStates.Gone;

            //_schedules = Client.ClassSchedules;// await Db.GetAll<ClassSchedule>();
            _submitButton.Enabled = !Client.EnrollmentCommited;
            _subjectsView.Adapter = new SubjectsAdapter(this, Client.ClassSchedules);

            _student = Client.CurrentStudent;
            if (_student == null)
            {
                StartActivity(typeof(StudentIntroActivity));
                Finish();
            }

            if (_pref.GetBoolean(Constants.ENROLLMENT_PROCESSING, false))
            {
                StartActivity(typeof(StatusActivity));
                Finish();
            }
            
            _addButton.Click += (s, args) =>
            {
                 StartActivityForResult(typeof(ScheduleBrowserActivity),7);
            };
            
            _submitButton.Click+= SubmitButtonOnClick;
        }

        private async void SubmitButtonOnClick(object sender, EventArgs eventArgs)
        {
            var edit = _pref.Edit();
            edit.PutBoolean("Subjects_Processing", true);
            edit.PutBoolean("Enrollment_Accepted", false);
            edit.Commit();

            _subjectsView.Enabled = false;
            _submitButton.Enabled = false;
            _addButton.Enabled = false;
            _progress.Visibility = ViewStates.Visible;
            
            var result = await Client.CommitEnrollment();
            
            edit = _pref.Edit();
            edit.PutBoolean("Subjects_Processing", true);
            edit.Commit();
            
            _subjectsView.Enabled = true;
            _addButton.Enabled = true;
            _submitButton.Enabled = true;
            _progress.Visibility = ViewStates.Gone;
            
            if (result?.Success ?? false)
            {
                StartActivity(typeof(StatusActivity));
                Finish();
            }
            else
            {
                var dlg = new AlertDialog.Builder(this);
                dlg.SetMessage(result?.ErrorMessage??"Request timeout");
                dlg.SetPositiveButton("OK", (o, a) => { });
                dlg.Show();
            }
            
        }

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode != 7 || resultCode != Result.Ok) return;
            
            _subjectsView.Adapter = new SubjectsAdapter(this, Client.ClassSchedules);
            _submitButton.Enabled = true;
        }
        
        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);

            //_schedules = await Db.GetAll<ClassSchedule>();
            _subjectsView.Adapter = new SubjectsAdapter(this, Client.ClassSchedules);
        }
    }
    
}