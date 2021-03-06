﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication19_1
{
    public delegate void AccountStateHandler(MobileAccount callReceiver,MobileAccount caller, string message);
    public class MobileAccount
    {
        public event AccountStateHandler Call;
        public event AccountStateHandler Message;
        private Dictionary<int, string> _adressBook; 
        private CallLog _callLogs;
       
        public int Numerber { get; set; }
        public string Name { get; set; }

        public MobileAccount(int number,string name)
        {
            _adressBook= new Dictionary<int, string>();
            _callLogs = new CallLog();
            Numerber = number;
            Name = name;
        }

        public void SendMessage(MobileAccount account,string message)
        {
          _callLogs.CoefficientActivity+=0.5;
          Call.Invoke(account,this,message);
        }

        public int GetCallCounts()
        {
            return _callLogs.CallCount;
        }

        public double GetCoefficientActivity()
        {
            return _callLogs.CoefficientActivity;
        }
        public void MakeCall(MobileAccount account,string message)
        {
            _callLogs.CoefficientActivity += 1;
            Message.Invoke(account,this, message);
        }
        public void ReceiveCall(MobileAccount caller, string message)
        {
            var name = _adressBook.Where(p => p.Key == caller.Numerber).Select(p => p.Value).FirstOrDefault();
            _callLogs.CallCount += 1;
            if (name==null)
            {
                Console.WriteLine("Received from: {0}", caller.Numerber);
            }
            else
            {
                Console.WriteLine("Received from: {0}", name);
            }
            Console.WriteLine("Message:  {0}", message);
        }
        public void ReceiveMesagge(MobileAccount caller, string message)
        {
            var name = _adressBook.Where(p => p.Key == caller.Numerber).Select(p => p.Value).FirstOrDefault();
            if (name == null)
            {
                Console.WriteLine("Received from: {0}", caller.Numerber);
            }
            else
            {
                Console.WriteLine("Received from: {0}", name);
            }
            Console.WriteLine("Message:  {0}", message);
        }
        public void AddIntoAddressBook(int number, string name)
        {
            _adressBook.Add(number, name);
        }
    }


}
