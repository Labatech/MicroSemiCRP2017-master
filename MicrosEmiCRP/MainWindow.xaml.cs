using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ordbase.Common.Databases;
using Ordbase.Common.DataServices;
using Ordbase.Common.Utilities;
using System.Xml;
using System.ComponentModel;
using System.Threading;

namespace MicrosEmiCRP
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //string myDevice = "";
        string Start = "";
        string Title1 = "";
        string Mode = "";

        public static MainWindow main;
        public MainWindow()
        {
            InitializeComponent();
            init();
            main = this;

            readText();

            SetTitle(Title1);
            SetInfoText(Start);

                     
            if(Mode == "RS232" || Mode == "rs232")
            {
                RS232 rs = new RS232();
            }else if(Mode == "TCP" || Mode == "tcp")
            {
                Thread newThread = new Thread(MainWindow.DoWork);
                newThread.Start();

                //TCP tcp = new TCP();

            }

        }

        public static void DoWork(object data)
        {
            TCP tcp = new TCP();
        }

        public void SetTitle(string value)
        {
            Title = value;
        }

        public void AddToTitle(string value)
        {
            Title = Title + " - " + value;
        }

        public void SetInfoText(string value)
        {
            TextBlockInfo.Text = value;
        }

        private void readText()
        {
            string str = File.ReadAllText(@"AxonLab\Texte\StartMicrosEmiCRP.disp");
            Start = str;
            str = File.ReadAllText(@"AxonLab\Texte\TitleMicrosEmiCRP.disp");
            Title1 = str;

            XmlDocument config = new XmlDocument();
            config.Load(@"AxonLab\Config\Mode.config");

            foreach (XmlNode node in config.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {

                    case "Mode":
                        Mode = node.InnerText;
                        break;
                }
            }
        }

        public void init()
        {
            if (!this._contentLoaded)
            {
                this._contentLoaded = true;
                Uri resourceLocator = new Uri("/MicrosEmiCRP;component/mainwindow.xaml", UriKind.Relative);
                Application.LoadComponent(this, resourceLocator);
            }
        }
    }
}