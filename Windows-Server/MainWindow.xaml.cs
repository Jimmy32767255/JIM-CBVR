using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace JIMCBVR.Server
{
    public partial class MainWindow : Window
    {
        private const string CONFIG_PATH = "config.cfg";
        public AppConfig Config { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            DataContext = this;
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(CONFIG_PATH))
                {
                    Config = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(CONFIG_PATH));
                }
                else
                {
                    Config = new AppConfig();
                    SaveConfig();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($