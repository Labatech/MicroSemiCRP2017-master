using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ordbase.Common.Databases;
using Ordbase.Common.DataServices;
using Ordbase.Common.Utilities;
using Newtonsoft.Json;
using System.IO;
using System.Xml;
using System.Windows.Media;
using System.Timers;
using System.Threading;

namespace MicrosEmiCRP
{
    class RS232
    {
        #region Properties

        private static SerialPort _serialPort;

        private int myBaudRate = 0;
        private int myDataBits = 8;
        private Parity myParity = Parity.Even;
        private string myPort = "";
        private StopBits myStopBits = StopBits.One;

        private List<byte> receivedData = new List<byte>();

        Parser parser;
        Thread ParserThread;

        System.Timers.Timer aTimer = new System.Timers.Timer();
        bool receivingStart = false;
        #endregion   

        #region ctor
        public RS232()
        {
            ParserThread = new Thread(DoWork);
            ParserThread.Start();
            //Thread.Sleep(7000);
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("|        CTOR RS232           |");
            WriteLogger.WriteLog("-------------------------------");
#endif
            MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
            {
                MainWindow.main.Background = Brushes.Beige;
            }));
            this.ReadConfigFromConfFile();
            this.Init();






            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 10000;
            aTimer.Enabled = true;
        }

        private void DoWork()
        {
            parser = new Parser();
        }
        #endregion

        #region TimeoutControl
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (receivingStart)
            {
                receivedData.Add(0x03);
                parser.dataParser(receivedData);
                aTimer.Stop();
            }
        }
        #endregion

        #region init
        private void Init()
        {
            try
            {
                _serialPort = new SerialPort(this.myPort, this.myBaudRate, this.myParity, this.myDataBits, this.myStopBits);
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(this.port_DataReceived);
                _serialPort.Open();
            }
            catch (Exception exception)
            {
                MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
                {
                    MainWindow.main.SetInfoText("Fehler: " + exception.Message);
                    MainWindow.main.Background = Brushes.LightPink;
                    WriteLogger.WriteLog("-------------------------------");
                    WriteLogger.WriteLog("RS232 - Fehler: " + exception.Message);
                    WriteLogger.WriteLog("-------------------------------");
                }));
                //Console.WriteLine("Exception: " + exception);
            }
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("RS232 - Config:");
            WriteLogger.WriteLog("Port: " + myPort);
            WriteLogger.WriteLog("BaudRate: " + this.myBaudRate);
            WriteLogger.WriteLog("Parity: " + myParity);
            WriteLogger.WriteLog("StopBits: " + myStopBits);
            WriteLogger.WriteLog("DataBits: " + myDataBits);
            WriteLogger.WriteLog("-------------------------------");
#endif

            //parser.receiveState = (int)Parser.ReceiveState.Receiving;
            
            
        }



        private void ReadConfigFromConfFile()
        {

            XmlDocument config = new XmlDocument();
            config.Load(@"AxonLab\Config\RS232.config");

            foreach (XmlNode node in config.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {

                    case "Port":
                        myPort = node.InnerText;
                        break;
                    case "BaudRate":
                        myBaudRate = Convert.ToInt16(node.InnerText);
                        break;
                    case "DataBits":
                        myDataBits = Convert.ToInt16(node.InnerText);
                        break;
                    case "StopBits":
                        switch (node.InnerText)
                        {
                            case "None":
                                myStopBits = StopBits.None;
                                break;
                            case "One":
                                myStopBits = StopBits.One;
                                break;
                            case "OnePointFive":
                                myStopBits = StopBits.OnePointFive;
                                break;
                            case "Two":
                                myStopBits = StopBits.Two;
                                break;
                        }
                        break;
                    case "Parity":
                        switch (node.InnerText)
                        {
                            case "Even":
                                myParity = Parity.Even;
                                break;
                            case "Mark":
                                myParity = Parity.Mark;
                                break;
                            case "None":
                                myParity = Parity.None;
                                break;
                            case "Odd":
                                myParity = Parity.Odd;
                                break;
                            case "Space":
                                myParity = Parity.Space;
                                break;
                        }
                        break;
                }
            }
        }
        #endregion

        #region RS232Listener
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("startReceiving.....");
            WriteLogger.WriteLog("-------------------------------");
#endif
            receivingStart = true;
            aTimer.Start();
            parser.receiveState = (int)Parser.ReceiveState.Start;
            for (int i = 0; i < _serialPort.BytesToRead; i++)
            {
                this.receivedData.Add((byte)_serialPort.ReadByte());
            }
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("myData:");
            string temp = "";
            foreach(Byte b in receivedData)
            {
                temp = temp + " " + b.ToString("X2");
            }
            WriteLogger.WriteLog(temp);
            WriteLogger.WriteLog("-------------------------------");
#endif
            try
            {
                
                parser.dataParser(receivedData);
            }catch (Exception ex)
            {
#if DEBUG
                WriteLogger.WriteLog("-------------------------------");
                WriteLogger.WriteLog("Exeption beim Aufruf dataParser: " + ex.Message);
                WriteLogger.WriteLog("-------------------------------");
#endif
            }
        }
        #endregion
    }
}