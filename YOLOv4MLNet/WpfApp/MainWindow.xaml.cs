using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using YOLOv4MLNet.DataStructures;
using YOLOv4MLNet;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Forms;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        public RecognisedObjects objects;
        private double progressBarStep;
        private string imageFolder;

        private static readonly object locker = new object();
        public MainWindow()
        {
            InitializeComponent();
            objects = new RecognisedObjects();

            ImageRecognitionClass.Notify += RecognizerEventHandler;
            ObjectsListBox.SelectionChanged += ObjectsListBoxChangedSelection;
            ObjectsListBox.ItemsSource = objects;
        }
        private void RecognizerEventHandler(string filePath, List<YoloV4Result> objectsList)
        {
            lock (locker)
            {
                Dispatcher.Invoke(new Action(() => { objects.Add(objectsList, filePath);
                    pbStatus.Value += progressBarStep;

                    if (ObjectsListBox.SelectedItem != null)
                        UpdateObjectsImageBox();

                }));
            }
        }

        private void UpdateObjectsImageBox()
        {
            var selectedItem = (string)ObjectsListBox.SelectedItem;
            
            ObjectsImagesListBox.Items.Clear();

            foreach (string file in objects.RecognisedObjectsDict[selectedItem].FilesToBoundariesDict.Keys)
            {
                foreach (float[] boundaries in objects.RecognisedObjectsDict[selectedItem].FilesToBoundariesDict[file])
                {
                    Image image = new Image();
                    image.Height = 100;

                    BitmapImage fileImage = new BitmapImage();
                    fileImage.BeginInit();
                    fileImage.UriSource = new Uri(file);
                    fileImage.EndInit();

                    var xCoord = (int)boundaries[0];
                    var yCoord = (int)boundaries[1];
                    var width = (int)boundaries[2] - xCoord;
                    var height = (int)boundaries[3] - yCoord;
                    Int32Rect newArea = new Int32Rect(xCoord, yCoord, width, height);
                    CroppedBitmap croppedImage = new CroppedBitmap(fileImage, newArea);
                    
                    image.Source = croppedImage;
                    ObjectsImagesListBox.Items.Add(image);
                }
            }
        }

        private void ObjectsListBoxChangedSelection(object sender, SelectionChangedEventArgs e)
        {
            if (ObjectsListBox.SelectedItem == null)
                return;
            UpdateObjectsImageBox();

        }

        private void StartProcessingButton(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(imageFolder))
            {
                progressBarStep = 100.0 / Directory.GetFiles(imageFolder, "*", SearchOption.TopDirectoryOnly).Length;
                Task t = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() => { pbStatus.Value = 0.0; }));
                        ImageRecognitionClass.Recognize(imageFolder);
                        Dispatcher.BeginInvoke(new Action(() => { pbStatus.Value = 100.0; }));
                    }
                    catch (Exception exc)
                    {
                        Console.Error.WriteLine(exc.Message);
                    }
                });
            }
        }

        private void OpenFolderButton(object sender, RoutedEventArgs e)
        {
            objects.Clear();
            ObjectsImagesListBox.Items.Clear();
            ImageRecognitionClass.cancellationTokenSource = new CancellationTokenSource();

            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OpenFolderTextBox.Text = dialog.SelectedPath;
                    imageFolder = dialog.SelectedPath;
                }
            }
        }
        private void AbortButton(object sender, RoutedEventArgs e)
        {
            ImageRecognitionClass.cancellationTokenSource.Cancel();
        }
    }
}
