﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NORSU.EncodeMe.Models;
using NORSU.EncodeMe.Properties;

namespace NORSU.EncodeMe.Network
{
    static partial class Server
    {
        public static void LoginHandler(PacketHeader packetheader, Connection connection, Login login)
        {
            var ip = ((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);
            if (!(client?.IsEnabled ?? false))
            {
                Activity.Log(
                    Activity.Categories.Network,
                    Activity.Types.Warning,
                    $"Login attempted at an unauthorized terminal ({ip}).");
                return;
            }

            client.IsOnline = true;
            if ((DateTime.Now - client.LastHeartBeat).TotalSeconds > Settings.Default.LoginAttemptTimeout)
                client.LoginAttempts = 0;
            
            client.Update("LastHeartBeat", DateTime.Now);
            client.LoginAttempts++;

            if (client.LoginAttempts > Settings.Default.MaxLoginAttempts)
            {
                TerminalLog.Add(client.Id, "Login is disabled. Maximum attempts reached.", TerminalLog.Types.Warning);
                new LoginResult(ResultCodes.Error, "Too many failed attempts").Send((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);
                return;
            }
            
            var encoder = Models.Encoder.Cache.FirstOrDefault(
                x => string.Equals(x.Username.Trim(), login.Username, StringComparison.CurrentCultureIgnoreCase) &&
                     !string.IsNullOrWhiteSpace(x.Username));

            if (encoder != null && !string.IsNullOrEmpty(login.Password) &&
                (encoder.Password == login.Password || string.IsNullOrEmpty(encoder.Password)))
            {
                encoder.Update(nameof(encoder.Password), login.Password);
                TerminalLog.Add(client.Id, $"{encoder.Username} has logged in.");
                //Logout previous session if any.
                var cl = Client.Cache.FirstOrDefault(x => x.Encoder?.Id == encoder.Id);
                cl?.Logout($"You are logged in at another terminal ({cl.Hostname}).");

                client.Encoder = encoder;
                new LoginResult(new Encoder()
                {
                    Username = encoder.Username,
                    FullName = encoder.FullName,
                    Picture = encoder.Thumbnail
                }).Send((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint);
                client.LoginAttempts = 0;
            }
            else
            {
                TerminalLog.Add(client.Id, $"{encoder.Username} attempted to login but failed because the password is invalid.");
                new LoginResult(ResultCodes.Error, "Invalid username/password").Send((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);
            }

            SendEncoderUpdates();
        }

        public static void HandShakeHandler(PacketHeader packetheader, Connection connection, EndPointInfo ep)
        {
            //Get known client or create new one.
            var client = Client.Cache.FirstOrDefault(x => x.IP == ep.IP) ?? new Client();
            
            client.IP = ep.IP;
            client.Hostname = ep.Hostname;
            client.Port = ep.Port;
            client.LastHeartBeat = DateTime.Now;
            var isNew = client.Id == 0;
            client.Save();
            client.IsOnline = true;
            if (isNew)
                TerminalLog.Add(client.Id, "Encoder terminal added.");

            TerminalLog.Add(client.Id, "Terminal is online.");
            
            var localEPs = Connection.AllExistingLocalListenEndPoints();
            var serverInfo = new ServerInfo(Environment.MachineName);
            var ip = new IPEndPoint(IPAddress.Parse(ep.IP), ep.Port);

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
            
            SendEncoderUpdates();
        }
        
        public static void GetWorkHandler(PacketHeader packetheader, Connection connection, GetWork req)
        {
            var ip = ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);
            if (!(client?.IsEnabled ?? false))
            {
                Activity.Log(
                    Activity.Categories.Network,
                    Activity.Types.Warning,
                    $"Work item requested at an unauthorized terminal ({ip}).");
                return;
            }

            client.IsOnline = true;
            TerminalLog.Add(client.Id, "Work item requested.");
            
            var work = Request.GetNextRequest();
            if (work == null)
            {
                new GetWorkResult(ResultCodes.NotFound).Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
                return;
            }
            
            work.Update(nameof(work.Status),Request.Statuses.Proccessing);

            var student = Models.Student.Cache.FirstOrDefault(x => x.StudentId == work.StudentId);
            
            var result = new GetWorkResult(ResultCodes.Success)
            {
                RequestId = work.Id,
                StudentId = work.StudentId?.ToUpper(),
                StudentName = $"{student?.FirstName} {student?.LastName}"
            };

            var items = RequestDetail.Cache.Where(x => x.RequestId == work.Id).ToList();
            foreach (var item in items)
            {
                var sched = Models.ClassSchedule.Cache.FirstOrDefault(x => x.Id == item.ScheduleId);
                result.ClassSchedules.Add(new ClassSchedule()
                {
                    ClassId = item.ScheduleId,
                    SubjectCode = item.SubjectCode,
                    Instructor = sched?.Instructor,
                    Room = sched?.Room,
                    Schedule = sched?.Description,
                });
            }

            result.Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
            
            SendEncoderUpdates();
        }

        private static void SaveWorkHandler(PacketHeader packetheader, Connection connection, SaveWork i)
        {
            var ip = ((IPEndPoint) connection.ConnectionInfo.RemoteEndPoint).Address.ToString();
            var client = Client.Cache.FirstOrDefault(x => x.IP == ip);
            if (!(client?.IsEnabled ?? false))
                return;

            client.IsOnline = true;
            TerminalLog.Add(client.Id, "Enrollment item completed.");

            var req = Request.Cache.FirstOrDefault(x => x.StudentId == i.StudentId);
            if (req == null) return;
            
            foreach (var sched in i.ClassSchedules)
            {
                var s = RequestDetail.Cache.FirstOrDefault(x => x.RequestId == req.Id && x.ScheduleId == sched.ClassId);
                if(s==null) continue;
                var stat = Request.Statuses.Pending;
                switch (sched.EnrollmentStatus)
                {
                    case ScheduleStatuses.Accepted:
                        stat = Request.Statuses.Accepted;
                        break;
                    case ScheduleStatuses.Conflict:
                        stat = Request.Statuses.Conflict;
                        break;
                    case ScheduleStatuses.Closed:
                        stat = Request.Statuses.Closed;
                        break;
                }
                if (stat > req.Status) req.Status = stat;
                s.Update(nameof(s.Status), stat);
            }
            req.Save();
            
            var result = new SaveWorkResult();
            result.Result = ResultCodes.Success;
            result.Send(new IPEndPoint(IPAddress.Parse(client.IP), client.Port));
        }
    }
}