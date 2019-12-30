using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Ordbase.Common.Databases;
using Ordbase.Common.DataServices;
using System.Xml;
using Ordbase.Common.Utilities;
using System.Windows.Media;
using System.Threading;
using System.Timers;

namespace MicrosEmiCRP
{
    class Parser
    {
        #region Properties
        public List<Laborergebnisse> Measurings = null;
        Laborergebnisse result = null;
        private Dictionary<string, string> parameter = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(@"AxonLab\Config\conversion.json"));
        private Dictionary<string, string> parameterext = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(@"AxonLab\Config\conversionext.json"));
        private Dictionary<string, string> reference = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(@"AxonLab\Config\referenzbereich.json"));


        private int recievedDataIndex = 0;
        private int recievedDataIndexEnd = 0;
        private int? recievedDataIndexStart = null;

        private string DataNotOk = "";
        private string DataOk = "";
        private string DBSave = "";
        private string Title = "";

        private int Latenz = 0;

        private long patientId;

        WorkstationDS workstationDS = new WorkstationDS();
        Patient patient;

        bool ExceptionOccuredParser = false;
        private bool saved = false;

        private bool SaveEmptyEntries = true;

        private string _Workstation = "";

        public static Parser _Parser;


        List<byte> myMessurement = new List<byte>();
        //System.Timers.Timer aTimer = new System.Timers.Timer();

        #endregion

        //#region events
        //public event EventHandler InitFinished;
        //protected virtual void OnInitFinished(EventArgs e)
        //{
        //    InitFinished?.Invoke(this, e);
        //}
        //#endregion

        #region Enums

        public enum ReceiveState
        {
            NotReady = 0,
            ReadyToReceive = 1,
            Start = 2,
            Receiving = 3,
            ReadyToParse = 4
        }
        public int receiveState { get; set; }
        
        public enum FindEnd
        {
            Byte = 0,
            Length = 1,
            Time = 2
        }
        public int findEnd { get; set; }
        #endregion

        #region ctor
        public Parser()
        {
            _Parser = this;
            InitWorkstation();
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("|        CTOR Parser          |");
            WriteLogger.WriteLog("-------------------------------");
#endif

#if !OFFLINE
            try
            {
                TEST.setCurrentOrdination();
                // Patient evaluieren
                //Workstation workstation = workstationDS.getWorkstations(Host.GetName());
                //Anpassung Dr. Stadler
                List<Workstation> workstationList;
                workstationList = workstationDS.getWorkstations();
                Workstation workstation;

                if (String.IsNullOrEmpty(_Workstation))
                {
                    workstation = workstationDS.getWorkstations(Host.GetName());
                }
                else
                {
                    workstation = workstationList.Where(x => x.Hostname == _Workstation).FirstOrDefault();
                }

                //if (String.IsNullOrEmpty(_Workstation))
                //{
                //    workstation = workstationDS.getWorkstations();
                //}else
                //{
                //    workstation = workstationDS.getWorkstations(/*_Workstation*/);
                //}
                patientId = (long)workstation.PatientId;
                patient = new PatientDS().getPatientenById(patientId);
            }
            catch (Exception exception)
            {
                MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
                {
                    MainWindow.main.SetInfoText("Fehler bei der PatientenId: " + exception.Message);
                    MainWindow.main.Background = Brushes.LightPink;
                    WriteLogger.WriteLog("-------------------------------");
                    WriteLogger.WriteLog("Fehler bei Patientenauswahl: " + exception.Message);
                    WriteLogger.WriteLog("-------------------------------");
                }));
            }

            MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
            {
                MainWindow.main.AddToTitle(patient.Vorname + " " + patient.Nachname);
            }));
