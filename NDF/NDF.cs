using System;
using System.Data;
using System.IO;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NDF
{
    /// <summary>
    /// NDF format C# library including Loader and Exporter
    /// NDF format is a universal format that could store C# DataTable into text format
    /// This format could used to exchange data between different C# applications
    /// </summary>
    public class NDF: IDisposable
    {
        public NDF()
        {

        }
        public void Dispose()
        {
            if (sw != null)
            {
                sw.Close();
                sw.Dispose();
                sw = null;
            }
            if (sr != null)
            {
                sr.Close();
                sr.Dispose();
                sr = null;
            }
            if (fs != null)
            {
                fs.Close();
                fs.Dispose();
                fs = null;
            }
            if(ds!=null)
            {
                ds.Tables.Clear();
                ds = null;
            }
        }


        #region Global Fields
        private DataSet ds = new DataSet();
        private DataTable BlockTable = null;
        private FileStream fs=null;
        private StreamWriter sw=null;
        private StreamReader sr = null;
        public int LineCount = 0;
        #endregion



        #region Functions to write to NDF
        public void CreateNdf(string filePath, string Desc)
        {
            if(fs!=null)
            {
                fs.Close();fs.Dispose();
            }
            fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            sw.WriteLine(@"<<NDF>>: (Dot) Net Data Format 1.0");
            sw.WriteLine(@"<<DESC>>: " + Desc);
        }
        public void WriteTableToNdf(string NdfTableName, string NdfTableCategory,DataTable table)
        {
            string block = "<["+ NdfTableName + "]>:"+ NdfTableCategory;
            sw.WriteLine(block);
            string para = "<Para>:";
            string type = "<Type>:";
            foreach(DataColumn dc in table.Columns)
            {
                para += dc.ColumnName + ",";
                type += dc.DataType.ToString()+",";
            }
            sw.WriteLine(para.TrimEnd(','));
            sw.WriteLine(type.TrimEnd(','));
            foreach(DataRow dr in table.Rows)
            {
                string data = "";
                for(int i=0;i<table.Columns.Count;i++)
                {
                    data += dr[i].ToString() + ",";
                }
                sw.WriteLine(data.Remove(data.Length - 1, 1));
            }
            sw.Flush();
        }
        public void WriteTableToNdf_WithLimits(string NdfTableName, string NdfTableCategory, DataTable dataTable, 
                DataTable specTable, string specKey, int UnitIdx, int UslIdx, int LslIdx)
        {
            string block = "<[" + NdfTableName + "]>:" + NdfTableCategory;
            sw.WriteLine(block);
            string para = "<Para>:";
            string type = "<Type>:";
            string unit = "<Unit>:";
            string usl = "<USL>:";
            string lsl = "<LSL>:";
            foreach (DataColumn dc in dataTable.Columns)
            {
                string ColName = dc.ColumnName.Trim();
                para += ColName + ",";
                type += dc.DataType.ToString() + ",";
                //look for specRow by ColName and update specs
                bool SpecFound = false;
                if (specTable != null && specTable.Rows.Count > 0)
                {
                    foreach (DataRow SpecRow in specTable.Rows)
                    {
                        string ParaName = SpecRow[specKey].ToString().Trim();
                        if(ColName == ParaName)
                        {
                            unit += SpecRow[UnitIdx]+",";
                            usl += SpecRow[UslIdx] + ",";
                            lsl += SpecRow[LslIdx] + ",";
                            SpecFound = true;
                            break;
                        }
                    }
                }
                //no spec found for current Column, use [,]
                if(!SpecFound)
                {

                }
            }
            sw.WriteLine(para.TrimEnd(','));
            sw.WriteLine(type.TrimEnd(','));
            sw.WriteLine(unit.TrimEnd(','));
            sw.WriteLine(usl.TrimEnd(','));
            sw.WriteLine(lsl.TrimEnd(','));
            foreach (DataRow dr in dataTable.Rows)
            {
                string data = "";
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    data += dr[i].ToString() + ",";
                }
                sw.WriteLine(data.Remove(data.Length -1, 1));
            }
            sw.Flush();
        }
        public void CloseNdf()
        {
            if(sw!=null)
            {
                sw.Close();
                sw.Dispose();
                sw = null;
            }
            if(fs!=null)
            {
                fs.Close();
                fs.Dispose();
                fs = null;
            }
        }
        #endregion



        #region Functions to Load NDF
        public DataSet LoadNDF(string filePath)
        {
            if (fs != null)
            {
                fs.Close(); fs.Dispose();
            }
            //initialize dataset
            LineCount = 0;
            ds = new DataSet();
            fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            sr = new StreamReader(fs);
            string lineText = sr.ReadLine();
            LineCount++;
            if (lineText != null)
            {
                if(lineText.Contains("<<NDF>>"))
                {
                    //bool BlockStart = false;
                    //bool DataStart = false;
                    while((lineText=sr.ReadLine())!=null)
                    {
                        LineCount++;
                        if (ContainRegExp(lineText, @"\<\[[\w]+\]\>"))
                        {
                            LoadDataBlock(lineText, sr);
                        }
                    }
                    //Add last table to dataset
                    if (BlockTable != null)
                    {
                        if (!ds.Tables.Contains(BlockTable.TableName))
                        {
                            ds.Tables.Add(BlockTable);
                        }
                        else
                        {
                            throw (new Exception("Duplicated table in NDF!"));
                        }
                    }
                }
            }
            sr.Close(); sr.Dispose();sr = null;
            fs.Close();fs.Dispose();fs = null;
            return ds;
        }

        private void LoadDataBlock(string Title, StreamReader sr)
        {
            //add previous table to Dataset
            if (BlockTable != null)
            {
                ds.Tables.Add(BlockTable);
            }
            //Table Name: BlockName and BlockDesc
            string pat = @"\<\[([\w]+)\]\>:([\w]*)";
            string[] Names = FindRegExp(Title, pat);
            string TableName = Names[0].Trim();
            string TableCategory = "";
            //if there is any Table Category after <[table]>:Category
            if (Names.Length > 1)
                TableCategory = Names[1].Trim();
            //TableName:TestCategory
            if (!string.IsNullOrEmpty(TableCategory))
                TableName += ":" + TableCategory;
            BlockTable = new DataTable(TableName);
            //Data for table structure
            List<string> ColumnNames = new List<string>();
            List<string> ColumnTypes = new List<string>();
            List<string> UnitList = new List<string>();
            List<string> UslList = new List<string>();
            List<string> LslList = new List<string>();
            //Flag to indicate whether it is first data line
            bool PreviousDefinitionLine = true;
            string lineText = null;
            while ((lineText = sr.ReadLine()) != null)
            {
                LineCount++;
                if (ContainRegExp(lineText, @"\<\[[\w]+\]\>"))
                {
                    //if(PreviousDefinitionLine==false)
                    //{
                    //    //add table to dataset
                    //    ds.Tables.Add(BlockTable);
                    //}
                    //Data block
                    LoadDataBlock(lineText, sr);
                }
                else if (ContainRegExp(lineText, @"\<[\w]+\>"))
                {
                    //Data Structure definition
                    pat = @"\<([\w]+)\>:[\b\s]*([\w\,\.\[\]\-_\b\s]*)";
                    Names = FindRegExp(lineText, pat);
                    string structName = Names[0].Trim();
                    string structValue = Names[1].Trim();
                    switch (structName.ToUpper())
                    {
                        case "PARA":
                            {
                                string[] sa = structValue.Split(',');
                                foreach (string s in sa)
                                {
                                    ColumnNames.Add(s.Trim());
                                }
                            }
                            break;
                        case "TYPE":
                            {
                                string[] sa = structValue.Split(',');
                                foreach (string s in sa)
                                {
                                    ColumnTypes.Add(s.Trim());
                                }
                            }
                            break;
                        case "UNIT":
                            {
                                string[] sa = structValue.Split(',');
                                foreach (string s in sa)
                                {
                                    UnitList.Add(s.Trim());
                                }
                            }
                            break;
                        case "USL":
                            {
                                string[] sa = structValue.Split(',');
                                foreach (string s in sa)
                                {
                                    UslList.Add(s.Trim());
                                }
                            }
                            break;
                        case "LSL":
                            {
                                string[] sa = structValue.Split(',');
                                foreach (string s in sa)
                                {
                                    LslList.Add(s.Trim());
                                }
                            }
                            break;
                        default:; break;
                    }

                }
                else
                {
                    /***** data ******/
                    //Create Table Structures
                    if (PreviousDefinitionLine)
                    {
                        //ColumnNames.Count == ColumnTypes.Count?
                        if (ColumnNames.Count == ColumnTypes.Count)
                        {
                            for (int c = 0; c < ColumnNames.Count; c++)
                            {
                                BlockTable.Columns.Add(ColumnNames[c], Type.GetType(ColumnTypes[c]));
                            }
                        }

                        //Create Test Table and udpate TID, Unit, USL and LSL
                        if (UslList.Count > 0)
                        {
                            if (UslList.Count == LslList.Count && LslList.Count == UnitList.Count)
                            {
                                //ParaName[string], Para_HiLimit[double], Para_LoLimit[double], Para_Unit[string]
                                DataTable BlockSpecTable = new DataTable(TableName + "_SPEC");
                                BlockSpecTable.Columns.Add("Para", typeof(string));
                                BlockSpecTable.Columns.Add("HiLimit", typeof(double));
                                BlockSpecTable.Columns.Add("LoLimit", typeof(double));
                                BlockSpecTable.Columns.Add("Unit", typeof(string));
                                for (int l = 0; l < UslList.Count; l++)
                                {
                                    if (!string.IsNullOrEmpty(UslList[l]) && !string.IsNullOrEmpty(LslList[l])
                                        && !string.IsNullOrEmpty(UnitList[l]))
                                    {
                                        DataRow dr = BlockSpecTable.NewRow();
                                        dr["Para"] = ColumnNames[l];
                                        dr["HiLimit"] = UslList[l];
                                        dr["LoLimit"] = LslList[l];
                                        dr["Unit"] = UnitList[l];
                                        BlockSpecTable.Rows.Add(dr);
                                    }
                                }
                                ds.Tables.Add(BlockSpecTable);
                            }
                            else
                            {
                                throw (new Exception("Spec Limit and Unit count does not match!"));
                            }
                        }
                        PreviousDefinitionLine = false;
                    }
                    //update data row
                    string[] dars = lineText.Split(',');
                    if (dars.Length == BlockTable.Columns.Count)
                    {
                        DataRow dr = BlockTable.NewRow();
                        for (int c = 0; c < BlockTable.Columns.Count; c++)
                        {
                            string valStr = dars[c];
                            Type valTyp = BlockTable.Columns[c].DataType;
                            if(valTyp.ToString() != "System.Char" || valStr.Length > 1) 
                                valStr = valStr.Trim();
                            if (string.IsNullOrEmpty(valStr) && valTyp.ToString() != "System.String")
                            {
                                //Empty string -> DBNull
                                dr[c] = DBNull.Value;
                            }
                            else
                            {
                                object v = null;
                                v = Convert.ChangeType(valStr, valTyp);
                                if (v != null)
                                    dr[c] = v;
                                else
                                    dr[c] = DBNull.Value;
                            }
                        }
                        BlockTable.Rows.Add(dr);
                    }
                    else
                    {
                        throw (new Exception("Parameters count does not match!"));
                    }
                }
            }
        }
        #endregion



        #region Helper
        //Use RegExp to capture sub-matches
        private string[] FindRegExp(string input, string pattern)
        {
            string[] matches = new string[0] { };
            MatchCollection mcs = Regex.Matches(input, pattern);
            if (mcs.Count > 0)
            {
                Match mt = mcs[0];
                matches = new string[mt.Groups.Count - 1];
                for (int i = 1; i < mt.Groups.Count; i++)
                {
                    matches[i - 1] = mt.Groups[i].Value;
                }
            }
            return matches;
        }
        private bool MatchRegExp(string input, string pattern)
        {
            return Regex.IsMatch(input, pattern);
        }
        private bool ContainRegExp(string input, string pattern)
        {
            if (Regex.Matches(input, pattern).Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}
