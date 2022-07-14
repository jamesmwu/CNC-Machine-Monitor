using System;
using System.Linq;
using System.Diagnostics;
using System.ServiceProcess;
//using Terminal.Gui;
using System.Collections.Generic;
using MySqlConnector;
using System.Threading.Tasks;




namespace DataGrabberApp
{
    class Program
    {
       
        static void Main(string[] args)
        {

            Start(args);
            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
            }

            //  Console.ReadKey(true);
            Stop();
        }
        private static void Start(string[] args)
        {
            var UpdateInterval = TimeSpan.FromSeconds(2.0);
            List<MachineInfo> listMachines = GetMachineList(5000);
            Dictionary<string, Task> collectorTasks = new Dictionary<string, Task>();
            for (int i = 0; i < listMachines.Count; i++)
            {
                var protocol = listMachines[i].Protocol;

                MachineInfo machine = new MachineInfo();
                machine = listMachines[i];
                Task task = Task.Factory.StartNew(() =>    
                {
                    if (protocol.Equals("mtconnect"))
                    {
                        new MTConnectDataGrabber(machine, UpdateInterval).Init();
                    }
                    else
                    {
                        new OPCUADataGrabber(machine, UpdateInterval).Init();
                    }
                  
                });
                collectorTasks.Add(machine.Url, task);
            }
        }

        private static void Stop() { }

        public static List<MachineInfo> GetMachineList(int port)
        {

     
            List<MachineInfo> machineInfoList = new List<MachineInfo>();
            MySqlDataReader reader = Database.SelectAll("trakconnect","Machines");

            int idx; 
            while (reader.Read())
            {
                idx = 0;
                int Id = reader.GetInt32(idx++);
                int MachineIdKey = reader.GetInt32(idx++);
                int CellId = reader.GetInt32(idx++);
                string MachineName = reader.GetString(idx++);
                string IpAddress = reader.GetString(idx++);
                string MACAddress = reader.GetString(idx++);
                string MasterVersion = reader.GetString(idx++);
                string SlaveVersion = reader.GetString(idx++);
                string PendantSerial = reader.GetString(idx++);
                string CMSerial = reader.GetString(idx++);
                string Protocol = reader.GetString(idx++);
                int ConfigZipSize = reader.GetInt32(idx++);
                byte[] ConfigZipData = new byte[ConfigZipSize];
                reader.GetBytes(reader.GetOrdinal("ConfigZipData"), 0, ConfigZipData, 0, (Int32)ConfigZipSize);



                MachineInfo machine;
                if (Protocol.Equals("mtconnect"))
                {
                    machine = new MachineInfo("http://" + IpAddress + ":" + port, Id, Protocol);
                }
                else
                {
                    machine = new MachineInfo("opc.tcp://" + IpAddress, Id, Protocol);
                }
                machineInfoList.Add(machine);
            }
            Database.Close();
            return machineInfoList;
        }
    }
}