#endif

            //aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            //aTimer.Interval = 5000;
            //aTimer.Enabled = true;

            ReadText();
            //OnInitFinished(EventArgs.Empty);
        }
        #endregion

        #region init
        private void InitWorkstation()
        {
            XmlDocument config = new XmlDocument();
            config.Load(@"AxonLab\Config\Mode.config");

            foreach (XmlNode node in config.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {

                    case "Workstation":
                        _Workstation = node.InnerText;
                        break;
                    
                }
            }
        }

        private void ReadText()
        {
            string str = File.ReadAllText(@"AxonLab\Texte\DataNotOk.disp");
            DataNotOk = str;
            str = File.ReadAllText(@"AxonLab\Texte\DataOk.disp");
            DataOk = str;
            str = File.ReadAllText(@"AxonLab\Texte\DBsave.disp");
            DBSave = str;
            Title = File.ReadAllText(@"AxonLab\Texte\TitleMicrosEmiCRP.disp");

            XmlDocument config = new XmlDocument();
            config.Load(@"AxonLab\Config\Latenz.config");

            foreach (XmlNode node in config.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {

                    case "Latenz":
                        Latenz = Convert.ToInt16(node.InnerText);
                        break;
                    case "SaveEmptyEntries":
                        SaveEmptyEntries = Convert.ToBoolean(node.InnerText);
                        break;
                }
            }
        }
        #endregion

        #region Parser
        public List<Laborergebnisse> parse(string buffer)
        {
            //DateTime datum = new DateTime();
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("Start Parsing!");
            WriteLogger.WriteLog("-------------------------------");
#endif
            
            Measurings = new List<Laborergebnisse>();
            buffer = buffer.Replace("\n", "");
            string[] werte = buffer.Split('\r');
            if (werte.Count() > 0)
            {
                int count = 0;
                DateTime datum = new DateTime();
                foreach (string w in werte)
                {
                    count++;
                    if (!String.IsNullOrEmpty(w))
                    {
                        
                        if (w.Substring(0, 1) == "Q" || w.Substring(0, 1) == "q")
                        {
                            if (!String.IsNullOrEmpty(w.Substring(2)))
                            {
                                string test = w.Substring(2).Replace("h", ":").Replace("H", ":").Replace("mn", ":").Replace("MN", ":").Replace("s", "").Replace("S", "").Replace("/", ".");
                                datum = DateTime.ParseExact(test, "yy.MM.dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            //datum = Convert.ToDateTime(w.Substring(2).Replace("h", ":").Replace("mn", ":").Replace("s", ""));
                            
                            //DateTime myDatum = DateTime.ParseExact(test, "dd.MM.yy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            //Datum = myDatum.Day.ToString("D2") + myDatum.Month.ToString("D2") + myDatum.Year.ToString("D4");
                        }
                        else
                        {
                            try
                            {
                                //string p = parameter[w.Substring(0, 1)];
                                string p1 = w.Substring(0, 1);
                                result = new Laborergebnisse
                                {
                                    PatientId = patientId,
                                    Datum = datum,
                                    Parameter = parameter[p1],
                                    Langbezeichnung = parameterext[p1],
                                    Wert = w.Substring(2, 5).TrimStart('0'),
                                    Bewertung = w.Substring(7, 2).Replace("l", "-").Replace("h", "+").Replace("L", "-").Replace("H", "+").Replace(" ", "").Replace("U","-").Replace("u","-"),
                                    Referenz = reference[p1].Split('_')[0],
                                    Dimension = reference[p1].Split('_')[1],
                                    Reihenfolge = 40000,
                                    Quelle = Title,
                                };
                                //if (result.Wert.Length >= 1)
                                if (!String.IsNullOrEmpty(result.Wert) && result.Wert.Length >= 1)
                                {
                                    result.Wert = result.Wert.TrimStart();
                                    if (result.Wert[0] == '.')
                                    {
                                        result.Wert = "0" + result.Wert;
                                    }
                                }

                                bool addResult = true;
                                if (!SaveEmptyEntries)
                                {
                                    addResult = float.TryParse(result.Wert, out float n);
                                }
                                
                                foreach (Laborergebnisse l in Measurings)
                                {
                                    if (l.Parameter == result.Parameter)
                                    {
                                        addResult = false;
                                    }
                                }
                                if (addResult)
                                    Measurings.Add(result);

                            }
                            catch (KeyNotFoundException)
                            {
                                ;
                            }
                            catch (Exception ex)
                            {
                                ExceptionOccuredParser = true;
                                MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
                                {
                                    MainWindow.main.SetInfoText("Fehler bei Parsen: " + ex.Message);
                                    MainWindow.main.Background = Brushes.LightPink;
                                    WriteLogger.WriteLog("-------------------------------");
                                    WriteLogger.WriteLog("Fehler beim Parser: " + ex.Message);
                                    WriteLogger.WriteLog("-------------------------------");
                                }));
                            }
                            
                        }
                    }
                }
            }
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("End Parsing!");
            WriteLogger.WriteLog("-------------------------------");
#endif
            return Measurings;
        }
        #endregion

        #region DataComplete?
        public void dataParser(string data)
        {
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("Finding Start and End of Transmission.....");
            WriteLogger.WriteLog("-------------------------------");
#endif
            List<Laborergebnisse> laborergebnisse = parse(data);

            if (!ExceptionOccuredParser && !saved)
            {
                saveLaborergebnisse(laborergebnisse);
            }
        }

            public void dataParser(List<byte> receivedData)
        {
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("Finding Start and End of Transmission.....");
            WriteLogger.WriteLog("-------------------------------");
#endif


            //if (findEnd == (int)FindEnd.Time)
            //{
            //    aTimer.Start();
            //}

            this.recievedDataIndexEnd = 0;
            this.recievedDataIndexStart = 0;
            this.recievedDataIndex = 0;
            int bytezaehler = 0;

            myMessurement = receivedData.GetRange((int)this.recievedDataIndexStart, receivedData.Count );

            foreach (byte b in receivedData)
            {
                bytezaehler++;
                if (0x02 == b) // 0x02 -> StartCondition
                {
#if DEBUG
                    WriteLogger.WriteLog("-------------------------------");
                    WriteLogger.WriteLog("Start Found!");
                    WriteLogger.WriteLog("-------------------------------");
#endif
                    this.receiveState = (int)ReceiveState.Start;
                    this.recievedDataIndexStart = this.recievedDataIndex;
                }

                if(findEnd == (int)FindEnd.Byte)
                {
                    if (0x03 == b) // 0x03 -> StopCondition
                    {
#if DEBUG
                        WriteLogger.WriteLog("-------------------------------");
                        WriteLogger.WriteLog("End Found -> StopByte(ETX)!");
                        WriteLogger.WriteLog("-------------------------------");
#endif
                        this.receiveState = (int)ReceiveState.ReadyToParse;
                        this.recievedDataIndexEnd = this.recievedDataIndex;
                    }
                }
                else if(findEnd == (int)FindEnd.Length)
                {
                    if(bytezaehler >= receivedData.Count) // alle Bytes da?
                    {
#if DEBUG
                        WriteLogger.WriteLog("-------------------------------");
                        WriteLogger.WriteLog("End Found -> Length!");
                        WriteLogger.WriteLog("-------------------------------");
#endif
                        this.receiveState = (int)ReceiveState.ReadyToParse;
                        this.recievedDataIndexEnd = this.recievedDataIndex;
                    }
                }

                
                this.recievedDataIndex++;
            }


            if (this.receiveState == (int)ReceiveState.ReadyToParse)
            {
                List<byte> Messurement = new List<byte>();
                Messurement = receivedData.GetRange((int)this.recievedDataIndexStart, ((this.recievedDataIndexEnd + 1) - (int)this.recievedDataIndexStart));

                byte[] tohexstring = new byte[Messurement.Count()];
                tohexstring = Messurement.ToArray<byte>();
                string hexstring = BitConverter.ToString(tohexstring);
                hexstring = hexstring.Replace("-", "");
                string ergstring = Converter.Hex2String(hexstring);

                List<Laborergebnisse> laborergebnisse = parse(ergstring);

                if (!ExceptionOccuredParser && !saved)
                {
                    saveLaborergebnisse(laborergebnisse);
                }
            }
        }

        //private void Parse(List<byte> receivedData)
        //{
        //    if (this.receiveState == (int)ReceiveState.ReadyToParse)
        //    {
        //        List<byte> Messurement = new List<byte>();
        //        Messurement = receivedData.GetRange((int)this.recievedDataIndexStart, ((this.recievedDataIndexEnd + 1) - (int)this.recievedDataIndexStart));

        //        byte[] tohexstring = new byte[Messurement.Count()];
        //        tohexstring = Messurement.ToArray<byte>();
        //        string hexstring = BitConverter.ToString(tohexstring);
        //        hexstring = hexstring.Replace("-", "");
        //        string ergstring = Converter.Hex2String(hexstring);

        //        List<Laborergebnisse> laborergebnisse = parse(ergstring);

        //        if (!ExceptionOccuredParser && !saved)
        //        {
        //            saveLaborergebnisse(laborergebnisse);
        //        }
        //    }
        //}

        //private void OnTimedEvent(object sender, ElapsedEventArgs e)
        //{
        //    aTimer.Stop();
        //    this.recievedDataIndexStart = 0;
        //    this.recievedDataIndexEnd = myMessurement.Count-1;
        //    this.receiveState = (int)ReceiveState.ReadyToParse;
        //    Parse(myMessurement);
        //}
        #endregion

        #region Saver
        public void saveLaborergebnisse(List<Laborergebnisse> value)
        {
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("Start Saving!");
            WriteLogger.WriteLog("-------------------------------");
#endif
            saved = true;
            bool ExceptionOccured = false;
            MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
            {
                MainWindow.main.SetInfoText(DataOk);
            }));
            
            System.Threading.Thread.Sleep(Latenz);

            try
            {
                foreach (Laborergebnisse l in value)
                {
                    new LaborergebnisseDS().insert(l);
                }
            }
            catch(Exception exception)
            {
                ExceptionOccured = true;
                MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
                {
                    MainWindow.main.SetInfoText("Fehler: " + exception.Message);
                    MainWindow.main.Background = Brushes.LightPink;
                    WriteLogger.WriteLog("-------------------------------");
                    WriteLogger.WriteLog("Fehler Saving: " + exception.Message);
                    WriteLogger.WriteLog("-------------------------------");
                }));
            }
#if DEBUG
            WriteLogger.WriteLog("-------------------------------");
            WriteLogger.WriteLog("Stop Saving!");
            WriteLogger.WriteLog("-------------------------------");
#endif

            if (!ExceptionOccured)
            {
                MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
                {
                    MainWindow.main.SetInfoText(DBSave);
                }));

                System.Threading.Thread.Sleep(Latenz);

           
                MainWindow.main.Dispatcher.Invoke(new Action(delegate ()
                {
                    MainWindow.main.Close();
                }));
            }
        }
        #endregion
    }
}
