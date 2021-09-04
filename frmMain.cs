using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
//using System.IO;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Utilities;

using EAutomation.Variable;

//using Keyence.AutoID.SDK;

using NationalInstruments.Vision;
using NationalInstruments.Vision.Analysis;
using NationalInstruments.Vision.WindowsForms;
using Vision_Assistant.Utilities;
using Vision_Assistant;


using Instrument;
using Motion;
using Vision.Camera.CameraRecipe;
using Cognex.DataMan.SDK;
using Cognex.DataMan.SDK.Discovery;
using Cognex.DataMan.SDK.Utils;
using Automation.BDaq;
using V2Tech.Automations.Apps.Forms;
using V2Tech.AutomationApps;

namespace V2Tech.AutomationApps
{
    public partial class frmMain : Form
    {
        //system 
        private string str_ret = "";
        VisionImage picture1 = new VisionImage();
        VisionImage picture2 = new VisionImage();
        VisionImage picture3 = new VisionImage();
        VisionImage picture4 = new VisionImage();
        VisionImage picture5 = new VisionImage();
        VisionImage picture6 = new VisionImage();
        VisionImage picture7 = new VisionImage();
        VisionImage picture8 = new VisionImage();
        VisionCameraStandard VCS = new VisionCameraStandard();
        public static frmMotionTeaching frmMotion = new frmMotionTeaching();
        ProcessResult result = new ProcessResult();
        int counter = 0;
        bool manualWriteLD = true;

        public VisionImage[] imageviewer = new VisionImage[8];

        //public string [] Unit = {"Unit_1","Unit_2","Unit_3","Unit_4"};
        public string[] unit = new string[4];
        //Adjust LED window (mini window) pop out 
        private frmManualMotion frmManualMotion;
        /*frmNkedOption window pop out*/
        private frmNkedOption frmNkedOption;

        private int total_Holder = 0, Holder_count = 0, Holder_row = 0, Holder_col = 0, Holder_minorcol = 0, Holder_minorrow = 0;
        private int total_Sleeve = 0, Sleeve_count = 0, Sleeve_row = 0, Sleeve_col = 0;
        private frmProductSetting frmPrdSetting;
        //private frmAlignmentProcess frmAlign = new frmAlignmentProcess();
        private frmIOControl frmIOctrl;
        private frmManualKeyIn frmManualLD;
        //for Motion use
        private frmMotionTeaching gMotionClass = new frmMotionTeaching();
        private bool gAdvMotionInstOk = true;
        private bool gHiwinMotionInstOk = true;
        //epoxytimer
        private Alarm_timer.timerfun epoxytime = new Alarm_timer.timerfun();
        //blink tower light
        private System.Timers.Timer tmr_blink_tower = new System.Timers.Timer(500);
        private bool isblinkon = false;
        private bool isblinkEN = false;
        private MsgType last_MsgType = MsgType.None;
        private Stopwatch sw_fullflow = new Stopwatch();
        private Stopwatch sw_process = new Stopwatch();
        private Stopwatch sw_timeout = new Stopwatch();
        private MachineState UserLoginState;
        private BackgroundWorker AutoAlignerWorker;
        private int progress = 1;
        //congnex barcode
        private DataManSystem _system = null;
        private DmccResponse res = null;
        int loop = 1;
        int current_loop = 0;

        GantryPos.locnames gantrypos = new GantryPos.locnames();

        public enum MsgType
        {
            INSTRUCTION,
            WARNING,
            ERROR,
            ASSISTANT,
            None
        }

        private enum MachineState
        {
            Logout,
            Operator,
            Technician,
            Engineer,
            Running,
        }

        private enum align_option
        {
            All,
            RX,
            TX,
            PM
        }

        private enum TowerLight
        {
            Red,
            Yellow,
            Green
        }

        private enum exisID
        {
            X = 0,
            Y = 1,
            Z = 2
        }
        /*Main Panel Starts Here*/
        public frmMain()
        {
            InitializeComponent();
            pictureBox1.Image = V2Tech.Automations.Apps.Properties.Resources.Warning; 
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            //shared ui 
            GlobalVar.ref_lblMsg = lblMsg;
            GlobalVar.ref_txtStatus = txtStatus;
            GlobalVar.ref_btnTowerLite = btnTowerLite;
            GlobalVar.handle = this.Handle;
            AutoAlignerWorker = new System.ComponentModel.BackgroundWorker();
            AutoAlignerWorker.DoWork += new DoWorkEventHandler(AutoAlignerWorker_DoWork);
            AutoAlignerWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AutoAlignerWorker_RunWorkerCompleted);
            AutoAlignerWorker.ProgressChanged += new ProgressChangedEventHandler(AutoAlignerWorker_ProgressChanged);
            AutoAlignerWorker.WorkerReportsProgress = true;
            AutoAlignerWorker.WorkerSupportsCancellation = true;
        }
        /*Main Panel Ends Here*/
        private void frmMain_Load(object sender, EventArgs e)
        {
            //loading form
            frmLoading frm = new frmLoading();
            frm.Show();
            Application.DoEvents();
            //initialization
            InitHardware();
            //Get last product save
            GlobalVar.CurrentProduct = GetAppConfig("CurrentProduct");
            LoadRecipe(GlobalVar.CurrentProduct);
            InitDataGridView();
            InitLogger();
            imageViewer.ZoomToFit = true;
            imageViewer.ShowToolbar = true;
            imageViewer.ToolsShown = ViewerTools.Pan | ViewerTools.ZoomOut | ViewerTools.ZoomIn;
            imageViewer.ActiveTool = ViewerTools.ZoomIn;
            imageViewer1.ZoomToFit = true;
            imageViewer2.ZoomToFit = true;
            imageViewer3.ZoomToFit = true;
            imageViewer4.ZoomToFit = true;
            imageViewer5.ZoomToFit = true;
            imageViewer6.ZoomToFit = true;
            imageViewer7.ZoomToFit = true;
            imageViewer8.ZoomToFit = true;
            //imageViewer.Attach(GlobalVar.SourceImage);            //NIZAMMM
            //mount blink event
            //tmr_blink_tower.Elapsed += this.onTowerLiteTimedEvent;
            //tmr_blink_tower.Enabled = true;            
            //set default UI state
            SetMachineState(MachineState.Logout);
            //load form close
            frm.Close();

        }

        protected void AutoAlignerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (AutoAlignerWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            if (progress == 1)
            {
                AutoAlignerWorker.ReportProgress(progress);
                progress = 0;
            }
        }

