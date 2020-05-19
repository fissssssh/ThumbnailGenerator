using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using ThumbnailGenerator.Annotations;

namespace ThumbnailGenerator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ThreadCounter Counter { get; set; } = ThreadCounter.Instance;

        public int ThreadNum
        {
            get => _threadNum;
            set
            {
                if (value != _threadNum)
                {
                    _threadNum = value;
                    OnPropertyChanged(nameof(ThreadNum));
                }
            }
        }

        private static object LockObject = new object();

        public int ThumbnailMaxWidth
        {
            get => _thumbnailMaxWidth;
            set
            {
                if (value != _thumbnailMaxWidth)
                {
                    _thumbnailMaxWidth = value;
                    OnPropertyChanged(nameof(ThumbnailMaxWidth));
                }
            }
        }

        private List<Task> tasks=new List<Task>();
        private int _threadNum = 0;
        private int _thumbnailMaxWidth = 120;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnSrcDir_Click(object sender, RoutedEventArgs e)
        {
            var path = SelectDirectory();
            if (Directory.Exists(path))
            {
                tbSrcDir.Text = path;
                var dirInfo = new DirectoryInfo(path);
                var imageItems = await Task.Run(() =>
                {
                    return dirInfo.GetFiles()
                        .Where(x => IsStaticImage(MimeMapping.GetMimeMapping(x.Name)))
                        .OrderBy(x => x.LastWriteTime)
                        .Select(x => new ImageItem(x, State.Pending))
                        .ToArray();
                });
                lvImages.ItemsSource = new ObservableCollection<ImageItem>(imageItems);
                btnStart.IsEnabled = true;
                pcbProcess.Value = 0;
            }
        }

        private void btnDestDir_Click(object sender, RoutedEventArgs e)
        {
            var path = SelectDirectory();
            if (path != null)
            {
                tbDestDir.Text = path;
            }
        }

        private string SelectDirectory()
        {
            using (var fbd = new CommonOpenFileDialog())
            {
                fbd.IsFolderPicker = true;
                if (fbd.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    return fbd.FileName;
                }
            }
            return null;
        }

        private bool IsStaticImage(string mimeType)
        {
            if (mimeType.Contains("image") && !mimeType.Contains("gif"))
            {
                return true;
            }
            return false;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbDestDir.Text))
            {
                MessageBox.Show("You must ensure the output direcotory is not null or empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(tbThumbnailMaxWidth.Text))
            {
                MessageBox.Show("You must ensure the thumbnail's max width is not null or empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Counter.Reset();
            tasks.Clear();
            var increment = 100f / lvImages.Items.Count;
            var outputDir = tbDestDir.Text;
            btnStart.IsEnabled = false;
            btnSrcDir.IsEnabled = false;
            btnDestDir.IsEnabled = false;
            tbThumbnailMaxWidth.IsEnabled = false;
            foreach (ImageItem item in lvImages.Items)
            {
                tasks.Add(Task.Run(async () =>
                {
                    //Counter.Increase(1);
                    lock (LockObject)
                    {
                        Dispatcher.InvokeAsync(() =>
                       {
                           ThreadNum++;
                           Debug.WriteLine(ThreadNum);
                       });
                    }
                    await Dispatcher.InvokeAsync(() =>
                    {
                        item.State = State.Processing;
                    });
                    await new ThumbnailTool().GenerateAndSaveAsync(item.File.FullName, ThumbnailMaxWidth, Path.Combine(outputDir, item.File.Name));
                    await Dispatcher.InvokeAsync(() =>
                    {
                        item.State = State.Solved;
                        pcbProcess.Value += increment;
                    });
                    lock (LockObject)
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            ThreadNum--;
                            Debug.WriteLine(ThreadNum);

                        });
                    }
                    //Counter.Reduce(1);
                }));
            }
            await Task.WhenAll(tasks);
            pcbProcess.Value = 100;
            MessageBox.Show("Thumbnails generation done!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            btnSrcDir.IsEnabled = true;
            btnDestDir.IsEnabled = true;
            tbThumbnailMaxWidth.IsEnabled = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}