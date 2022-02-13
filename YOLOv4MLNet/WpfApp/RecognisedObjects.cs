using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using YOLOv4MLNet.DataStructures;
using YOLOv4MLNet;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp
{
    // class contains dictionary [Type of object -> [total count of objects, [files -> list of boundaries]]
    public class RecognisedObjects : IEnumerable<string>, INotifyCollectionChanged
    {
        public class Value
        {
            public int Count { get; set; }
            public Dictionary<string, List<float[]>> FilesToBoundariesDict { get; set; }

            public Value(int count, Dictionary<string, List<float[]>> dict)
            {
                Count = count;
                FilesToBoundariesDict = dict;
            }
        }
        public Dictionary<string, Value> RecognisedObjectsDict { get; set; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public RecognisedObjects()
        {
            RecognisedObjectsDict = new Dictionary<string, Value>();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Clear()
        {
            RecognisedObjectsDict.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Add(List<YoloV4Result> resultList, string file)
        {
            foreach (var res in resultList)
            {
                if (RecognisedObjectsDict.ContainsKey(res.Label)) 
                { 
                    if (RecognisedObjectsDict[res.Label].FilesToBoundariesDict.ContainsKey(file)) 
                        RecognisedObjectsDict[res.Label].FilesToBoundariesDict[file].Add(res.BBox);
                    else
                        RecognisedObjectsDict[res.Label].FilesToBoundariesDict.Add(file, new List<float[]> { res.BBox });

                    ++RecognisedObjectsDict[res.Label].Count;
                }
                else
                    RecognisedObjectsDict.Add(res.Label, new Value(1, new Dictionary<string, List<float[]>> { { file, new List<float[]> { res.BBox } } }));
            }


            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public IEnumerator<string> GetEnumerator()
        {
            foreach (string obj in RecognisedObjectsDict.Keys)
            {
                yield return obj;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