        protected void AutoAlignerWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AutoAlignerWorker.CancelAsync();
            progress = 1;
        }

        protected void AutoAlignerWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LogMessage("Starting Auto Assembly", MsgType.INSTRUCTION);
            SetMachineState(MachineState.Running);
            Do_AutoProcess();
        }

        private bool InitHardware()
        {
            //App Name
            GlobalVar.AppName = GetAppConfig("AppName");
            //init App setting 
            GlobalVar.SystemSettingPath = GetAppConfig("SystemSettingPath");
            GlobalVar.ProductSettingPath = GetAppConfig("ProductSettingPath");
            //version
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(assembly.Location);
            string version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            string lastdate = fileInfo.LastWriteTime.ToString("ddMMMyyyy - hh:mm:ss");
            GlobalVar.AppVersion = "v" + version + "  " + lastdate;
            lblSoftwareVersion.Text = "Version: " + GlobalVar.AppVersion;

            //Machine Name
            GlobalVar.MachineName = SystemInformation.ComputerName.ToUpper();
            lblMachineID.Text = "MachineID: " + GlobalVar.MachineName;

            //Mes
            //lblMESStatus.Text = "MES Status: NA";
            //frm.lblLoading.Text = "Init Mes";
            GlobalVar.mes.Init();
            if (GlobalVar.mes.isMesConnected)
                lblMESStatus.Text = "MES Status: Connected";
            else
                lblMESStatus.Text = "MES Status: Connection Failed";

            try
            {
                bool isinit = false;
                GlobalVar.system_var = Utilities.XML_Utilities.ReadFromXmlFile<SystemVariable>(GlobalVar.SystemConfig_location);
                if (GlobalVar.system_var == null)
                    GlobalVar.system_var = new SystemVariable();

                if (GlobalVar.system_var != null)
                {
                    //Init io 
                    GlobalVar.g_IOAdvantech[0] = new AdvantechIoFunction();

                    if (GlobalVar.system_var.IOVendor[0] == "Advantech")
                    {
                        string errMsg = "";

                        GlobalVar.g_IOAdvantech[0].IO_Initialize(GlobalVar.system_var.IOBoardID[0], ref errMsg);
                        if (!string.IsNullOrEmpty(errMsg))
                        {
                            str_ret = "IO Initilization Failed";
                            DialogResult dialogResult = MessageBox.Show("Hardware initilisation Error\n" + str_ret +
                                "\nPress 'Yes' to continue.", "Initilisation Hardware Error", MessageBoxButtons.YesNo);
                            if (dialogResult == DialogResult.No) Application.Exit();
                        }
                        else
                        {
                            GlobalVar.g_IOControl = new IOControl(ref GlobalVar.g_IOAdvantech); //global io control
                        }
                    }

                    //init Vision camera
                    GlobalVar.g_Cameras = new Vision.Cameras[4];

                    GlobalVar.g_Cameras = new Vision.Cameras[GlobalVar.system_var.camerasSerial.Length];
                    GlobalVar.vision_vars = new VisionVariable[GlobalVar.system_var.camerasSerial.Length];

                    for (int i = 0; i < GlobalVar.system_var.camerasSerial.Length; i++)
                    {
                        if (GlobalVar.system_var.camerasSerial[i] != "")
                        {
                            GlobalVar.g_Cameras[i] = new Vision.Cameras(GlobalVar.system_var.camerasVendor[i], GlobalVar.system_var.camerasSerial[i]);
                            GlobalVar.vision_vars[i] = new VisionVariable();
                            Thread.Sleep(1);
                        }
                    }

                    //GlobalVar.g_Cameras = new Vision.Cameras();


                    ////Init lighting controller
                    //GlobalVar.g_ledpros = new Vision.LedPro[GlobalVar.system_var.LEDCtrlPorts.Length];

                    //for (int i = 0; i < GlobalVar.g_ledpros.Length; i++)
                    //{
                    //    GlobalVar.g_ledpros[i] = new Vision.LedPro(GlobalVar.SystemSettingPath + "led_setting" + (i + 1).ToString() + ".xml");

                    //    int ledport = GlobalVar.system_var.LEDCtrlPorts[i];
                    //    if (ledport > 0)
                    //    {
                    //        GlobalVar.g_ledpros[i].LE_ComportConnect((byte)GlobalVar.system_var.LEDCtrlPorts[i]);
                    //        GlobalVar.g_ledpros[i].init_controller();
                    //    }
                    //}

                    //ts_cbLed.Items.Clear();
                    //for (int i = 0; i < GlobalVar.g_ledpros.Length; i++)
                    //{
                    //    if ((byte)GlobalVar.system_var.LEDCtrlPorts[i] > 0)
                    //    {
                    //        ts_cbLed.Items.Add("Led " + (i + 1).ToString());
                    //    }
                    //}

                    int ComPort;

                    GlobalVar.g_ledpros = new Vision.LedPro();
                    ComPort = 1;
                    // LedChannel = Convert.ToInt16(comboBox5.Text.Remove(0, 8));
                    GlobalVar.g_ledpros.LE_ComportDisConnect((byte)ComPort);
                    GlobalVar.g_ledpros.LE_ComportConnect((byte)ComPort);//comport2
                    GlobalVar.g_ledpros.init_controller();
                    //GlobalVar.g_ledpros.LE_SetConstInt((byte)2, 0);
                    //camera control form
                    GlobalVar.g_CameraTest = new Vision.Camera.CameraRecipe.CameraRecipe();

                    //Galil Main - 1

                    string[] axesMain = new string[] { "Main X Axis", "Main Y Axis", "Main Z Axis", "Camera Z Axis" };
                    byte[] idsMain = new byte[] { 0x01, 0x02, 0x04, 0x08 };
                    GlobalVar.Galil_Main = new Hardware.Motion_Galil();
                    isinit = GlobalVar.Galil_Main.Connect("169.254.65.58", @"D:\Finisar\HotBar\", axesMain, idsMain);
                    if (!isinit) goto InitError;
                    Hardware.Motion_Galil.IOAddress io_pin;
                    io_pin.Alm_Rst_OUT = 1;
                    io_pin.Alm_IN = 13;
                    io_pin.InPos_IN = 14;
                    io_pin.PLmt_IN = 1;
                    io_pin.Home_IN = 2;
                    io_pin.NLmt_IN = 3;
                    GlobalVar.Galil_Main.SetIOAddress(io_pin, 0);
                    io_pin.Alm_Rst_OUT = 2;
                    io_pin.Alm_IN = 15;
                    io_pin.InPos_IN = 16;
                    io_pin.PLmt_IN = 4;
                    io_pin.Home_IN = 5;
                    io_pin.NLmt_IN = 6;
                    GlobalVar.Galil_Main.SetIOAddress(io_pin, 1);
                    io_pin.Alm_Rst_OUT = 3;
                    io_pin.Alm_IN = 17;
                    io_pin.InPos_IN = 18;
                    io_pin.PLmt_IN = 7;
                    io_pin.Home_IN = 8;
                    io_pin.NLmt_IN = 9;
                    GlobalVar.Galil_Main.SetIOAddress(io_pin, 2);
                    io_pin.Alm_Rst_OUT = 4;
                    io_pin.Alm_IN = 19;
                    io_pin.InPos_IN = 20;
                    io_pin.PLmt_IN = 10;
                    io_pin.Home_IN = 11;
                    io_pin.NLmt_IN = 12;
                    GlobalVar.Galil_Main.SetIOAddress(io_pin, 3);

                    GlobalVar.Galil_Main.MotorOn(0);
                    GlobalVar.Galil_Main.MotorOn(1);
                    GlobalVar.Galil_Main.MotorOn(2);
                    GlobalVar.Galil_Main.MotorOn(3);

                    ////init barcode
                    //if (GlobalVar.system_var.BarcodeAdrr > 0)
                    //{
                    //    SerSystemConnector con = new SerSystemConnector("COM" + GlobalVar.system_var.BarcodeAdrr.ToString());
                    //    _system = new DataManSystem(con);
                    //    _system.DefaultTimeout = 5000;
                    //    _system.Connect();
                    //}

                }

                //timer idle
                timer_idle.Enabled = true;
                timer_idle.Start();

                epoxytime.startTimer(ref lblEpoxyTimer);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init Hardware Error\n" + ex.Message, "Init Hardware Error");
            }
        InitError:
            MessageBox.Show("Init Hardware Error\n" + str_ret, "Init Hardware Error");
            return false;
        }

        private void LoadRecipe(string recipe_name)
        {
            try
            {
                this.Text = GlobalVar.AppName + " - " + recipe_name;
                GlobalVar.rootpath = GlobalVar.ProductSettingPath + recipe_name + "\\";

                //load motion variables
                GlobalVar.GantryPos_Path = GlobalVar.rootpath + "Gantry_Pos.xml";
                GlobalVar.Gantry_Pos = Utilities.XML_Utilities.ReadFromXmlFile<GantryPos>(GlobalVar.GantryPos_Path);
                if (GlobalVar.Gantry_Pos == null) GlobalVar.Gantry_Pos = new GantryPos();


                //load alignment profile 
                GlobalVar.FiberAlign_Profile_Path = GlobalVar.rootpath + "FiberAlign_Profile.xml";
                if (File.Exists(GlobalVar.FiberAlign_Profile_Path))
                    GlobalVar.FiberAlign_profile = Utilities.XML_Utilities.ReadFromXmlFile<AlignmentFlow>(GlobalVar.FiberAlign_Profile_Path);
                else
                    GlobalVar.FiberAlign_profile = new AlignmentFlow();

                GlobalVar.FineFiberAlign_Profile_Path = GlobalVar.rootpath + "FineFiberAlign_Profile.xml";
                if (File.Exists(GlobalVar.FineFiberAlign_Profile_Path))
                    GlobalVar.FineFiberAlign_profile = Utilities.XML_Utilities.ReadFromXmlFile<AlignmentFlow>(GlobalVar.FineFiberAlign_Profile_Path);
                else
                    GlobalVar.FineFiberAlign_profile = new AlignmentFlow();

                //load vision variables   
                for (int i = 0; i < GlobalVar.g_Cameras.Length; i++)
                {
                    if (GlobalVar.system_var.camerasSerial[i] != "")
                    {
                        string vision_var_path = GlobalVar.rootpath + "cam_" + GlobalVar.system_var.camerasSerial[i] + ".xml";
                        if (File.Exists(vision_var_path))
                            GlobalVar.vision_vars[i] = Utilities.XML_Utilities.ReadFromXmlFile<VisionVariable>(vision_var_path);

                        //load vision pattern macthing template into memory
                        using (VisionImage template = new VisionImage())
                        {
                            try
                            {
                                for (int j = 0; j < GlobalVar.vision_vars[i].rois.Count; j++)
                                {

                                    VisionFunctions functionType = (VisionFunctions)Enum.Parse(typeof(VisionFunctions), GlobalVar.vision_vars[i].VisionType[j]);
                                    if (functionType == VisionFunctions.Pattern_Matching)
                                    {

                                        string template_path = GlobalVar.vision_vars[i].VisionTempPath[j];
                                        if (File.Exists(template_path))
                                        {
                                            if (!GlobalVar.VisionTemplateBank.ContainsKey(template_path))
                                            {
                                                template.ReadVisionFile(template_path);
                                                GlobalVar.VisionTemplateBank.Add(template_path, new VisionImage());
                                                Algorithms.Copy(template, GlobalVar.VisionTemplateBank[template_path]);
                                            }
                                        }
                                        //else
                                        //    MessageBox.Show("Missing Vision Template Path\r\n" + template_path);

                                    }

                                }
                            }
                            catch //(Exception ex)
                            {
                                //MessageBox.Show("Init Vision Parameter Error: " + ex.Message);
                            }
                        }
                    }
                }

                //load prod Settings
                GlobalVar.product_var = Utilities.XML_Utilities.ReadFromXmlFile<ProductSettings>(GlobalVar.rootpath + "ProductSettings.xml");

                //reset image results variable
                for (int i = 0; i < GlobalVar.ResultImages.Length; i++)
                    GlobalVar.ResultImages[i] = new VisionImage();

                //total_SiPh = holder = col * row 
                // total_Sleeve = GlobalVar.product_var.SiPh_ColCount * GlobalVar.product_var.SiPh_RowCount;

                Application.DoEvents();
            }
            catch
            {
                MessageBox.Show("Init Production Recipe Error", "Init Error");
            }
        }

        private void InitLogger()
        {
            GlobalVar.Machinelog = new Utilities.Logger(GlobalVar.LogPath + "\\MachineLog\\");
            if (GlobalVar.system_var.ResultPath == "") GlobalVar.system_var.ResultPath = GlobalVar.LogPath;
            GlobalVar.Resultlog = new Utilities.Logger(GlobalVar.system_var.ResultPath + "\\ResultLog\\");
            var fieldNames = typeof(ProcessResult).GetFields()
                                                  .Select(field => field.Name)
                                                  .ToList();
            string[] columns = new string[fieldNames.Count];
            for (int i = 0; i < fieldNames.Count; i++)
            {
                columns[i] = fieldNames[i];
            }
            //GlobalVar.Resultlog.AddHeader(string.Join("\t", columns));

        }

        private void InitDataGridView()
        {
            try
            {
                DataGridViewRow row;

                #region Tray

                ////total_SiPh = holder = col * row 
                //dgvTray.RowHeadersVisible = false;
                //total_Sleeve = GlobalVar.product_var.SiPh_ColCount * GlobalVar.product_var.SiPh_RowCount;
                //for (int Col = 0; Col < GlobalVar.product_var.SiPh_ColCount; Col++)
                //{
                //    dgvTray.Columns.Add("", "");
                //    dgvTray.Columns[Col].Width = 115;
                //    dgvTray.Columns[Col].HeaderText = (Col + 1).ToString();
                //}

                //for (int Row = 0; Row < GlobalVar.product_var.SiPh_RowCount; Row++)
                //{
                //    row = new DataGridViewRow();
                //    row.Height = 48;
                //    dgvTray.Rows.Add(row);
                //    dgvTray.Rows[Row].HeaderCell.Value = (Row + 1).ToString();
                //}

                ////change color
                //for (int i = 0; i < total_Sleeve; i++)
                //{
                //    int Hrow = i / GlobalVar.product_var.SiPh_ColCount;
                //    int Hcol = i % GlobalVar.product_var.SiPh_ColCount;
                //    dgvTray.Rows[Hrow].Cells[Hcol].Style.BackColor = Color.White;
                //    dgvTray.Rows[Hrow].Cells[Hcol].Value = "";
                //}
                //dgvTray.AllowUserToAddRows = false;
                //dgvTray.CurrentCell = null;
                //dgvTray.ClearSelection();

                #endregion

                #region Result table
                //****Result table
                dgvResult.RowHeadersVisible = false;
                var fieldNames = typeof(ProcessResult).GetFields().ToList();
                for (int i = 0; i < fieldNames.Count; i++)
                {
                    dgvResult.Columns.Add(fieldNames[i].Name, fieldNames[i].Name);
                }
                dgvResult.CurrentCell = null;
                dgvResult.ClearSelection();
                #endregion

                #region Process Flow List
                dgvProcessFlow.RowHeadersVisible = false;
                dgvProcessFlow.Columns.Add("#", "#");
                dgvProcessFlow.Columns.Add("Process Flow", "Process Flow");
                //dgvProcessFlow.Columns.Add("Enable", "Enable");
                dgvProcessFlow.Columns[0].Width = 20;
                dgvProcessFlow.Columns[1].Width = 170;
                //dgvProcessFlow.Columns[2].Width = 50;
                dgvProcessFlow.CurrentCell = null;
                dgvProcessFlow.ClearSelection();
                dgvProcessFlow.Rows.Clear();
                int j = 0;
                foreach (string value in Enum.GetNames(typeof(GlobalVar.ProcessFlow)))
                {
                    row = (DataGridViewRow)dgvProcessFlow.Rows[0].Clone();
                    row.Cells[0].Value = (j).ToString();
                    row.Cells[1].Value = value;
                    //row.Cells[2].Value = "true";
                    row.DefaultCellStyle.BackColor = Color.White;
                    dgvProcessFlow.Rows.Add(row);
                    j++;
                }
                dgvProcessFlow.AllowUserToAddRows = false;
                dgvProcessFlow.CurrentCell = null;
                dgvProcessFlow.ClearSelection();
                #endregion

            }
            catch { MessageBox.Show("Init Datagridview Error"); }

        }

        public static string GetAppConfig(string key)
        {
            string value = "";
            try
            {
                value = ConfigurationManager.AppSettings[key];
            }
            catch { }
            return value;
        }
        public static string GetAppConfig(string key, string path)
        {
            string value = "";
            try
            {
                ExeConfigurationFileMap exeFileMap = new ExeConfigurationFileMap();
                exeFileMap.ExeConfigFilename = path;
                System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(exeFileMap, ConfigurationUserLevel.None);
                value = Convert.ToString(config.AppSettings.Settings[key].Value) ?? "Error";

            }
            catch { }
            return value;
        }
        public static void SetAppConfig(string key, string value)
        {
            try
            {
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings[key].Value = value;
                config.AppSettings.SectionInformation.ForceSave = true;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch { }
        }

        private void SetMachineState(MachineState level)
        {


            try
            {
                this.BeginInvoke(new MethodInvoker(delegate
                {
                    switch (level)
                    {
                        case MachineState.Logout:
                            btnIOCtrl.Enabled = false;
                            btnVisonManager.Enabled = false;
                            btnAlignment.Enabled = false;
                            btnManualCtrl.Enabled = false;
                            btnSysHome.Enabled = false;
                            btnStop.Enabled = false;
                            btnStart.Enabled = false;
                            btnLogout.Enabled = false;
                            btnLogin.Enabled = true;
                            tsSettingMenu.Enabled = false;
                            tsSystemConfig.Enabled = false;
                            tsProductSettings.Enabled = false;
                            tsDeviceMenu.Enabled = false;
                            tsFileMenu.Enabled = false;
                            btnResetTimer.Enabled = false;
                            cbOption.Enabled = false;
                            btnResetHolder.Enabled = false;
                            btnMovetoPark.Enabled = false;
                            //btnFiberGrip.Enabled = false;
                            btnStdVision.Enabled = false;

                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_34_TowerLight_Green, false);
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_35_TowerLight_Orange, false);
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_36_TowerLight_Red, true);
                            break;
                        case MachineState.Operator:
                            btnVisonManager.Enabled = false;
                            btnManualCtrl.Enabled = false;
                            btnSysHome.Enabled = true;
                            btnStop.Enabled = false;
                            btnStart.Enabled = true;
                            btnLogout.Enabled = true;
                            btnLogin.Enabled = false;
                            tsSettingMenu.Enabled = false;
                            tsSystemConfig.Enabled = false;
                            tsProductSettings.Enabled = false;
                            tsDeviceMenu.Enabled = false;
                            tsFileMenu.Enabled = true;
                            btnResetTimer.Enabled = true;
                            cbOption.Enabled = true;
                            btnResetHolder.Enabled = true;
                            btnMovetoPark.Enabled = true;
                            //btnFiberGrip.Enabled = true;
                            btnStdVision.Enabled = false;
                            break;
                        case MachineState.Technician:
                            btnIOCtrl.Enabled = true;
                            btnVisonManager.Enabled = true;
                            btnManualCtrl.Enabled = true;
                            btnSysHome.Enabled = true;
                            btnStop.Enabled = false;
                            btnStart.Enabled = true;
                            btnLogout.Enabled = true;
                            btnLogin.Enabled = false;
                            tsSettingMenu.Enabled = true;
                            tsSystemConfig.Enabled = false;
                            tsProductSettings.Enabled = true;
                            tsDeviceMenu.Enabled = true;
                            tsFileMenu.Enabled = true;
                            btnResetTimer.Enabled = true;
                            cbOption.Enabled = true;
                            btnResetHolder.Enabled = true;
                            btnMovetoPark.Enabled = true;
                            //btnFiberGrip.Enabled = true;
                            btnStdVision.Enabled = true;
                            break;
                        case MachineState.Engineer:
                            btnVisonManager.Enabled = true;
                            btnIOCtrl.Enabled = true;
                            btnManualCtrl.Enabled = true;
                            btnSysHome.Enabled = true;
                            btnStop.Enabled = false;
                            btnStart.Enabled = true;
                            btnLogout.Enabled = true;
                            btnLogin.Enabled = false;
                            tsSettingMenu.Enabled = true;
                            tsSystemConfig.Enabled = true;
                            tsProductSettings.Enabled = true;
                            tsDeviceMenu.Enabled = true;
                            tsFileMenu.Enabled = true;
                            btnAlignment.Enabled = true;
                            btnResetTimer.Enabled = true;
                            cbOption.Enabled = true;
                            btnResetHolder.Enabled = true;
                            btnMovetoPark.Enabled = true;
                            //btnFiberGrip.Enabled = true;
                            btnStdVision.Enabled = true;
                            break;
                        case MachineState.Running:
                            //unlock_door.Enabled = false;
                            //lock_door.Enabled = false;
                            btnIOCtrl.Enabled = false;
                            btnVisonManager.Enabled = false;
                            btnAlignment.Enabled = false;
                            btnManualCtrl.Enabled = false;
                            btnSysHome.Enabled = false;
                            btnStop.Enabled = true;
                            btnStart.Enabled = false;
                            btnLogout.Enabled = false;
                            btnLogin.Enabled = false;
                            tsSettingMenu.Enabled = false;
                            tsSystemConfig.Enabled = false;
                            tsProductSettings.Enabled = false;
                            tsDeviceMenu.Enabled = false;
                            tsFileMenu.Enabled = false;
                            btnResetTimer.Enabled = false;
                            cbOption.Enabled = false;
                            btnResetHolder.Enabled = false;
                            btnMovetoPark.Enabled = false;
                            //btnFiberGrip.Enabled = false;
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_34_TowerLight_Green, true);
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_35_TowerLight_Orange, false);
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_36_TowerLight_Red, false);
                            break;
                    }
                }));
            }
            catch { }
        }

        private void LogMessage(string Message, MsgType type)
        {
            try
            {
                this.BeginInvoke(new MethodInvoker(delegate
                {
                    switch (type)
                    {
                        case MsgType.INSTRUCTION:
                            GlobalVar.ref_lblMsg.ForeColor = Color.Black;
                            if (type != last_MsgType)
                            {
                                StopBlinkTowerLight();
                                //SetTowerLight(TowerLight.Green);
                                GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_37_TowerLight_Buzz, false);
                            }
                            break;
                        case MsgType.WARNING:
                            GlobalVar.ref_lblMsg.ForeColor = Color.Orange;
                            if (type != last_MsgType)
                            {
                                StopBlinkTowerLight();
                                SetTowerLight(TowerLight.Yellow);
                                GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_37_TowerLight_Buzz, false);
                            }
                            break;
                        case MsgType.ASSISTANT:
                            GlobalVar.ref_lblMsg.ForeColor = Color.Orange;
                            if (type != last_MsgType)
                            {
                                StartBlinkTowerLight();
                                GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_37_TowerLight_Buzz, true);
                            }
                            break;
                        case MsgType.ERROR:
                            GlobalVar.ref_lblMsg.ForeColor = Color.Red;
                            if (type != last_MsgType)
                            {
                                StopBlinkTowerLight();
                                SetTowerLight(TowerLight.Red);
                                GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_37_TowerLight_Buzz, true);
                            }
                            break;
                    }
                    last_MsgType = type;
                    GlobalVar.ref_lblMsg.Text = Message;
                    AddMachineLogText(Message);
                }));
            }
            catch
            {

            }
        }

        private void SetTowerLight(TowerLight lite)
        {
            //set io here
            this.BeginInvoke(new MethodInvoker(delegate
            {
                switch (lite)
                {
                    case TowerLight.Green:
                        GlobalVar.ref_btnTowerLite.BackColor = Color.LimeGreen;
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_34_TowerLight_Green, true);
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_35_TowerLight_Orange, false);
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_36_TowerLight_Red, false);
                        break;
                    case TowerLight.Yellow:
                        GlobalVar.ref_btnTowerLite.BackColor = Color.Yellow;
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_34_TowerLight_Green, false);
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_35_TowerLight_Orange, true);
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_36_TowerLight_Red, false);
                        break;
                    case TowerLight.Red:
                        GlobalVar.ref_btnTowerLite.BackColor = Color.Red;
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_34_TowerLight_Green, false);
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_35_TowerLight_Orange, false);
                        GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_36_TowerLight_Red, true);
                        break;
                }
            }));
        }

        private void StartBlinkTowerLight()
        {
            isblinkEN = true;
        }

        private void StopBlinkTowerLight()
        {
            isblinkEN = false;
        }

        private void AddMachineLogText(string Log)
        {
            string logText = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "  " + Log;
            GlobalVar.ref_txtStatus.AppendText(logText + "\r\n");
            GlobalVar.Machinelog.AddData(logText);
            GlobalVar.ref_txtStatus.ScrollToCaret();
        }

        private void AddResultLogText()
        {
            try
            {
                string Log = "";
                int i = 0;
                GlobalVar.PrcsRslt.TimeStamp = DateTime.Now.ToString();//"yyyyMMMdd_hhmmss"  //nizammm
                GlobalVar.PrcsRslt.Operator = "";
                GlobalVar.PrcsRslt.SerialNum = txtSerialNum.Text;
                GlobalVar.PrcsRslt.PartNum = txtPartNum.Text;
             
                //GlobalVar.PrcsRslt.LD_SN = "";
                //GlobalVar.PrcsRslt.LD_Unit2 = "";
                //GlobalVar.PrcsRslt.LD_Unit3 = "";
                //GlobalVar.PrcsRslt.LD_Unit4 = "";
                //GlobalVar.PrcsRslt.LD_Unit5 = "";
                //GlobalVar.PrcsRslt.LD_Unit6 = "";
                //GlobalVar.PrcsRslt.LD_Unit7 = "";
                //GlobalVar.PrcsRslt.LD_Unit8 = "";
                //GlobalVar.PrcsRslt.Result = "";

                string[] columns = new string[dgvResult.Columns.Count];
                columns[i] = GlobalVar.PrcsRslt.TimeStamp; i++;
                columns[i] = GlobalVar.PrcsRslt.Operator; i++;
                columns[i] = GlobalVar.PrcsRslt.Unit.ToString(); i++;
                columns[i] = GlobalVar.PrcsRslt.SerialNum; i++;
                columns[i] = GlobalVar.PrcsRslt.PartNum; i++;
                columns[i] = GlobalVar.PrcsRslt.Barcode_SN; i++;
                columns[i] = GlobalVar.PrcsRslt.LD_SN; i++;
                columns[i] = GlobalVar.PrcsRslt.Child_ID; i++;

                //columns[i] = GlobalVar.PrcsRslt.LD_Unit2; i++;
                //columns[i] = GlobalVar.PrcsRslt.LD_Unit3; i++;
                //columns[i] = GlobalVar.PrcsRslt.LD_Unit4; i++;
                //columns[i] = GlobalVar.PrcsRslt.LD_Unit5; i++;
                //columns[i] = GlobalVar.PrcsRslt.LD_Unit6; i++;
                //columns[i] = GlobalVar.PrcsRslt.LD_Unit7; i++;
                //columns[i] = GlobalVar.PrcsRslt.LD_Unit8; i++;
                columns[i] = GlobalVar.PrcsRslt.Wafer_ID; i++;                
                columns[i] = GlobalVar.PrcsRslt.Result; i++;
                //columns[i] = GlobalVar.PrcsRslt.Verdict; i++;
                //columns[i] = GlobalVar.PrcsRslt.isPass.ToString(); i++;

                dgvResult.AllowUserToAddRows = true;
                DataGridViewRow row = (DataGridViewRow)dgvResult.Rows[0].Clone();
                DataGridViewRow row1 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                for (int j = 0; j < dgvResult.Columns.Count; j++)
                {
                    row.Cells[j].Value = columns[j];
                }   
                    //my datagridview1  = dgvResult
                    row1.Cells[0].Value = columns[2];//unit
                    row1.Cells[1].Value = columns[5];//barcode sn
                    row1.Cells[2].Value = columns[6];//ld num  
                    row1.Cells[3].Value = columns[0];//timestamp
                    if(checkBox1.Checked==true){
                        row1.Cells[5].Value = columns[7];//child id is generated
                        row1.Cells[6].Value = columns[8];//wafer id is generated
                    }else{
                        row1.Cells[5].Value = "";//empty child id 
                        row1.Cells[6].Value = "";//empty wafer id
                    }
                
                //add to datagridview
                dgvResult.Rows.Add(row);
                dgvResult.CurrentCell = null;
                dgvResult.AllowUserToAddRows = false;
                //add to datagridview1
                dataGridView1.Rows.Add(row1);
                dataGridView1.CurrentCell = null;
                dataGridView1.AllowUserToAddRows = false;

                //log to text file
                Log = String.Join("\t", columns);
                ///GlobalVar.Resultlog.AddData(Log);
            }
            catch { MessageBox.Show("Error Text"); }

            
            try
            {
                //var fieldNames = typeof(MESFunction.MesWriteData).GetFields().ToList();
                var fieldNames = typeof(ProcessResult).GetFields().ToList();

                string[] columns = new string[fieldNames.Count];
                if (InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate
                    {
                        //Your code here, like set text box content or get text box contents etc..
                        dgvResult.AllowUserToAddRows = true;
                        DataGridViewRow row = (DataGridViewRow)dgvResult.Rows[0].Clone();
                        for (int i = 0; i < fieldNames.Count; i++)
                        {
                            //row.Cells[i].Value = fieldNames[i].GetValue(GlobalVar.MESResult).ToString();
                            //columns[i] = fieldNames[i].GetValue(GlobalVar.MESResult).ToString();

                            row.Cells[i].Value = fieldNames[i].GetValue(GlobalVar.Result).ToString();
                            columns[i] = fieldNames[i].GetValue(GlobalVar.Result).ToString();
                        }
                        dgvResult.Rows.Add(row);
                        dgvResult.CurrentCell = null;
                        dgvResult.AllowUserToAddRows = false;
                        //scroll to bottom
                        dgvResult.FirstDisplayedScrollingRowIndex = dgvResult.RowCount - 1;
                        dgvResult.Invalidate();
                        Application.DoEvents();
                        dgvResult.Rows[dgvResult.Rows.Count - 1].DefaultCellStyle.BackColor = Color.Green;
                        //log header if needed
                        if (!File.Exists(GlobalVar.Resultlog.FileNameWithShift()))
                        {
                            var fieldNameHeader = typeof(ProcessResult).GetFields()
                                  .Select(field => field.Name)
                                  .ToList();
                            string[] columnsHeader = new string[fieldNameHeader.Count];
                            for (int i = 0; i < fieldNames.Count; i++)
                            {
                                columnsHeader[i] = fieldNameHeader[i];
                            }

                            ///GlobalVar.Resultlog.AddHeader(string.Join("\t", columnsHeader));
                        }

                        //log to text file
                        string Log = String.Join("\t", columns);
                        ///GlobalVar.Resultlog.AddData(Log);
                        Sleep_ms(5);

                    }));
                }
                else
                {
                    // Your code here, like set text box content or get text box contents etc..
                    dgvResult.AllowUserToAddRows = true;
                    DataGridViewRow row = (DataGridViewRow)dgvResult.Rows[0].Clone();
                    for (int i = 0; i < fieldNames.Count; i++)
                    {
                        row.Cells[i].Value = fieldNames[i].GetValue(result).ToString();
                        columns[i] = fieldNames[i].GetValue(result).ToString();
                    }

                    dgvResult.Rows.Add(row);
                    dgvResult.CurrentCell = null;
                    dgvResult.AllowUserToAddRows = false;
                    //scroll to bottom
                    dgvResult.FirstDisplayedScrollingRowIndex = dgvResult.RowCount - 1;
                    dgvResult.Invalidate();
                    Application.DoEvents();
                    dgvResult.Rows[dgvResult.Rows.Count - 1].DefaultCellStyle.BackColor = Color.Green;
                    //log header if needed
                    if (!File.Exists(GlobalVar.Resultlog.FileNameWithShift()))
                    {
                        var fieldNameHeader = typeof(ProcessResult).GetFields()
                              .Select(field => field.Name)
                              .ToList();
                        string[] columnsHeader = new string[fieldNameHeader.Count];
                        for (int i = 0; i < fieldNames.Count; i++)
                        {
                            columnsHeader[i] = fieldNameHeader[i];
                        }

                        ///GlobalVar.Resultlog.AddHeader(string.Join("\t", columnsHeader));
                    }

                    //log to text file
                    string Log = String.Join("\t", columns);
                    ///GlobalVar.Resultlog.AddData(Log);

                    Sleep_ms(5);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        private bool System_Homing()
        {
            try
            {
                LogMessage("System Homing Started", MsgType.INSTRUCTION);

                #region safety check
                //if (!GlobalVar.g_IOControl.IsInputOn(IOControl.IO_In.I_15_WaferRingSens))
                //{
                //    LogMessage("Please Remove Wafer Ring!!", MessageType.IM_ERROR);
                //    return false;
                //}

                //if (!GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_00_PickToolExt, false, true, 2000))
                //{
                //    LogMessage("Failed to Retract Pick up Tool!!", MessageType.IM_ERROR);
                //    return false;
                //}

                //if (!GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_01_DispExt, false, true, 2000))
                //{
                //    LogMessage("Failed to Retract Dispense Tool!!", MessageType.IM_ERROR);
                //    return false;
                //} 
                #endregion
                bool isTaskA = false, isTaskB = false, isTaskC = false;
                StartBlinkTowerLight();
                Sleep_ms(100);
                Task taskA = Task.Factory.StartNew(() => isTaskA = Home_X());
                Task taskB = Task.Factory.StartNew(() => isTaskB = Home_Y());
                Task taskC = Task.Factory.StartNew(() => isTaskC = Home_Z());
                taskA.Wait();
                taskB.Wait();
                taskC.Wait();
                StopBlinkTowerLight();

                LogMessage("Homing completed!!", MsgType.INSTRUCTION);
                GlobalVar.isabort = false;
                if (!isTaskA || !isTaskB || !isTaskC)// || !isTaskD || !isTaskE || !isTaskF)
                {
                    LogMessage("System Homing Error", MsgType.ERROR);
                    return false;
                }
                else
                    return true;
            }
            catch
            {
                LogMessage("System Error", MsgType.ERROR);
                return false;
            }
        }

        private bool System_Parking()
        {
            try
            {
                LogMessage("Move Machine to Park", MsgType.INSTRUCTION);
                gMotionClass.Gantry_GoToPos(GantryPos.locnames.locSafety);
                LogMessage("Machine Parked Successfully!!", MsgType.INSTRUCTION);
                GlobalVar.isabort = false;
                return true;
            }
            catch
            {
                LogMessage("Failed To Park Machine!!", MsgType.WARNING);
                return false;
            }
        }

        private bool HomeAxis(MotionGroup group, AxisID axisID, ref string errMsg)
        {
            int iretry = 0;
        retry:
            if (!gMotionClass.HomeAxis((MotionGroup)group, (AxisID)axisID, true, 30))
            {
                errMsg = group.ToString() + "" + axisID.ToString() + "" + "Failed To Home";
                if (iretry == 0)
                {
                    gMotionClass.stopMotion();//(int)group, (int)axisID);
                    goto retry;
                }
                else
                    return false;
            }
            return true;

        }

        private bool PreCheck()
        {
            if (!GlobalVar.isHomeDone)
            {
                LogMessage("Please Home First", MsgType.WARNING);
                return false;
            }

            if (!GlobalVar.g_IOControl.IsInputOn(IOControl.IO_In.I_00_MainAirPressureSensor))
            {
                LogMessage("Main Air Pressure Low", MsgType.WARNING);
                return false;
            }

            if (!GlobalVar.g_IOControl.IsInputOn(IOControl.IO_In.I_36_DoorSensorTop))
            {
                LogMessage("Please Close Top Safety Door", MsgType.ASSISTANT);
                return false;
            }

            if (!GlobalVar.g_IOControl.IsInputOn(IOControl.IO_In.I_37_DoorSensorMain))
            {
                LogMessage("Please Close Main Safety Door", MsgType.ASSISTANT);
                return false;
            }


            return true;
        }

        private bool IsUserAbort()
        {
            if (GlobalVar.isabortalignment)
            {
                //stop machine
                LogMessage("User Stop Machine on Alignement!!", MsgType.WARNING);
                SetMachineState(UserLoginState);
                return true;
            }
            else if (GlobalVar.isabort)
            {
                LogMessage("User Pause Machine!", MsgType.WARNING);
                DialogResult result = MessageBox.Show("User Abort Detected....\r\nClick 'OK' to Stop Machine\r\nClick 'CANCEL' to Continue",
                                                                   "Confirm", MessageBoxButtons.OKCancel);
                if (result == DialogResult.OK)
                {
                    //stop machine
                    LogMessage("User Stop Machine!!", MsgType.WARNING);
                    SetMachineState(UserLoginState);
                    GlobalVar.isAbortClicked = true;
                    return true;
                }
                else
                {
                    //continue
                    GlobalVar.isabort = false;
                    LogMessage("Machine Continue.", MsgType.INSTRUCTION);
                    SetMachineState(MachineState.Running);
                    return false;
                }
            }
            return false;
        }

        private void ResetLensDataGridView()
        {
            this.BeginInvoke(new MethodInvoker(delegate
            {
                dgvFiberHolder.Rows.Clear();
                dgvFiberHolder.Columns.Clear();

                DataGridViewRow row = new DataGridViewRow(); ;

                #region****Tray - Filter Mirror

                //total_SiPh = holder = col * row 
                dgvTray.Rows.Clear();
                dgvTray.Columns.Clear();
                //****Tray - Filter Mirror
                dgvTray.RowHeadersVisible = false;
                total_Sleeve = GlobalVar.product_var.SiPh_ColCount * GlobalVar.product_var.SiPh_RowCount;
                for (int Col = 0; Col < GlobalVar.product_var.SiPh_ColCount; Col++)
                {
                    dgvTray.Columns.Add("", "");
                    dgvTray.Columns[Col].Width = 115;
                    dgvTray.Columns[Col].HeaderText = (Col + 1).ToString();
                }

                for (int Row = 0; Row < GlobalVar.product_var.SiPh_RowCount; Row++)
                {
                    row = new DataGridViewRow();
                    row.Height = 48;
                    dgvTray.Rows.Add(row);
                    dgvTray.Rows[Row].HeaderCell.Value = (Row + 1).ToString();
                }

                //change color
                for (int i = 0; i < total_Sleeve; i++)
                {
                    int Hrow = i / GlobalVar.product_var.SiPh_ColCount;
                    int Hcol = i % GlobalVar.product_var.SiPh_ColCount;
                    dgvTray.Rows[Hrow].Cells[Hcol].Style.BackColor = Color.White;
                    dgvTray.Rows[Hrow].Cells[Hcol].Value = "";
                }
                dgvTray.AllowUserToAddRows = false;
                dgvTray.CurrentCell = null;
                dgvTray.ClearSelection();


                #endregion
            }));


        }

        private void Sleep_ms(int ms)
        {
            if (ms < 1) return;
            DateTime _desired = DateTime.Now.AddMilliseconds(ms);
            while (DateTime.Now < _desired)
            {
                Thread.Sleep(1);
                Application.DoEvents();
            }
        }

        /*Attach_Image + OCR3 + getResponse + IsUserAbort()*/
        public void Attach_Image(int value)
        {
            string text = "";
            switch (value)
            {
                case 1:
                    Sleep_ms(100);
                    Algorithms.Copy(GlobalVar.SourceImage, picture1);
                    imageViewer1.Attach(picture1);
                    imageViewer10.Attach(picture1);
                    /*canOCRRead*/
                    if(GlobalVar.control.optionSelected==1){
                        OCR3(ref text);
                    }else if(GlobalVar.control.optionSelected==2){
                        OCR4(ref text);
                    }
                    /*canOCRRead*/
                    if (GlobalVar.attachImage == true) {
                        Algorithms.Copy(GlobalVar.SourceImage, picture1);
                        imageViewer1.Attach(picture1);
                        GlobalVar.attachImage = false;
                    }
                    if (GlobalVar.isSystemHalt == true) { break; }
                    getResponse();
                    break;
                case 2:
                    Sleep_ms(100);
                    Algorithms.Copy(GlobalVar.SourceImage, picture2);
                    imageViewer2.Attach(picture2);
                    imageViewer10.Attach(picture2);
                    /*canOCRRead*/
                    if (GlobalVar.control.optionSelected == 1) {
                        OCR3(ref text);
                    } else if (GlobalVar.control.optionSelected == 2) {
                        OCR4(ref text);
                    }
                    /*canOCRRead*/
                    if (GlobalVar.attachImage == true) {
                        Algorithms.Copy(GlobalVar.SourceImage, picture2);
                        imageViewer2.Attach(picture2);
                        GlobalVar.attachImage = false;
                    }
                    if (GlobalVar.isSystemHalt == true) { break; }
                    getResponse();
                    break;
                case 3:
                    Sleep_ms(100);
                    Algorithms.Copy(GlobalVar.SourceImage, picture3);
                    imageViewer3.Attach(picture3);
                    imageViewer10.Attach(picture3);
                    /*canOCRRead*/
                    if (GlobalVar.control.optionSelected == 1) {
                        OCR3(ref text);
                    } else if (GlobalVar.control.optionSelected == 2) {
                        OCR4(ref text);
                    }
                    /*canOCRRead*/
                    if (GlobalVar.attachImage == true) {
                        Algorithms.Copy(GlobalVar.SourceImage, picture3);
                        imageViewer3.Attach(picture3);
                        GlobalVar.attachImage = false;
                    }
                    if (GlobalVar.isSystemHalt == true) { break; }
                    getResponse();
                    break;
                case 4:
                    Sleep_ms(100);
                    Algorithms.Copy(GlobalVar.SourceImage, picture4);
                    imageViewer4.Attach(picture4);
                    imageViewer10.Attach(picture4);
                    /*canOCRRead*/
                    if (GlobalVar.control.optionSelected == 1) {
                        OCR3(ref text);
                    } else if (GlobalVar.control.optionSelected == 2) {
                        OCR4(ref text);
                    }
                    /*canOCRRead*/
                    if (GlobalVar.attachImage == true) {
                        Algorithms.Copy(GlobalVar.SourceImage, picture4);
                        imageViewer4.Attach(picture4);
                        GlobalVar.attachImage = false;
                    }
                    if (GlobalVar.isSystemHalt == true) { break; }
                    getResponse();
                    break;
                case 5:
                    Sleep_ms(100);
                    Algorithms.Copy(GlobalVar.SourceImage, picture5);
                    imageViewer5.Attach(picture5);
                    imageViewer10.Attach(picture5);
                    /*canOCRRead*/
                    if (GlobalVar.control.optionSelected == 1) {
                        OCR3(ref text);
                    } else if (GlobalVar.control.optionSelected == 2) {
                        OCR4(ref text);
                    }
                    /*canOCRRead*/
                    if (GlobalVar.attachImage == true) {
                        Algorithms.Copy(GlobalVar.SourceImage, picture5);
                        imageViewer5.Attach(picture5);
                        GlobalVar.attachImage = false;
                    }
                    if (GlobalVar.isSystemHalt == true) { break; }
                    getResponse(); 
                    break;
                case 6:
                    Sleep_ms(100);
                    Algorithms.Copy(GlobalVar.SourceImage, picture6);
                    imageViewer6.Attach(picture6);
                    imageViewer10.Attach(picture6);
                    /*canOCRRead*/
                    if (GlobalVar.control.optionSelected == 1) {
                        OCR3(ref text);
                    } else if (GlobalVar.control.optionSelected == 2) {
                        OCR4(ref text);
                    }
                    /*canOCRRead*/
                    if (GlobalVar.attachImage == true) {
                        Algorithms.Copy(GlobalVar.SourceImage, picture6);
                        imageViewer6.Attach(picture6);
                        GlobalVar.attachImage = false;
                    }
                    if (GlobalVar.isSystemHalt == true) { break; }
                    getResponse();
                    break;
                case 7:
                    Sleep_ms(100);
                    Algorithms.Copy(GlobalVar.SourceImage, picture7);
                    imageViewer7.Attach(picture7);
                    imageViewer10.Attach(picture7);
                    /*canOCRRead*/
                    if (GlobalVar.control.optionSelected == 1) {
                        OCR3(ref text);
                    } else if (GlobalVar.control.optionSelected == 2) {
                        OCR4(ref text);
                    }
                    /*canOCRRead*/
                    if (GlobalVar.attachImage == true) {
                        Algorithms.Copy(GlobalVar.SourceImage, picture7);
                        imageViewer7.Attach(picture7);
                        GlobalVar.attachImage = false;
                    }
                    if (GlobalVar.isSystemHalt == true) { break; }
                    getResponse();
                    break;
                case 8:
                    Sleep_ms(100);
                    Algorithms.Copy(GlobalVar.SourceImage, picture8);
                    imageViewer8.Attach(picture8);
                    imageViewer10.Attach(picture8);
                    /*canOCRRead*/
                    if (GlobalVar.control.optionSelected == 1) {
                        OCR3(ref text);
                    } else if (GlobalVar.control.optionSelected == 2) {
                        OCR4(ref text);
                    }
                    /*canOCRRead*/
                    if (GlobalVar.attachImage == true) {
                        Algorithms.Copy(GlobalVar.SourceImage, picture8);
                        imageViewer8.Attach(picture8);
                        GlobalVar.attachImage = false;
                    }
                    if (GlobalVar.isSystemHalt == true) { break; }
                    getResponse();
                    break;
            }

        }
        /*Attach_Image + OCR3 + getResponse + IsUserAbort()*/

        /*OCR Read*/
        public static bool canOCRRead() { 
            return GlobalVar.canOCRRead;
        }
        /*OCR Read*/

        /*LEDPos*/
        public void LEDpos () {
            frmManualMotion = new frmManualMotion();
            frmManualMotion.ShowDialog();
            frmManualMotion.BringToFront();
        }
        /*LEDPos*/

        /*(Event Handler) Mini starts here*/
        private void Mini_window_Click(object sender, EventArgs e)
        {
            frmManualMotion = new frmManualMotion();
            frmManualMotion.ShowDialog();
            frmManualMotion.BringToFront();
            //MessageBox.Show(myDateAfter);
            //string ld_unit = model.ld_unit;
            //string underscore = model.underscore;
            //string exe = model.png;
            //string a;
            //a = "" + DateTime.Now;
            //a = a.Replace("/", " ");
            //a = a.Replace(":", " ");
            //GlobalVar.SourceImage.WritePngFile(@"D:\Syamil'sLD\" + ld_unit + GlobalVar.myLD + a + GlobalVar.myUnit + underscore + GlobalVar.PrcsRslt.Barcode_SN + exe);
        }
        /*(Event Handler) Mini ends here*/

        /*Toogle LED OFF starts here*/
        private void toggleLEDOff_Click(object sender, EventArgs e)
        {   
            GlobalVar.g_ledpros.LE_SetConstInt((byte)2, 0);//com2 channel1
            GlobalVar.g_ledpros.LE_SetStrobeInt((byte)2, 0);
            GlobalVar.g_ledpros.LE_SetStrobeWidth((byte)2, 0); //set none active chnl to 0
            GlobalVar.g_ledpros.LE_SetCHMode((byte)2, 0);
            GlobalVar.isItTrue = false;
        }
        /*Toogle LED OFF ends here*/

        /*Toogle LED ON starts here*/
        private void toogleLED_Click(object sender, EventArgs e)
        {
            GlobalVar.isItTrue = true;
            GlobalVar.lightIntensity = Convert.ToByte(trackBar1.Value);
            GlobalVar.LedChannel = 2;
            GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, GlobalVar.lightIntensity);
            GlobalVar.g_ledpros.LE_SetStrobeInt((byte)GlobalVar.LedChannel, 0);
            GlobalVar.g_ledpros.LE_SetStrobeWidth((byte)GlobalVar.LedChannel, 0); //set none active chnl to 0
            GlobalVar.g_ledpros.LE_SetCHMode((byte)GlobalVar.LedChannel, 0);
            GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, GlobalVar.lightIntensity); //custom brightness
            GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, GlobalVar.lightIntensity);
        }
        /*Toogle LED ON ends here*/

        /*Adjust light starts here*/
        public void adjustLED()
        {
            if (GlobalVar.isItTrue == true)
            {
                GlobalVar.LedChannel = 2;
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, GlobalVar.lightIntensity);
                GlobalVar.g_ledpros.LE_SetStrobeInt((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetStrobeWidth((byte)GlobalVar.LedChannel, 0); //set none active chnl to 0
                GlobalVar.g_ledpros.LE_SetCHMode((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, GlobalVar.lightIntensity); //custom brightness
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, GlobalVar.lightIntensity);
            }
            else {
                GlobalVar.LedChannel = 2;
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetStrobeInt((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetStrobeWidth((byte)GlobalVar.LedChannel, 0); //set none active chnl to 0
                GlobalVar.g_ledpros.LE_SetCHMode((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, 2); //default brightness
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, 0);            
            }
        }
        /*Adjust light ends here*/

        /*Trackbar starts here*/
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox1.Text = "" + trackBar1.Value;
            trackBar1.LargeChange = 10;
            trackBar1.SmallChange = 10;
            trackBar1.TickFrequency = 51;
            trackBar1.Maximum = 255;
            GlobalVar.lightIntensity = Convert.ToByte(trackBar1.Value);
            adjustLED();
        }
        /*Trackbar ends here*/

        /*Prompt pass or fail starts here*/
        public void getResponse()
        {
            var model = new window_model();
            string noLdnum = model.noLdNum;
            DialogResult dialogResult = MessageBox.Show(model.clickMsg, model.LD_Compare, MessageBoxButtons.YesNo);
            dataGridView1.AllowUserToAddRows = true;

            if (dialogResult == DialogResult.Yes)
            {

                string anyResult = "PASS";
                dataGridView2.Rows.Add(anyResult);
                GlobalVar.lDCustomOverwrite = noLdnum;
            }
            else if (dialogResult == DialogResult.No)
            {
                string anyResult = "FAIL";
                dataGridView2.Rows.Add(anyResult);
                GlobalVar.lDCustomOverwrite = noLdnum;
            }
        }
        /*Prompt pass or fail ends here*/

        /*Push Data in Push_DatatoDb state Starts Here*/
        /*public bool pushDatatoCamstarnow() {
            bool conn = false;
            int MAXXI = dgvResult.Rows.Count - 1;
            string[] unitActualNum = new string[MAXXI];
            string[] barcodeActualNum = new string[MAXXI];
            string[] ldActualNum = new string[MAXXI];
            string[] verdictActualNum = new string[MAXXI];
            string[] TimeStamp = new string[MAXXI];
            for(int owo=0;owo<MAXXI;owo++){
                unitActualNum.SetValue(dataGridView1.Rows[owo].Cells[0].Value,owo);
                barcodeActualNum.SetValue(dataGridView1.Rows[owo].Cells[1].Value,owo);
                ldActualNum.SetValue(dataGridView1.Rows[owo].Cells[2].Value,owo);
                verdictActualNum.SetValue(dataGridView1.Rows[owo].Cells[4].Value,owo);
                TimeStamp.SetValue(dataGridView1.Rows[owo].Cells[3].Value,owo);
            }
            for (int uwu = 0; uwu < MAXXI;uwu++ ){
                GlobalVar.MESResult.WhichTest = "ImgCapture_2x200G";
                GlobalVar.MESResult.TxnId = unitActualNum.GetValue(uwu).ToString();
                GlobalVar.MESResult.BaseAssemblyNum = barcodeActualNum.GetValue(uwu).ToString();
                GlobalVar.MESResult.BasePartNum = ldActualNum.GetValue(uwu).ToString();
                GlobalVar.MESResult.TestResult = verdictActualNum.GetValue(uwu).ToString();
                GlobalVar.MESResult.TestPlanDate = TimeStamp.GetValue(uwu).ToString();

                if (!GlobalVar.mes.WriteData(GlobalVar.MESResult)){
                    conn = false;
                }
                else {
                    conn = true;
                }
            }
            return conn;
        }*/
        /*Push Data in Push_DatatoDb state Ends Here*/

        /*Push Actual Data to Camstar Starts Here*/
        public bool pushActualDatatoCamstar() { 
            int MAXX = dgvResult.Rows.Count - 1;
            string[] unitMES = new string[MAXX];
            string[] barcodeMES = new string[MAXX];
            string[] ldMES = new string[MAXX];
            string[] verdictMES = new string[MAXX];
            string[] dateMES = new string[MAXX];
            string[] waferMES = new string[MAXX];
            string[] childMES = new string[MAXX];
            int[] verdictInt_MES = new int[MAXX];
            string compareStr = "PASS";
            int[] pf_Param = new int[2] {0,1};
            bool conn=false;
            for(int m=0;m<MAXX;m++){
                unitMES.SetValue(dataGridView1.Rows[m].Cells[0].Value.ToString(), m);
                barcodeMES.SetValue(dataGridView1.Rows[m].Cells[1].Value.ToString(), m);
                ldMES.SetValue(dataGridView1.Rows[m].Cells[2].Value.ToString(), m);
                dateMES.SetValue(dataGridView1.Rows[m].Cells[3].Value.ToString(),m);
                verdictMES.SetValue(dataGridView2.Rows[m].Cells[0].Value.ToString(), m);
                waferMES.SetValue(dataGridView1.Rows[m].Cells[6].Value.ToString(),m);
                childMES.SetValue(dataGridView1.Rows[m].Cells[5].Value.ToString(),m);
            }
            /*Change PASS / FAIL to 0 / 1 respesctively*/
            for (int x = 0; x < MAXX;x++ ) {
                //0 PASS 1 & -1 FAIL
                if (compareStr.CompareTo(verdictMES[x]) == 0) {
                    //PASS
                    verdictInt_MES.SetValue(pf_Param[1], x);
                } else { 
                    //FAIL
                    verdictInt_MES.SetValue(pf_Param[0],x);
                }
            }
            /*Change PASS / FAIL to 0 / 1 respesctively*/
            for (int n = 0; n < MAXX; n++)
            {
                GlobalVar.MESResult.WhichTest = "2x200_Record_Image";
                GlobalVar.MESResult.HeaderSerialNum = barcodeMES.GetValue(n).ToString(); ;//"CM2204037-14";//CM214303D-00//CM2150038-1"//LOL12345 //A05A014 //2x200_Record_Image
                GlobalVar.MESResult.IsRetest = unitMES.GetValue(n).ToString();
                GlobalVar.MESResult.BaseAssemblyNum = barcodeMES.GetValue(n).ToString();
                GlobalVar.MESResult.BasePartNum = ldMES.GetValue(n).ToString();
                GlobalVar.MESResult.TestPlanDate = dateMES.GetValue(n).ToString();
                GlobalVar.MESResult.TestResult = Convert.ToInt32(verdictInt_MES.GetValue(n).ToString());
                GlobalVar.MESResult.WaferID = waferMES.GetValue(n).ToString();
                GlobalVar.MESResult.ReWork = childMES.GetValue(n).ToString();
                if (!(GlobalVar.mes.WriteData(GlobalVar.MESResult)))
                {
                    conn = false;
                }
                else {
                    conn = true;
                }
            }
            return conn;
        }
        /*Push Actual Data to Camstar Ends Here*/

        /*activateFrmNkedOption starts here*/
        public void activateFrmNkedOption() {
            frmNkedOption = new frmNkedOption();
            frmNkedOption.ShowDialog();
            frmNkedOption.BringToFront();
        }
        /*activeFrmNkedOption ends here*/
        
        /*change reference image starts here*/
        public void changeReferenceImage() {
            //true denoted solid eye(SE_pro) while false denoted liquid eye(Sample2)
            if (GlobalVar.control.optionSelected == 1) {
                pictureBox1.Image = V2Tech.Automations.Apps.Properties.Resources.SE_pro;
            } else {
                pictureBox1.Image = V2Tech.Automations.Apps.Properties.Resources.Sample2;
            }
        }
        /*change reference image starts here*/
        
        /*turn on history starts here*/
        public void turnOnHistory() {
            var modl = new window_model();
            string tab = modl.tab;
            string rootPath = modl.rootPath;
            string machineName = modl.machineName;
            string u = modl.underscore;
            string yymmdd = modl.yymmdd;
            string tt = modl.tt;
            string empty = modl.empty;
            string slash = modl.slash;
            string[] headers = modl.header;
            string txt = modl.txt;
            DateTime getCurrentDate = DateTime.Now.Date;
            DateTime getAMPM = DateTime.Now.Date;
            string myDateOnly = getCurrentDate.ToString(yymmdd).Replace(slash,empty);
            string AMPM = getAMPM.ToString(tt);
            string logInfo = machineName + u + myDateOnly + u + AMPM;
            string rootPathPath = modl.rootPath + machineName + u + myDateOnly + u + AMPM + txt;
            if (!File.Exists(rootPathPath)) {
                try {
                    using (StreamWriter sw = File.CreateText(rootPathPath)) {
                        for (int i = 0; i < 10; i++) {
                            sw.Write(headers[i] + tab);
                        }
                    }

                    using (StreamWriter fw = File.AppendText(rootPathPath)) {
                        int MAXXo = dataGridView1.Rows.Count ;
                        if (dataGridView1.Rows.Count > 0) {
                            string[] unit = new string[MAXXo];
                            string[] barcodeSN = new string[MAXXo];
                            string[] ldNum = new string[MAXXo];
                            string[] timeStamp = new string[MAXXo];
                            string[] verdict = new string[MAXXo];
                            string[] childId = new string[MAXXo];
                            string[] waferId = new string[MAXXo];
                            for (int i = 0; i < MAXXo; i++) {
                                unit.SetValue(dataGridView1.Rows[i].Cells[0].Value.ToString(), i);
                                barcodeSN.SetValue(dataGridView1.Rows[i].Cells[1].Value.ToString(), i);
                                ldNum.SetValue(dataGridView1.Rows[i].Cells[2].Value.ToString(), i);
                                timeStamp.SetValue(dataGridView1.Rows[i].Cells[3].Value.ToString(), i);
                                verdict.SetValue(dataGridView1.Rows[i].Cells[4].Value.ToString(), i);
                                childId.SetValue(dataGridView1.Rows[i].Cells[5].Value.ToString(), i);
                                waferId.SetValue(dataGridView1.Rows[i].Cells[6].Value.ToString(), i);
                            }
                            for (int y = 0; y < MAXXo; y++) {
                                fw.WriteLine(timeStamp[y] + tab + tab + unit[y] + tab + tab + tab + tab + barcodeSN[y] + tab + ldNum[y] + tab + childId[y] + tab + waferId[y] + tab + tab + verdict[y]);
                            }
                        }
                    }
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString());
                }            
            } else {
                try {
                    using (StreamWriter fw = File.AppendText(rootPathPath)) {
                        int MAXXo = dataGridView1.Rows.Count;
                        if (dataGridView1.Rows.Count > 0) {
                            string[] unit = new string[MAXXo];
                            string[] barcodeSN = new string[MAXXo];
                            string[] ldNum = new string[MAXXo];
                            string[] timeStamp = new string[MAXXo];
                            string[] verdict = new string[MAXXo];
                            string[] childId = new string[MAXXo];
                            string[] waferId = new string[MAXXo];
                            for (int i = 0; i < MAXXo; i++) {
                                unit.SetValue(dataGridView1.Rows[i].Cells[0].Value.ToString(), i);
                                barcodeSN.SetValue(dataGridView1.Rows[i].Cells[1].Value.ToString(), i);
                                ldNum.SetValue(dataGridView1.Rows[i].Cells[2].Value.ToString(), i);
                                timeStamp.SetValue(dataGridView1.Rows[i].Cells[3].Value.ToString(), i);
                                verdict.SetValue(dataGridView1.Rows[i].Cells[4].Value.ToString(), i);
                                childId.SetValue(dataGridView1.Rows[i].Cells[5].Value.ToString(), i);
                                waferId.SetValue(dataGridView1.Rows[i].Cells[6].Value.ToString(), i);
                            }
                            for (int y = 0; y < MAXXo; y++) {
                                fw.WriteLine(timeStamp[y] + tab + tab + unit[y] + tab + tab + tab + tab + barcodeSN[y] + tab + ldNum[y] + tab + childId[y] + tab + waferId[y] + tab + tab + verdict[y]);
                            }
                        }
                    }                
                }catch(Exception ex){
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        /*turn on history ends here*/

        public bool Do_AutoProcess()
        {
            GlobalVar.isSystemHalt = false;
            changeReferenceImage();
            DialogResult dlgresult;
            Random rnd = new Random();
            bool ret_IO = false;
            bool ret_Motion = false;
            bool ret_SafetyCheck = false;
            bool ispass = false;
            string a, serialnum = "";
            Vision.Image_Processing.Results result = new Vision.Image_Processing.Results();
            try
            {
                //GlobalVar.g_ledpros[0].LE_SetConstInt((byte)2, 0);//com2 channel1
                //GlobalVar.g_ledpros[0].LE_SetStrobeInt((byte)2, 0);
                //GlobalVar.g_ledpros[0].LE_SetStrobeWidth((byte)2, 0); //set none active chnl to 0
                //GlobalVar.g_ledpros[0].LE_SetCHMode((byte)2, 0);
                GlobalVar.g_ledpros.LE_SetConstInt((byte)2, 0);//com2 channel1
                GlobalVar.g_ledpros.LE_SetStrobeInt((byte)2, 0);
                GlobalVar.g_ledpros.LE_SetStrobeWidth((byte)2, 0); //set none active chnl to 0
                GlobalVar.g_ledpros.LE_SetCHMode((byte)2, 0);
                sw_fullflow.Restart();
                GlobalVar.Result.ResetData();
                //init motion class if null
                if (gMotionClass == null || gMotionClass.IsDisposed)
                    gMotionClass = new frmMotionTeaching();
                //init vision class if null
                if (GlobalVar.g_CameraTest == null || GlobalVar.g_CameraTest.IsDisposed)
                    GlobalVar.g_CameraTest = new Vision.Camera.CameraRecipe.CameraRecipe();
                //reset all dgv white colour
                foreach (GlobalVar.ProcessFlow current_flow in Enum.GetValues(typeof(GlobalVar.ProcessFlow)))
                {
                    dgvProcessFlow.Rows[(int)current_flow].DefaultCellStyle.BackColor = Color.White;
                }
                int current_step = 0;
                //loop process flow by enum sequence
                foreach (GlobalVar.ProcessFlow current_flow in Enum.GetValues(typeof(GlobalVar.ProcessFlow)))
                {
                    current_step = (int)current_flow;
                    sw_process.Restart();
                    //update in progress step color yellow
                    //dgvTray.Rows[Sleeve_row].Cells[Sleeve_col].Style.BackColor = Color.Yellow;
                    dgvProcessFlow.Rows[(int)current_flow].DefaultCellStyle.BackColor = Color.Yellow;
                    Application.DoEvents();
                    LogMessage("Starting " + current_flow.ToString() + "...", MsgType.INSTRUCTION);

                    //skip checking abort signal for Safety_PreCheck state
                    if (current_flow != GlobalVar.ProcessFlow.Safety_PreCheck) { if (IsUserAbort()) goto JobDone; }
                    Sleep_ms(10);//debug use only 
                    //all process flow need to develop and debug 1 by 1
                    switch (current_flow)
                    {


                        case GlobalVar.ProcessFlow.Safety_PreCheck:                         //nizammm
                            #region Safety PreCheck

                            //Safety check
                             ret_SafetyCheck = PreCheck(); if (!ret_SafetyCheck) goto _SafetyError;

                            //lock Top And Main Door
                            ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, true, false, 3000); if (!ret_IO) goto _IOError;
                            ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, true, true, 3000); if (!ret_IO) goto _IOError;
                            //Task.Factory.StartNew(frmMain.closeDoorPlease);

                            /*Clear Table & lblDataStatus Starts Here*/
                            dataGridView1.Rows.Clear();
                            dataGridView2.Rows.Clear();
                            dgvResult.Rows.Clear();
                            /*Clear Table & lblDataStatus Ends Here*/

                            #endregion
                            break;
                        case GlobalVar.ProcessFlow.Scan_BarCode:
                            #region Scan_BarCode
                            //if (IsUserAbort()) goto JobDone;
                            //res = _system.SendCommand("TRIGGER ON");
                            //res = _system.SendCommand("GET RESULT");
                            //GlobalVar.Result.PartNum = txtPartNum.Text;
                            //GlobalVar.Result.SerialNum = res.PayLoad;
                            //txtSerialNum.Text = GlobalVar.Result.SerialNum;
                            //GlobalVar.Result.Operator = GlobalVar.UserName;
                            #endregion
                            break;
                        case GlobalVar.ProcessFlow.Move_CaptureReference:
                            #region MovetoTopCam1
                            //ret_Motion = gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam1); if (!ret_Motion) goto _MotionError;
                            //Sleep_ms(50);
                            //ret_Motion = GlobalVar.Galil_Main.SetSpeed(10, (int)GalilAxis.X); if (!ret_Motion) goto _MotionError; if (IsUserAbort()) goto JobDone;
                            //ret_Motion = GlobalVar.Galil_Main.SetSpeed(10, (int)GalilAxis.Y); if (!ret_Motion) goto _MotionError; if (IsUserAbort()) goto JobDone;
                            //ret_Motion = GlobalVar.Galil_Main.SetSpeed(10, (int)GalilAxis.Z); if (!ret_Motion) goto _MotionError; if (IsUserAbort()) goto JobDone;
                            //GlobalVar.g_CameraTest.CaptureAndInspect(ref result, VisionPosition.CamPos.cam1_Reference); if (!result.Status) goto _VisionError; if (IsUserAbort()) goto JobDone;

                            #endregion
                            break;


                        case GlobalVar.ProcessFlow.Move_CaptureSN:
                            /*Clear Table & lblDataStatus Starts Here*/
                            dataGridView1.Rows.Clear();
                            dataGridView2.Rows.Clear();
                            dgvResult.Rows.Clear();
                            /*Clear Table & lblDataStatus Ends Here*/
                            //Testing Capture_LD
                            var view = new window_model();
                            lblDataStatus.Text = view.dataPush;
                            for (int Unit = 1; Unit < 5; Unit++)
                            {
                                //string data_read = "";
                                string data_read = "CM1122333-01";
                                string[] txtbcdArrays = new string[4]; 
                                var model = new window_model();
                                switch (Unit)
                                {
                                    case 1:
                                        if (!gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam2_Barcode_Unit1)) goto _MotionError;
                                        Sleep_ms(50);
                                        txtbcd1.Text = GlobalVar.g_barcodeRead.getreading();
                                        GlobalVar.myUnit = "1";
                                        if (txtbcd1.Text == "No Barcode")
                                        {
                                            manualData();
                                            txtbcd1.Text = GlobalVar.PrcsRslt.Barcode_SN;

                                            if (GlobalVar.PrcsRslt.Barcode_SN == "")//.....
                                            {
                                                txtbcd1.Text = "No Barcode";
                                                goto _SKIP;
                                            }
                                        }
                                           //continue;
            
                                        if (!gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam2_LD1_Unit1)) goto _MotionError;

                                        break;
                                    case 2:
                                        if (!gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam2_Barcode_Unit2)) goto _MotionError;
                                        Sleep_ms(50);
                                        txtbcd2.Text = GlobalVar.g_barcodeRead.getreading(); 
                                        GlobalVar.myUnit = "2";
                                        if (txtbcd2.Text == "No Barcode")
                                        {
                                            manualData();
                                            txtbcd2.Text = GlobalVar.PrcsRslt.Barcode_SN;

                                            if (GlobalVar.PrcsRslt.Barcode_SN == "")//.....
                                            {
                                                txtbcd2.Text = "No Barcode";
                                                goto _SKIP;
                                            }
                                        }
                                          //continue;
                                      
                                        if (!gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam2_LD1_Unit2)) goto _MotionError;

                                        break;
                                    case 3:
                                        if (!gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam2_Barcode_Unit3)) goto _MotionError;
                                        Sleep_ms(50);
                                        txtbcd3.Text = GlobalVar.g_barcodeRead.getreading(); 
                                        GlobalVar.myUnit = "3";
                                        if (txtbcd3.Text == "No Barcode")
                                        {
                                            manualData();
                                            txtbcd3.Text = GlobalVar.PrcsRslt.Barcode_SN;

                                            if (GlobalVar.PrcsRslt.Barcode_SN == "")//.....
                                            {
                                                txtbcd3.Text = "No Barcode";
                                                goto _SKIP;
                                            }
                                        }
                                            //continue;
                                        
                                        if (!gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam2_LD1_Unit3)) goto _MotionError;

                                        break;
                                    case 4:
                                        if (!gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam2_Barcode_Unit4)) goto _MotionError;
                                        Sleep_ms(50);
                                        txtbcd4.Text = GlobalVar.g_barcodeRead.getreading();
                                        GlobalVar.myUnit = "4";
                                        if (txtbcd4.Text == "No Barcode")
                                        {
                                            manualData();
                                            txtbcd4.Text = GlobalVar.PrcsRslt.Barcode_SN;

                                            if (GlobalVar.PrcsRslt.Barcode_SN == "")//.....
                                            {
                                                txtbcd4.Text = "No Barcode";
                                                goto _SKIP;
                                            }
                                        }
                                            //continue;
                                        
                                        if (!gMotionClass.Gantry_GoToPos(GantryPos.locnames.locTopCam2_LD1_Unit4)) goto _MotionError;
                                        break;
                                }

                                for (int LD = 1; LD < 9; LD++)
                                {
                                    //int retryCounter = 0;
                                    string text = "";
                                    string ID = "";

                                    bool result1 = false;
                                    GlobalVar.myLD = LD.ToString();
                                    if (LD != 1)
                                    {
                                        ret_Motion = GlobalVar.Galil_Main.MoveRel(0.85, (int)GalilAxis.X, true); if (IsUserAbort()) goto JobDone;
                                        Sleep_ms(100);
                                    }

                                    LogMessage("Move Next LD_" + LD, MsgType.INSTRUCTION);
                                    Sleep_ms(50);
                                    capture_Image();
                                    save_image(Unit.ToString(), LD.ToString());

                                    Attach_Image(LD);

                                    //manual keyin LD number
                                    if (!manualWriteLD)
                                    {
                                        frmManualLD = new frmManualKeyIn();
                                        frmManualLD.ShowDialog();
                                        this.BringToFront();
                                        frmManualLD.BringToFront();
                                    }

                                    else
                                        //Code to retrieve waferID
                                        if(checkBox1.Checked==true){
                                            GlobalVar.PrcsRslt.Unit = LD;

                                            GlobalVar.mes.GetWaferID(data_read, LD, ref ID);

                                            GlobalVar.PrcsRslt.Child_ID = ID + GlobalVar.PrcsRslt.LD_SN;
                                            //GlobalVar.mes.GetGenericData();                                        
                                        }else{
                                            GlobalVar.PrcsRslt.Child_ID = "";
                                        }
                                       
                                       
                                    GlobalVar.PrcsRslt.Unit = LD;

                                    AddResultLogText();

                                    /*Concat specific rows and columns at datagridview1 from datagridview2 starts here*/
                                    int MAXX = dgvResult.Rows.Count - 1;
                                        if (dgvResult.Rows.Count > 0)
                                        {
                                            int ai = 0;
                                            while(ai>=0){
                                                dataGridView1.Rows[ai].Cells[4].Value = dataGridView2.Rows[ai].Cells[0].Value;
                                                ai++;
                                            if(ai>=MAXX)break;
                                            }
                                        }
                                    /*Concat specific rows and columns at datagridview1 from datagridview2 ends here*/
                                }


                            _SKIP:
                                AddMachineLogText("Barcode cancel");
                            }
                            break;
                        case GlobalVar.ProcessFlow.PushDatatoDb:
                            var controller = new window_model();
                            string ERRmsg = controller.NOdataPush;
                            string SCCmsg = controller.sccMsg;
                            string dataPush = controller.dataPush;
                            int AMT = dataGridView1.Rows.Count;
                            int dgvAMT = dgvResult.Rows.Count - 1;
                            
                            textBox3.Text = dgvAMT.ToString();
                            if (dataGridView1.Rows.Count > 0)
                            {
                                //pushActualDatatoCamstar();
                                if (!pushActualDatatoCamstar())
                                {
                                    lblDataStatus.Text = ERRmsg;
                                    MessageBox.Show(ERRmsg);
                                }
                                else
                                {
                                    lblDataStatus.Text = dataPush + AMT;
                                    MessageBox.Show(SCCmsg);
                                }
                            }
                            break;
                        case GlobalVar.ProcessFlow.Writing_History:
                            int MAXXo = dgvResult.Rows.Count - 1;
                            var db = new window_model();
                            /*GetValues from datagridview1 then change them into an array and put in nkedEye.txt starts here*/
                            //int[] myNum = new int[4] { 8, 16, 24, 32 };
                            int yuyu = 0;
                            do {
                                //if (dataGridView1.Rows.Count >= myNum[yuyu]) {
                                if (dataGridView1.Rows.Count >0) {
                                    try {
                                        int o;
                                        string[] verdictRows = new string[MAXXo];
                                        string[] barcodeRows = new string[MAXXo];
                                        string[] ldRows = new string[MAXXo];
                                        string[] unitRows = new string[MAXXo];
                                        string[] timeRows = new string[MAXXo];
                                        string path = @db.pathName;

                                        for (o = 0; o < MAXXo; o++) {
                                            unitRows.SetValue(dataGridView1.Rows[o].Cells[0].Value.ToString(), o);
                                            barcodeRows.SetValue(dataGridView1.Rows[o].Cells[1].Value.ToString(), o);
                                            ldRows.SetValue(dataGridView1.Rows[o].Cells[2].Value.ToString(), o);
                                            timeRows.SetValue(dataGridView1.Rows[o].Cells[3].Value.ToString(), o);
                                            verdictRows.SetValue(dataGridView1.Rows[o].Cells[4].Value.ToString(), o);
                                        }
                                        using (StreamWriter sw = File.AppendText(path)) {

                                            for (o = 0; o < MAXXo; o++) {
                                                sw.WriteLine(unitRows[o] + db.tab + barcodeRows[o] + db.tab + ldRows[o] + db.tab + timeRows[o] + db.tab + verdictRows[o]);
                                            }
                                        }
                                    } catch (Exception) { }
                                } else { }
                                yuyu++;
                            } while (yuyu < 4);
                        /*GetValues from datagridview1 then change them into an array and put in nkedEye.txt ends here*/
                            turnOnHistory();
                            break;
                        case GlobalVar.ProcessFlow.FinishTask:
                            #region FinishTask
                            GlobalVar.Result.TimeStamp = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
                            GlobalVar.Result.Result = "Pass";
                            //turnOnHistory();
                            /*Clear Table & lblDataStatus Starts Here*/
                            //dataGridView1.Rows.Clear();
                            //dataGridView2.Rows.Clear();
                            //dgvResult.Rows.Clear();
                            /*Clear Table & lblDataStatus Ends Here*/

                            //Unlock Top And Main Door 
                            ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, false, false, 3000);
                            ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, false, false, 3000);
                            
                            //Task.Factory.StartNew(frmMain.openDoorPlease);

                            ret_Motion = gMotionClass.Gantry_GoToPos(GantryPos.locnames.locSafety); if (!ret_Motion) goto _MotionError; if (IsUserAbort()) goto JobDone;
                            sw_fullflow.Stop();
                            sw_process.Stop();
                            LogMessage("Process Completed!...Cycle Time - " + sw_fullflow.Elapsed.TotalSeconds.ToString("N2") + "s", MsgType.WARNING);
        
                            //update Tray result color
                            //dgvTray.Rows[Sleeve_row].Cells[Sleeve_col].Style.BackColor = Color.Green; //pass
                            //Sleeve_count++;
                            #endregion
                            break;

                        default:
                            break;

                    }

                    if (current_flow != GlobalVar.ProcessFlow.FinishTask)
                        LogMessage("Process " + current_flow.ToString() + " Done - " + sw_process.Elapsed.TotalSeconds.ToString("N2") + "s", MsgType.INSTRUCTION);
                    dgvProcessFlow.Rows[(int)current_flow].DefaultCellStyle.BackColor = Color.Green;
                    Application.DoEvents();
                }

            JobDone:
                SetMachineState(UserLoginState);
                return true;
            _VisionError:
                SetMachineState(UserLoginState);
                dgvProcessFlow.Rows[current_step].DefaultCellStyle.BackColor = Color.Red;
                LogMessage("Vision Inspection Error", MsgType.ERROR);
                //Unlock Top And Main Door 
                ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, false, true, 3000);
                ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, false, true, 3000);
                GlobalVar.isabort = true;
                return false;
            _IOError:
                SetMachineState(UserLoginState);
                dgvProcessFlow.Rows[current_step].DefaultCellStyle.BackColor = Color.Red;
                LogMessage("IO Error", MsgType.ERROR);
                //Unlock Top And Main Door 
                ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, false, true, 3000);
                ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, false, true, 3000);
                GlobalVar.isabort = true;
                return false;
            _MotionError:
                SetMachineState(UserLoginState);
                dgvProcessFlow.Rows[current_step].DefaultCellStyle.BackColor = Color.Red;
                LogMessage("Motion Error", MsgType.ERROR);
                //Unlock Top And Main Door 
                ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, false, true, 3000);
                ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, false, true, 3000);
                GlobalVar.isabort = true;
                return false;
            _SafetyError:
                SetMachineState(UserLoginState);
                dgvProcessFlow.Rows[current_step].DefaultCellStyle.BackColor = Color.Red;
                LogMessage("Safety Pre Check Failed", MsgType.ERROR);
                //Unlock Top And Main Door 
                ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, false, true, 3000);
                ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, false, true, 3000);
                GlobalVar.isabort = true;
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                LogMessage("System Error While Running Process", MsgType.ERROR);
                //dgvTray.Rows[Sleeve_row].Cells[Sleeve_col].Style.BackColor = Color.Red;
                SetMachineState(UserLoginState);
                GlobalVar.Result.TimeStamp = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
                GlobalVar.Result.Result = "Error";
                GlobalVar.isabort = true;
                AddResultLogText();
            }

            return true;

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //start auto align
            //AutoAlignerWorker.RunWorkerAsync();
            GlobalVar.isSystemHalt = false;
            GlobalVar.isAbortClicked = false;
            if (!GlobalVar.isHomeDone)
            {
                LogMessage("Please Home all axes", MsgType.ERROR);
            }
            else
            {
                activateFrmNkedOption();
                if (GlobalVar.system_var.AutoRun)
                {
                    //please check Do_AutoProcess for Current Flow 
                    GlobalVar.isAbort = false;
                    GlobalVar.isPause = false;
                    LogMessage("User Set Machine Online...", MsgType.INSTRUCTION);
                    SetMachineState(MachineState.Running);
                    Application.DoEvents();
                }
                else
                {
                    SetMachineState(MachineState.Running);

                    Do_AutoProcess();
                    Application.DoEvents();
                }
            }
        }

        private void onTowerLiteTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                this.BeginInvoke(new MethodInvoker(delegate
                {
                    if (isblinkEN)
                    {
                        if (isblinkon)
                        {
                            isblinkon = false;
                            GlobalVar.ref_btnTowerLite.BackColor = Color.Yellow;
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_34_TowerLight_Green, false);
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_35_TowerLight_Orange, true);
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_36_TowerLight_Red, false);
                        }
                        else
                        {
                            isblinkon = true;
                            GlobalVar.ref_btnTowerLite.BackColor = Color.White;
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_34_TowerLight_Green, false);
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_35_TowerLight_Orange, false);
                            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_36_TowerLight_Red, false);
                        }
                        Application.DoEvents();
                    }
                }));
            }
            catch { }
        }

        private void tsExit_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Do you want to Exit?", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Cancel)
                    return;

                if (gHiwinMotionInstOk)
                {
                    GlobalVar.g_Hiwin[(int)GalilAxis.X].Disconnect();
                    GlobalVar.g_Hiwin[(int)GalilAxis.Y].Disconnect();
                    GlobalVar.g_Hiwin[(int)GalilAxis.Z].Disconnect();

                }
                gHiwinMotionInstOk = false;
                if (gAdvMotionInstOk)
                {
                    GlobalVar.g_Advantech[0].Disconnect();
                }
                gAdvMotionInstOk = false;

                this.Close();
            }
            catch { this.Close(); }
        }

        private void tshiwinMotion_Click(object sender, EventArgs e)
        {
            frmHiwinParam frm = new frmHiwinParam();//ref m_Hiwinctrl, StaticVariable.sys_var.Hiwinprm);
            frm.Show();
        }

        private void tsSystemConfig_Click(object sender, EventArgs e)
        {
            SysConfig frm = new SysConfig();
            frm.ShowDialog();
        }

        private void ts_cbLed_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ts_cbLed.SelectedIndex >= 0)
            {
                //Vision.LedProForm frm = new Vision.LedProForm(ref GlobalVar.g_ledpros[ts_cbLed.SelectedIndex], 0);
                //frm.Show();
            }
        }

        private void btnVisonManager_Click(object sender, EventArgs e)
        {
            //if (GlobalVar.g_CameraTest == null || GlobalVar.g_CameraTest.IsDisposed)
            //    GlobalVar.g_CameraTest = new Vision.Camera.CameraRecipe.CameraRecipe();
            //GlobalVar.g_CameraTest.Show();
            //GlobalVar.g_CameraTest.BringToFront();

            if (GlobalVar.g_CameraStandard == null || GlobalVar.g_CameraStandard.IsDisposed)
                GlobalVar.g_CameraStandard = new V2Tech.AutomationApps.VisionCameraStandard();

            GlobalVar.g_CameraStandard.Show();
            GlobalVar.g_CameraStandard.BringToFront();

        }

        private void btnIOCtrl_Click(object sender, EventArgs e)
        {
            if (frmIOctrl == null || frmIOctrl.IsDisposed)
                frmIOctrl = new frmIOControl();
            frmIOctrl.Show();
            frmIOctrl.BringToFront();
        }

        private void btnManualCtrl_Click(object sender, EventArgs e)
        {
            //if (!GlobalVar.isHomeDone)
            //{
            //    MessageBox.Show("Please Make Sure you perform Homing First before running any motion sequence!", "Please Home First");
            //    return;
            //}
            //if (GlobalVar.g_CameraTest == null || GlobalVar.g_CameraTest.IsDisposed)
            //    GlobalVar.g_CameraTest = new Vision.Camera.CameraTestFrom.CameraTest();

            if (gMotionClass == null || gMotionClass.IsDisposed)
                gMotionClass = new frmMotionTeaching();

            gMotionClass.Show();
            gMotionClass.BringToFront();
        }

        private void btnSysHome_Click(object sender, EventArgs e)
        {
            try
            {
                SetMachineState(MachineState.Running);
                GlobalVar.control.optionSelected = 3;
                if (MessageBox.Show("Machine will Perform System Homing\nPress 'Yes' to continue and 'No' to cancel", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Information)
                    == System.Windows.Forms.DialogResult.Yes)
                {
                    if (System_Homing())
                    {
                        GlobalVar.isHomeDone = true;
                        System_Parking();
                        LogMessage("Ready to start Auto Process", MsgType.INSTRUCTION);
                    }
                    else
                    {
                        GlobalVar.isHomeDone = false;
                    }
                }

                SetMachineState(UserLoginState);
            }
            catch
            {
                SetMachineState(UserLoginState);
            }
        }

        private void tsadvanMotion_Click(object sender, EventArgs e)
        {
            frmAdvantechParam frm = new frmAdvantechParam();
            frm.ShowDialog();
            //frmAdvmotSet frm = new frmAdvmotSet(ref m_advCtrl, StaticVariable.sys_var.Advprm);
            //frm.Show();
        }

        private void tsCameraMenu_Click(object sender, EventArgs e)
        {
            Vision.Camera.CameraRecipe.CameraRecipe frm = new Vision.Camera.CameraRecipe.CameraRecipe();
            frm.Show();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            Common.Login.LoginForm login = new Common.Login.LoginForm();
            login.ShowDialog();

            if (GlobalVar.LoginSuccess)
            {
                lblOperatorID.Text = "User Name: " + GlobalVar.UserName;

                if (GlobalVar.UserLevel.ToUpper() == "ENGINEER")
                {
                    SetMachineState(MachineState.Engineer);
                    UserLoginState = MachineState.Engineer;
                }
                else if (GlobalVar.UserLevel.ToUpper() == "TECHNICIAN")
                {
                    SetMachineState(MachineState.Technician);
                    UserLoginState = MachineState.Technician;
                }
                else if (GlobalVar.UserLevel.ToUpper() == "OPERATOR")
                {
                    SetMachineState(MachineState.Operator);
                    UserLoginState = MachineState.Operator;
                }
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to logout?", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.OK)
                SetMachineState(MachineState.Logout);
        }

        private void tsHelp_Click(object sender, EventArgs e)
        {
            Help frmhelp = new Help();
            frmhelp.Show();
            frmhelp.Help_writeInformation(lblSoftwareVersion.Text);
        }

        private void tsSelectProduct_Click(object sender, EventArgs e)
        {
            SelectProduct frm = new SelectProduct();
            frm.ShowDialog();
            string tempCurrentProduct = frm.SelectedProduct;
            if (tempCurrentProduct != "")
            {
                SetAppConfig("CurrentProduct", tempCurrentProduct);
                GlobalVar.CurrentProduct = GetAppConfig("CurrentProduct");
                LoadRecipe(GlobalVar.CurrentProduct);
            }
        }

        private void btnMachineLog_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Path.GetDirectoryName(GlobalVar.Machinelog.GetFileName()), "explorer.exe");
        }

        private void btnBrowseResult_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Path.GetDirectoryName(GlobalVar.Resultlog.GetFileName()), "explorer.exe");
        }

        private void tsProductSettings_Click(object sender, EventArgs e)
        {
            if (frmPrdSetting == null || frmPrdSetting.IsDisposed)
                frmPrdSetting = new frmProductSetting();//ref m_Hiwinctrl);
            frmPrdSetting.ShowDialog();

            ResetLensDataGridView();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            GlobalVar.isabort = true;
            //IsUserAbort();
        }

        private void btnResetHolder_Click(object sender, EventArgs e)
        {
            Holder_count = 0;
        }

        private void btnResetTimer_Click(object sender, EventArgs e)
        {
            GlobalVar.system_var.EpoxyStartDate = DateTime.Now;
            LogMessage("User '" + GlobalVar.UserName + "' Reset Expoy Life", MsgType.WARNING);
            //Save Setting to xml
            Utilities.XML_Utilities.WriteToXmlFile<SystemVariable>(GlobalVar.SystemConfig_location, GlobalVar.system_var);
        }

        private void btnResetImage_Click(object sender, EventArgs e)
        {
            imageViewer.ZoomToFit = true;
        }

        private bool Home_X()
        {
            int Axisid = (int)exisID.X;
            GlobalVar.Galil_Main.SetSpeed(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.DriveFastV, Axisid);
            //GlobalVar.Galil_Main.MoveRel(5, Axisid, true);
            GlobalVar.Galil_Main.MotorOff(Axisid);
            GlobalVar.Galil_Main.AlmRst(MotionGalil.MotionGalil.TOutputStatus.Hi, Axisid);
            Sleep_ms(250);
            GlobalVar.Galil_Main.AlmRst(MotionGalil.MotionGalil.TOutputStatus.Lo, Axisid);
            GlobalVar.Galil_Main.MotorOn(Axisid);
            //if (!GlobalVar.Galil_Main.MtrAlm(Axisid)) goto _Error;
            if (!GlobalVar.Galil_Main.DisableSoftwareLimit(Axisid)) goto _Error;
            GlobalVar.Galil_Main.SetHardwareLimit(0, Axisid);

            #region Home
            Stopwatch timeout = new Stopwatch();
            timeout.Restart();
            if (!GlobalVar.Galil_Main.Jog_Home(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.FastV, false, Axisid)) goto _Error;
            while (true)
            {
                //negative limie
                if (GlobalVar.Galil_Main.SensLmtN(Axisid))
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    break;
                }

                ////positive limit
                //if (GlobalVar.Galil_Main.SensLmtP(Axisid))
                //{
                //    GlobalVar.Galil_Main.MotionStop(Axisid);
                //    break;
                //}

                if (timeout.Elapsed.TotalMilliseconds > GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.TimeOut * 1000)
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    timeout.Stop();
                    MessageBox.Show("Home X time out", "Time Out", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    goto _Error;
                }
            }
            while (true)
            {
                if (GlobalVar.Galil_Main.MotionComplete(Axisid)) break;
                Sleep_ms(2);
            }
            Sleep_ms(100);
            //fine search home
            GlobalVar.Galil_Main.MoveRel(2, Axisid, true);
            timeout.Restart();
            if (!GlobalVar.Galil_Main.Jog_Home(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.SlowV, false, Axisid)) goto _Error;
            while (true)
            {
                //negative limit
                if (GlobalVar.Galil_Main.SensLmtN(Axisid))
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    break;
                }

                ////positive limit
                //if (GlobalVar.Galil_Main.SensLmtP(Axisid))
                //{
                //    GlobalVar.Galil_Main.MotionStop(Axisid);
                //    break;
                //}

                if (timeout.Elapsed.TotalMilliseconds > 5000)
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    timeout.Stop();
                    MessageBox.Show("Home X time out", "Time Out", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    goto _Error;
                }

                //GlobalVar.Galil_Main.MoveAbs(50, (int)GalilAxis.X, true); if (IsUserAbort()) goto _Error; //nizammm
            }
            while (true)
            {
                if (GlobalVar.Galil_Main.MotionComplete(Axisid)) break;
                Sleep_ms(2);
            }
            Sleep_ms(100);
            #endregion

            #region Reset Counter
            GlobalVar.Galil_Main.SetLogicalPos(0, Axisid);
            GlobalVar.Galil_Main.SetRealPos(0, Axisid);

            #endregion

            GlobalVar.Galil_Main.SetSpeed(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.DriveMedV, Axisid);
            GlobalVar.Galil_Main.MoveAbs(5.0, Axisid, true);

            return true;
        _Error:
            return false;
        }

        private bool Home_Y()
        {
            int Axisid = (int)exisID.Y;
            GlobalVar.Galil_Main.SetSpeed(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.DriveSlowV, Axisid);
            GlobalVar.Galil_Main.MoveRel(2, Axisid, true);
            GlobalVar.Galil_Main.MotorOff(Axisid);
            GlobalVar.Galil_Main.AlmRst(MotionGalil.MotionGalil.TOutputStatus.Hi, Axisid);
            Sleep_ms(250);
            GlobalVar.Galil_Main.AlmRst(MotionGalil.MotionGalil.TOutputStatus.Lo, Axisid);
            GlobalVar.Galil_Main.MotorOn(Axisid);
            //if (!GlobalVar.Galil_Main.MtrAlm(Axisid)) goto _Error;
            if (!GlobalVar.Galil_Main.DisableSoftwareLimit(Axisid)) goto _Error;
            GlobalVar.Galil_Main.SetHardwareLimit(0, Axisid);

            #region Home
            Stopwatch timeout = new Stopwatch();
            timeout.Restart();
            if (!GlobalVar.Galil_Main.Jog_Home(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.SlowV, false, Axisid)) goto _Error;
            while (true)
            {
                //negative limit
                if (GlobalVar.Galil_Main.SensLmtN(Axisid))
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    break;
                }

                ////positive limit
                //if (GlobalVar.Galil_Main.SensLmtP(Axisid))
                //{
                //    GlobalVar.Galil_Main.MotionStop(Axisid);
                //    break;
                //}

                if (timeout.Elapsed.TotalMilliseconds > GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.TimeOut * 1000)
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    timeout.Stop();
                    MessageBox.Show("Home Y time out", "Time Out", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    goto _Error;
                }
            }
            while (true)
            {
                if (GlobalVar.Galil_Main.MotionComplete(Axisid)) break;
                Sleep_ms(2);
            }
            Sleep_ms(100);
            //fine search home
            GlobalVar.Galil_Main.MoveRel(2, Axisid, true);
            timeout.Restart();
            if (!GlobalVar.Galil_Main.Jog_Home(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.SlowV, false, Axisid)) goto _Error;
            while (true)
            {
                //negative limit
                if (GlobalVar.Galil_Main.SensLmtN(Axisid))
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    break;
                }

                ////positive limit
                //if (GlobalVar.Galil_Main.SensLmtP(Axisid))
                //{
                //    GlobalVar.Galil_Main.MotionStop(Axisid);
                //    break;
                //}

                if (timeout.Elapsed.TotalMilliseconds > 5000)
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    timeout.Stop();
                    MessageBox.Show("Home X time out", "Time Out", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    goto _Error;
                }
            }
            while (true)
            {
                if (GlobalVar.Galil_Main.MotionComplete(Axisid)) break;
                Sleep_ms(2);
            }
            Sleep_ms(100);
            #endregion

            #region Reset Counter
            GlobalVar.Galil_Main.SetLogicalPos(0, Axisid);
            GlobalVar.Galil_Main.SetRealPos(0, Axisid);

            #endregion

            GlobalVar.Galil_Main.SetSpeed(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.DriveMedV, Axisid);
            GlobalVar.Galil_Main.MoveAbs(2.0, Axisid, true);

            return true;
        _Error:
            return false;
        }

        private bool Home_Z()
        {
            int Axisid = (int)exisID.Z;
            GlobalVar.Galil_Main.SetSpeed(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.DriveSlowV, Axisid);
            GlobalVar.Galil_Main.MoveRel(-2, Axisid, true);
            //GlobalVar.Galil_Main.MotorOff(Axisid);
            GlobalVar.Galil_Main.AlmRst(MotionGalil.MotionGalil.TOutputStatus.Hi, Axisid);
            Sleep_ms(250);
            GlobalVar.Galil_Main.AlmRst(MotionGalil.MotionGalil.TOutputStatus.Lo, Axisid);
            GlobalVar.Galil_Main.MotorOn(Axisid);
            //if (!GlobalVar.Galil_Main.MtrAlm(Axisid)) goto _Error;
            if (!GlobalVar.Galil_Main.DisableSoftwareLimit(Axisid)) goto _Error;
            GlobalVar.Galil_Main.SetHardwareLimit(0, Axisid);

            #region Home
            Stopwatch timeout = new Stopwatch();
            timeout.Restart();
            if (!GlobalVar.Galil_Main.Jog_Home(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.SlowV, false, Axisid)) goto _Error;
            while (true)
            {
                //negative limit
                if (GlobalVar.Galil_Main.SensLmtN(Axisid))
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    break;
                }

                ////positive limit
                //if (GlobalVar.Galil_Main.SensLmtP(Axisid))
                //{
                //    GlobalVar.Galil_Main.MotionStop(Axisid);
                //    break;
                //}

                if (timeout.Elapsed.TotalMilliseconds > GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.TimeOut * 1000)
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    timeout.Stop();
                    MessageBox.Show("Home Y time out", "Time Out", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    goto _Error;
                }
            }
            while (true)
            {
                if (GlobalVar.Galil_Main.MotionComplete(Axisid)) break;
                Sleep_ms(2);
            }
            Sleep_ms(100);
            //fine search home
            GlobalVar.Galil_Main.MoveRel(2, Axisid, true);
            timeout.Restart();
            if (!GlobalVar.Galil_Main.Jog_Home(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.Home.SlowV, false, Axisid)) goto _Error;
            while (true)
            {
                //negative limit
                if (GlobalVar.Galil_Main.SensLmtN(Axisid))
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    break;
                }

                ////positive limit
                //if (GlobalVar.Galil_Main.SensLmtP(Axisid))
                //{
                //    GlobalVar.Galil_Main.MotionStop(Axisid);
                //    break;
                //}

                if (timeout.Elapsed.TotalMilliseconds > 5000)
                {
                    GlobalVar.Galil_Main.MotionStop(Axisid);
                    timeout.Stop();
                    MessageBox.Show("Home X time out", "Time Out", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    goto _Error;
                }
            }
            while (true)
            {
                if (GlobalVar.Galil_Main.MotionComplete(Axisid)) break;
                Sleep_ms(2);
            }
            Sleep_ms(100);
            #endregion

            #region Reset Counter
            GlobalVar.Galil_Main.SetSpeed(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.DriveFastV, Axisid);
            //GlobalVar.Galil_Main.MoveRel(-45, Axisid, true);
            GlobalVar.Galil_Main.SetLogicalPos(0, Axisid);
            GlobalVar.Galil_Main.SetRealPos(0, Axisid);

            #endregion

            GlobalVar.Galil_Main.SetSpeed(GlobalVar.Galil_Main.Axes[Axisid].AxisParam.DriveMedV, Axisid);
            //GlobalVar.Galil_Main.MoveAbs(-2.0, Axisid, true);

            return true;
        _Error:
            return false;
        }

        private void btnResetAlarm_Click(object sender, EventArgs e)
        {
            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_37_TowerLight_Buzz, false);
        }

        private void btnMovetoPark_Click(object sender, EventArgs e)
        {
            if (!GlobalVar.isHomeDone)
            {
                LogMessage("Please Home First", MsgType.WARNING);
                return;
            }
            gMotionClass.Gantry_GoToPos(GantryPos.locnames.locSafety);
            //Unlock Top And Main Door 
            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, false, true, 3000);
            GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, false, true, 3000);
        }

        private void btnFiberGrip_Click(object sender, EventArgs e)
        {
            if (GlobalVar.g_IOControl.IsOutputOn(IOControl.IO_Out.O_04_Cy5_FiberGrip))
                GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_04_Cy5_FiberGrip, false, true, 2000);
            else
                GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_04_Cy5_FiberGrip, true, true, 2000);
        }

        public void Image_Capture()
        {

            //GlobalVar.g_ledpros[0].LE_SetConstInt((byte)2, (byte)3);
            //GlobalVar.g_Cameras[1].GetImage(false);
            //GlobalVar.g_ledpros[0].LE_SetConstInt((byte)2, 0);

            //imageViewer.ResetPalette();
            //g_ledpros.LE_SetConstInt((byte)LedChannel, 0);//com2 channel1
            //g_ledpros.LE_SetStrobeInt((byte)LedChannel, 0);
            //g_ledpros.LE_SetStrobeWidth((byte)LedChannel, 0); //set none active chnl to 0
            //g_ledpros.LE_SetCHMode((byte)LedChannel, g_ledpros.CONSTANT);
            //g_ledpros.LE_SetConstInt((byte)LedChannel, (byte)Convert.ToInt16(textBox1.Text));
            //g_Cameras.GetImage(false);
            //Thread.Sleep(1000);
            //g_ledpros.LE_SetConstInt((byte)LedChannel, 0);
            //imageViewer.Attach(GlobalVar.SourceImage);



        }

        /*Check File Exist or Not*/
        public bool checkFolderName(string pathFolderName) {
            if (!System.IO.File.Exists(pathFolderName)){
                GlobalVar.isThisFileExist = false;
            }else{
                GlobalVar.isThisFileExist = true;
            }
            return GlobalVar.isThisFileExist;
        }
        /*Check File Exist or Not*/

        /*Save Image Starts Here*/
        public void save_image(string Unit, string LD)
        {
            var model = new window_model();
            string save_image_ExistedFile = model.save_image_ExistedFile;
            string jpg = model.exe;
            string underscore = model.underscore;
            string ld_unit = model.ld_unit;
            string slasher = model.slasher;
            string dateOnly = DateTime.Now.ToString("d");
            string dateFolderName = dateOnly.Replace("/", " ");
            string rootFolder = "D:\\ImageCOC";
            string unitNumber = "Unit";
            string SPACE = model.space;
            //string rootFolder = "D:\\Syamil'sLD";
            string pathFolderName = System.IO.Path.Combine(rootFolder, dateFolderName);
            checkFolderName(pathFolderName);
            if (!checkFolderName(pathFolderName)){
                System.IO.Directory.CreateDirectory(pathFolderName);
                //string a;
                //a = "" + DateTime.Now;
                //a = a.Replace("/", " ");
                //a = a.Replace(":", " ");
                string filename = rootFolder + slasher + dateFolderName + slasher + ld_unit + LD + SPACE + dateFolderName + SPACE + unitNumber + Unit + underscore + GlobalVar.PrcsRslt.Barcode_SN + jpg;
                imageViewer.Image.WriteJpegFile(filename);
            }
            else {
                System.IO.Directory.CreateDirectory(pathFolderName);
                //string a;
                //a = "" + DateTime.Now;
                //a = a.Replace("/", " ");
                //a = a.Replace(":", " ");
                string filename = rootFolder + slasher + dateFolderName + slasher + ld_unit + underscore + LD + SPACE + dateFolderName + SPACE + unitNumber + Unit + underscore + GlobalVar.PrcsRslt.Barcode_SN + jpg;
                imageViewer.Image.WriteJpegFile(filename);
                MessageBox.Show(save_image_ExistedFile);
            }
            //MessageBox.Show(dateOnly);
        }
        /*Save Image Ends Here*/

        private void btnStdVision_Click(object sender, EventArgs e)
        {
            if (GlobalVar.g_barcodeRead == null || GlobalVar.g_barcodeRead.IsDisposed)
                GlobalVar.g_barcodeRead = new V2Tech.AutomationApps.KeyenceBarcodeSR750();
            GlobalVar.g_barcodeRead.Show();
            GlobalVar.g_barcodeRead.BringToFront();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            // string barcode = GlobalVar.g_barcodeRead.getreading();
            // capture_Image();
            frmManualLD = new frmManualKeyIn();
            frmManualLD.Show();
            frmManualLD.BringToFront();
        }

        public void capture_Image()
        {
            GlobalVar.LedChannel = 2;
            if(Convert.ToInt16(textBox1.Text)>0){
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetStrobeInt((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetStrobeWidth((byte)GlobalVar.LedChannel, 0); //set none active chnl to 0
                GlobalVar.g_ledpros.LE_SetCHMode((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, (byte)Convert.ToInt16(textBox1.Text)); //custom brightness
                GlobalVar.g_Cameras[0].GetImage(false);
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, 0);

                imageViewer.Attach(GlobalVar.SourceImage);
                // string filename = "D:\\ImageCOC\\" + "LD_Unit" + 1 + "_" + 1 + ".png";

                imageViewer.Attach(GlobalVar.SourceImage);
                // string filename = "D:\\ImageCOC\\" + "LD_Unit" + 1 + "_" + 1 + ".png";            
            }else{
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetStrobeInt((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetStrobeWidth((byte)GlobalVar.LedChannel, 0); //set none active chnl to 0
                GlobalVar.g_ledpros.LE_SetCHMode((byte)GlobalVar.LedChannel, 0);
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, 2); //default brightness
                GlobalVar.g_Cameras[0].GetImage(false);
                GlobalVar.g_ledpros.LE_SetConstInt((byte)GlobalVar.LedChannel, 0);

                imageViewer.Attach(GlobalVar.SourceImage);
                // string filename = "D:\\ImageCOC\\" + "LD_Unit" + 1 + "_" + 1 + ".png";

                imageViewer.Attach(GlobalVar.SourceImage);
                // string filename = "D:\\ImageCOC\\" + "LD_Unit" + 1 + "_" + 1 + ".png";            
            }
        }
        /*OCR 4 Starts Here*/
        public bool OCR4(ref string text) {
            /*Limelight start here*/
            bool res = false;
            string output;
            char[] findMe = { '?' };
    
            VisionImage tempImage = new VisionImage();

            Algorithms.Copy(imageViewer.Image, tempImage);
            
            imageViewer.Palette.Type = Limelight_Num1.ProcessImage(imageViewer.Image);
            output = Limelight_Num1.vaOCRReport.ReadString + "\r\n";

            string[] values1 = output.Split('\r', '\n');
            string outputstring1 = "";

            
            foreach (string value1 in values1) {
                outputstring1 = outputstring1 + value1;
                GlobalVar.model.uniqueLDNum1 = outputstring1;
            }
            while (GlobalVar.model.uniqueLDNum1.Length >= 6 || GlobalVar.model.uniqueLDNum1.IndexOfAny(findMe)>=0) {
                int remQuestAt = GlobalVar.model.uniqueLDNum1.IndexOfAny(findMe);
                string falloutNV = GlobalVar.model.uniqueLDNum1.Remove(remQuestAt, 1);
                GlobalVar.model.uniqueLDNum1 = falloutNV;
            }

            if (GlobalVar.model.uniqueLDNum1.Length < 5 || GlobalVar.model.uniqueLDNum1.Length > 5) {
                LEDpos();
                if (GlobalVar.isSystemHalt == true) {
                    GlobalVar.isabortalignment = true;
                    IsUserAbort();
                }
                string stringResult = GlobalVar.lDCustomOverwrite;
                GlobalVar.PrcsRslt.Result = "FAIL";
                GlobalVar.PrcsRslt.LD_SN = stringResult;
                LogMessage("Serial Number =" + stringResult, MsgType.None);
            } else {
                string stringResult = GlobalVar.model.uniqueLDNum1;
                GlobalVar.PrcsRslt.Result = "PASS";
                GlobalVar.PrcsRslt.LD_SN = stringResult;
                LogMessage("Serial Number =" + stringResult, MsgType.None);
            }
            return res;
            /*Limelight start here*/        
        }
        /*OCR 4 Ends Here*/

        /*OCR 3 Starts Here*/
        public bool OCR3(ref string text)
        {
            bool res = false;
            string result;
            string searchMe = "?";
            int at = 0;
            int end = 6;
            string stringNum1, stringNum2, stringResult;
            //Sleep_ms(9999);
            VisionImage tempImage = new VisionImage();
            VisionImage tempImage2 = new VisionImage();

            Algorithms.Copy(GlobalVar.SourceImage, tempImage);
            Algorithms.Copy(GlobalVar.SourceImage, tempImage2);

            /*LD5 & LD6*/
            imageViewer.Palette.Type = Moonlight_char1.ProcessImage(tempImage);
            result = Moonlight_char1.vaOCRReport.ReadString + "\r\n";


            string[] values1 = result.Split('\r', '\n');
            string outputstring1 = "";

            foreach (string value1 in values1)  {
                outputstring1 = outputstring1 + value1;
            }
            stringNum1 = outputstring1;

            imageViewer.Palette.Type = Moonlight_char2.ProcessImage(tempImage2);
            result = Moonlight_char2.vaOCRReport.ReadString + "\r\n";
            string[] values2 = result.Split('\r', '\n');
            string outputstring2 = "";

            foreach (string value2 in values2) {
                outputstring2 = outputstring2 + value2;
            }

            stringNum2 = outputstring2;

            stringResult = stringNum1 + stringNum2;

            if (stringResult.Length < 6 || stringResult.Length > 6 || stringResult.IndexOf(searchMe, at, end) >= 0 || stringResult.Length > 6) {
                LEDpos();
                if (GlobalVar.isSystemHalt == true) {
                    GlobalVar.isabortalignment = true;
                    IsUserAbort();
                }
                stringResult = GlobalVar.lDCustomOverwrite;
                GlobalVar.PrcsRslt.Result = "FAIL";
                LogMessage("Serial Number =" + stringResult, MsgType.None);
            } else {
                GlobalVar.PrcsRslt.Result = "PASS";
                LogMessage("Serial Number =" + stringResult, MsgType.None);
            }

            GlobalVar.PrcsRslt.LD_SN = stringResult;
            return res;
        }
        /*OCR 3 Ends Here*/

        private void txtbcd4_TextChanged(object sender, EventArgs e)
        {

        }

        private void toolstrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void clearData()
        {

        }

        public static bool UpdateData()
        {
            string sMsg = "";

            GlobalVar.NonDCData.IndexID = "";
            GlobalVar.NonDCData.ContainerName = "";
            GlobalVar.NonDCData.WaferPosition = GlobalVar.PrcsRslt.Unit.ToString();
            GlobalVar.NonDCData.ComponentPN = "";
            GlobalVar.NonDCData.WaferID = "";
            GlobalVar.NonDCData.UpdatedBy = "";
            GlobalVar.NonDCData.TesterName = "AT-DEVMAP-01";
            GlobalVar.NonDCData.ID = "";
            GlobalVar.NonDCData.Component = "";

            bool status = GlobalVar.mes.WriteNonDCData(GlobalVar.NonDCData, ref sMsg);

            return status;

        }

        /*manualData starts here*/
        public void manualData()
        {
            GlobalVar.PrcsRslt.Barcode_SN = "";
            //GlobalVar.PrcsRslt.Child_ID = "";
            GlobalVar.isThiswdOpened = true;
            if (GlobalVar.isThiswdOpened == true)
            {
                openDoorPlease();
            }

            frmManualLD = new frmManualKeyIn();
            frmManualLD.ShowDialog();
            this.BringToFront();
            frmManualLD.BringToFront();

            if (GlobalVar.isThiswdClosed == true)
            {
                closeDoorPlease();
            }
        }
        /*manualData ends here*/

        /*openDoorPlease starts here*/
        public static void openDoorPlease(){
            bool ret_IO = false;

            //Unlock Top And Main Door 
            ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, false, true, 3000);
            ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, false, false, 3000); 
        }
        /*openDoorPlease ends here*/

        /*closeDoorPlease starts here*/
        private static void closeDoorPlease() {
            bool ret_IO = false;

            //lock Top And Main Door
            ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_05_Cy6_DoorLockBottom, true, false, 2000);
            ret_IO = GlobalVar.g_IOControl.OnOff_IO(IOControl.IO_Out.O_06_Cy7_DorLockTop, true, true, 3000);
        }
        /*closeDoorPlease ends here*/

        /*unlock_door starts here*/
        private void unlock_door_Click(object sender, EventArgs e)
        {
            /*Open Door Starts Here*/
            openDoorPlease();
            /*Open Door Ends Here*/
        }
        /*unlock_door ends here*/

        /*lock_door starts here*/
        private void lock_door_Click(object sender, EventArgs e)
        {
            /*Close Door Starts Here*/
            closeDoorPlease();
            /*Close Door Ends Here*/
        }

        private void turnOnHistory_btn_Click(object sender, EventArgs e) {
            turnOnHistory();
        }
        /*lock_door ends here*/
    }
}
