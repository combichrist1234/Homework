﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SerializationTypes;

namespace WpfChatClient
{
    internal class ServerHandler : IDisposable
    {
        private static TcpClient _messageClient;
        private static NetworkStream _messageStream;
        private static TcpClient _dataClient;
        private static NetworkStream _dataStream;
        private readonly string _host;
        private readonly int _port;
        private readonly int _port2;

        public ServerHandler(string host,int port,int port2)
        {
            _messageClient = new TcpClient();
            _dataClient = new TcpClient();
            _host = host;
            _port = port;
            _port2 = port2;
        }

        // получение сообщений
        public  string ReceiveMessage()
        {
                try
                {
                    var data = new byte[64]; // буфер для получаемых данных
                    var builder = new StringBuilder();
                    do
                    {
                        var read = _messageStream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, read));
                    } while (_messageStream.DataAvailable);
                    return builder.ToString();
                }
                catch
                {  
                    Dispose();
                    return "Подключение прервано!\n";
                }
        }
        // получение сообщений
        public Shape ReceiveData()
        {
            try
            {
                _dataStream = _dataClient.GetStream();
                IFormatter formatter = new BinaryFormatter();
                var deserializingObject = (Shape)formatter.Deserialize(_dataStream);
                return deserializingObject;
            }
            catch
            {
                Dispose();
            }
            return null;
        }


        public void SendMessage(string message)
        {
            var data = Encoding.Unicode.GetBytes(message);
            _messageStream.Write(data, 0, data.Length);
        }

        public void SendData(Shape Object)
        {
            var binaryFormatter = new BinaryFormatter();
             binaryFormatter.Serialize(_dataStream, Object);
        }

        public void CloseClientConnection()
        {
            if (_messageClient.Connected)
            {
                const string closeConnectionMessage = "Close connection message";
                var data = Encoding.Unicode.GetBytes(closeConnectionMessage);
                _messageStream.Write(data, 0, data.Length);
            }
            Dispose();
        }

        public string ConnectClient(string username)
        {
            try
            {
                _messageClient.Connect(_host, _port); //подключение клиента
                _dataClient.Connect(_host,_port2);
                _messageStream = _messageClient.GetStream(); // получаем поток
                _dataStream = _dataClient.GetStream();
                var message = username;
                var data = Encoding.Unicode.GetBytes(message);
                _messageStream.Write(data, 0, data.Length);
                // запускаем новый поток для получения данных
                return "Добро пожаловать, " + username + Environment.NewLine;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public void Dispose()
        {
            if (_messageStream != null)
                _messageStream.Close();//отключение потока
            if (_messageClient != null)
                _messageClient.Close();//отключение клиента
            if (_dataStream != null)
                _dataStream.Close();//отключение потока
            if (_dataClient != null)
                _dataClient.Close();//отключение клиента
            Environment.Exit(0); //завершение процесса
        }
    }
}
