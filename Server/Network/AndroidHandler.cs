﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NORSU.EncodeMe.Models;
using NORSU.EncodeMe.Properties;

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
                
                serverInfo.MaxReceipts = Settings.Default.MaxOR;
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
            if (string.IsNullOrWhiteSpace(incomingobject.StudentId)) return;
            
            var student = Models.Student.Cache.FirstOrDefault(x => x.StudentId?.ToLower() == incomingobject.StudentId?.ToLower());
            
            if (student == null)
            {
                result.Success = false;
                result.ErrorMessage = "Student ID does not exists. Please check and try again.";
                SendStudentInfoResult(result, connection);
                return;
            }

            if (string.IsNullOrEmpty(student.Password))
                student.Update(nameof(Models.Student.Password), incomingobject.Password);

            if (student.Password != incomingobject.Password)
            {
                result.Success = false;
                result.ErrorMessage = "Invalid password. Please try again.";
                SendStudentInfoResult(result, connection);
                return;
            }

            result.Success = true;
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
                YearLevel = (int)student.YearLevel,
                Status = (int)student.Status,
            };

            var req = Models.Request.Cache.FirstOrDefault(x =>
                x.Student.StudentId.ToLower() == incomingobject.StudentId.ToLower());
            if (req == null)
            {
                req = new Models.Request()
                {
                    StudentId = student.Id,
                };
                req.Save();
            }
            
            result.RequestStatus = new RequestStatus()
            {
                Id=req.Id,
                IsSubmitted = req.Submitted,
                QueueNumber = req.GetQueueNumber(),
            };
            foreach (var reqReceipt in req.Receipts)
            {
                result.RequestStatus.Receipts.Add(new Receipt()
                {
                    Amount = reqReceipt.AmountPaid,
                    DatePaid = reqReceipt.DatePaid,
                    Number = reqReceipt.Number
                });
            }
            
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

            var student = Models.Student.Cache.FirstOrDefault(x => x.Id == incomingobject.StudentId);
            var subject = Models.Subject.Cache
                .FirstOrDefault(x => x.Code.ToLower() == incomingobject.SubjectCode.ToLower());

            if (student == null || subject == null)
            {
                result.Success = false;
                result.ErrorMessage = $"{incomingobject.SubjectCode.ToUpper()} not found!";
            }
            else
            {

                if (!(Models.CourseSubject.Cache
                    .Any(x => x.CourseId == student?.CourseId &&
                              x.SubjectId == subject.Id)))
                {
                    result.Success = false;
                    result.ErrorMessage = $"{incomingobject.SubjectCode.ToUpper()} is not in your course.";
                }
                else
                {
                    var schedules = Models.ClassSchedule.Cache.Where(x => x.Subject.Code.ToLower() == subject.Code.ToLower() && Models.ClassSchedule.GetEnrolled(x.Id)<x.Slots);
                    result.Success = true;
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
            var student = Models.Student.Cache.FirstOrDefault(x => x.StudentId.ToLower() == incomingobject.StudentId.ToLower());
            if (student == null)
            {
                new EnrollResult(ResultCodes.Error).Send(ep);
                return;
            }
            
            var req = Request.Cache.FirstOrDefault(x => x.StudentId == student.Id);
            
            if(req==null) req = new Request()
            {
                StudentId = student.Id,
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
            req.Save();
            
            foreach (var or in incomingobject.Receipts)
            {
                new Models.Receipt()
                {
                    AmountPaid = or.Amount,
                    DatePaid = or.DatePaid,
                    Number = or.Number,
                    RequestId = req.Id,
                }.Save();
            }
            
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


            var  request = Models.Request.Cache.FirstOrDefault(x=>!x.Submitted && x.Student.Id==req.StudentId);
            
            foreach (var or in req.Receipts)
            {
                var r = Models.Receipt.Cache.FirstOrDefault(x => x.Number?.ToLower() == or.Number?.ToLower());
                
                if (r != null && r.Request.Submitted && r?.Request.Student.Id != req.StudentId)
                {
                    await new StartEnrollmentResult()
                    {
                        Success = false,
                        ErrorMessage = "Invalid OR number"
                    }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
                    return;
                }
            }
            
            if (request == null)
            {
                request = new Request()
                {
                    StudentId = req.StudentId,
                };    
            }
            
            if(request.IsDeleted)
                request.Undelete();
            
            request.Save();
            
            foreach (var or in req.Receipts)
            {
                new Models.Receipt()
                {
                    AmountPaid = or.Amount,
                    DatePaid = or.DatePaid,
                    RequestId = request.Id,
                    Number = or.Number,
                }.Save();
            }
            
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

        public static async void AddScheduleHandler(PacketHeader packetheader, Connection connection, AddSchedule req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null) return;

            var request = Models.Request.Cache.FirstOrDefault(x => x.Id == req.TransactionId);
            var sched = Models.ClassSchedule.Cache.FirstOrDefault(x => x.Id == req.ClassId);
            
            if ((request == null) || (request.StudentId != req.StudentId) || sched==null)
            {
                await new AddScheduleResult()
                {
                    Success = false,
                    ErrorMessage = "Invalid request"
                }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
                return;
            }
          
            var prevSched = request.Details.FirstOrDefault(x => x.Schedule.SubjectId == sched.SubjectId);

            var result = new AddScheduleResult()
            {
                Success = true,
                ReplacedId = prevSched?.ScheduleId??0,
            };

            prevSched?.Delete();
            
            new RequestDetail()
            {
                RequestId = req.TransactionId,
                ScheduleId = req.ClassId,
            }.Save();
            
            await result.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }

        public static async void CommitEnrollmentHandler(PacketHeader packetheader, Connection connection,
            CommitEnrollment req)
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
            
            if(Settings.Default.ResetQueueOnFailure)
                request.DateSubmitted = DateTime.Now;
            else
            if(!request.Details.Any(x=>x.Status==Request.Statuses.Closed || x.Status == Request.Statuses.Conflict))
                request.DateSubmitted = DateTime.Now;

            request.Status = Request.Statuses.Pending;
            
            request.Save();
            var qn = request.GetQueueNumber();

            var receipts = new List<Network.Receipt>();
            foreach (var or in request.Receipts)
            {
                receipts.Add(new Receipt()
                {
                    Amount = or.AmountPaid,
                    DatePaid = or.DatePaid,
                    Number = or.Number,
                });
            }
            
            await new CommitEnrollmentResult()
            {
                Success = true,
                QueueNumber = qn,
                RequestStatus = new RequestStatus()
                {
                    Id = request.Id,
                    IsSubmitted = request.Submitted,
                    QueueNumber = request.GetQueueNumber(),
                    Status = EnrollmentStatus.Pending,
                    Receipts = receipts
                }
            }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));

            Messenger.Default.Broadcast(Messages.RequestUpdated, request);
        }

        private static EnrollmentStatus GetStatus(Request.Statuses requestStatus)
        {
            switch(requestStatus)
            {
                case Request.Statuses.Pending:
                    return EnrollmentStatus.Pending;
                case Request.Statuses.Proccessing:
                    return EnrollmentStatus.Processing;
                case Request.Statuses.Conflict:
                    return EnrollmentStatus.Conflict;
                case Request.Statuses.Closed:
                    return EnrollmentStatus.Closed;
                case Request.Statuses.Accepted:
                    return EnrollmentStatus.Accepted;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestStatus), requestStatus, null);
            }
        }

        public static async void StatusRequestHandler(PacketHeader packetheader, Connection connection, StatusRequest req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null)
                return;

            var student = Models.Student.Cache.FirstOrDefault(x => x.Id == req.StudentId);
            
            var request = Models.Request.Cache.FirstOrDefault(x => x.Id == req.RequestId && x.StudentId==req.StudentId);

            if (student == null || request == null)
            {
                await new StatusResult()
                {
                    Success = false,
                    ErrorMessage = "Invalid Request",
                }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
                return;
            }

            var receipts = new List<Network.Receipt>();
            foreach (var or in request.Receipts)
            {
                receipts.Add(new Receipt()
                {
                    Amount = or.AmountPaid,
                    DatePaid = or.DatePaid,
                    Number = or.Number,
                });
            }

            var res = new StatusResult()
            {
                Success = true,
                RequestStatus = new RequestStatus()
                {
                    Id = request.Id,
                    IsSubmitted = request.Submitted,
                    QueueNumber = request.GetQueueNumber(),
                    Receipts = receipts,
                    Status = GetStatus(request.Status),
                },
            };

            foreach(var item in request.Details)
            {
                res.ClassSchedules.Add(new ClassSchedule()
                {
                    ClassId = item.ScheduleId,
                    Enrolled = Models.ClassSchedule.GetEnrolled(item.ScheduleId),
                    Instructor = item.Schedule.Instructor,
                    Schedule = item.Schedule.Description,
                    Room = item.Schedule.Room,
                    Slots = item.Schedule.Slots,
                    SubjectCode = item.Schedule.Subject.Code,
                    EnrollmentStatus = GetClassStatus(item.Status)
                });
            }

            await res.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }

        private static ScheduleStatuses GetClassStatus(Request.Statuses itemStatus)
        {
            switch(itemStatus)
            {
                case Request.Statuses.Pending:
                    return ScheduleStatuses.Pending;
                case Request.Statuses.Proccessing:
                    return ScheduleStatuses.Pending;
                case Request.Statuses.Conflict:
                    return ScheduleStatuses.Conflict;
                case Request.Statuses.Closed:
                    return ScheduleStatuses.Closed;
                case Request.Statuses.Accepted:
                    return ScheduleStatuses.Accepted;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemStatus), itemStatus, null);
            }
        }

        public static async void CancelEnrollmentHandler(PacketHeader packetheader, Connection connection, CancelEnrollment req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null)
                return;

            var student = Models.Student.Cache.FirstOrDefault(x => x.Id == req.StudentId);
            var request = Models.Request.Cache.FirstOrDefault(x => x.Id == req.RequestId);

            if (student == null || request == null) return;
            
            request.Update(nameof(Models.Request.Submitted),false);

            Messenger.Default.Broadcast(Messages.RequestUpdated,request);
            
            await new CancelEnrollmentResult()
            {
                Success = true,
            }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }

        public static async void RemoveScheduleHandler(PacketHeader packetheader, Connection connection, RemoveSchedule req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null)
                return;

            var request = Models.Request.Cache.FirstOrDefault(x => x.Id == req.TransactionId);
            var sched = Models.ClassSchedule.Cache.FirstOrDefault(x => x.Id == req.ClassId);
            
            var reqDetail = request?.Details.FirstOrDefault(x => req.ClassId == x.ScheduleId);
            
            if ((request == null) || (request.Student?.Id != req.StudentId) || sched == null || reqDetail==null)
            {
                await new RemoveScheduleResult()
                {
                    Success = false,
                    ErrorMessage = "Invalid request"
                }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
                return;
            }
            
            reqDetail.Delete(false);

            await new RemoveScheduleResult()
            {
                Success = true,
            }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }

        public static async void UpdateStudentHandler(PacketHeader packetheader, Connection connection, UpdateStudent req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null)
                return;

            var student = Models.Student.Cache.FirstOrDefault(x => x.Id == req.Id);
            
            if (student==null)
            {
                await new UpdateStudentResult()
                {
                    Success = false,
                    ErrorMessage = "Invalid request"
                }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
                return;
            }

            student.Address = req.Address;
            student.Scholarship = req.Scholarship;
            student.YearLevel = (YearLevels) req.YearLevel;
            student.Status = (StudentStatus) req.Status;
            student.Save();

            await new UpdateStudentResult()
            {
                Success = true,
            }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }

        public static async void ChangePictureHandler(PacketHeader packetheader, Connection connection, ChangePicture req)
        {
            var dev = AndroidDevice.Cache.FirstOrDefault(
                d => d.IP == ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString());

            //Maybe do not ignore this on production
            if (dev == null)
                return;
            var student = Models.Student.Cache.FirstOrDefault(x => x.Id == req.Id);

            if (student == null)
            {
                await new ChangePictureResult()
                {
                    Success = false,
                    ErrorMessage = "Invalid request"
                }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
                return;
            }
            
            student.Update(nameof(Models.Student.Picture),req.Data);

            await new ChangePictureResult()
            {
                Success = true,
            }.Send(new IPEndPoint(IPAddress.Parse(dev.IP), dev.Port));
        }
    }
}
