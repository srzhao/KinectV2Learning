//--------------------------------------------------------------------//
//KinectV2获取人体红外帧数据（红外数据流）
//InfraredFrameSource: 分辨率512*424 30fps(帧/秒) 每个像素16-bit
//date:2015-2-2
//------------------------------------------------------------------//
using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
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


using Microsoft.Kinect;                       //Kinect自带命名空间
using System.ComponentModel;                  //System.ComponentModel供给用于完成组件和控件的运行时和设计时行动的类。 此命名空间包罗用于属性和类型转换器的完成、数据源绑定和组件受权的基类和接口    
using System.Diagnostics;                     //该命名空间提供了用于与事件日志、性能计数器和系统进程进行交互的类。您可以在生产应用程序中保持对此监控代码的启用，并在发生问题时查看相关信息
namespace Get_InfraredBasics
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ///<summary>
        ///Maximum value(as a float) that can be returned by the InfraredFrame（红外帧能返回的最大值）
        ///</summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        ///<summary>
        ///The value by which the infrared source data will be scaled
        ///</summary>
        private const float InfraredSourceScale = 0.75f;

        /// <summary>
        /// Smallest value to display when the infrared data is normalized（最小的红外数据值）
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// Largest value to display when the infrared data is normalized（最大的红外数据值）
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        ///<summary>
        ///Kinect类
        
        ///</summary>
        private KinectSensor myKinectsensor = null;

        /// <summary>
        /// Reader for infrared frames（红外帧阅读器）
        /// </summary>
        private InfraredFrameReader myinfraredFrameReader = null;

        /// <summary>
        /// Description(width, height, etc) of the infrared frame data（红外帧数描述）
        /// </summary>
        private FrameDescription myinfraredFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap myinfraredBitmap = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        public MainWindow()
        {    
            //获取当前的Kinect传感器
            this.myKinectsensor = KinectSensor.GetDefault();
            //open the reader for the depth frames
            //打开深度帧的阅读器
            this.myinfraredFrameReader = this.myKinectsensor.InfraredFrameSource.OpenReader();
            //wire handler for frame arrival
            this.myinfraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;
            //get FrameDescription from InfraredFrameSource
            this.myinfraredFrameDescription = this.myKinectsensor.InfraredFrameSource.FrameDescription;
            //create the bitmap to display
            this.myinfraredBitmap = new WriteableBitmap(this.myinfraredFrameDescription.Width,this.myinfraredFrameDescription.Height,96.0,96.0,PixelFormats.Gray32Float,null);
            //set IsAvailanleChanged event notifier
            this.myKinectsensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            //打开Kinect
            this.myKinectsensor.Open();
            //set the status text
            this.StatusText = this.myKinectsensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                              : Properties.Resources.NoSensorStatusText;
            //use the window object as the view model in this simple example
            this.DataContext = this; 

            //初始化Windows界面
            this.InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.myinfraredBitmap;
            }
        }
        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
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
                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
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
            if (this.myinfraredFrameReader != null)
            {
                // InfraredFrameReader is IDisposable
                this.myinfraredFrameReader.Dispose();
                this.myinfraredFrameReader = null;
            }

            if (this.myKinectsensor != null)
            {
                this.myKinectsensor.Close();
                this.myKinectsensor = null;
            }
        }


        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event-对象发送事件</param>
        /// <param name="e">event arguments-事件参数</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.myinfraredBitmap != null)
            {
                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();
                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(this.myinfraredBitmap));

                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

               string path = Path.Combine(myPhotos, "KinectScreenshot-Infrared-" + time + ".png");

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }
                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.SavedScreenshotStatusTextFormat, path);
                }
                catch (IOException)
                {
                    this.StatusText = string.Format(CultureInfo.CurrentCulture, Properties.Resources.FailedScreenshotStatusTextFormat, path);
                }
            }
        }
        /// <summary>
        /// Handles the infrared frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            // InfraredFrame is IDisposable
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    // the fastest way to process the infrared frame data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                    {
                        // verify data and write the new infrared frame data to the display bitmap
                        if (((this.myinfraredFrameDescription.Width * this.myinfraredFrameDescription.Height) == (infraredBuffer.Size / this.myinfraredFrameDescription.BytesPerPixel)) &&
                            (this.myinfraredFrameDescription.Width == this.myinfraredBitmap.PixelWidth) && (this.myinfraredFrameDescription.Height == this.myinfraredBitmap.PixelHeight))
                        {
                            this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrameData">Pointer to the InfraredFrame image data</param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            // infrared frame data is a 16 bit value （一个像素帧 16-bits）
            ushort* frameData = (ushort*)infraredFrameData;

            // lock the target bitmap
            this.myinfraredBitmap.Lock();

            // get the pointer to the bitmap's back buffer
            float* backBuffer = (float*)this.myinfraredBitmap.BackBuffer;

            // process the infrared data
            for (int i = 0; i < (int)(infraredFrameDataSize / this.myinfraredFrameDescription.BytesPerPixel); ++i)
            {
                // since we are displaying the image as a normalized grey scale image, we need to convert from
                // the ushort data (as provided by the InfraredFrame) to a value from [InfraredOutputValueMinimum, InfraredOutputValueMaximum]
                backBuffer[i] = Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
            }

            // mark the entire bitmap as needing to be drawn
            this.myinfraredBitmap.AddDirtyRect(new Int32Rect(0, 0, this.myinfraredBitmap.PixelWidth, this.myinfraredBitmap.PixelHeight));
            // unlock the bitmap
            this.myinfraredBitmap.Unlock();
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the status text
            this.StatusText = this.myKinectsensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

    }
}
