using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using Microsoft.Win32;
using Ripple.Components;

namespace Ripple.Plugins.ArrayToTextVisualizer
{
    class ViewModel : INotifyPropertyChanged
    {
        private const char Delim = '\t';

        private readonly Array array;
        private readonly bool arrayIsArrayOfArrays;
        private readonly WeakReference<string>[] cachedStrings;

        public ViewModel(Array array)
        {
            this.array = array;
            this.arrayIsArrayOfArrays = array.GetValue(0) is Array;
            this.cachedStrings = new WeakReference<string>[array.Length];

            currentPage = -1;   // 0で無ければよい
            this.CurrentPage = 0;
        }

        private int currentPage;
        public int CurrentPage
        {
            get
            {
                return currentPage;
            }

            set
            {
                if (currentPage != value && value >= 0)
                {
                    currentPage = value % array.Length;

                    string resultString;

                    if (cachedStrings[currentPage] == null)
                    {
                        resultString = ArrayToString(GetAtOrWhole(currentPage), Delim);
                        cachedStrings[currentPage] = new WeakReference<string>(resultString);
                    }
                    else if (!cachedStrings[currentPage].TryGetTarget(out resultString))
                    {
                        resultString = ArrayToString(GetAtOrWhole(currentPage), Delim);
                        cachedStrings[currentPage].SetTarget(resultString);
                    }

                    CurrentText = resultString;

                    RaisePropertyChanged("CurrentPage");
                    RaisePropertyChanged("CurrentText");
                }
            }
        }

        private bool enablesPreview;
        public bool EnablesPreview
        {
            get { return enablesPreview; }
            set
            {
                if (enablesPreview != value)
                {
                    enablesPreview = value;
                    RaisePropertyChanged("EnablesPreview");
                }
            }
        }

        public void ExecuteSave()
        {
            const string Filter = "テキストファイル - カンマ区切り (*.csv)|*.csv|テキストファイル - タブ区切り (*.tsv;*.txt)|*.tsv;*.txt|Ripple バイナリファイル (*.bin)|*.bin|すべてのファイル (*.*)|*.*";

            SaveFileDialog ofd = new SaveFileDialog()
            {
                Filter = Filter
            };

            if (ofd.ShowDialog().Value)
            {
                string fileName = ofd.FileName;
                Array writeArray = GetAtOrWhole(currentPage);
                int numDimensions = GetDimensions(writeArray);

                switch (System.IO.Path.GetExtension(fileName).ToLower())
                {
                    case ".csv":
                        using (var writer = new StreamWriter(fileName))
                        {
                            if (numDimensions == 1)
                                ArrayFormatter.Format1DText(writer, (dynamic)writeArray, ",");
                            else
                                ArrayFormatter.Format2DText(writer, (dynamic)writeArray, ",");
                        }
                        break;
                    case ".tsv":
                    case ".txt":
                        using (var writer = new StreamWriter(fileName))
                        {
                            if (numDimensions == 1)
                                ArrayFormatter.Format1DText(writer, (dynamic)writeArray, "\t");
                            else
                                ArrayFormatter.Format2DText(writer, (dynamic)writeArray, "\t");
                        }
                        break;
                    case ".bin":
                    default:
                        using (var stream = File.OpenWrite(fileName))
                        {
                            if (numDimensions == 1)
                                ArrayFormatter.Format1DBinary(stream, (dynamic)writeArray);
                            else
                                ArrayFormatter.Format2DBinary(stream, (dynamic)writeArray);
                        }
                        break;
                }
            }
        }

        public string CurrentText { get; private set; }

        private Array GetAtOrWhole(int index)
        {
            if (arrayIsArrayOfArrays)
            {
                return (Array)array.GetValue(index);
            }
            else
            {
                return array;
            }
        }

        private static int GetDimensions(object array)
        {
            int dimension = 0;
            while (array is Array)
            {
                array = ((Array)array).GetValue(0);
                dimension++;
            }
            return dimension;
        }

        private static string ArrayToString(Array array, char delim)
        {
            int dimension = GetDimensions(array);

            if (dimension > 2)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var elem in array)
                {
                    _2DArrayToString((Array)array, sb, delim);
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                if (dimension == 1)
                    _1DArrayToString(array, sb, delim);
                else
                    _2DArrayToString(array, sb, delim);

                return sb.ToString();
            }
        }

        private static void _2DArrayToString(Array array, StringBuilder sb, char delim)
        {
            foreach (var line in array)
            {
                _1DArrayToString((Array)line, sb, delim);
                sb.AppendLine();
            }
        }

        private static void _1DArrayToString(Array array, StringBuilder sb, char delim)
        {
            if (array is bool[])
            {
                var boolArray = array as bool[];
                for (int i = 0; i < boolArray.Length; i++)
                {
                    sb.Append(boolArray[i] ? "1 " : "0 ");
                }
            }
            else if (array.GetValue(0).GetType().IsValueType)
            {
                _ValueType1DArrayToString((dynamic)array, sb, delim);
            }
            else
            {
                foreach (var elem in array)
                {
                    sb.Append(elem.ToString());
                    sb.Append(delim);
                }
            }
        }

        private static void _ValueType1DArrayToString<T>(T[] array, StringBuilder sb, char delim) where T : struct
        {
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString());
                sb.Append(delim);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string name)
        {
            var d = PropertyChanged;
            if (d != null)
            {
                d(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
