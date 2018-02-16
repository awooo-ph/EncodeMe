using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NORSU.EncodeMe.Models;

namespace NORSU.EncodeMe.Network
{
    static class AndroidHandler
    {

        public static void HandShakeHandler(PacketHeader packetheader, Connection connection,
            AndroidInfo ad)
        {
            var device = AndroidDevice.Cache.FirstOrDefault(x => x.IP == ad.IP);
            if (device == null)
            {
                device = new AndroidDevice();
            }

            device.IP = ad.IP;
            device.Port = ad.Port;
            device.DeviceId = ad.DeviceId;
            device.MAC = ad.MAC;
            device.Model = ad.Model;
            device.SIM = ad.Sim;
            device.Save();

            var localEPs = Connection.AllExistingLocalListenEndPoints();
            var serverInfo = new ServerInfo(Environment.MachineName);
            var ip = new IPEndPoint(IPAddress.Parse(ad.IP), ad.Port);

            foreach (var localEP in localEPs[ConnectionType.UDP])
            {
                var lEp = localEP as IPEndPoint;

                if (lEp == null) continue;
                if (!ip.Address.IsInSameSubnet(lEp.Address)) continue;

                serverInfo.IP = lEp.Address.ToString();
                serverInfo.Port = lEp.Port;
                serverInfo.Send(ip);
                break;
            }
        }

        public static void StudentInfoRequested(PacketHeader packetheader, Connection connection,
            StudentInfoRequest incomingobject)
        {
            var result = new StudentInfoResult();
            if (string.IsNullOrWhiteSpace(incomingobject.StudentId))
            {
                result.Result = ResultCodes.NotFound;
                SendStudentInfoResult(result, connection);
                return;
            }
            //Not for production
            var student = Models.Student.Cache.FirstOrDefault(x => x.StudentId == incomingobject.StudentId);
            
            if (student == null)
            {
                result.Result = ResultCodes.NotFound;
                SendStudentInfoResult(result, connection);
                return;
            }

            if (string.IsNullOrEmpty(student.Password))
                student.Password = incomingobject.Password;

            if (student.Password != incomingobject.Password)
            {
                result.Result = ResultCodes.NotFound;
                SendStudentInfoResult(result, connection);
                return;
            }
            
            result.Result = ResultCodes.Success;
            result.Student = new Student()
            {
                Course = student.Course.Acronym,
                FirstName = student.FirstName,
                Id = student.Id,
                LastName = student.LastName,
                Picture = student.Picture,
                StudentId = student.StudentId,
                BirthDate = student.BirthDate,
                Male = student.Sex==Sexes.Male,
                Address = student.Address,
                Major = student.Major,
                Minor = student.Minor,
                Scholarship = student.Scholarship,
            };
            
            SendStudentInfoResult(result,connection);
        }

