using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
namespace Get_ColorFrame
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window,INotifyPropertyChanged
    {
       /// <summary>
       /// Kinect传感器类
       /// </summary>
        private KinectSensor Mykinectsensor=null;   
        /// <summary>
        /// 彩色图像帧读取框架类
        /// </summary>
        private ColorFrameReader MyColorFrameReader = null;

        /// <summary>
        /// Bitmap to display
        /// 更新和呈现位图
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        /// <summary>
        /// 当前状态展示
        /// </summary>
        private string statusText = null;

        public MainWindow()
        {
            //传感器实例化
            this.Mykinectsensor = KinectSensor.GetDefault();
            //打开Kinect传感器
            this.Mykinectsensor.Open();
            //打开对应彩色图像数据源的框架
            this.MyColorFrameReader = Mykinectsensor.ColorFrameSource.OpenReader();
            //线程处理来自图像帧的框架
            this.MyColorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;  
            //创建图像帧描述
            FrameDescription colorFrameDescription = this.Mykinectsensor.ColorFrameSource.CreateFrameDescription((ColorImageFormat.Bgra)); //彩色图像有多种格式选择
            //创建图像显示位图
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width,colorFrameDescription.Height,96.0,96.0,PixelFormats.Bgr32,null);
            //***** set IsAvailableChanged event notifier
            this.Mykinectsensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
 
            //打开kinect
            this.Mykinectsensor.Open();
            //set the status text
            this.StatusText += this.Mykinectsensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
            //use the window object as the view model in this simple example
            this.DataContext = this;
            //初始化界面
            this.InitializeComponent();
        }

        ///<summary>
        ///INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        ///</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        ///<summary>
        ///Gets the bitmap to display
        ///</summary>
        public ImageSource ImageSource
        {
            get 
            {
                return this.colorBitmap;
            }
        }

        ///<summary>
        ///Gets or sets the current status text to display
        ///</summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }
            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;
                    //notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// 
        ///<summary>
        ///关闭窗口事件(关闭窗口时，对应的Kinect传感器也关闭)
        ///</summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Main_Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.MyColorFrameReader != null)
            {
                this.MyColorFrameReader.Dispose();
                this.MyColorFrameReader = null;
            }
            if (this.Mykinectsensor != null)
            {
                this.Mykinectsensor.Close();    
                this.Mykinectsensor = null;
            }    
        }
        /// <summary>
        /// 图像帧数据
        /// Handles the color frame data arriving from this sensor
        /// </summmary>
        /// <param name="sender">object sending the event</parm>
        /// <param name="e">event argmnets</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())  //发送帧事件参数
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();
                        //verfy data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra
                                );     //将图像帧数据转化为整型数据
                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }
                        this.colorBitmap.Unlock();
                    }
                }
            }
        }
        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.Mykinectsensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.colorBitmap != null)
            {
                //create a png bitmap encoder which knows how to save a .png file(创建一个png位图编码器-它知道怎样保存一张png图片文件)
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.colorBitmap));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                string path = Path.Combine(myPhotos, "KinectScreenshot-Color-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }

                  //this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                   //this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }
    }
}

