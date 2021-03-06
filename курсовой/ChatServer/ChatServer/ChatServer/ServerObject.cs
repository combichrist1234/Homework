﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using SerializationTypes;

namespace ChatServer
{
    public class ServerObject
    {
        private static TcpListener _tcpMessageListener; // сервер для прослушивания
        private static TcpListener _tcpDataListener; // сервер для прослушивания
        private readonly List<ClientObject> _clients = new List<ClientObject>(); // все подключения

        protected internal void AddConnection(ClientObject clientObject)
        {
            _clients.Add(clientObject);
        }
        protected internal void RemoveConnection(string id)
        {
            // получаем по id закрытое подключение
            ClientObject client = _clients.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
                _clients.Remove(client);
        }
        // прослушивание входящих подключений
        protected internal void Listen()
        {
            try
            {
                _tcpMessageListener = new TcpListener(IPAddress.Any, 8888);
                _tcpDataListener = new TcpListener(IPAddress.Any, 8889);
                _tcpMessageListener.Start();
                _tcpDataListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    var tcpMessageClient = _tcpMessageListener.AcceptTcpClient();
                    var tcpDataClient = _tcpDataListener.AcceptTcpClient();

                    var clientObject = new ClientObject(tcpMessageClient,tcpDataClient,this);
                    var clientMessageThread = new Thread(clientObject.ProcessMessages);
                    var clientDataThread = new Thread(clientObject.ProcessData);
                    clientMessageThread.Start();
                    clientDataThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

        // трансляция сообщения подключенным клиентам
        protected internal void BroadcastMessage(string message, string id)
        {
            var data = Encoding.Unicode.GetBytes(message);
            for (var i = 0; i < _clients.Count; i++)
            {
                if (_clients[i].Id != id) // если id клиента не равно id отправляющего
                {
                    _clients[i].MessageStream.Write(data, 0, data.Length); //передача данных
                }
            }
        }
        // трансляция сообщения подключенным клиентам
        protected internal void BroadcastData(Shape Object, string id)
        {
            var binaryFormatter = new BinaryFormatter();
            for (var i = 0; i < _clients.Count; i++)
            {
                if (_clients[i].Id != id) // если id клиента не равно id отправляющего
                {
                    binaryFormatter.Serialize(_clients[i].DataStream, Object);//передача данных
                }
            }
        }
        // отключение всех клиентов
        protected internal void Disconnect()
        {
            _tcpMessageListener.Stop(); //остановка сервера
            _tcpDataListener.Stop(); //остановка сервера

            for (var i = 0; i < _clients.Count; i++)
            {
                _clients[i].Close(); //отключение клиента
            }
            Environment.Exit(0); //завершение процесса
        }
    }
}
