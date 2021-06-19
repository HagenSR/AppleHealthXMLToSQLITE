using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers;
using System.Configuration;

namespace AppleXMLToSQLITE
{
    class Program
    {
        private static Logger Logger = new LoggerConfiguration().Enrich.With(new ThreadIdEnricher()).WriteTo.Console(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({ThreadId}) {Message}{NewLine}{Exception}", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error).MinimumLevel.Error().CreateLogger();
        private static Dictionary<string, int> tableKeyCount = new Dictionary<string, int>();

        private static Utilities util = new Utilities("health.db");

        private static List<Tuple<string, string>> attributeList = new List<Tuple<string, string>>();

        private static Stack<string> tableParentNames = new Stack<string>();

        static void Main(string[] args)
        {

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            XmlReader reader = XmlReader.Create(ConfigurationManager.ConnectionStrings["filePath"].ConnectionString, settings);
            reader.MoveToContent();

            // Parse the file and display each of the nodes.
            while (reader.Read())
            {
                try
                {
                    // Single Element
                    if (reader.IsEmptyElement && tableParentNames.Count < 1)
                    {
                        handleXMLRecordRow(reader);
                    }
                    // Enter record with children
                    else if (reader.NodeType != XmlNodeType.EndElement && tableParentNames.Count < 1 && reader.NodeType != XmlNodeType.Whitespace)
                    {
                        tableParentNames.Push(reader.Name);
                        handleXMLRecordRow(reader);
                    }
                    //Enter meta data
                    else if(reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Whitespace)
                    {
                        handleXMLMetaRow(reader);
                    }
                    else if(reader.NodeType == XmlNodeType.EndElement)
                    {
                        // Parent node has ended, remove one parent from the list.
                        tableParentNames.Pop();
                    }
                }
                catch (Exception e)
                {
                    // Reset to a stable state after exception
                    Logger.Error(e.Message);
                    tableParentNames = new Stack<string>();
                    attributeList = new List<Tuple<string, string>>();
                }


            }

        }

        public static void handleXMLRecordRow(XmlReader reader)
        {
            string nodeName = reader.Name;
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

        private static void handleXMLMetaRow(XmlReader reader)
        {
            string metaDataTableName = reader.Name;
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);
                attributeList.Add(new Tuple<string, string>(reader.Name, reader.GetAttribute(i).Replace("HKQuantityTypeIdentifier", "")));
            }
            if (attributeList.Count > 0)
            {
                util.enterMeta(attributeList, tableKeyCount.GetValueOrDefault(tableParentNames.Peek()), tableParentNames.Peek(), metaDataTableName);
                attributeList = new List<Tuple<string, string>>();
            }
            if (!reader.IsStartElement())
            {
                tableParentNames.Push(metaDataTableName);
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
