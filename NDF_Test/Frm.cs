using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace NDF
{
    /// <summary>
    /// A winForm to simulate NDF exportation and loading.
    /// </summary>
    public partial class Frm : Form
    {
        public Frm()
        {
            InitializeComponent();
        }



        #region global fields
        //Dataset used to store data loaded from NDF file
        DataSet loadedDataset = new DataSet();

        //NDF library object used to load/export NDF data
        NDF ndf = null;

        //table index of Datasets
        int tabIdx = 0;
        #endregion



        #region event functions
        private void Frm_Load(object sender, EventArgs e)
        {
            //Init NDF library
            ndf = new NDF();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Dispose NDF library before closing form
            if (ndf != null)
                ndf.Dispose();
        }
        private void buttonCreate_Click(object sender, EventArgs e)
        {
            DataSet sampleDataSet = CreateSampleDataSet();
            if (sampleDataSet != null && sampleDataSet.Tables.Count > 0)
            {
                //Sample NDF data file location, same path as executable file located
                string file = Application.StartupPath + "\\NDF_Test_Sample.ndf";

                //Init NDF file 
                ndf.CreateNdf(file, "Sample NDF data, north wind table");

                //write all tables to NDF file from DataSet
                foreach (DataTable dt in sampleDataSet.Tables)
                {
                    ndf.WriteTableToNdf(dt.TableName, dt.TableName, dt);
                }

                //close NDF file
                ndf.CloseNdf();
                ndf.Dispose();

                openFileDialog1.FileName = file;
                StatusLabel.Text = "Sample data written to NDF file " + file;
                MessageBox.Show($"Data has been written to {Path.GetFileName(file)}", "Write Successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                StatusLabel.Text = "No data to be written to NDF file.";
            }
        }
        private void buttonRead_Click(object sender, EventArgs e)
        {
            //Show open file dialog to select NDF file
            DialogResult dlgr = openFileDialog1.ShowDialog();
            if (dlgr == DialogResult.OK)
            {
                string ndfFile = openFileDialog1.FileName;
                if (string.IsNullOrEmpty(ndfFile) || !File.Exists(ndfFile))
                {
                    MessageBox.Show("The file you specified does not exist!", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                //Initialize NDF libaray
                ndf = new NDF();

                try
                {
                    //Load ndf file
                    loadedDataset = ndf.LoadNDF(ndfFile);
                    if (loadedDataset != null && loadedDataset.Tables.Count > 0)
                    {
                        StatusLabel.Text = "NDF loaded, tables:" + loadedDataset.Tables.Count.ToString();
                        //Bind data gridview to current table in dataset
                        dataGridView1.DataSource = loadedDataset.Tables[0];
                        label1.Text = loadedDataset.Tables[0].TableName;
                        labelInex.Text = $"{tabIdx + 1}/{loadedDataset.Tables.Count}";
                    }
                    else
                    {
                        StatusLabel.Text = "Warnning, there is no data in the NDF file that just parsed.";
                    }
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.ToString());
                    //Dispose NDF library
                    if (ndf != null)
                    {
                        ndf.CloseNdf();
                        ndf.Dispose();
                    }
                }
            }
        }
        private void buttonNextTable_Click(object sender, EventArgs e)
        {
            if(tabIdx< loadedDataset.Tables.Count-1)
            {
                dataGridView1.DataSource = loadedDataset.Tables[++tabIdx];
                label1.Text = loadedDataset.Tables[tabIdx].TableName;
                labelInex.Text = $"{tabIdx + 1}/{loadedDataset.Tables.Count}";
            }
        }
        private void buttonPreviousTable_Click(object sender, EventArgs e)
        {
            if (tabIdx >0)
            {
                dataGridView1.DataSource = loadedDataset.Tables[--tabIdx];
                label1.Text = loadedDataset.Tables[tabIdx].TableName;
                labelInex.Text = $"{tabIdx + 1}/{loadedDataset.Tables.Count}";
            }
        }
        #endregion



        #region Helper 
        private DataSet CreateSampleDataSet()
        {
            DataSet ds = new DataSet();
            //first datatale in dataset
            DataTable dt = new DataTable("NorthWind");
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Birthday", typeof(DateTime));
            dt.Columns.Add("Height", typeof(float));
            dt.Rows.Add(1, "Iory", DateTime.Parse("2020-08-10"), 172.5);
            dt.Rows.Add(2, "Yvong", DateTime.Parse("1980-03-12"), 162.1);
            dt.Rows.Add(3, "Jesy", DateTime.Parse("1999-09-03"), 167.0);
            ds.Tables.Add(dt);

            //2nd datatable in data set
            //DataTable
            dt = new DataTable("WindData");
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("WindName", typeof(string));
            dt.Columns.Add("Direction", typeof(string));
            dt.Columns.Add("Level", typeof(float));
            dt.Rows.Add(1000, "Trade Wind", "South west", 6);
            dt.Rows.Add(2000, "Sea Wind", "East", 2.5);
            dt.Rows.Add(3000, "Code Wind", "North", 4);
            dt.Rows.Add(4000, "Typhoon", "South east", 10.6);
            dt.Rows.Add(5000, "Dry wind", "West", 3);
            ds.Tables.Add(dt);

            return ds;   
        }
        #endregion

    }
}
