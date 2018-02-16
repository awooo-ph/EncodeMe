using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using NORSU.EncodeMe.Network;
using SQLite;

namespace NORSU.EncodeMe
{
    [Activity(Label = "Schedule Browser", Theme = "@style/Theme")]
    public class ScheduleBrowserActivity : Activity
    {
        private Button _goButton;
        private EditText _subjectCode;
        private ListView _schedulesListView;
        private ProgressBar _progress;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Create your application here
            SetContentView(Resource.Layout.ScheduleBrowser);

            _goButton = FindViewById<Button>(Resource.Id.GoButton);
            _subjectCode = FindViewById<EditText>(Resource.Id.SubjectText);
            _schedulesListView = FindViewById<ListView>(Resource.Id.SchedulesListView);
            _progress = FindViewById<ProgressBar>(Resource.Id.Progress);

            _schedulesListView.ItemClick += SchedulesListViewOnItemClick;
            
            _progress.Visibility = ViewStates.Gone;
            _goButton.Click += GoButtonOnClick;
        }

        private async void GoButtonOnClick(object sender, EventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(_subjectCode.Text)) return;

            _progress.Visibility = ViewStates.Visible;
            _schedulesListView.Enabled = false;
            
            var result = await Client.GetSchedules(_subjectCode.Text);

            _progress.Visibility = ViewStates.Gone;
            _schedulesListView.Enabled = true;

            if (result == null) return;

            _schedulesListView.Adapter = new SchedulesAdapter(this, result.Schedules);

            if (result.Result != ResultCodes.Success)
            {
                var dlg = new AlertDialog.Builder(this);

                switch (result.Result)
                {
                    case ResultCodes.Offline:
                        dlg.SetMessage("You are not connected to the server.");
                        break;
                    case ResultCodes.Timeout:
                        dlg.SetMessage("Request timeout.");
                        break;
                    case ResultCodes.NotFound:
                        dlg.SetMessage($"{_subjectCode.Text} does not exists.");
                        break;
                    case ResultCodes.Error:
                        dlg.SetTitle("Request failed.");
                        break;
                }
                dlg.SetPositiveButton("OK", (o, args) => { });
                dlg.Show();

            }
            else if (result.Schedules != null)
            {
                await Db.DropTable<ClassSchedule>("cache.db");
                await Db.InsertAllAsync(result.Schedules,"cache.db");
            }
        }

        protected override async void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            _schedulesListView.Adapter = new SchedulesAdapter(this,
                await Db.GetAll<ClassSchedule>("cache.db"));
            var proc = savedInstanceState.GetBoolean("browser_processing", false);
            _progress.Visibility = proc ? ViewStates.Visible : ViewStates.Gone;
            _schedulesListView.Enabled = !proc;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("browser_subject",_subjectCode.Text);
            outState.PutBoolean("browser_processing",_progress.Visibility == ViewStates.Visible);
            base.OnSaveInstanceState(outState);
        }

        private async void SchedulesListViewOnItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var adapter = _schedulesListView.Adapter as SchedulesAdapter;
            if (adapter == null) return;
            var sched = adapter[e.Position];
            
            var resultIntent = new Intent();
            resultIntent.PutExtra("id", sched.ClassId);

            var res = await Client.AddSchedule(sched);
            if (res?.Success ?? false)
            {
                Client.ClassSchedules.Add(sched);
                SetResult(Result.Ok, resultIntent);
                Finish();
            }
            else
            {
                var dlg = new AlertDialog.Builder(this);
                dlg.SetTitle(res == null ? "Can not find server" : res.ErrorMessage);
                dlg.SetPositiveButton("Okay", (o, args) =>
                {

                });

                dlg.Show();
            }
            
            
        }
    }
}