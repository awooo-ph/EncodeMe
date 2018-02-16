using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe
{
    class CoursesAdapter : BaseAdapter<Course>
    {
        private List<Course> _items;
        private Activity _context;

        public CoursesAdapter(Activity context, List<Course> items)
        {
            _items = items;
            _context = context;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override Course this[int position] => _items[position];
        public override int Count => _items.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var item = _items[position];

            var view = convertView ?? _context.LayoutInflater.Inflate(Resource.Layout.CourseRow, null);

            view.FindViewById<TextView>(Resource.Id.course).Text = item.Name;

            return view;
        }
    }
}