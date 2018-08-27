using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace AirQuality
{
    // ReSharper disable once ArrangeTypeModifiers
    // ReSharper disable once ClassNeverInstantiated.Global
    class Program
    {
        private static readonly string _baudArg = "-b";
        private static readonly string _listArg = "-l";
        private static readonly string _portArg = "-p";
        private static readonly string _sampleArg = "-s";
        private static readonly string _verboseArg = "-v";
        private static readonly string _thingspeakArg = "-t";

        private static string _apiKey = string.Empty;
        private static bool _isVerbose = false;

        static void Main(string[] args)
        {
            if (args.Contains(_listArg))
            {
                //list
                PrintPortListing();
            }

            _isVerbose = args.Contains(_verboseArg);

            var portArg = SerialPort.GetPortNames().FirstOrDefault();
            if (args.Contains(_portArg))
            {
                portArg = GetStringArg(args, _portArg);
            }

            if (args.Contains(_thingspeakArg))
            {
                _apiKey = GetStringArg(args, _thingspeakArg);
            }

            var baud = 9600;
            if (args.Contains(_baudArg))
            {
                baud = GetBaudRate(args);
            }

            int? samples = null;
            if (args.Contains(_sampleArg)) //0 equals keep running
            {
                samples = GetSamples(args);
            }

            Console.WriteLine($"Attempting to connect to {portArg} at {baud}");

            using (var port = new SerialPort(portArg, baud))
            {
                port.Open();

                Console.WriteLine($"Is connected {port.IsOpen}");

                if (port.IsOpen)
                {
                    //todo
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                    var avgCount = 0;
                    var avgPm10 = 0.0;
                    var avgPm25 = 0.0;
                    var lastThingSpeakSend = DateTime.Now;

                    //run forever
                    while (samples == 0)
                    {
                        var airQualityData = DoSample(port);
                        Thread.Sleep(3000);

                        avgCount++;
                        avgPm10 += airQualityData.Item2;
                        avgPm25 += airQualityData.Item1;

                        //send to thing speak every minute
                        if (DateTime.Now - lastThingSpeakSend >= TimeSpan.FromMinutes(1))
                        {
                            lastThingSpeakSend = DateTime.Now;
                            SendToThingSpeak((avgPm25 / (double) avgCount), (avgPm10 / (double) avgCount));
                        }
                    }

                    //or run for x samples
                    while (samples > 0)
                    {
                        samples--;
                        
                        var airQualityData = DoSample(port);
                        Thread.Sleep(3000);
                    }

                    port.Close();
                }
            }

#if DEBUG
            Console.WriteLine("Any key to exit");
            Console.ReadKey();
#endif
        }
        
        private static void SendToThingSpeak(double pm25, double pm10)
        {
            using (var client = new HttpClient())
            {
                var postData = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("api_key",_apiKey),
                    new KeyValuePair<string, string>("field1",pm25.ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string>("field2",pm10.ToString(CultureInfo.InvariantCulture))
                };

                using (var content = new FormUrlEncodedContent(postData))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    try
                    {
                        var response = client.PostAsync(new Uri("https://api.thingspeak.com/update.json"), content).Result;
                        Console.WriteLine(!response.IsSuccessStatusCode
                            ? "Error posting to thingspeak"
                            : "Data posted to thingspeak");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending to thingspeak {ex}");
                    }
                }
            }
        }

        static Tuple<double,double> DoSample(SerialPort port)
        {
            if(port == null || !port.IsOpen) throw new ArgumentException();

            var dataBuf = new byte[19];
            dataBuf[0] = 0xAA;//we read that already

            //discard until we get a head byte
            while (port.ReadByte() != 0xAA)
            {
            }

            port.Read(dataBuf, 1, dataBuf.Length -1);
            if (_isVerbose)
            {
                Console.Write("Packet ");
                for (var i = 0; i <= 9; i++)
                {
                    Console.Write($"{dataBuf[i]:X2} ");
                }

                Console.WriteLine("");
            }

            if (dataBuf[1] == 0xC0) //query data response
            {
                return PrintQueryDataResponse(dataBuf);
            }

            return null;
        }

        static void PrintReportResponse(byte[] dataBuf)
        {
            if(dataBuf[0] != 0xAA) throw new FormatException();
            //todo check checksum

            var databyte2 = String.Format("{0:X2}",dataBuf[3]);
            if (dataBuf[3] == 0) databyte2 = "query the current mode";
            else if(dataBuf[3] == 1) databyte2 = "set reporting mode";

            var databyte3 = String.Format("{0:X2}",dataBuf[4]);
            if (dataBuf[4] == 0) databyte3 = "report active mode";
            else if(dataBuf[4] == 1) databyte3 = "report query mode";

            var deviceId = String.Format("{0:X2}",dataBuf[6]) + String.Format("{0:X2}",dataBuf[7]);

            Console.WriteLine($"Data byte 2 {databyte2}");
            Console.WriteLine($"Data byte 3 {databyte3}");
            Console.WriteLine($"Device Id {deviceId}");
        }

        static Tuple<double,double> PrintQueryDataResponse(byte[] dataBuf)
        {
            if(dataBuf[0] != 0xAA) throw new FormatException();
            if (!ValidQueryChecksum(dataBuf))
            {
                Console.WriteLine("Invalid query packet");
                return null;
            }

            var pm25Low = dataBuf[2];
            var pm25High = dataBuf[3];
            int pm25Result = pm25High << 8 | pm25Low;
            var pm25 = (double)pm25Result / 10.0;

            var pm10Low = dataBuf[4];
            var pm10High = dataBuf[5];
            int pm10Result = pm10High << 8 | pm10Low;
            var pm10 = (double)pm10Result / 10.0;

            Console.WriteLine($"PM2.5 {pm25}\tPM10 {pm10}");

            return new Tuple<double, double>(pm25,pm10);
        }

        static bool ValidQueryChecksum(byte[] dataBuf)
        {
            var checksum = dataBuf[8];
            var sum = dataBuf[2] + dataBuf[3] + dataBuf[4] + dataBuf[5] + dataBuf[6] + dataBuf[7];
            var lower = (byte) (sum & 0xff);
            return checksum == lower;
        }

        static void PrintPortListing()
        {
            Console.WriteLine("Discovered serial ports");

            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                Console.WriteLine(port);
            }
        }

        private static string GetStringArg(string[] args, string argName)
        {
            var argIndex = Array.IndexOf(args, argName);
            argIndex++;//next arg is our value

            if (args.Length < argIndex) throw new ArgumentException("Invalid argument specified");
            if(argIndex < 0) throw new ArgumentException("No argument supplied");

            return args[argIndex];
        }

        //refactor
        static int GetBaudRate(string[] args)
        {
            var argIndex = Array.IndexOf(args, _baudArg);
            argIndex++;//next arg is port

            if (args.Length < argIndex) throw new ArgumentException("Invalid baud rate specified");
            if(argIndex < 0) throw new ArgumentException("No baud rate argument supplied");
            if (int.TryParse(args[argIndex], out var baud))
            {
                return baud;
            }
            
            throw new Exception("Invalid baud rate specified");
        }

        //refactor
        static int? GetSamples(string[] args)
        {
            var argIndex = Array.IndexOf(args, _sampleArg);
            argIndex++;//next arg is port

            if (args.Length < argIndex) throw new ArgumentException("Invalid sample count specified");
            if(argIndex < 0) throw new ArgumentException("No sample count argument supplied");
            if (int.TryParse(args[argIndex], out var sample))
            {
                return sample;
            }
            
            throw new Exception("Invalid sample count specified");
        }

    }
}
