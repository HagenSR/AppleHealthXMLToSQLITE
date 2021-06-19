using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers;

namespace AppleXMLToSQLITE
{
    class Program
    {
        private static Logger Logger = new LoggerConfiguration().Enrich.With(new ThreadIdEnricher()).WriteTo.Console(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({ThreadId}) {Message}{NewLine}{Exception}", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error).MinimumLevel.Error().CreateLogger();
        private static Dictionary<string, int> tableKeyCount = new Dictionary<string, int>();
        static void Main(string[] args)
        {

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            XmlReader reader = XmlReader.Create(@".\..\..\export\apple_health_export\export.xml", settings);
            reader.MoveToContent();

            Utilities util = new Utilities("health.db");
            List<Tuple<string, string>> attributeList = new List<Tuple<string, string>>();
            string nodeName = "";
            bool inrecord = false;
            // Parse the file and display each of the nodes.
            while (reader.Read())
            {
                try
                {
                    // Single Element
                    if (reader.IsEmptyElement && reader.Name != "MetadataEntry" && !inrecord)
                    {
                        nodeName = reader.Name;
                        int i = 0;
                        if (nodeName == "Record")
                        {
                            reader.MoveToAttribute(0);
                            i++;
                            nodeName = reader.Value.Replace("HKQuantityTypeIdentifier", "");
                        }
                        for (; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            attributeList.Add(new Tuple<string, string>(reader.Name, reader.GetAttribute(i).Replace("HKQuantityTypeIdentifier", "")));
                        }
                        if (attributeList.Count > 0)
                        {
                            util.enterRecord(attributeList, nodeName);
                            attributeList = new List<Tuple<string, string>>();
                            updateDict(nodeName);
                        }
                    }
                    // Enter big record
                    else if (reader.NodeType != XmlNodeType.EndElement && !inrecord && reader.NodeType != XmlNodeType.Whitespace)
                    {
                        int i = 0;
                        inrecord = true;
                        reader.MoveToAttribute(0);
                        i++;
                        nodeName = reader.Value.Replace("HKQuantityTypeIdentifier", "");

                        for (; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            attributeList.Add(new Tuple<string, string>(reader.Name, reader.GetAttribute(i).Replace("HKQuantityTypeIdentifier", "")));
                        }
                        if (attributeList.Count > 0)
                        {
                            util.enterRecord(attributeList, nodeName);
                            attributeList = new List<Tuple<string, string>>();
                            updateDict(nodeName);
                        }
                    }
                    //Enter meta data
                    else if(reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Whitespace)
                    {
                        string metaDataTableName = reader.Name;
                        for (int i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            attributeList.Add(new Tuple<string, string>(reader.Name, reader.GetAttribute(i).Replace("HKQuantityTypeIdentifier", "")));
                        }
                        if (attributeList.Count > 0)
                        {
                            util.enterMeta(attributeList, tableKeyCount.GetValueOrDefault(nodeName), nodeName, metaDataTableName);
                            attributeList = new List<Tuple<string, string>>();
                        }
                    }
                    else
                    {
                        inrecord = false;
                        if (attributeList.Count > 0)
                        {
                            util.enterRecord(attributeList, nodeName);
                            attributeList = new List<Tuple<string, string>>();
                            updateDict(nodeName);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    inrecord = false;
                    attributeList = new List<Tuple<string, string>>();
                }


            }

        }

        private static void updateDict(string tableName)
        {

            if (tableKeyCount.ContainsKey(tableName))
            {
                int count = 0;
                tableKeyCount.Remove(tableName, out count);
                tableKeyCount.Add(tableName, ++count);
            }
            else
            {
                tableKeyCount.Add(tableName, 0);
            }

        }
    }
}
