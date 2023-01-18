using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
//using FaceDetection;
using System.Threading;
using System.Management;
using System.IO;
using System.Speech.Synthesis;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using DirectShowLib;
using OpenCvSharp;
using System.Net;
using Newtonsoft.Json;
using OpenCvSharp.Extensions;
using System.Runtime.InteropServices;
using DlibDotNet;
using System.Reflection;
using CenterFaceDotNet;

namespace Dlib_Sample_Project
{

    [StructLayout(LayoutKind.Sequential)]
    struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }
    public partial class FaceANDEye : Form
    {
        [DllImport("user32.dll")]
        static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        static bool IsDLL_Issue_Occured = false;

        public static string AppRootPath1 = AppDomain.CurrentDomain.BaseDirectory;
        public static string AppRootPath = AppDomain.CurrentDomain.BaseDirectory + "\\Faces\\";

        SpeechSynthesizer reader;
        bool soundflag = false;

        //  string[] GS_set =  { "Tilt_Right","Tilt_Left",  "Turn_Left", "Turn_Right","SMILE", "EYE_BLINK","LEFT_RIGHT", "LOOK_STRAIGHT" };// new string[4];



        //string[] GS_str1 = { "Tilt_Right", "Tilt_Left", "Turn_Left", "Turn_Right", "SMILE", "EYE_BLINK"};// new string[4];
        string[] GS_str1 = ConfigurationManager.AppSettings["GS_str1"].Split(',');
        string[] GS_str;
        int pgcount = 6;

        bool imagesaved = false;


        public bool facedetected = false;
        public bool eyesDetected = false;
        public bool noseDetected = false;



        public bool openeyesDetected = false;
        public bool closeeyeDetected = false;

        public bool captureSecondPhoto = false;


        private VideoCapture cap;
        private CascadeClassifier haarface;
        private CascadeClassifier haareye;
        private CascadeClassifier haarglasses;
        private CascadeClassifier haareyesplit;

        private CascadeClassifier haarmouth;
        private CascadeClassifier haarnose;
        private CascadeClassifier haarrightear;
        private CascadeClassifier haarleftear;

        private CascadeClassifier haareyepair_small;

        private CascadeClassifier haarrighteyes;
        private CascadeClassifier haarlefteyes;

        Mat imgOrg = new Mat();


        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        //private static FaceRecognition fr;
        private static Image RegisteredStaffImage;
        private static string RegisteredStaffName = "";
        //private static IEnumerable<FaceEncoding>[] RegisteredStaffEncoding = null;
        private static CenterFaceParameter param = null;

        public static List<Matrix<RgbPixel>> RegisteredAllignedImageLIST = new List<Matrix<RgbPixel>>();
        public static string Dlib_Sample_Project_Version = "1.0.8.16";

        public FaceANDEye()
        {
            InitializeComponent();

            if (!Directory.Exists(AppRootPath))
                Directory.CreateDirectory(AppRootPath);

            pgcount = 4;

            //GS_str = new string[pgcount];

            //for (int t = 0; t < pgcount; t++)
            //{
            //    GS_str[t] = "P";
            //}

            //// Initialize param

            var binPath = AppRootPath1 + "centerface.bin";//"ncnn /centerface.bin";// args[0];
            var paramPath = AppRootPath1 + "centerface.param";//"ncnn /centerface.param";// args[1];

            param = new CenterFaceParameter
            {
                BinFilePath = binPath,
                ParamFilePath = paramPath
            };

        }


        private void FaceANDEye_Load(object sender, EventArgs e)
        {
            this.button2.Enabled = false;

            loger.WriteLog("sts", "In FaceANDEye_Load ");                     

            string startuppath = Application.StartupPath;
            loger.WriteLog("sts", "startuppath - " + startuppath);

            // string deviceconnected = GetUSBDevices();

            ///// CAMERA LIST///

            Device_Connect_Disconnect_Thread();

            comboBox1.Items.Clear();
            string cameras = "";
            //List<string> Camera_Selection = new List<string>();
            DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            

            for (int i = 0; i < _SystemCamereas.Length; i++)
            {
                comboBox1.Items.Add(_SystemCamereas[i].Name);

                cameras += i + " " + _SystemCamereas[i].Name + Environment.NewLine;


            }
            if (comboBox1.Items.Count != 0)
            {
                comboBox1.SelectedIndex = 0;
            }


            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "camlst.cnf", cameras);



            //cap = new VideoCapture(0);

            string facefile = startuppath + @"\xmls\haarcascade_frontalface_default.xml";//haarcascade_profileface.xml";//haarcascade_frontalface_default.xml";//LBP_ProfileFace.xml";//
            haarface = new CascadeClassifier(facefile);

            string eyefile = startuppath + @"\xmls\haarcascade_eye.xml";
            haareye = new CascadeClassifier(eyefile);

            string nose = startuppath + @"\xmls\nose.xml";//haarcascade_mcs_mouth.xml";//haarcascade_smile
            haarnose = new CascadeClassifier(nose);
        }

        public void Device_Connect_Disconnect_Thread()
        {
            try
            {
                loger.WriteLog("sts", "Device_Connect_Disconnect invoked");

                WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
                ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
                insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
                insertWatcher.Start();

                // insertWatcher.WaitForNextEvent();

                WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
                ManagementEventWatcher removeWatcher = new ManagementEventWatcher(removeQuery);
                removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
                removeWatcher.Start();
            }
            catch (Exception ex)
            {
                loger.WriteLog("err", "Device_Connect_Disconnect_Thread() - " + ex.ToString());
            }
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                comboBox1.Invoke((MethodInvoker)(() => comboBox1.Items.Clear()));
                //comboBox1.Items.Clear();
                //List<string> Camera_Selection = new List<string>();
                DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

                for (int i = 0; i < _SystemCamereas.Length; i++)
                {
                    comboBox1.Invoke((MethodInvoker)(() => comboBox1.Items.Add(_SystemCamereas[i].Name)));
                    //comboBox1.Items.Add(_SystemCamereas[i].Name);
                    //Camera_Selection.Add(_SystemCamereas[i].Name);
                }
                if (comboBox1.Items.Count != 0)
                {
                    comboBox1.Invoke((MethodInvoker)(() => comboBox1.SelectedIndex = 0));
                }
            }
            catch (Exception ex)
            {
                loger.WriteLog("err", "DeviceInsertedEvent() - " + ex.ToString());
            }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            try
            {
                comboBox1.Invoke((MethodInvoker)(() => comboBox1.Items.Clear()));
                //comboBox1.Items.Clear();
                //List<string> Camera_Selection = new List<string>();
                DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

                for (int i = 0; i < _SystemCamereas.Length; i++)
                {
                    comboBox1.Invoke((MethodInvoker)(() => comboBox1.Items.Add(_SystemCamereas[i].Name)));
                    //comboBox1.Items.Add(_SystemCamereas[i].Name);
                    //Camera_Selection.Add(_SystemCamereas[i].Name);
                }
                //comboBox1.DataSource = Camera_Selection;
            }
            catch (Exception ex)
            {
                loger.WriteLog("err", "DeviceRemovedEvent() - " + ex.ToString());
            }
        }


        DateTime time1 = DateTime.Now;

        int i = 0, j = 0;
        static Mat CroppedfinalFrame = new Mat();
        private void timer1_Tick(object sender, EventArgs e)
        {
            //pppppppppppppppppp            
            try
            {

                facedetected = false;
                eyesDetected = false;
                noseDetected = false;


                if (imagesaved == false)
                {

                    if (cap.IsOpened() == false)
                    {
                        int camIndex = comboBox1.SelectedIndex;
                        //cap = new VideoCapture(0);
                        cap = new VideoCapture(camIndex);
                    }

                    if (cap.IsOpened() == true)
                    {

                        label3_status.Visible = false;
                        var nextFrame1 = cap.RetrieveMat().Flip(FlipMode.Y);
                        using (var nextFrame = nextFrame1)// cap.QuerySmallFrame().ToImage<Bgr, Byte>().Flip(FlipType.Horizontal))
                        {
                            if (nextFrame != null)
                            {
                                var grayImage = new Mat();
                                Cv2.CvtColor(nextFrame, grayImage, ColorConversionCodes.BGRA2GRAY);
                                Cv2.EqualizeHist(grayImage, grayImage);

                                var faces = haarface.DetectMultiScale(
                                                image: grayImage,
                                                scaleFactor: 1.1,
                                                minNeighbors: 2,
                                                minSize: new OpenCvSharp.Size(100, 100)
                                                );//DetectMultiScale(grayImage, 1.1, 10, new OpenCvSharp.Size(0, 0), new OpenCvSharp.Size(0, 0));



                                if (faces.Count() > 0)  //face detected
                                {

                                    foreach (Rect face in faces)
                                    {
                                        Cv2.Rectangle(nextFrame, new OpenCvSharp.Point(face.X, face.Y), new OpenCvSharp.Point(face.X + face.Width, face.Y + face.Height), Scalar.LightGreen, 2);

                                        //// Now Crop the face for detecting 
                                        Rect rectcrop = new Rect(face.X, face.Y, face.Width, face.Height);
                                        CroppedfinalFrame = new Mat(nextFrame, rectcrop);

                                        //var grayImageNew = new Mat();
                                        //Cv2.CvtColor(CroppedfinalFrame, grayImageNew, ColorConversionCodes.BGRA2GRAY);
                                        //Cv2.EqualizeHist(grayImage, grayImage);
                                    }

                                    var eyes = haareye.DetectMultiScale(
                                                image: CroppedfinalFrame,
                                                scaleFactor: 1.1,
                                                minNeighbors: 2,
                                                minSize: new OpenCvSharp.Size(30, 30),
                                                maxSize: new OpenCvSharp.Size(40, 40)
                                                );
                                    var nose = haarnose.DetectMultiScale(
                                                image: CroppedfinalFrame,
                                                scaleFactor: 1.1,
                                                minNeighbors: 2,
                                                minSize: new OpenCvSharp.Size(30, 30),
                                                maxSize: new OpenCvSharp.Size(50, 50)
                                                );

                                    foreach (Rect eye in eyes)
                                    {
                                        Cv2.Rectangle(CroppedfinalFrame, new OpenCvSharp.Point(eye.X, eye.Y), new OpenCvSharp.Point(eye.X + eye.Width, eye.Y + eye.Height), Scalar.Gray, 2);
                                    }

                                    if (eyes.Count() > 1)
                                    {
                                        eyesDetected = true;
                                    }

                                    foreach (Rect noseObj in nose)
                                    {
                                        Cv2.Rectangle(CroppedfinalFrame, new OpenCvSharp.Point(noseObj.X, noseObj.Y), new OpenCvSharp.Point(noseObj.X + noseObj.Width, noseObj.Y + noseObj.Height), Scalar.Gray, 2);
                                    }
                                    if (nose.Count() > 0)
                                    {
                                        noseDetected = true;
                                    }

                                    facedetected = true;

                                    if (facedetected && eyesDetected && noseDetected)
                                    {

                                        if (pictureBox2.Image == null)
                                        {
                                            var diffInSeconds = Math.Round((DateTime.Now - time1).TotalSeconds, 1);

                                            var finalFrame = cap.RetrieveMat();
                                            soundflag = true;
                                            if (File.Exists(AppRootPath1 + "\\Faces\\samplePhoto1.Jpeg"))
                                            {
                                                File.Delete(AppRootPath1 + "\\Faces\\samplePhoto1.Jpeg");
                                            }
                                            finalFrame.SaveImage(AppRootPath1 + "\\Faces\\samplePhoto1.Jpeg");
                                            pictureBox2.Image = Image.FromFile(AppRootPath1 + "\\Faces\\samplePhoto1.Jpeg");

                                        }
                                        else if (pictureBox3.Image == null && captureSecondPhoto)
                                        {
                                            var diffInSeconds = Math.Round((DateTime.Now - time1).TotalSeconds, 1);

                                            var finalFrame = cap.RetrieveMat();
                                            soundflag = true;
                                            if (File.Exists(AppRootPath1 + "\\Faces\\samplePhoto2.Jpeg"))
                                            {
                                                File.Delete(AppRootPath1 + "\\Faces\\samplePhoto2.Jpeg");
                                            }
                                            finalFrame.SaveImage(AppRootPath1 + "\\Faces\\samplePhoto2.Jpeg");
                                            pictureBox3.Image = Image.FromFile(AppRootPath1 + "\\Faces\\samplePhoto2.Jpeg");

                                            System.Windows.Forms.Application.DoEvents();

                                        }

                                        if (pictureBox3.Image != null && pictureBox2.Image != null)
                                        {
                                            this.timer1.Dispose();
                                            this.button2.Enabled = true;
                                            this.button4.Enabled = true;
                                        }
                                    }
                                }
                                pictureBox1.Image = nextFrame.ToBitmap();
                            }
                        }

                    }
                    else
                    {
                        label3_status.Visible = true;
                        // label3_status.BeginInvoke(new Action(() => label3_status.Text = "Camera Not Detected...!"));
                        label3_status.Text = "Camera Not Detected...!";
                    }
                }
            }
            catch (Exception ex)
            {
                this.button2.Enabled = true;
                // MessageBox.Show(ex.ToString()+ "gss"+gss.ToString());
            }

        }


        private void button2_Click(object sender, EventArgs e)
        {
            //Application.Restart();
            i = 0; j = 0;
            this.timer1.Dispose();
            //this.timer1.Stop();
            this.cap.Release();
            this.cap.Dispose();
            this.Controls.Clear();
            this.InitializeComponent();
            this.FaceANDEye_Load(sender, e);
            this.Refresh();
            captureSecondPhoto = false;
        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            if (button1.Text == "Start Capture")
            {
                int camIndex = comboBox1.SelectedIndex;
                //now save this camera index in file to use in exe
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "camindx.cnf", camIndex.ToString());

                cap = new VideoCapture(camIndex);
                reader = new SpeechSynthesizer();
                reader.Rate = (int)-2;
                reader.SpeakAsync("Please Be Ready, and Look inside Camera");
                this.timer1.Enabled = true;
                this.timer1.Start();
                this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
                pictureBox2.Image = null;
                button1.Enabled = false;
                this.button4.Enabled = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                loger.WriteLog("sts", "Refresh Button");
                comboBox1.Items.Clear();
                //List<string> Camera_Selection = new List<string>();
                DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);



                for (int i = 0; i < _SystemCamereas.Length; i++)
                {
                    comboBox1.Items.Add(_SystemCamereas[i].Name);
                }
                if (comboBox1.Items.Count != 0)
                {
                    comboBox1.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                loger.WriteLog("err", "Error in Refresh camera - " + ex.ToString());
            }
        }

        private static string GetMACAddress()
        {
            try
            {
                loger.WriteLog("sts", "Get_Mac");
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                String sMacAddress = string.Empty;
                foreach (NetworkInterface adapter in nics)
                {

                    // if (sMacAddress == String.Empty)// only return MAC Address from first card  
                    if (adapter.Name == "Ethernet")
                    {
                        IPInterfaceProperties properties = adapter.GetIPProperties();
                        sMacAddress = adapter.GetPhysicalAddress().ToString();
                    }
                    else if (adapter.Name.Contains("Ethernet"))
                    {
                        IPInterfaceProperties properties = adapter.GetIPProperties();
                        sMacAddress = adapter.GetPhysicalAddress().ToString();
                        // Console.WriteLine("MacAddresss - " + sMacAddress);
                    }
                }
                loger.WriteLog("sts", "MacAddresss - " + sMacAddress);

                return sMacAddress;
            }
            catch (Exception ex)
            {
                loger.WriteLog("err", "GetMACAddress()" + ex.ToString());
                return null;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            captureSecondPhoto = true;
            button3.Enabled = false;
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            if (pictureBox2.Image != null && pictureBox3 != null)
            {
                GetImageFromFolderAnd_AuthenticateUser();
            }
        }

        public static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static Matrix<RgbPixel> AllignImage(string imagePath)
        {
            try
            {
                Matrix<RgbPixel> faceChip = null;
                // The first thing we are going to do is load all our models. First, since we need to
                // find faces in the image we will need a face detector:
                using (var detector = Dlib.GetFrontalFaceDetector())
                // We will also use a face landmarking model to align faces to a standard pose: (see face_landmark_detection_ex.cpp for an introduction)
                using (var sp = ShapePredictor.Deserialize(AppRootPath1 + "\\shape_predictor_5_face_landmarks.dat"))
                // And finally we load the DNN responsible for face recognition.
                using (var net = DlibDotNet.Dnn.LossMetric.Deserialize(AppRootPath1 + "\\dlib_face_recognition_resnet_model_v1.dat"))
                {
                    var img = Dlib.LoadImageAsMatrix<RgbPixel>(imagePath);

                    foreach (var face in detector.Operator(img))
                    {
                        var shape = sp.Detect(img, face);
                        var faceChipDetail = Dlib.GetFaceChipDetails(shape, 150, 0.25);
                        faceChip = Dlib.ExtractImageChip<RgbPixel>(img, faceChipDetail);
                    }
                }
                return faceChip;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("DLL"))
                {
                    IsDLL_Issue_Occured = true;
                }

                loger.WriteLog("err", "Error in AllignImage - " + ex.ToString());
                loger.WriteLog("err", ex.Message);
                loger.WriteLog("sts", "IsDLL_Issue_Occured = " + IsDLL_Issue_Occured);
                loger.WriteLog("sts", "##############################");
                return null;
            }
        }
        public void GetImageFromFolderAnd_AuthenticateUser()
        {
            string filename = "";
            try
            {
                const int ENUM_CURRENT_SETTINGS = -1;

                DEVMODE devMode = default;
                devMode.dmSize = (short)Marshal.SizeOf(devMode);
                EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode);

                loger.WriteLog("sts", " in GetImageFromFolderAnd_AuthenticateUser()");
                               
                try
                {
                    filename = AppDomain.CurrentDomain.BaseDirectory + "Faces\\samplePhoto1.jpeg";
                    var image = OpenCvSharp.Cv2.ImRead(AppDomain.CurrentDomain.BaseDirectory + "\\white.PNG");
                    string filenamewithoutExt = Path.GetFileNameWithoutExtension(filename);
                    string txtFileName = AppDomain.CurrentDomain.BaseDirectory + "\\Faces\\" + filenamewithoutExt + ".txt";
                    double photo_matching_threshold = Convert.ToDouble(0.62);

                    var Live_image = OpenCvSharp.Cv2.ImRead(filename);

                    var blurriness = VarianceOfLaplacian(Live_image);
                    string Blurriness = blurriness.ToString();

                    ///// Check if image file is already processed or not
                    if (!File.Exists(txtFileName))
                    {
                        //// Now check if DLL not found issue occured... if Yes then skip authentication else do authentication
                        if (IsDLL_Issue_Occured == true)
                        {
                        }
                        else
                        {
                            int facecount = 0;
                            float FaceScore = 0;
                            double match__result = -1;
                            Calculate_Face_Score_And_Count(param, filename, out facecount, out FaceScore);


                            //if (facecount == 1)
                            //{
                                //Stopwatch sw1 = new Stopwatch();
                                //sw1.Start();

                                Matrix<RgbPixel> ImageToMatchFromFolder = AllignImage(filename);
                                Matrix<RgbPixel> RegisterAllignedImage12 = AllignImage(AppDomain.CurrentDomain.BaseDirectory + "\\Faces\\samplePhoto2.Jpeg");


                                //foreach (var RegisterAllignedImage12 in RegisteredAllignedImageLIST)
                                //{
                                    match__result = MatchImage(RegisterAllignedImage12, ImageToMatchFromFolder);

                            //if (match__result < photo_matching_threshold)
                            //{
                            //    break;
                            //}
                            //}

                            //sw1.Stop();
                            //var elapsedTime = sw1.ElapsedMilliseconds;
                            lblScore.Text = "Matching Score : " + match__result.ToString("0.00") + " / " + photo_matching_threshold.ToString() ; 
                            if (match__result == 0)
                            {
                                LBLStatus.Text = "Please Sit Properly";
                                LBLStatus.ForeColor = Color.Yellow;
                                LBLStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                LBLStatus.Visible = true;


                                loger.WriteLog("alert", "#### #### ERROR IN MATCH ####");
                            }
                            else if (match__result < photo_matching_threshold)
                            {
                            //// Match Found
                            //// Do nothing
                                LBLStatus.Text = "Face Matched Successfully.";
                                LBLStatus.ForeColor = Color.Green;
                                LBLStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                LBLStatus.Visible = true;
                                loger.WriteLog("alert", "#### #### MATCH FOUND ####");
                                        
                            }
                            else if (match__result > photo_matching_threshold)
                            {
                                LBLStatus.Text = "Unauthorized User";
                                LBLStatus.ForeColor = Color.Red;
                                LBLStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                LBLStatus.Visible = true;
                                loger.WriteLog("alert", "#### #### UNAUTHORISED USER FOUND ####");
                                // loger.WriteLog("alert_n", "#### #### UNAUTHORISED USER FOUND ####");
                            }
                            //}
                            //else
                            //{
                            //    loger.WriteLog("alert", "facecount - " + facecount + " ####  facescore - " + FaceScore);
                            //}
                        }
                    }
                }
                catch (Exception ex)
                {
                    string textfilename = Path.GetFileNameWithoutExtension(filename) + ".txt";
                    loger.WriteLog("err", "Error in GetImageFromFolderAnd_AuthenticateUser() - " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                loger.WriteLog("err", "Error in GetImageFromFolderAnd_AuthenticateUser() - " + ex.Message);
            }
        }

        static double VarianceOfLaplacian(Mat image)
        {
            using (var laplacian = new Mat())
            {
                Cv2.Laplacian(image, laplacian, MatType.CV_64FC1);
                Cv2.MeanStdDev(laplacian, out var mean, out var stddev);
                return stddev.Val0 * stddev.Val0;
            }
        }

        private static void Calculate_Face_Score_And_Count(CenterFaceParameter param, string inputFileName, out int FaceCount, out float FaceScore)
        {
            loger.WriteLog("sts", "Calculate_Face_Score_And_Count () ...");
            FaceCount = 0;
            FaceScore = 0;
            try
            {
                float score = 0;

                using (var centerFace = CenterFace.Create(param))
                {
                    var image = OpenCvSharp.Cv2.ImRead(inputFileName);

                    if (image.Height != 0 && image.Width != 0)
                    {
                        //////////// Code to get face score
                        var inMat = NcnnDotNet.Mat.FromPixels(image.Data, NcnnDotNet.PixelType.Bgr2Rgb, image.Cols, image.Rows);

                        var faceInfos = centerFace.Detect(inMat, image.Cols, image.Rows).ToArray();

                        for (var i = 0; i < faceInfos.Length; i++)
                        {
                            var face = faceInfos[i];
                            var pt1 = new OpenCvSharp.Point(face.X1, face.Y1);
                            var pt2 = new OpenCvSharp.Point(face.X2, face.Y2);
                            score = face.Score;
                            Console.WriteLine("SCore: " + score);
                            OpenCvSharp.Cv2.Rectangle(image, pt1, pt2, new OpenCvSharp.Scalar(0, 255, 0), 2);
                            for (var j = 0; j < 5; j++)
                            {
                                var center = new OpenCvSharp.Point(face.Landmarks[2 * j], face.Landmarks[2 * j + 1]);
                                OpenCvSharp.Cv2.Circle(image, center, 2, new OpenCvSharp.Scalar(255, 255, 0), 2);
                            }
                            //CSV Logging for each face detected                            
                            //////var faceCount = faceInfos.Length;
                            //////var faceSCore = score;                           
                            ///
                            FaceCount = faceInfos.Length;
                            FaceScore = score;

                        }
                    }
                }
                loger.WriteLog("sts", "Completed - Calculate_Face_Score_And_Count () ...");


            }
            catch (Exception ex)
            {
                loger.WriteLog("err", "Error in Calculate_Face_Score_And_Count() - " + ex.ToString());
                //loger.WriteLog("err", "Error in Calculate_Face_Score_And_Count() - " +ex.Message);
            }
        }

        public static double MatchImage(Matrix<RgbPixel> RegisteredImage, Matrix<RgbPixel> ImageToVerify)
        {
            double MatchResult = 0;
            try
            {
                using (var net = DlibDotNet.Dnn.LossMetric.Deserialize("C:\\WFH_Client\\dlib_face_recognition_resnet_model_v1.dat"))
                {
                    var faceDescriptor1 = net.Operator(RegisteredImage);
                    var faceDescriptor2 = net.Operator(ImageToVerify);

                    for (var j = 0; j < faceDescriptor1.Count; ++j)
                    {
                        // Faces are connected in the graph if they are close enough. Here we check if
                        // the distance between two face descriptors is less than 0.6, which is the
                        // decision threshold the network was trained to use. Although you can
                        // certainly use any other threshold you find useful.

                        var diff1 = faceDescriptor1[j] - faceDescriptor2[j];
                        MatchResult = Dlib.Length(diff1);
                        ////if (Dlib.Length(diff1) < 0.6)
                        ////{
                        ////    Console.WriteLine("<0.6");
                        ////    Console.WriteLine(str);
                        ////}
                        ////else
                        ////    Console.WriteLine(str);
                    }
                }

                return MatchResult;

            }
            catch (Exception ex)
            {
                loger.WriteLog("err", "Error in MatchImage() - " + ex.Message);

                loger.WriteLog("err", "Error in MatchImage() - " + ex.ToString());
                return MatchResult;
            }
        }
    }
}