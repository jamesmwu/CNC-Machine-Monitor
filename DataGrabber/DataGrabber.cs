using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Configuration;
using System.IO;
using System.Threading.Tasks;



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
    class OPCUADataGrabber : DataGrabber
    {
        public OPCUADataGrabber(MachineInfo machine, TimeSpan interval) : base(machine, interval) { }
        ~OPCUADataGrabber() 
        {
        
        }
        public override void Init() 
        {
            Console.WriteLine("OPCUADataGrabber Init.  Machine URL = " + Machine.Url);
            Task task = ProbeAsync();
        }

        public async Task ProbeAsync()
        {
            
            bool renewCertificate = false;
            string password = null;
            var applicationName = "UAClient";
            var configSectionName = "UAClient";
            int timeout = Timeout.Infinite;
            bool autoAccept = false;
            TextWriter output = Console.Out;
            try
            {
                string lampStatusLast = "";
                string lampStatusCurrent = "";
                DeviceDatum stackLight = new StackLight();
                Uri serverUrl = new Uri(Machine.Url);

                CertificatePasswordProvider PasswordProvider = new CertificatePasswordProvider(password);
                ApplicationInstance application = new ApplicationInstance
                {
                    ApplicationName = applicationName,
                    ApplicationType = ApplicationType.Client,
                    ConfigSectionName = configSectionName,
                    CertificatePasswordProvider = PasswordProvider
                };

                // load the application configuration.
                var config = await application.LoadApplicationConfiguration(silent: false);
                // delete old certificate
                if (renewCertificate)
                {
                    await application.DeleteApplicationInstanceCertificate().ConfigureAwait(false);
                }
                // check the application certificate.
                bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, minimumKeySize: 0).ConfigureAwait(false);
                if (!haveAppCertificate)
                {
                    throw new Exception("Application instance certificate invalid!\n server Uri: " + Machine.Url);
                }

                // wait for timeout or Ctrl-C
                var quitEvent = OPCUAClient.ConsoleUtils.CtrlCHandler();

                // connect to a server until application stopped
                bool quit = false;
                DateTime start = DateTime.UtcNow;
                int waitTime = int.MaxValue;

                do
                {
                    if (timeout > 0)
                    {
                        waitTime = timeout - (int)DateTime.UtcNow.Subtract(start).TotalMilliseconds;
                        if (waitTime <= 0)
                        {
                            break;
                        }
                    }
                 
                    // create the UA Client object and connect to configured server.
                    using (OPCUAClient.UAClient uaClient = new OPCUAClient.UAClient(
                        application.ApplicationConfiguration, output, ClientBase.ValidateResponse)
                    {
                        AutoAccept = autoAccept
                    })
                    {
                        bool connected = await uaClient.ConnectAsync(serverUrl.ToString());
                        if (connected)
                        {
                            // enable subscription transfer
                            uaClient.Session.TransferSubscriptionsOnReconnect = true;

                            // Run tests for available methods on reference server.
                            var samples = new OPCUAClient.ClientSamples(output, ClientBase.ValidateResponse);
                            var lampColor = samples.ReadNodes(uaClient.Session);
                         
                            lampStatusCurrent = lampColor;

                            if (!lampStatusCurrent.Equals(lampStatusLast))
                            {
                                lampStatusLast = lampStatusCurrent;
                                stackLight.PushData(Machine.Url, Machine.Id, "Stacklight", "stack_light_state", lampStatusCurrent, DateTime.Now);
                            }
                       
                            // Wait for some DataChange notifications from MonitoredItems
                            quit = quitEvent.WaitOne(timeout > 0 ? waitTime : 2000); 
                            uaClient.Disconnect();
                        }
                        else
                        {
                            output.WriteLine("Could not connect to server! Retry in 10 seconds or Ctrl-C to quit.");
                            quit = quitEvent.WaitOne(Math.Min(10_000, waitTime));
                        }
                        
                    }

                } while (!quit);

                output.WriteLine("Client stopped.");
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.Message);
            }
        }
    }
}