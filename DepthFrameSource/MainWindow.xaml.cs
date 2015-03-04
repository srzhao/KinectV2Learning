//------------------------------------------------------------------------------
//获取深度帧数据
//测量范围0.5-4.5米
//每一个像素为16-bits,该数据表示从深度（红外）摄像头到该物体的位置，单位为毫米
//------------------------------------------------------------------------------
namespace Get_DepthFrameSource
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Map depth range to byte range
        ///</summary>
        private const int MapDepthToByte = 8000 / 256;

        /// <summary>
        /// Kinect 传感器
        /// </summary>
        private KinectSensor myKinectSensor = null;

        /// <summary>
        /// 深度帧数据阅读器
        /// </summary>
        private DepthFrameReader depthFrameReader = null;

        /// <summary>
        /// 深度帧包含数据的描述
        /// </summary>
        private FrameDescription depthFrameDescription = null;

        /// <summary>
        /// 展示位图(Bitmap to display)
        /// </summary>
        private WriteableBitmap depthBitmap = null;

        /// <summary>
        ///中间储藏（帧数据转换成颜色）
        /// </summary>
        private byte[] depthPixels = null;

        /// <summary>
        /// 当前状态文本（to display）
        /// </summary>
        private string statusText = null;

        public MainWindow()
        {
            //获取Kinect传感器(1 sensor)
            this.myKinectSensor = KinectSensor.GetDefault();
             //打开深度帧阅读器(2 reader)
            this.depthFrameReader = this.myKinectSensor.DepthFrameSource.OpenReader();
            //wire handler for  frame arrivel(arrive(到达)-arrivel（到达者）)
            this.depthFrameReader.FrameArrived += this.Reader_FrameArrived;
            //get FrameDescription from DepthFrameSource
            this.depthFrameDescription = this.myKinectSensor.DepthFrameSource.FrameDescription;
            //allocate(分配) space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height]; 
            //create 展示位图
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            //设置有效改变的事件通知器
            this.myKinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            //打开Kinect传感器
            this.myKinectSensor.Open();
            //设置状态文本
            this.StatusText = this.myKinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                              : Properties.Resources.NoSensorStatusText;
            //使用窗口对象作为展示模型
            this.DataContext = this;

            InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged 事件允许窗口控制绑定可变的数据
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        ///<summary>
        ///获取位图（用来展示）
        ///</summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.depthBitmap;
            }
        }

        ///<summary>
        ///Gets or Sets 当前状态 to display
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
                    //通知任何相关的elements: the text 已经发生改变
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        
        /// <summary>
        /// Execute 关闭窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.depthFrameReader != null)
            {
                //DepthFrameReader is IDisposable
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }
            if (this.myKinectSensor != null)
            {
                this.myKinectSensor.Close();
                this.myKinectSensor = null;
            }
        }


        /// <summary>
        /// 保存图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            //create a png bitmap encoder which knows how to save a .png file 
            BitmapEncoder encoder = new PngBitmapEncoder();
            //create frame from the writeable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(this.depthBitmap));

            string time = System.DateTime.UtcNow.ToString("hh'-'mm'-'ss",CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = Path.Combine(myPhotos, "KinectScreenshot-Depth-" + time + ".png");
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

        /// <summary>
        /// 处理从传感器中来的深度帧数据
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    //the fastest way to process the body index data is to directly access
                    //the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        //查证数据和写颜色数据到展示位图中
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance
                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }
            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
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
