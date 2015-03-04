using System;
using System.ComponentModel;    //Kinect帧事件有关
using System.Diagnostics;       //图像帧有关
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;         //Kinect命名空间
namespace KinectV2_Start
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window,INotifyPropertyChanged
    {
        /// <summary>
        ///kinect类对象
        /// </summary>
        private KinectSensor myKinectSensor=null;

        /// <summary>
        ///Readers(Give access to frames) 读取数据框架（对应6种数据源）
        /// </summary>
        private ColorFrameReader myColorFrameReader=null;
       // private BodyFrameReader myKinectBodyFr=null;
       // private InfraredFrameReader myInfrareFrameReader=null;

        /// <summary>
        /// Bitmap to display
        /// Net_Framework库，基于每个框架来更新和呈现位图。这对于生成算法内容（如分形图像）和数据可视化（如音乐可视化工具）很有用。
        /// </summary>
        private WriteableBitmap colorBitmap = null;  

        /// <summary>
        /// 当前状态展示   
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            //实例化kinect类
            this.myKinectSensor = KinectSensor.GetDefault();

            //******打开 读取数据的框架（对应图像数据源）
            //open the reader for the color frames
            this.myColorFrameReader = this.myKinectSensor.ColorFrameSource.OpenReader();

            //线程处理图像帧到来的框架函数
            //****** wire handler for frame arrival
            this.myColorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            //****** create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.myKinectSensor.ColorFrameSource.CreateFrameDescription((ColorImageFormat.Bgra));
            
            //****** create the bitmap to display（图像位图）
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width,colorFrameDescription.Height,96.0,96.0,PixelFormats.Bgr32,null);

            //***** set IsAvailableChanged event notifier
            this.myKinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
    
            //打开kinect
            this.myKinectSensor.Open();

            // set the status text
            this.StatusText = this.myKinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
            // use the window object as the view model in this simple example
            this.DataContext = this;       
            //
            //初始化界面
            this.InitializeComponent();
            //关闭kinect
          //  myKinectSensor.Close();
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
                return this.colorBitmap;  //将图像帧位图返回 在Windows窗口中显示
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
                        this.PropertyChanged(this,new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.myColorFrameReader != null)
            {
               // ColorFrameReder is IDisposable
                this.myColorFrameReader.Dispose();
                this.myColorFrameReader = null;
            }
            if (this.myKinectSensor != null)
            {
                this.myKinectSensor.Close();     //关闭窗口，对应的Kinect传感器也关闭
                this.myKinectSensor = null;
            }
        }

        /// <summary>
        /// 图像帧数据
        /// Handles the color frame data arriving from the sensor（获取从传感器获取的图像帧）
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame()) //发送帧事件参数
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

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
            this.StatusText = this.myKinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
