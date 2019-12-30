using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;

namespace MicrosEmiCRP
{

    class TCP
    {


        private List<byte> receivedData = new List<byte>();
        Int32 port = 13000;
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        private string Encoding = "";
        private string Stopping = "";

        private int DataOldLength = 0;
        private int DataLength = 0;

        private bool GetData = true;

        Parser parser = new Parser();

        #region ctors
        public TCP()
        {
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("|        CTOR TCP             |");
            WriteLogger.WriteLog("-------------------------------");
#endif
            MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
            {
                MainWindow.main.Background = Brushes.LightGray;
            }));

            ReadConfigFromConfFile();

            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 13000.


                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;
                bool work = true;
                // Enter the listening loop.
                while (work)
                {

#if DEBUG
                    Console.Write("Waiting for a connection... ");
                    WriteLogger.WriteLog("-------------------------------");
                    WriteLogger.WriteLog("Waiting for a connection... ");
                    WriteLogger.WriteLog("-------------------------------");
#endif
                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();

#if DEBUG
                    Console.WriteLine("Connected!");
                    WriteLogger.WriteLog("-------------------------------");
                    WriteLogger.WriteLog("Connected!");
                    WriteLogger.WriteLog("-------------------------------");
#endif
                    data = "";

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.

                    //while (stream.DataAvailable)
                    //while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    while (GetData)
                    {
                        // Read Data from Stream
                        i = stream.Read(bytes, 0, bytes.Length);


                        //Encoding einstallbar 
                        if (Encoding.Equals("ASCII"))
                        {
                            data = data + System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        }
                        else if (Encoding.Equals("BigEndianUnicode"))
                        {
                            data = System.Text.Encoding.BigEndianUnicode.GetString(bytes, 0, i);
                        }
                        else if (Encoding.Equals("Default"))
                        {
                            data = System.Text.Encoding.Default.GetString(bytes, 0, i);
                        }
                        else if (Encoding.Equals("Unicode"))
                        {
                            data = System.Text.Encoding.Unicode.GetString(bytes, 0, i);
                        }
                        else if (Encoding.Equals("UTF32"))
                        {
                            data = System.Text.Encoding.UTF32.GetString(bytes, 0, i);
                        }
                        else if (Encoding.Equals("UTF7"))
                        {
                            data = System.Text.Encoding.UTF7.GetString(bytes, 0, i);
                        }
                        else
                        {
                            data = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                        }



#if DEBUG
                        Console.WriteLine("Received: {0}", data);
                        WriteLogger.WriteLog("-------------------------------");
                        WriteLogger.WriteLog("Received: " + data);
                        WriteLogger.WriteLog("-------------------------------");
#endif


                        // Process the data sent by the client.
                        data = data.ToUpper();

                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                        for (int z = 0; z < msg.Length; z++)
                        {
                            receivedData.Add(msg[z]);
                        }



                        if (Stopping.Equals("Standard") && !(i != 0))
                        {
                            GetData = false;
                        }

                        if (Stopping.Equals("DataAvailable") && !stream.DataAvailable)
                        {
                            GetData = false;
                        }

                        if (Stopping.Equals("Byte") && receivedData.Contains(0x03))
                        {
                            GetData = false;
                        }

                    }

                    // Shutdown and end connection
                    client.Close();
                    parser.dataParser(data);
                    work = false;

                    //parser.dataParser(receivedData);
                }
            }
            catch (Exception e)
            {

#if DEBUG
                Console.WriteLine("Exception: {0}", e);
                WriteLogger.WriteLog("-------------------------------");
                WriteLogger.WriteLog("Exception: " + e);
                WriteLogger.WriteLog("-------------------------------");
#endif
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
#if DEBUG
                WriteLogger.WriteLog("-------------------------------");
                WriteLogger.WriteLog("Server Stop ");
                WriteLogger.WriteLog("-------------------------------");
#endif
            }

        }
        #endregion

        private void ReadConfigFromConfFile()
        {
            XmlDocument config = new XmlDocument();
            config.Load(@"AxonLab\Config\TCP.config");

            foreach (XmlNode node in config.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {

                    case "Port":
                        port = Convert.ToInt32(node.InnerText);
                        break;
                    case "IP":
                        localAddr = IPAddress.Parse(node.InnerText);
                        break;
                    case "FindMode":
                        parser.findEnd = Convert.ToInt32(node.InnerText);
                        break;
                    case "Encoding":
                        Encoding = node.InnerText;
                        break;
                    case "StopServer":
                        Stopping = node.InnerText;
                        break;
                }
            }
        }

    }
}
