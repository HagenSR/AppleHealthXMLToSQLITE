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

        private static Utilities util = new Utilities("health.db");

        static void Main(string[] args)
        {

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            XmlReader reader = XmlReader.Create(ConfigurationManager.ConnectionStrings["filePath"].ConnectionString, settings);
            reader.MoveToContent();

            // Note: C#'s DataSet class does allow for the easy conversion of XML to a DataSet ( A C# Database ). However, The health export can 
            // Become Excedingly large, requiring a large amount of RAM. This approach uses less Ram at the cost of time

            // Parse the file and display each of the nodes.
            while (reader.Read())
            {
                try
                {
                    // Single Element
                    if (reader.IsEmptyElement && util.tableParentNames.Count < 1)
                    {
                        util.handleXMLRecordRow(reader);
                    }
                    // Enter record with children
                    else if (reader.NodeType != XmlNodeType.EndElement && util.tableParentNames.Count < 1 && reader.NodeType != XmlNodeType.Whitespace)
                    {
                        util.tableParentNames.Push(reader.Name);
                        util.handleXMLRecordRow(reader);
                    }
                    //Enter meta data
                    else if(reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Whitespace)
                    {
                        util.handleXMLMetaRow(reader);
                    }
                    else if(reader.NodeType == XmlNodeType.EndElement)
                    {
                        // Parent node has ended, remove one parent from the list.
                        util.tableParentNames.Pop();
                    }
                }
                catch (Exception e)
                {
                    // Reset to a stable state after exception
                    Logger.Error(e.Message);
                    util.tableParentNames = new Stack<string>();
                }
            }
        }
    }
}
