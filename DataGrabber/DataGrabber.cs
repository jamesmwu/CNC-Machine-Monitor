﻿using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;




namespace DataGrabberApp
{
    public enum DataType_t
    {
        eDTT_STACKLIGHT = 1,
        eDTT_SPINDLE_ON,
        eDTT_SPINDLE_RPM,
        eDTT_SPINDLE_OVERRIDE,
        eDTT_FEED_OVEERIDE
    };

    class MachineInfo
    {
        public string Url { get; set; }
        public int Id;
        public string Protocol { get; set; }

        public MachineInfo() { }
        public MachineInfo(string address, int machineId, string protocol)
        {
            Url = address;
            Id = machineId;
            Protocol = protocol;
        }
    }
    abstract class DeviceDatum
    {
        protected string DatumName { get; set; }
        protected string DatumId { get; set; }
        protected string DatumValue { get; set; }
        protected string DatumParameterName { get; set; }
        protected string DatumParameterDescritpion { get; set; }
        protected DataType_t DatumDataType;
        protected DateTime DatumTimeStamp;
        protected string LastDatumValue { get; set; }
        public abstract void PushData( string url, int machineId, string name, string id, string value, DateTime time);
        public abstract bool IsChanged(DeviceDatum lastDatum);
    }
    class StackLight : DeviceDatum
    {

        public StackLight() 
        {

        }
        ~ StackLight()
        {
         
        }

        private  void InsertData()
        {
        
        }
        public override bool IsChanged(DeviceDatum lastDatum)
        {
            return false;
        }



        public override  async void PushData(string url, int machineId, string name, string id, string value, DateTime time)
        {
            DatumName = name;
            DatumId = id;
            DatumValue = value;
            DatumParameterName = "Stack Light";
            DatumParameterDescritpion = " PT10 Control stack light";
            DatumDataType = DataType_t.eDTT_STACKLIGHT;
            DatumTimeStamp = time;

            if (DatumValue != LastDatumValue)
            {
                Console.WriteLine("url = " + url + " , machine id = " + machineId + " , TimeStamp = " + time + " , datum id = " + id + " , value = " + value);

                bool pushData = false;  //turn if off if don't want to push data to db

                if (pushData)
                {

                    string connString = System.Configuration.ConfigurationManager.ConnectionStrings["TrakConnect"].ConnectionString;

                    MySqlCommand cmd = new MySqlCommand();
                    MySqlConnection DBConnection;
                    DBConnection = new MySqlConnection(connString);
                    await DBConnection.OpenAsync();
                    cmd = DBConnection.CreateCommand();
                    cmd.CommandText = "INSERT INTO MachineTimeSeriesData (Time, BoolValue, NumberValue, TextValue, Machines_ID, DataParameters_ID)" +
                    " VALUES(?_Time, ?_BoolValue,  ?_NumberValue, ?_TextValue, ?_Machines_ID, ?_DataParameters_ID)";
                    cmd.Parameters.Add("?_Time", MySqlDbType.DateTime).Value = time;
                    cmd.Parameters.Add("?_BoolValue", MySqlDbType.Bool).Value = false;
                    cmd.Parameters.Add("?_NumberValue", MySqlDbType.Double).Value = 0;
                    cmd.Parameters.Add("?_TextValue", MySqlDbType.VarString).Value = DatumValue;
                    cmd.Parameters.Add("?_Machines_ID", MySqlDbType.Int32).Value = machineId;
                    cmd.Parameters.Add("?_DataParameters_ID", MySqlDbType.Int32).Value = 1;
                    await cmd.ExecuteNonQueryAsync();
                }

                LastDatumValue = DatumValue; 
            }
        }
    }


    class DataGrabber
    {
        protected MachineInfo Machine;
        protected TimeSpan Interval { get; set; }
        public DataGrabber(MachineInfo machine, TimeSpan interval)
        {
            Machine = new MachineInfo();
            Machine = machine;
            Interval = interval;
        }
        public virtual void Init() { }
    }

    class MTConnectDataGrabber : DataGrabber
    {
        public MTConnectDataGrabber(MachineInfo machine, TimeSpan interval) : base(machine, interval) { }
        ~MTConnectDataGrabber() { }
    
        public override void Init()
        {
            Console.WriteLine("MTConnectDataGrabber Init.  Machine URL = " + Machine.Url);
            DeviceDatum stackLight = new StackLight();
            var client = new MTConnectSharp.MTConnectClient()
            {
                AgentUri = Machine.Url,
                AgentMachineId = Machine.Id,
                UpdateInterval = Interval,
            };

            client.ProbeCompleted += (sender, info) =>
            {
                var items = client.Devices
                    .SelectMany(d => d.DataItems.Select(i => new { d = d.LongName, i = i.LongName }))
                    .ToArray();
                client.StartStreaming();
            };

            client.DataItemChanged += (sender, info) =>
            {
                var list = new List<string> { "stack_light_state" };
                if (list.Any(info.DataItem.Id.Contains))
                {
                    stackLight.PushData( client.AgentUri, client.AgentMachineId, info.DataItem.Name, info.DataItem.Id, info.DataItem.CurrentSample.Value, DateTime.Now);
                }
            };
            client.Probe();
        }
    }
    
}