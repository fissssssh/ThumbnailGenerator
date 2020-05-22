using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;

namespace ThumbnailGenerator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private ThumbnailTool _tool;
        public ThreadCounter Counter { get; set; } = ThreadCounter.Instance;
        public int ThumbnailMaxWidth { get; set; } = 360;

        private List<Task> tasks;

        public MainWindow()
        {
            InitializeComponent();
            _tool = new ThumbnailTool();
            tasks = new List<Task>();
        }

        private async void btnSrcDir_Click(object sender, RoutedEventArgs e)
        {
            var path = SelectDirectory();
            if (path == null)
            {
                return;
            }
            if (!Directory.Exists(path))
            {
                MessageBox.Show("No such directory!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var dirInfo = new DirectoryInfo(path);
            var imageItems = await Task.Run(() =>
            {
                return dirInfo.GetFiles().Where(x => IsStaticImage(MimeMapping.GetMimeMapping(x.Name))).OrderBy(x => x.LastWriteTime).Select(x => new ImageItem(x, State.Pending)).ToArray();
            });
            if (imageItems.Length == 0)
            {
                MessageBox.Show("No static image file in this directory!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            tbSrcDir.Text = path;
            lvImages.ItemsSource = new ObservableCollection<ImageItem>(imageItems);
            btnStart.IsEnabled = true;
            pcbProcess.Value = 0;
        }

        private void btnDestDir_Click(object sender, RoutedEventArgs e)
        {
            string path = null;
            while (true)
            {
                path = SelectDirectory();
                if (path == null)
                {
                    return;
                }
                if (!Directory.Exists(path))
                {
                    MessageBox.Show("No such directory!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dirInfo = new DirectoryInfo(path);

                if (dirInfo.GetFiles().Length == 0)
                {
                    break;
                }
                var r = MessageBox.Show("There are files in this folder. In order to prevent the files in this folder from being lost, we recommend using an empty folder\nAre you sure want to use this folder?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r == MessageBoxResult.Yes)
                {
                    break;
                }
            }
            tbDestDir.Text = path;

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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (ImageItem item in lvImages.Items)
            {
                tasks.Add(Task.Run(async () =>
                {
                    Counter.Increase(1);
                    item.State = State.Processing;
                    Dispatcher.Invoke(() =>
                    {
                        lvImages.SelectedItem = item;
                        lvImages.ScrollIntoView(item);

                    });
                    await _tool.GenerateAndSaveAsync(item.File.FullName, ThumbnailMaxWidth, Path.Combine(outputDir, item.File.Name));
                    item.State = State.Solved;
                    Counter.Reduce(1);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        pcbProcess.Value += increment;
                    });
                }));
            }
            await Task.WhenAll(tasks);
            pcbProcess.Value = 100;
            sw.Stop();
            MessageBox.Show($"Thumbnails generation done! \nIt took {sw.ElapsedMilliseconds}ms", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            btnSrcDir.IsEnabled = true;
            btnDestDir.IsEnabled = true;
            tbThumbnailMaxWidth.IsEnabled = true;
        }
    }
}