        private static void SendStudentInfoResult(StudentInfoResult result, Connection con)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) con.ConnectionInfo.RemoteEndPoint).Address.ToString());
            
            //Maybe do not ignore this on production
            if (dev == null) return;

            var ip = new IPEndPoint(IPAddress.Parse(dev.IP),dev.Port);
            result.Send(ip);
        }

        public static void ScheduleRequestHandler(PacketHeader packetheader, Connection con, SchedulesRequest incomingobject)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) con.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null) return;
            
            var result = new SchedulesResult(){Serial = incomingobject.Serial,Subject = incomingobject.SubjectCode};
            var subject = Models.Subject.Cache.FirstOrDefault(x=>x.Code.ToLower()==incomingobject.SubjectCode.ToLower());
            if (subject == null)
                result.Result = ResultCodes.NotFound;
            else
            {
                var schedules = Models.ClassSchedule.Cache.Where(x => x.Subject.Code == subject.Code);
                result.Result = ResultCodes.Success;
                result.Schedules = new List<ClassSchedule>();
                foreach (var sched in schedules)
                {
                    result.Schedules.Add(new ClassSchedule()
                    {
                        ClassId = sched.Id,
                        Instructor = sched.Instructor,
                        Schedule = sched.Description,
                        SubjectCode = sched.Subject.Code,
                        Room = sched.Room,
                        Slots = sched.Slots,
                        Enrolled = GetEnrolled(sched.Id)
                    });
                }
            }
            
            result.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }

        private static int GetEnrolled(long id)
        {
            return RequestDetail.Cache.Count(
                x =>x.ScheduleId == id &&
                    (Request.Cache.FirstOrDefault(d => d.Id == x.RequestId)?.Status == Request.Statuses.Accepted));
        }
        
        public static void EnrollRequestHandler(PacketHeader packetheader, Connection connection, EnrollRequest incomingobject)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null) return;

            var ep = new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port);
            
            var req = Request.Cache.FirstOrDefault(x => x.StudentId == incomingobject.StudentId);
            if(req==null) req = new Request()
            {
                StudentId = incomingobject.StudentId,
            };

            if (req.Status == Request.Statuses.Proccessing)
            {
                new EnrollResult(ResultCodes.Processing).Send(ep);
                return;
            }

            if (req.Status == Request.Statuses.Accepted)
            {
                new EnrollResult(ResultCodes.Enrolled).Send(ep);
                return;
            }
            
            req.DateSubmitted = DateTime.Now;
            req.Status = Request.Statuses.Pending;
            req.ReceiptNumber = incomingobject.ReceiptNumber;
            req.Save();
            
            //RequestDetail.DeleteWhere(nameof(RequestDetail.RequestId),req.Id);

            foreach (var sched in incomingobject.ClassSchedules)
            {
                var detail = RequestDetail.Cache.FirstOrDefault(x => x.Schedule.Subject.Code == sched.SubjectCode) ?? new RequestDetail()
                {
                    RequestId = req.Id,
                    ScheduleId = sched.ClassId,
                    Status = Request.Statuses.Pending,
                };
                detail.Save();
            }
            
            var result = new EnrollResult(ResultCodes.Success)
            {
                QueueNumber = Request.Cache.Count(x => x.Status == Request.Statuses.Pending),
            };
            if (req.Status == Request.Statuses.Proccessing)
                result.QueueNumber = 0;

            result.Send(ep);
        }

        public static void RegisterStudentHandler(PacketHeader packetheader, Connection connection, RegisterStudent reg)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null) return;

            var stud = Models.Student.Cache.FirstOrDefault(x => x.StudentId == reg.Student.StudentId);
            stud = new Models.Student
            {
                FirstName = reg.Student.FirstName,
                LastName = reg.Student.LastName,
                //Course = reg.Student.Course,
                StudentId = reg.Student.StudentId
            };
            stud.Save();

            new RegisterStudentResult(ResultCodes.Success) { StudentId = stud.Id}
                .Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }

        public static void GetCoursesHandler(PacketHeader packetheader, Connection connection, GetCourses incomingobject)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null)
                return;
            
            var result = new Courses();
            foreach (var course in Models.Course.Cache.ToList())
            {
                result.Items.Add(new Course()
                {
                    Id = course.Id,
                    Name = course.Acronym,
                });
            }

            result.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }

        public static async void StartEnrollmentHandler(PacketHeader packetheader, Connection connection, StartEnrollment req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null)
                return;

            var request = Models.Request.Cache.FirstOrDefault(x => x.ReceiptNumber?.ToLower() == req.Receipt.ToLower());
            if (request == null)
            {
                request = new Request()
                {
                    StudentId = req.StudentId,
                    ReceiptNumber = req.Receipt,
                    
                };
                await request.SaveAsync();
            }
            
            if (request.StudentId == req.StudentId)
            {
                var result = new StartEnrollmentResult()
                {
                    Success = true,
                    TransactionId = request.Id,
                    Submitted = request.Submitted,
                };

                foreach (var item in request.Details)
                {
                    result.ClassSchedules.Add(new ClassSchedule()
                    {
                        ClassId = item.ScheduleId,
                        Enrolled = Models.ClassSchedule.GetEnrolled(item.ScheduleId),
                        Instructor = item.Schedule.Instructor,
                        Schedule = item.Schedule.Description,
                        Room = item.Schedule.Room,
                        Slots = item.Schedule.Slots,
                        SubjectCode = item.Schedule.Subject.Code
                    });
                }

                await result.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
            }
            else
            {
                await new StartEnrollmentResult()
                {
                    Success = false,
                    ErrorMessage = "OR Number is already used"
                }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
            }
        }

        public static async void AddScheduleHandler(PacketHeader packetheader, Connection connection, AddSchedule req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null) return;

            var request = Models.Request.Cache.FirstOrDefault(x => x.Id == req.TransactionId);
            if ((request == null) || (request.StudentId != req.StudentId))
            {
                await new AddScheduleResult()
                {
                    Success = false,
                    ErrorMessage = "Invalid request"
                }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
                return;
            }
            
            if(request.Details.Any(x=>x.ScheduleId==req.ClassId)) return;
            
            new RequestDetail()
            {
                RequestId = req.TransactionId,
                ScheduleId = req.ClassId,
            }.Save();

            await new AddScheduleResult()
            {
                Success = true,
            }.Send(new IPEndPoint(IPAddress.Parse(dev.IP),dev.Port));
        }

        public static async void CommitEnrollmentHandler(PacketHeader packetheader, Connection connection, CommitEnrollment req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null)
                return;
            var request = Models.Request.Cache.FirstOrDefault(x => x.Id == req.TransactionId);
            if (request == null || request.StudentId != req.StudentId)
            {
                await new CommitEnrollmentResult()
                {
                    Success = false,
                    ErrorMessage = "Invalid request"
                }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
                return;
            }
            request.Submitted = true;
            request.DateSubmitted = DateTime.Now;
            request.Save();
            
            await new CommitEnrollmentResult()
            {
                Success = true,
                QueueNumber = request.GetQueueNumber()
            }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }
    }
}
