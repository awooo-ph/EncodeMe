using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Preferences;
using Android.Widget;
using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe
{
    [Activity(Label = "@string/app_name",Icon = "@drawable/ic_launcher", Theme = "@style/Theme.FullScreen", NoHistory = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class EditStudentActivity : Activity
    {
        private ProgressBar _progressBar;
        private EditText _firstname;
        private EditText _lastname;
        private Spinner _course;
        private Button _nextButton;
        private string StudentId;

        private const string FIRST_NAME = "Edit_Firstname";
        private const string LAST_NAME = "Edit_Lastname";
        private const string COURSE = "Edit_Course";
        private const string PROCESSING = "Edit_Processing";
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.EditStudent);

            var pref = PreferenceManager.GetDefaultSharedPreferences(this);
            StudentId = pref.GetString("studentId", "");
            
            _progressBar = FindViewById<ProgressBar>(Resource.Id.Progress);
            _firstname = FindViewById<EditText>(Resource.Id.FirstName);
            _lastname = FindViewById<EditText>(Resource.Id.LastName);
            _course = FindViewById<Spinner>(Resource.Id.Course);
            _nextButton = FindViewById<Button>(Resource.Id.NextButton);
            
            _nextButton.Click += NextButtonOnClick;
            
            GetCourses();
        }

        private async void GetCourses()
        {
            var courses = await Client.GetCourses();
            var adapter = new CoursesAdapter(this,courses.Items);
            _course.Adapter = adapter;
        }

        private async void NextButtonOnClick(object sender, EventArgs eventArgs)
        {
            var stud = new Student()
            {
                StudentId = StudentId,
                FirstName = _firstname.Text,
                LastName = _lastname.Text,
               // Course = _course.Text
            };

            _progressBar.Indeterminate = true;
            _firstname.Enabled = false;
            _lastname.Enabled = false;
            _course.Enabled = false;
            _nextButton.Enabled = false;
            
            var result = await Client.Register(stud);

            _progressBar.Indeterminate = false;
            _firstname.Enabled = true;
            _lastname.Enabled = true;
            _course.Enabled = true;
            _nextButton.Enabled = true;

            if (result == ResultCodes.Success)
            {
                await Db.DropTable<Student>();
                await Db.Save(stud);
                StartActivity(typeof(SubjectsActivity));
                Finish();
            }
            var dlg = new AlertDialog.Builder(this);
            if (result == ResultCodes.Offline)
                dlg.SetMessage("Server is unavailable.");
            else if (result == ResultCodes.Timeout)
                dlg.SetMessage("Request timeout");
            else
                dlg.SetMessage("Request failed.");

            dlg.SetPositiveButton("OK", (o, args) => { });
            dlg.Show();
        }
        
        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString(FIRST_NAME, _firstname.Text);
            outState.PutString(LAST_NAME,_lastname.Text);
          //  outState.PutString(COURSE,_course.Text);
            outState.PutBoolean(PROCESSING,_progressBar.Indeterminate);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            _firstname.Text = savedInstanceState.GetString(FIRST_NAME, "");
            _lastname.Text = savedInstanceState.GetString(LAST_NAME, "");
         //   _course.Text = savedInstanceState.GetString(COURSE, "");
            var proc = savedInstanceState.GetBoolean(PROCESSING, false);
            _progressBar.Indeterminate = proc;
            _firstname.Enabled = !proc;
            _lastname.Enabled = !proc;
            _course.Enabled = !proc;
            _nextButton.Enabled = !proc;
            base.OnRestoreInstanceState(savedInstanceState);
        }
    }
}