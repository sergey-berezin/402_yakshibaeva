using System;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Collections.Generic;
using YOLOv4MLNet.DataStructures;
using YOLOv4MLNet;
using System.Drawing;
using System.Linq;

namespace ImageRecognitionDB
{

    // database rows
    class ImageObject
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public byte[] Image { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    // database
    class DBContext : DbContext
    {
        public DbSet<ImageObject> Table { get; set; }

        public DBContext() : base()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder a) => a.UseSqlite(@"Data Source=C:\Users\ayakshibaeva\source\repos\402_yakshibaeva\YOLOv4MLNet\ImageRecognitionDB\database.db");
    }

    class Program
    {
        private const string imageFolder = @"C:\Users\ayakshibaeva\source\repos\402_yakshibaeva\YOLOv4MLNet\YOLOv4MLNet\Assets\Images";
        static Object locker = new Object();
        static int imagesProcessed = 0;

        static void Main()
        {
            ImageRecognitionClass.Notify += PrintProgressAndUpdateDB;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancellationHandler);

            while (true)
            {
                PrintDatabaseContent();
                DeleteDatabaseContent();

                Console.WriteLine("Do you want to start image recognition process? Yes/No");
                string userAnswer = Console.ReadLine();
                if (userAnswer == "Yes" || userAnswer == "yes")
                    ImageRecognitionClass.Recognize(imageFolder);
                

                Console.WriteLine("Do you want to exit the programm? Yes/No");
                userAnswer = Console.ReadLine();
                if (userAnswer == "Yes" || userAnswer == "yes")
                    break;
            }
        }

        protected static void CancellationHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("\nImage recognition progress was stopped");
            ImageRecognitionClass.cancellationTokenSource.Cancel();
        }
        private static void PrintDatabaseContent()
        {
            Console.WriteLine("Do you want to see database content? Yes/No");
            string userAnswer = Console.ReadLine();
            if (userAnswer == "Yes" || userAnswer == "yes")
            {
                using (DBContext context = new DBContext())
                {
                    var query = context.Table;

                    Console.Write(String.Format("{0,5} | {1,15} | {2,8} | {3,8} | {4,8} | {5,8} |\n", "Id", "Label", "X", "Y", "Width", "Height"));
                    Console.WriteLine(new string('_', 70));
                    foreach (var item in query)
                        Console.Write(String.Format("{0,5} | {1,15} | {2,8} | {3,8} | {4,8} | {5,8} |\n", item.Id, item.Label, item.X, item.Y, item.Width, item.Height));

                }
            }
        }
        private static void DeleteDatabaseContent()
        {
            Console.Write("Do you want to delete content from Database? Yes/No");
            string userAnswer = Console.ReadLine();
            using (DBContext context = new DBContext())
            {
                if (userAnswer == "Yes" || userAnswer == "yes")
                {
                    context.Table.RemoveRange(context.Table);
                    context.SaveChanges();
                }
            }
        }
   

        //copypaste from Task1 to print progress in console
        private static void ProgressBar(string message, List<YoloV4Result> objectsList)
        {
            lock (locker)
            {
                imagesProcessed++;
                int progressPercent = (int)((float)imagesProcessed / ImageRecognitionClass.imagesCount * 100);

                const char barPart = '█';
                string bar = " ";
                for (int i = 0; i < progressPercent; ++i)
                    bar += barPart;

                Console.WriteLine(bar + $" {progressPercent}%");
                Console.Write($"{Path.GetFileName(message)} : ");
                foreach (YoloV4Result detectedObject in objectsList)
                    Console.Write($"{detectedObject.Label}, ");
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private static byte[] ConvertImageToByteArray(Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);

                return ms.ToArray();
            }
        }
        private static void PrintProgressAndUpdateDB(string message, List<YoloV4Result> objectsList)
        {
            lock (locker)
            {
                ProgressBar(message, objectsList);

                using (DBContext context = new DBContext())
                {
                    foreach (YoloV4Result obj in objectsList)
                    {

                        var xCoord = (int)obj.BBox[0];
                        var yCoord = (int)obj.BBox[1];
                        var width = (int)obj.BBox[2] - xCoord;
                        var height = (int)obj.BBox[3] - yCoord;
                        Rectangle newArea = new Rectangle(xCoord, yCoord, width, height);

                        Image image = Image.FromFile(message);
                        Bitmap bitmapImage = new Bitmap(image);
                        Bitmap croppedImage = bitmapImage.Clone(newArea, bitmapImage.PixelFormat);
                        var binaryArray= ConvertImageToByteArray(croppedImage);

                        var query = from item in context.Table where item.X == xCoord && item.Y == yCoord && item.Width == width && item.Height == height select item.Image;
                        bool isAlreadyinDB = false;
                         foreach (var item in query)
                        {
                            // DB already has this obj
                            if (item.SequenceEqual(binaryArray))
                                isAlreadyinDB = true;
                                
                        }

                        if (!isAlreadyinDB)
                        {
                            ImageObject imageObject = new ImageObject { Label = obj.Label, X = xCoord, Y = yCoord, Width = width, Height = height, Image = binaryArray };
                            context.Table.Add(imageObject);
                        }
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
