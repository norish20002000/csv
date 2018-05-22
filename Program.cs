using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExCsvParser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start Read CSV");

            var watch = new Stopwatch();
            watch.Start();

            var enumCsv = (from field in
                              UtilityCsv.Context(@"CSV_FILES\KEN_ALL.CSV", ",", Encoding.GetEncoding(932))
                           //orderby field[2]
                           select new ZIPDB
                          {
                              ZIP = field[2],
                              ZIPSUBNO = 1,
                              KANA3 = field[8]
                          }).ToList();

            var enumOverLap = enumCsv.GroupBy(key => key.ZIP).Where(overLap => overLap.Count() > 1);
            foreach (var data in enumOverLap)
            {
                var groupData = data.Select((g, index) => new { ZipDb = g, Index = index + 1 }).ToList();
                foreach (var subno in groupData)
                {
                    subno.ZipDb.ZIPSUBNO = subno.Index;
                }
            }

            watch.Start();
            Console.WriteLine(watch.Elapsed);

            UtilityCsv.WriteListToFile<ZIPDB>(
                @"CONVERT_CSV\Converted.CSV",
                enumCsv);

            var exceptdata = CheckDataLengthAndRemakeData(enumCsv).ToList();

            // Generic置き換えよう
            Encoding encode = Encoding.GetEncoding(932);
            var propss = typeof(ZIPDB).GetProperties();
            var exceptData = (from dataClass in enumCsv
                             from field in propss
                             where encode.GetByteCount(field.GetValue(dataClass).ToString())
                                    >
                                    (Attribute.GetCustomAttribute(field, typeof(DataInfoAttribute)) as DataInfoAttribute).Datalength
                             select new { PropInfo = field, DataClass = dataClass }).ToList();
            foreach (var exData in exceptData)
            {
                var lineArray = propss.Select(t => t.GetValue(exData.DataClass).ToString()).ToArray();
                Console.WriteLine(exData.PropInfo.Name + "データ異常 : " + string.Join(",", lineArray));

                var attInfo = Attribute.GetCustomAttribute(exData.PropInfo, typeof(DataInfoAttribute)) as DataInfoAttribute;
                string propData = exData.PropInfo.GetValue(exData.DataClass).ToString();
                string reData = new String(propData.TakeWhile((c, i) =>
                    encode.GetByteCount(propData.Substring(0, i + 1)) <= attInfo.Datalength).ToArray());
                exData.PropInfo.SetValue(exData.DataClass, Convert.ChangeType(reData, exData.PropInfo.PropertyType));
            }
            var toList = exceptData.ToList();

            List<string> pNameList = new List<string>();
            var eData = enumCsv.Where(t =>
                {
                    var props = t.GetType().GetProperties();
                    var enumData = props
                        .Where(p =>
                            {
                                pNameList.Add(p.Name);
                                return Encoding.GetEncoding(932).GetByteCount(p.GetValue(t, null).ToString())
                                >
                                (Attribute.GetCustomAttribute(p, typeof(DataInfoAttribute)) as DataInfoAttribute).Datalength;
                            })
                        .Select(x => new { x.Name, x });

                    if (enumData.Count() > 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                });

            var properties = typeof(ZIPDB).GetProperties();

            // ZIPチェック
            var propZIP = typeof(ZIPDB).GetProperty("ZIP");
            var attZIP = (Attribute.GetCustomAttribute(propZIP, typeof(DataInfoAttribute)) as DataInfoAttribute);
            var fieldData = "111";
            var fieldDataCast = fieldData.GetType() == typeof(String) ? fieldData : fieldData.ToString();
            var eDataZip = enumCsv.Where(t =>
                Encoding.GetEncoding(932).GetByteCount(fieldData)
                >
                attZIP.Datalength
                );
            foreach (var zipData in eDataZip)
            {
                var lineArray = properties.Select(t => t.GetValue(zipData).ToString()).ToArray();
                Console.WriteLine("ZIPデータ異常 : " + string.Join(",", lineArray));

                string reData = new String(zipData.ZIP.TakeWhile((c, i) =>
                    encode.GetByteCount(zipData.ZIP.Substring(0, i + 1)) > attZIP.Datalength).ToArray());
                zipData.ZIP = reData;
            }

            //var enumCsv =
            //    from field in
            //        UtilityCsv.Context(@"C:\WORK\PG\C#\ExCsvParser\ExCsvParser\bin\Debug\CSV\47OKINAW.CSV",
            //        ",", Encoding.GetEncoding(932))
            //    select new string[] { field[2], field[6], field[7], field[8] };

            IEnumerable<string[]> results =
                from csvpath in
                    Directory.GetFiles("CSV_FILES", "*.csv", System.IO.SearchOption.TopDirectoryOnly)
                from fields in
                    UtilityCsv.Context(csvpath, ",", Encoding.GetEncoding(932))
                select new string[] { fields[2], fields[6], fields[7], fields[8] };

            var xx = Directory.GetFiles("CSV_FILES", "*.csv", System.IO.SearchOption.TopDirectoryOnly)
                .SelectMany(csvPath => UtilityCsv.Context(csvPath, ",", Encoding.GetEncoding(932))
                )
                .Select(fields => new string[] { fields[2], fields[6] });

            //var x = UtilityCsv.Context(@"CSV_FILES\47OKINAW.CSV", ",", Encoding.GetEncoding(932))
            //    .Select(fields => new string[]{fields[2], fields[6]});

            UtilityCsv.WriteFileCsv(
                @"CONVERT_CSV\Converted.CSV",
                xx);

            Console.ReadKey();
        }

        private static IEnumerable<T> CheckDataLengthAndRemakeData<T>(IEnumerable<T> enumData)
        {
            Encoding encode = Encoding.GetEncoding(932);
            var propss = typeof(T).GetProperties();
            var exceptData = (from dataClass in enumData
                              from field in propss
                              where encode.GetByteCount(field.GetValue(dataClass).ToString())
                                     >
                                     (Attribute.GetCustomAttribute(field, typeof(DataInfoAttribute)) as DataInfoAttribute).Datalength
                              select new { PropInfo = field, DataClass = dataClass }).ToList();
            foreach (var exData in exceptData)
            {
                //var lineArray = propss.Select(t => t.GetValue(exData.DataClass).ToString()).ToArray();
                //Console.WriteLine(exData.PropInfo.Name + "データ異常 : " + string.Join(",", lineArray));
                var orderAttNo = (Attribute.GetCustomAttribute(
                    exData.PropInfo, typeof(OrderAttribute)) as OrderAttribute).OrderNo; 
                Console.WriteLine(
                    exData.PropInfo.Name + 
                    "データ異常[" + orderAttNo + "] : " + 
                    propss.ExportDataClassData(exData.DataClass));

                var attInfo = Attribute.GetCustomAttribute(exData.PropInfo, typeof(DataInfoAttribute)) as DataInfoAttribute;
                string propData = exData.PropInfo.GetValue(exData.DataClass).ToString();
                string reData = new String(propData.TakeWhile((c, i) =>
                    encode.GetByteCount(propData.Substring(0, i + 1)) <= attInfo.Datalength).ToArray());
                exData.PropInfo.SetValue(exData.DataClass, Convert.ChangeType(reData, exData.PropInfo.PropertyType));

                yield return exData.DataClass;
            }
        }
    }

    public static class Extention
    {
        public static string ExportDataClassData<T>(this IEnumerable<PropertyInfo> enumProp, T t)
        {
            var propsArry = enumProp.Select(prop => prop.GetValue(t).ToString()).ToArray();
            string dataStr = string.Join(",", propsArry);

            return dataStr;
        }
    }
}
