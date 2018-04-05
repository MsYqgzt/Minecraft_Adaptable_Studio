﻿using Ionic.Zip;
using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Updater
{
    /// <summary> MainWindow.xaml 的交互逻辑 </summary>
    public partial class MainWindow : MetroWindow
    {
        static string AppPath = Environment.CurrentDirectory;//应用程序根目录

        #region 更新模块
        //在线更新日志链接
        const string updatelog = "http://minecraft-1254149191.coscd.myqcloud.com/adaptable%20studio/update.log";
        //更新资源包
        const string pack = "http://minecraft-1254149191.coscd.myqcloud.com/adaptable%20studio/mas_package.zip";
        //核心文件
        const string core = "http://minecraft-1254149191.coscd.myqcloud.com/adaptable%20studio/Minecraft%20Adaptable%20Studio.exe";
        #endregion

        DispatcherTimer timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 1) };//计时器

        public MainWindow()
        {
            InitializeComponent();
            timer.Tick += Timer_Tick;//计时器事件
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            string download = AppPath + @"\download\",
                              textures = AppPath + @"\textures\",
                              json = AppPath + @"\json\";
            int t = 0;
            //创建文件夹
            if (!Directory.Exists(download)) Directory.CreateDirectory(download);
            if (!Directory.Exists(textures)) Directory.CreateDirectory(textures);
            if (!Directory.Exists(json)) Directory.CreateDirectory(json);

            //升级结束响应
            Thread th6 = new Thread(new ThreadStart(delegate
        {
            Thread.Sleep(100);
            if (t >= 5) Dispatcher.Invoke(new ThreadStart(delegate { State.Text = "升级完成"; }));
            else Dispatcher.Invoke(new ThreadStart(delegate { State.Text = "升级异常"; }));
            Dispatcher.Invoke(new ThreadStart(delegate { Tip.Text = "等待响应"; ProgressBar.Value = 100; }));
            timer.Start();
        }));

            //删除多余文件
            Thread th5 = new Thread(new ThreadStart(delegate
            {
                Thread.Sleep(100);
                Dispatcher.Invoke(new ThreadStart(delegate
                {
                    Tip.Text = "正在清理升级缓存...";
                    ProgressBar.Value += 20;
                }));
                Directory.Delete(AppPath + @"\download\", true);
                t++;
                th6.Start();
            }));

            //覆盖文件
            Thread th4 = new Thread(new ThreadStart(delegate
            {
                Thread.Sleep(100);
                Dispatcher.Invoke(new ThreadStart(delegate { ProgressBar.Value += 20; }));
                bool pass;
                do
                {
                    pass = false;
                    try
                    {
                        Dispatcher.Invoke(new ThreadStart(delegate { Tip.Text = "正在升级核心程序..."; }));
                        File.Delete(AppPath + @"\Minecraft Adaptable Studio.exe");
                        Directory.Move(AppPath + @"\download\Minecraft Adaptable Studio.temp", AppPath + @"\Minecraft Adaptable Studio.exe");
                        t++;
                    }
                    catch
                    {
                        MessageBoxResult r = MessageBox.Show("程序被拒绝访问\n是否强制结束进程?", "Updater", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (r == MessageBoxResult.Yes)
                        {
                            KillProcess("Minecraft Adaptable Studio", true);
                            pass = true;
                        }
                        else
                        {
                            Dispatcher.Invoke(new ThreadStart(delegate { Tip.Text = "升级终止"; }));
                            t = 0;
                        }
                    }
                } while (pass);
                th5.Start();
            }));

            //解压资源
            Thread th3 = new Thread(new ThreadStart(delegate
            {
                Thread.Sleep(100);
                Dispatcher.Invoke(new ThreadStart(delegate
                {
                    State.Text = "升级中";
                    Tip.Text = "正在解压升级资源...";
                    ProgressBar.Value += 20;
                }));
                using (ZipFile zip = new ZipFile(download + @"\mas_package.zip"))
                { zip.ExtractAll(AppPath, ExtractExistingFileAction.OverwriteSilently); }
                t++;
                th4.Start();
            }));

            //下载核心文件
            Thread th2 = new Thread(new ThreadStart(delegate
            {
                Thread.Sleep(100);
                try
                {
                    Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        Tip.Text = "正在下载Minecraft Adaptable Studio.temp";
                        ProgressBar.Value += 10;
                    }));
                    Download(download, core, @"\Minecraft Adaptable Studio.temp");
                    t++;
                    th3.Start();
                }
                catch
                {
                    Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        Tip.Text = "下载Minecraft Adaptable Studio.temp失败";
                        th6.Start();
                    }));
                }
            }));

            //下载更新资源
            Thread th1 = new Thread(new ThreadStart(delegate
            {
                Thread.Sleep(100);
                try
                {
                    Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        State.Text = "下载中";
                        Tip.Text = "正在下载mas_package.zip";
                        ProgressBar.Value += 10;
                    }));
                    Download(download, pack, @"\mas_package.zip");
                    t++;
                    th2.Start();
                }
                catch
                {
                    Tip.Text = "下载mas_package.zip失败";
                    th6.Start();
                }
            }));

            //获取升级版本号
            Thread th0 = new Thread(new ThreadStart(delegate
            {
                Thread.Sleep(800);
                string VerStr;
                try
                {
                    using (StreamReader sr = new StreamReader(WebRequest.Create(updatelog).GetResponse().GetResponseStream(), Encoding.UTF8))
                    {
                        VerStr = sr.ReadLine();//主程序版本信息                        
                    }
                    Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        if (VerStr != null) VerCode.Text = VerStr;
                    }));
                }
                catch { }

                th1.Start();
            }));

            th0.Start();
        }

        private void Main_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Opacity -= 0.005;
            if (Opacity <= 0.3)
                try
                {
                    Process.Start(AppPath + @"\Minecraft Adaptable Studio.exe");
                    Close();
                }
                catch { }
        }

        ///<summary> 资源文件下载 </summary>
        private static void Download(string download_path, string url, string fileName)
        {
            try
            {
                using (Stream responseStream = WebRequest.Create(url).GetResponse().GetResponseStream())
                {
                    //创建本地文件写入流
                    using (Stream stream = new FileStream(download_path + fileName, FileMode.Create))
                    {
                        byte[] bArr = new byte[1024];
                        int size = responseStream.Read(bArr, 0, bArr.Length);
                        while (size > 0)
                        {
                            stream.Write(bArr, 0, size);
                            size = responseStream.Read(bArr, 0, bArr.Length);
                        }
                    }
                }
            }
            catch (Exception ee) { throw ee; }//抛出异常
        }

        /// <summary> 终止外部进程 </summary>
        private void KillProcess(string processName, bool output)
        {
            try
            {
                Process[] thisproc = Process.GetProcessesByName(processName);
                //thisproc.lendth:名字为进程总数
                if (thisproc.Length > 0)
                {
                    for (int i = 0; i < thisproc.Length; i++)
                    {
                        if (!thisproc[i].CloseMainWindow()) thisproc[i].Kill();//尝试关闭进程 释放资源,否则强制关闭                        
                    }
                }
                else if (output) MessageBox.Show("进程关闭失败!", processName);
            }
            catch { MessageBox.Show("结束进程出错!", processName); }
        }
    }
}