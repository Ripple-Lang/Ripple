using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualBasic.FileIO;

namespace Ripple.Components
{
    public static class ArrayFormatter
    {
        #region テキスト形式

        public static void Format1DText<T>(TextWriter writer, T[] array, string delim)
        {
            for (int i = 0; i < array.Length; i++)
            {
                writer.Write(array[i]);
                if (i < array.Length - 1)
                {
                    writer.Write(delim);
                }
            }
        }

        public static T[] Unformat1DText<T>(TextReader reader, Func<string, T> parser, string delim)
        {
            return Unformat2DText(reader, parser, delim)[0];
        }

        public static void Format2DText<T>(TextWriter writer, T[][] array, string delim)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Format1DText(writer, array[i], delim);
                writer.WriteLine();
            }
        }

        public static T[][] Unformat2DText<T>(TextReader reader, Func<string, T> parser, string delim)
        {
            using (var tfp = new TextFieldParser(reader)
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = new[] { delim },
            })
            {
                var arrays = new List<T[]>();

                while (!tfp.EndOfData)
                {
                    T[] line = tfp.ReadFields().TrimLastEmpties().Select(s => parser(s)).ToArray();
                    arrays.Add(line);
                }

                return arrays.ToArray();
            }
        }

        #endregion

        #region バイナリ形式

        public static void Format1DBinary<T>(Stream stream, T[] array)
        {
            new BinaryFormatter().Serialize(stream, array);
        }

        public static T[] Unformat1DBinary<T>(Stream stream)
        {
            return (T[])new BinaryFormatter().Deserialize(stream);
        }

        public static void Format2DBinary<T>(Stream stream, T[][] array)
        {
            new BinaryFormatter().Serialize(stream, array);
        }

        public static T[][] Unformat2DBinary<T>(Stream stream)
        {
            return (T[][])new BinaryFormatter().Deserialize(stream);
        }

        #endregion

        #region ヘルパーメソッド

        private static string[] TrimLastEmpties(this string[] array)
        {
            int lastIndex = Array.FindLastIndex(array, s => !string.IsNullOrWhiteSpace(s));

            if (lastIndex == array.Length - 1)
            {
                return array;
            }
            else
            {
                string[] ret = new string[lastIndex + 1];
                Array.Copy(array, ret, lastIndex + 1);
                return ret;
            }
        }

        #endregion
    }
}
