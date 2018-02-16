using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using NetworkCommsDotNet.Connections.UDP;
using NORSU.EncodeMe.Annotations;
using ProtoBuf;

namespace NORSU.EncodeMe.Network
{
    [ProtoContract]
    abstract class Message<T> :  INotifyPropertyChanged where T : Message<T>
    {
        
        
        private static Dictionary<Type, string> _headers = new Dictionary<Type, string>();
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static object _headerLock = new object();
        public static string GetHeader()
        {
            lock (_headerLock)
            {
                if (_headers.ContainsKey(typeof(T)))
                    return _headers[typeof(T)];
            
                var header = typeof(T).Name;
                _headers.Add(typeof(T),header);
                return header;
            }
        }

        public virtual Task Send(IPEndPoint ip)
        {
            return Send(GetHeader(), (T)this, ip);
        }

        protected static async Task Send(string msgType, T message, IPEndPoint ip)
        {
            var sent = false;
            while (!sent)
            {
                try
                {
                    UDPConnection.SendObject(msgType, message, ip, NetworkComms.DefaultSendReceiveOptions, ApplicationLayerProtocolStatus.Enabled);
                    sent = true;
                    break;
                }
                catch (Exception ex)
                {
#if __ANDROID__
                    await Task.Delay(100);
#else
                    
                    await TaskEx.Delay(100);
#endif
                }
            }
        }

        private static IPEndPoint _broadcastEP;

        public static Task Broadcast(string type, T message)
        {
            if (_broadcastEP == null)
                _broadcastEP = new IPEndPoint(IPAddress.Broadcast, Config.ServerPort);
            return Send(type, message, _broadcastEP);
        }
    }
}
