//------------------------------------------------------------------------------
// 人体框架骨骼数据
//------------------------------------------------------------------------------
namespace Get_BodyIndexBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
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
        /// RGB像素在位图中的尺寸
        /// </summary>
        private const int BytesPerPixel = 4;
         
        /// <summary>
        /// 用来展示人体指数框架数据的颜色集合
        /// </summary>
        private static readonly uint[] BodyColor =
        {
            0x0000FF00,
            0x00FF0000,
            0xFFFF4000,
            0x40FFFF00,
            0xFF40FF00,
            0xFF808000,
        };

        /// <summary>
        /// Kinect传感器
        /// </summary>
        private KinectSensor myKinectSensor = null;

        /// <summary>
        /// 人体指数框架阅读器
        /// </summary>
        

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        ///  关闭主窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        /// <summary>
        /// 保存图片功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
