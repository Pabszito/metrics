using DSharp4Webhook.Core;
using Newtonsoft.Json;
using OpenHardwareMonitor.Hardware;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using SystemMetrics.Util;

namespace SystemMetrics
{
    public partial class MetricsService : ServiceBase
    {
        private readonly FileLogger logger = new FileLogger();
        private readonly Timer timer = new Timer();
        private readonly HttpClient client = new HttpClient();
        private readonly IniFile file = new IniFile();
        private string url;

        private readonly string defaultIni =
            @"[SystemMetrics]
            Interval = 60000
            APIKey = your-api-key-goes-here
            PageId = your-page-id-goes-here
            MetricId = your-metric-id-goes-here
            Webhook = optional-discord-webhook-url";

        public MetricsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            logger.Log("Attempting to start service...", Level.INFO);

            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "config.ini"))
            {
                logger.Log("Unable to find a configuration file, creating one with the default values...", Level.WARN);
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "config.ini", defaultIni);
            }

            file.Load(AppDomain.CurrentDomain.BaseDirectory + "config.ini");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = file["SystemMetrics"]["Interval"].ToDouble();
            timer.Enabled = true;

            string key = file["SystemMetrics"]["APIKey"].GetString();
            string pageId = file["SystemMetrics"]["PageId"].GetString();
            string metricId = file["SystemMetrics"]["MetricId"].GetString();

            this.url = $"https://api.statuspage.io/v1/pages/{pageId}/metrics/{metricId}/data.json";

            client.DefaultRequestHeaders.Add("Authorization", $"OAuth {key}");

            logger.Log("Service started!", Level.INFO);
        }

        protected override void OnStop()
        {
            logger.Log("Service stopped.", Level.INFO);
        }

        private async void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            float? temp = GetTemperature();

            if(temp == 0)
            {
                logger.Log($"Got an invalid temperature while trying to submit data: {temp}°C", Level.ERROR);
                string webhookURL = file["SystemMetrics"]["Webhook"].GetString();
                if(webhookURL != null && !webhookURL.Equals("optional-discord-webhook-url"))
                {
                    var provider = new WebhookProvider("");
                    var webhook = provider.CreateWebhook(webhookURL);

                    webhook.SendMessage($":warning: GPU temperature returned {temp}°C, unable to submit data.")
                        .Queue(() => logger.Log("Sent webhook warning about invalid temperature", Level.DEBUG));
                }

                return;
            }

            string json = string.Format("{{\"data\":{{ \"timestamp\": \"{0}\", \"value\": \"{1}\" }}}}",
                (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds,
                temp);


            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, content);
            string serializedResponse = await response.Content.ReadAsStringAsync();
            dynamic deserializedResponse = JsonConvert.DeserializeObject(serializedResponse);

            if (deserializedResponse != null && deserializedResponse.error != null)
            {
                logger.Log("Unable to submit data. Is your API key correct?", Level.ERROR);
                return;
            }

            logger.Log("Submitted GPU temperature: " + temp + "°C", Level.INFO);
        }

        protected class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }

            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware)
                {
                    subHardware.Accept(this);
                }
            }
            
            public void VisitSensor(ISensor sensor) 
            {
                throw new NotSupportedException();
            }
            
            public void VisitParameter(IParameter parameter) 
            {
                throw new NotSupportedException();
            }
        }

        protected float? GetTemperature()
        {
            UpdateVisitor updateVisitor = new UpdateVisitor();
            Computer computer = new Computer();
            computer.Open();
            computer.GPUEnabled = true;
            computer.Accept(updateVisitor);
            foreach (var hardwareItem in computer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.GpuNvidia || hardwareItem.HardwareType == HardwareType.GpuAti)
                {
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            float? temp = sensor.Value;
                            computer.Close();
                            return temp;
                        }
                    }
                }
            }

            computer.Close();
            return 0;
        }
    }
}
