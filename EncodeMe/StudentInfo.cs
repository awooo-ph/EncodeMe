using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe
{
    [Activity(Label = "@string/app_name", Icon = "@drawable/ic_launcher", Theme = "@style/Theme.FullScreen",
        ConfigurationChanges = ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
    public class StudentInfo : Activity
    {
        private TextView _fullname, _studentId, _birthDate, _gender, _address, _course, _major, _minor, _scholarship;
        private ImageView _picture;
        private Button _enroll;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            if (Client.CurrentStudent == null)
            {
                StartActivity(typeof(StudentIntroActivity));
                Finish();
                return;
            }
            
            base.OnCreate(savedInstanceState);
            
            SetContentView(Resource.Layout.StudentInfo);

            _fullname = FindViewById<TextView>(Resource.Id.Fullname);
            _studentId = FindViewById<TextView>(Resource.Id.StudentId);
            _birthDate = FindViewById<TextView>(Resource.Id.BirthDate);
            _gender = FindViewById<TextView>(Resource.Id.Gender);
            _address = FindViewById<TextView>(Resource.Id.Address);
            _course = FindViewById<TextView>(Resource.Id.Course);
            _major = FindViewById<TextView>(Resource.Id.Major);
            _minor = FindViewById<TextView>(Resource.Id.Minor);
            _scholarship = FindViewById<TextView>(Resource.Id.Scholarship);
            _picture = FindViewById<ImageView>(Resource.Id.Picture);
            _enroll = FindViewById<Button>(Resource.Id.EnrollButton);
            SetupValues();
            
            _enroll.Click+= EnrollOnClick;
        }

        private void EnrollOnClick(object sender, EventArgs eventArgs)
        {
            StartActivity(typeof(GetReceiptActivity));
        }

        private void SetupValues()
        {
            _fullname.Text = $"{Client.CurrentStudent.FirstName} {Client.CurrentStudent.LastName}".ToUpper();
            _studentId.Text = Client.CurrentStudent.StudentId?.ToUpper();
            _birthDate.Text = Client.CurrentStudent.BirthDate?.ToString("MMM d, yyyy")??"N/A";
            _gender.Text = Client.CurrentStudent.Male ? "MALE" : "FEMALE";
            _address.Text = Client.CurrentStudent.Address;
            _course.Text = Client.CurrentStudent.Course;
            _major.Text = Client.CurrentStudent.Major;
            _minor.Text = Client.CurrentStudent.Minor;
            _scholarship.Text = Client.CurrentStudent.Scholarship;
            _picture.SetImageResource(Resource.Drawable.profile);
        }
    }
}