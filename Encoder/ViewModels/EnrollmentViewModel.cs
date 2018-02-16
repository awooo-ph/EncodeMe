using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using NORSU.EncodeMe.Network;

namespace NORSU.EncodeMe.ViewModels
{
    class EnrollmentViewModel : ViewModelBase
    {
        private EnrollmentViewModel() { }

        private static EnrollmentViewModel _instance;
        public static EnrollmentViewModel Instance => _instance ?? (_instance = new EnrollmentViewModel());

        private ObservableCollection<Course> _courses;

        public ObservableCollection<Course> Courses
        {
            get
            {
                if (_courses != null)
                    return _courses;
                _courses = new ObservableCollection<Course>();
                Task.Factory.StartNew(async () =>
                {
                    var res = await Client.GetCoursesDesktop();
                    if (res == null)
                        return;
                    foreach (var course in res.Courses)
                    {
                        if (_courses.Any(x => x.Id == course.Id))
                            continue;
                        awooo.Post(() =>_courses.Add(course));
                    }
                });
                return _courses;
            }
        }

        private Student _Student = new Student()
        {
            BirthDate = DateTime.Now.AddYears(-16)
        };
        
        public Student Student
        {
            get => _Student;
            set
            {
                if(value == _Student)
                    return;
                _Student = value;
                OnPropertyChanged(nameof(Student));
            }
        }

        private bool _IsProcessing;

        public bool IsProcessing
        {
            get => _IsProcessing;
            set
            {
                if(value == _IsProcessing)
                    return;
                _IsProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

        private bool _EnrollmentSuccess;

        public bool EnrollmentSuccess
        {
            get => _EnrollmentSuccess;
            set
            {
                if(value == _EnrollmentSuccess)
                    return;
                _EnrollmentSuccess = value;
                OnPropertyChanged(nameof(EnrollmentSuccess));
            }
        }

        private bool _EnrollmentError;

        public bool EnrollmentError
        {
            get => _EnrollmentError;
            set
            {
                if(value == _EnrollmentError)
                    return;
                _EnrollmentError = value;
                OnPropertyChanged(nameof(EnrollmentError));
            }
        }

        private string _Status;

        public string Status
        {
            get => _Status;
            set
            {
                if(value == _Status)
                    return;
                _Status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public static DateTime MinDate => DateTime.Now.AddYears(-74);

        private ICommand _enrollCommand;

        public ICommand EnrollCommand => _enrollCommand ?? (_enrollCommand = new DelegateCommand(async d =>
        {
            if (Student?.HasError ?? true) return;
            IsProcessing = true;
            EnrollmentSuccess = false;
            EnrollmentError = false;

            var res = await Client.EnrollStudent(Student);

            if (res?.Success ?? false)
            {
                EnrollmentSuccess = true;
                await TaskEx.Delay(2222);
                Student = new Student();
                IsProcessing = false;
            }
            else
            {
                EnrollmentError = true;
                Status = res?.ErrorMessage ?? "Enrollment Failed";
                await TaskEx.Delay(2222);
                IsProcessing = false;
            }

        },d=>!IsProcessing && !(Student?.HasError??true)));
    }
}
