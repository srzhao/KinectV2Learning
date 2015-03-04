//------------------------------------------------------------------------------
//   骨骼帧的获取
//   测量范围：0.5-4.5米
//   帧数据是一个人体25个关节点的集合，每个帧都包含关节的3D位置和方向
//   最多支持6个人
//   30fps(帧/秒)
//   可以识别其中两个人体的手势
//   人体跟踪的三种状态：Not tracked, Inferred（推测）, Tracked(跟踪)  
//------------------------------------------------------------------------------
namespace Get_BodyFrame
{
    using System;
    using System.Collections.Generic;
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
       /// Kinect类
       /// </summary>
        private KinectSensor myKinectsenosr = null;

        /// <summary>
        /// Radius of draw hand circles(所画手掌圆的半径)
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness(厚度) of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangels
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant (恒定的) for clamping (夹紧) Z values of camera space points from being negative(消极的、负数的)
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128,255,0,0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred(推测关节点的颜色)
        /// </summary>
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Coordinate(坐标) mapper（映射） to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames(人体帧阅读器)
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// 身体数组
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// 骨骼定义
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;
        
        /// <summary>
        /// 展示宽度（深度空间）
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// 展示高度（深度空间）
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked(身体跟踪的颜色列表)
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        public MainWindow()
        {
            //获取当前的Kinect传感器
            this.myKinectsenosr = KinectSensor.GetDefault();

            //获取映射坐标
            this.coordinateMapper = this.myKinectsenosr.CoordinateMapper;

            //获取扩展深度
            FrameDescription frameDescription = this.myKinectsenosr.DepthFrameSource.FrameDescription;

            //获取关节空间尺寸
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            //打开人体帧阅读器
            this.bodyFrameReader = this.myKinectsenosr.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints(将两个关节点之间的线定义成一个骨骼)
            this.bones = new List<Tuple<JointType, JointType>>();

            //躯干
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));               //1头部到脖子
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));      //2脖子到（肩）脊椎
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));  //3（肩）脊椎到（中间）脊椎
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));      //4（中间）脊椎到（基）脊椎
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight)); //5（肩）脊椎到右肩
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));  //6（肩）脊椎到左肩
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));          //7（基）脊椎到右臀部
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));           //8（基）脊椎到左臀部

            // 右手臂
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));  //右边肩到右肘
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));     //右肘到右手腕
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));       //右手腕到右手
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));     //右手到右手指尖
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));      //右手腕到右手拇指

            //左手臂
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // 右腿
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));    //右臀部到右膝盖
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));  //右膝盖到右踝
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));  //右踝到右足

            // 左腿
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            //populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));  //靛蓝
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));  //紫罗兰

            // 打开Kinect传感器
            this.myKinectsenosr.Open();

            // 设置状态文本
            this.StatusText = this.myKinectsenosr.IsAvailable ? Properties.Resources.RunningStatusText
                                                              : Properties.Resources.NoSensorStatusText;

            // set IsAvailableChanged event notifier
            this.myKinectsenosr.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            //创建the drawing group (我们用来绘图的)
            this.drawingGroup = new DrawingGroup();

            //创建一个image source that 我们能用来控制我们的图像的
            this.imageSource = new DrawingImage(this.drawingGroup);

            //use the window object as the view model in this simple example
            this.DataContext = this;

            InitializeComponent();
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
                 return this.imageSource;
             }
        }

        /// <summary>
        /// gets或者sets当前展示状态文本
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
                   //通知任何绑定的元素：文本已经发展改变
                   if (this.PropertyChanged != null)
                   {
                       this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                   }
               }
            }
        }
       /// <summary>
       /// Execute start up tasks
       /// </summary>
       /// <param name="sender">object sending the event</param>
       /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }
            if (this.myKinectsenosr != null)
            {
                this.myKinectsenosr.Close();
                this.myKinectsenosr = null;
            }

        }
        ///<summary>
        ///处理来自传感器的人体帧数据
        ///</summary>
     private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">jonits to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="darwingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
     private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
     {
         // Draw the bones
         foreach (var bone in this.bones)
         {
             this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
         }

         // Draw the joints
         foreach (JointType jointType in joints.Keys)
         {
             Brush drawBrush = null;

             TrackingState trackingState = joints[jointType].TrackingState;

             if (trackingState == TrackingState.Tracked)
             {
                 drawBrush = this.trackedJointBrush;
             }
             else if (trackingState == TrackingState.Inferred)
             {
                 drawBrush = this.inferredJointBrush;
             }

             if (drawBrush != null)
             {
                 drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
             }
         }
     }

     /// <summary>
     /// Draws one bone of a body (joint to joint)
     /// </summary>
     /// <param name="joints">joints to draw</param>
     /// <param name="jointPoints">translated positions of joints to draw</param>
     /// <param name="jointType0">first joint of bone to draw</param>
     /// <param name="jointType1">second joint of bone to draw</param>
     /// <param name="drawingContext">drawing context to draw to</param>
     /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
     private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
     {
         Joint joint0 = joints[jointType0];
         Joint joint1 = joints[jointType1];

         // If we can't find either of these joints, exit
         if (joint0.TrackingState == TrackingState.NotTracked ||
             joint1.TrackingState == TrackingState.NotTracked)
         {
             return;
         }

         // We assume all drawn bones are inferred unless BOTH joints are tracked
         Pen drawPen = this.inferredBonePen;
         if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
         {
             drawPen = drawingPen;
         }

         drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
     }

     /// <summary>
     /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
     /// </summary>
     /// <param name="handState">state of the hand</param>
     /// <param name="handPosition">position of the hand</param>
     /// <param name="drawingContext">drawing context to draw to</param>
     private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
     {
         switch (handState)
         {
             case HandState.Closed:
                 drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                 break;

             case HandState.Open:
                 drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                 break;

             case HandState.Lasso:
                 drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                 break;
         }
     }

     /// <summary>
     /// Draws indicators to show which edges are clipping body data
     /// </summary>
     /// <param name="body">body to draw clipping information for</param>
     /// <param name="drawingContext">drawing context to draw to</param>
     private void DrawClippedEdges(Body body, DrawingContext drawingContext)
     {
         FrameEdges clippedEdges = body.ClippedEdges;

         if (clippedEdges.HasFlag(FrameEdges.Bottom))
         {
             drawingContext.DrawRectangle(
                 Brushes.Red,
                 null,
                 new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
         }

         if (clippedEdges.HasFlag(FrameEdges.Top))
         {
             drawingContext.DrawRectangle(
                 Brushes.Red,
                 null,
                 new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
         }

         if (clippedEdges.HasFlag(FrameEdges.Left))
         {
             drawingContext.DrawRectangle(
                 Brushes.Red,
                 null,
                 new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
         }

         if (clippedEdges.HasFlag(FrameEdges.Right))
         {
             drawingContext.DrawRectangle(
                 Brushes.Red,
                 null,
                 new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
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
            this.StatusText = this.myKinectsenosr.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
