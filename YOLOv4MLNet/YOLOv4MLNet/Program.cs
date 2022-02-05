using System;
using System.IO;
using System.Collections.Generic;
using YOLOv4MLNet.DataStructures;

// 2b var: TPL Flow + progress bar + list of detected object classes with the total number of found instances
namespace YOLOv4MLNet
{
   class Program
    {
        static Object locker = new Object();
        static int imagesProcessed = 0;
        static void Main()
        {
            Console.WriteLine("  Please, enter the path to the images folder (by default it is C:\\Users\\ayakshibaeva\\Source\\Repos\\YOLOv4MLNet\\YOLOv4MLNet\\Assets\\Images): ");
            string imagePath = Console.ReadLine();
            if (String.IsNullOrEmpty(imagePath))
                imagePath = @"C:\Users\ayakshibaeva\source\repos\402_yakshibaeva\YOLOv4MLNet\YOLOv4MLNet\Assets\Images";
            else if (!Directory.Exists(imagePath))
                {
                    Console.WriteLine("  Incorrect input");
                    return;
                }

            ImageRecognitionClass.Notify += ProgressBar;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancellationHandler);

            List<YoloV4Result> modelOutput = ImageRecognitionClass.Recognize(imagePath);
            Dictionary<string, int> dictObjectsAndAmounts = new Dictionary<string, int>();

            foreach (YoloV4Result item in modelOutput)
                if (dictObjectsAndAmounts.ContainsKey(item.Label))
                    ++dictObjectsAndAmounts[item.Label];
                else
                    dictObjectsAndAmounts.Add(item.Label, 1);

            if (dictObjectsAndAmounts.Count == 0)
            { Console.WriteLine("  No objects were found"); }
            else
            {
                Console.WriteLine($"  Objects that were found:");
                foreach (KeyValuePair<string, int> item in dictObjectsAndAmounts)
                    Console.WriteLine($"   Object: {item.Key}, Amount: {item.Value} ");
            }
        }

        private static void CancellationHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("\nExiting the program");
            ImageRecognitionClass.cancellationTokenSource.Cancel();
        }
        private static void ProgressBar(string message, List<YoloV4Result> objectsList)
        {
            lock (locker)
            {
                //Console.Clear();
                imagesProcessed++;
                int progressPercent = (int)((float)imagesProcessed / ImageRecognitionClass.imagesCount * 100);

                const char barPart = '█';
                string bar = " ";
                for (int i = 0; i < progressPercent; ++i)
                    bar += barPart;

                Console.WriteLine(bar + $" {progressPercent}%");
                Console.Write($"  {Path.GetFileName(message)} : ");
                foreach (YoloV4Result detectedObject in objectsList)
                    Console.Write($"{detectedObject.Label}, ");
                Console.WriteLine();
                Console.WriteLine();
            }
        }

    }
}
