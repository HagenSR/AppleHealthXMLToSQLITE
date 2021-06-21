using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data.SQLite;
using System.Collections;
using Serilog.Core;
using Serilog;
using Serilog.Enrichers;
using System.Text.RegularExpressions;

namespace AppleXMLToSQLITE
{
    class Utilities
    {
        private SQLiteConnection sqlConn;
        private List<string> tableList;
        private static Logger Logger = new LoggerConfiguration().Enrich.With(new ThreadIdEnricher()).WriteTo.Console(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({ThreadId}) {Message}{NewLine}{Exception}").WriteTo.File("./logs/log").CreateLogger();

        public Utilities(string filePath)
        {
            if (!File.Exists("health.db"))
            {
                SQLiteConnection.CreateFile("health.db");
            }
            this.sqlConn = new SQLiteConnection("Data Source=health.db;Version=3;");
            this.sqlConn.Open();
            this.tableList = new List<string>();

        }

        //private bool checkIfTableExists(string table)
        //{
        //    SQLiteCommand com = this.sqlConn.CreateCommand();
        //    com.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=@tableName;";
        //    com.Parameters.AddWithValue("@tableName", table);
        //    int count = Convert.ToInt32(com.ExecuteScalar());
        //    return count > 0 ? true : false;

        //}

        private bool createTable(List<Tuple<string, string>> attributes, string table, bool meta)
        {
            SQLiteCommand com = this.sqlConn.CreateCommand();
            string qry = "CREATE TABLE " + table + " ( ";
            if (meta && attributes.Count > 0)
            {
                qry += " FK INT, tableReference TEXT,";
            }
            else if(meta)
            {
                qry += " FK INT, tableReference TEXT";
            }

            for (int i = 0; i < attributes.Count; i++)
            {
                Tuple<string, string> tup = attributes[i];
                if (i != attributes.Count - 1)
                {
                    if (tup.Item1.ToLower().Contains("date"))
                    {
                        qry += tup.Item1 + " DATETIME, ";
                    }
                    else if (tup.Item1.ToLower().Contains("value"))
                    {
                        qry += tup.Item1 + " INT, ";
                    }
                    else
                    {
                        qry += tup.Item1 + " TEXT, ";
                    }
                }
                else
                {
                    if (tup.Item1.ToLower().Contains("date"))
                    {
                        qry += tup.Item1 + " DATETIME ";
                    }
                    else if (tup.Item1.ToLower().Contains("value"))
                    {
                        qry += tup.Item1 + " INT ";
                    }
                    else
                    {
                        qry += tup.Item1 + " TEXT ";
                    }
                }
            }
            qry += " );";
            com.CommandText = qry;
            Logger.Information(String.Format("Created table {0}", table));
            int count = com.ExecuteNonQuery();
            this.tableList.Add(table);
            return count > 0 ? true : false;

        }

        private List<string> getCollumns(string table)
        {
            List<string> list = new List<string>();
            SQLiteCommand com = this.sqlConn.CreateCommand();
            string qry = string.Format("pragma table_info({0});", table);
            com.CommandText = qry;
            SQLiteDataReader res = com.ExecuteReader();
            while (res.Read())
            {
                list.Add(res.GetString(1));
            }
            return list;
        }

        private bool addColumn(Tuple<string, string> tup, string table)
        {
            SQLiteCommand com = this.sqlConn.CreateCommand();
            string qry = "ALTER TABLE " + table + " ADD COLUMN ";
            if (tup.Item1.ToLower().Contains("date"))
            {
                qry += tup.Item1 + " DATETIME ";
            }
            else if (tup.Item1.ToLower().Contains("value"))
            {
                qry += tup.Item1 + " INT ";
            }
            else
            {
                qry += tup.Item1 + " TEXT ";
            }
            com.CommandText = qry;
            int count = com.ExecuteNonQuery();
            Logger.Information(String.Format("Added collumn {0} to {1}", tup.Item1, table));
            return count > 0 ? true : false;
        }


        private void updateColumnList(List<Tuple<string, string>> attributes, string table)
        {
            List<string> list = getCollumns(table);
            for (int i = 0; i < attributes.Count; i++)
            {
                Tuple<string, string> tup = attributes[i];
                if (!list.Contains(tup.Item1))
                {
                    addColumn(tup, table);
                }
            }
        }

        public bool enterRecord(List<Tuple<string, string>> attributes, string tableName)
        {
            if (!this.tableList.Contains(tableName))
            {
                createTable(attributes, tableName, false);
            }
            SQLiteCommand com = this.sqlConn.CreateCommand();
            string qry = "INSERT INTO " + tableName + "( ";
            string vals = " ( ";
            for (int i = 0; i < attributes.Count; i++)
            {
                Tuple<string, string> tup = attributes[i];
                if (i != attributes.Count - 1)
                {
                    qry += tup.Item1 + ", ";
                    vals += "@" + tup.Item1 + ", ";
                    com.Parameters.AddWithValue("@" + tup.Item1, cleanInput(tup));
                }
                else
                {
                    qry += tup.Item1;
                    vals += "@" + tup.Item1;
                    com.Parameters.AddWithValue("@" + tup.Item1, tup.Item2);
                }

            }
            qry += " ) VALUES " + vals + " )";
            com.CommandText = qry;
            int count = 0;
            try
            {
                count = com.ExecuteNonQuery();
                //Logger.Information(String.Format("Entered Record into {0}", tableName));
            }
            catch (Exception e)
            {
                updateColumnList(attributes, tableName);
                com = this.sqlConn.CreateCommand();
                com.CommandText = qry;
                count = com.ExecuteNonQuery();
            }


            return count > 0 ? true : false;
        }

        public bool enterMeta(List<Tuple<string, string>> attributes, int fk, string tableNameReference, string metaTableName)
        {
            if (!this.tableList.Contains(metaTableName))
            {
                createTable(attributes, metaTableName, true);
            }
            SQLiteCommand com = this.sqlConn.CreateCommand();
            string qry = "INSERT  INTO " + metaTableName + "( FK, tableReference";
            string vals = " " + fk + ", '" + tableNameReference + "'";
            for (int i = 0; i < attributes.Count; i++)
            {
                Tuple<string, string> tup = attributes[i];
                qry += "," + tup.Item1;
                vals += ", @" + tup.Item1;
                com.Parameters.AddWithValue("@" + tup.Item1, tup.Item2);

            }
            qry += " ) VALUES (  " + vals + " )";
            com.CommandText = qry;
            int count = 0;
            try
            {
                count = com.ExecuteNonQuery();
                //Logger.Information(String.Format("Entered Record into {0}", "MetadataEntry"));
            }
            catch (Exception e)
            {
                updateColumnList(attributes, metaTableName);
                com = this.sqlConn.CreateCommand();
                com.CommandText = qry;
                count = com.ExecuteNonQuery();
            }


            return count > 0 ? true : false;
        }

        public string cleanInput(Tuple<string, string> ins)
        {
            String outs = ins.Item2;
            if (ins.Item1.ToLower().Contains("date"))
            {
                Regex regEx = new Regex("-\\d\\d\\d\\d");
                outs = regEx.Replace(outs, "");

            }
            return outs;
        }


    }
}
