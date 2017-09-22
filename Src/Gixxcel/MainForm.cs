/*  
    Copyright 2013 That Shaman - thatshaman.blogspot.com
    This file is part of Gixxcel.

    Gixxcel is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Gixxcel is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Gixxcel.  If not, see <http://www.gnu.org/licenses/>
 */

using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Media;

namespace Gixxcel
{
    public partial class MainForm : Form
    {
        // Dataset and table for gridview.
        DataSet stringData = new DataSet();
        DataTable stringTable;

        // Default language
        string Language = "English";

        // Store data in Local appdata, we don't want this much stuff in roaming.
        string datafolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Gixxcel\";

        public MainForm()
        {
            InitializeComponent();
        }

        private void main_Load(object sender, EventArgs e)
        {
            // Set title
            this.Text = "Gixxcel [" + this.ProductVersion.ToString() + "]";

            // Make sure the data folder exists
            if (!Directory.Exists(datafolder)) Directory.CreateDirectory(datafolder);

            // Load the grid
            refreshGridAsync();
        }

        private void updateFromFolder()
        {
            // Show open file dialog and parse files in a seperate thread.
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "String Files (*.raw;*.strs)|*.raw;*.strs|All files (*.*)|*.*";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Lazy and unsafe threading, what did you expect for an application put together in no time at all?
                //
                // Moving on.
                //
                new Thread(delegate ()
                {
                    updateFromFileList(openFileDialog.FileNames);
                }).Start();
            }
        }

        private void updateFromFileList(string[] files)
        {
            // Set the timestamp for new / altered items.
            DateTime timestamp = DateTime.Now;

            // Make sure your UI will update
            this.BeginInvoke((MethodInvoker)delegate ()
            {
                toolstrip.Enabled = false;
                progress.Maximum = files.Length;
                progress.Value = 0;
                status.Text = "0 / " + files.Length.ToString();
            });

            // Create data directory if necessary
            if (!Directory.Exists(datafolder + "data")) Directory.CreateDirectory(datafolder + "data");

            // Loop through the files
            for (int i = 0; i < files.Length; i++)
            {
                // Thread sometimes likes to go out of sync... dirty fix
                if (i < (files.Length - 1))
                {
                    string output = "";

                    // Update UI
                    this.BeginInvoke((MethodInvoker)delegate ()
                    {
                        if (i < files.Length)
                        {
                            progress.Value = i;
                            status.Text = "Updating: " + (i + 1).ToString() + " / " + files.Length.ToString() + " - " + files[i].ToString();
                        }
                    });

                    // Read new file, and deserialize current file
                    GW2StringFile newFile = new GW2StringFile(files[i], timestamp);
                    GW2StringFile oldFile = new GW2StringFile();

                    // Make sure your new file actually has data
                    if (newFile.Items.Count > 0)
                    {
                        // Create language directory if necessary
                        output = datafolder + @"data\" + newFile.Language.ToString() + @"\";
                        if (!Directory.Exists(output)) Directory.CreateDirectory(output);

                        // Deserialize old data if availible
                        if (File.Exists(output + Path.GetFileNameWithoutExtension(newFile.Filename) + ".gw2"))
                        {
                            using (FileStream filestream = new FileStream(output + Path.GetFileNameWithoutExtension(newFile.Filename) + ".gw2", FileMode.Open))
                            {
                                BinaryFormatter serializer = new BinaryFormatter();
                                oldFile = (GW2StringFile)serializer.Deserialize(filestream);
                            }
                        }

                        // Make sure the language and filename are reset (it tends to forget this one)
                        oldFile.Language = newFile.Language;
                        oldFile.Filename = newFile.Filename;

                        // Loop through new items
                        foreach (GW2Entry item in newFile.Items)
                        {
                            // Check if row is already present
                            GW2Entry oldEntry = oldFile.Items.FirstOrDefault(a => a.row == item.row);

                            if (oldEntry != null)
                            {
                                // Check if row has changed
                                if (oldEntry.value != item.value)
                                {
                                    // Row has changed, update.
                                    oldEntry.value = item.value;
                                    oldEntry.stamp = item.stamp;
                                }
                            }
                            else
                            {
                                // No row found, add it.
                                oldFile.Items.Add(item);
                            }
                        }

                        // Serialize back to file.
                        using (FileStream filestream = new FileStream(output + Path.GetFileNameWithoutExtension(newFile.Filename) + ".gw2", FileMode.Create))
                        {
                            BinaryFormatter serializer = new BinaryFormatter();
                            serializer.Serialize(filestream, oldFile);
                        }

                        oldFile = null;
                        newFile = null;
                    }
                }
            }

            // Restore UI to previous state and reload grid.
            this.BeginInvoke((MethodInvoker)delegate ()
                {
                    toolstrip.Enabled = true;
                    progress.Value = 0;
                    status.Text = "";
                    refreshGridAsync();
                });
        }

        private void refreshGridAsync()
        {
            new Thread(delegate ()
                    {
                        refreshGrid();
                    }).Start();
        }

        // Updates the gridview from serialized files
        private void refreshGrid()
        {
            // Clear previous data
            stringData = null;
            stringData = new DataSet();

            // Generate columns
            stringTable = stringData.Tables.Add();

            stringTable.Columns.Add("timestamp", typeof(DateTime));
            stringTable.Columns.Add("key");
            stringTable.Columns.Add("value");

            string input = datafolder + @"data\" + Language + @"\";

            this.BeginInvoke((MethodInvoker)delegate ()
                {
                    // Clear data grid
                    grid.DataSource = null;

                    // Set status text
                    status.Text = "Loading...";
                    toolstrip.Enabled = false;
                });

            // Make sure the data directory actually exists
            if (Directory.Exists(input))
            {
                // Get file list
                string[] files = Directory.GetFiles(input, "*.gw2");

                // Deserialize files
                foreach (string file in files)
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open))
                    {
                        BinaryFormatter serializer = new BinaryFormatter();
                        GW2StringFile gw2StringFile = (GW2StringFile)serializer.Deserialize(fs);

                        // Add row to data table
                        string key = Path.GetFileNameWithoutExtension(gw2StringFile.Filename) + "_";
                        foreach (GW2Entry entry in gw2StringFile.Items)
                        {
                            DataRow row = stringTable.NewRow();

                            // Timestamp (system format)
                            row["timestamp"] = entry.stamp;

                            // Key = filename_row
                            row["key"] = key + entry.row.ToString("0000");

                            // Make sure to replace UNIX \n with \r\n for copy paste function.
                            row["value"] = entry.value.Replace("\n", Environment.NewLine);

                            stringTable.Rows.Add(row);
                        }

                        gw2StringFile = null;
                    }
                }
            }

            this.BeginInvoke((MethodInvoker)delegate ()
                {
                    status.Text = stringTable.Rows.Count.ToString() + " strings found.";
                    toolstrip.Enabled = true;

                    // Bind datasource to grid
                    grid.DataSource = stringTable.DefaultView;

                    // Set column style
                    grid.Columns[0].Width = DeviceDpi + 20;
                    grid.Columns[1].Width = DeviceDpi + 20;
                    grid.Columns[2].Width = this.Width - 40 - (DeviceDpi * 3);
                    grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
                    grid.Columns[2].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

                    filterGrid(string.Empty);
                    filterButton.Checked = false;
                });
        }


        private void refreshbutton_Click(object sender, EventArgs e)
        {
            refreshGridAsync();
        }

        private void openbutton_Click(object sender, EventArgs e)
        {
            updateFromFolder();
        }

        // Escapes character for search funtion
        public static string EscapeSearchString(string value)
        {
            // Create the stringbuilder with a little bit of overhead
            StringBuilder stringBuilder = new StringBuilder(value.Length + 8);

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '*' || character == '%' || character == '[' || character == ']')
                {
                    stringBuilder.Append("[").Append(character).Append("]");
                }
                else if (character == '\'')
                {
                    stringBuilder.Append("''");
                }
                else
                {
                    stringBuilder.Append(character);
                }
            }

            return stringBuilder.ToString();
        }

        private void exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Apply search
        private void searchbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Get rid of that annoying DING sound                
                e.Handled = false;
                e.SuppressKeyPress = true;

                // Apply search filter
                searchGrid(searchbox.Text);
            }
        }

        // Filter grid on value
        private void filterGrid(string Value)
        {
            if (Value.Length == 0)
            {
                stringTable.DefaultView.RowFilter = null;
            }
            else
            {
                stringTable.DefaultView.RowFilter = "value LIKE '%" + EscapeSearchString(Value) + "%'";
            }

            // Update rowcount
            this.BeginInvoke((MethodInvoker)delegate ()
               {
                   status.Text = grid.Rows.Count.ToString() + " strings found.";
               });
        }

        private void filterButton_Click(object sender, EventArgs e)
        {
            if (!filterButton.Checked)
            {
                // Clear filter
                filterGrid(string.Empty);
                filterButton.ToolTipText = "Enable filter";
            }
            else
            {
                // Enbable filter
                if (searchbox.Text.Length > 0)
                {
                    filterGrid(searchbox.Text);
                    filterButton.ToolTipText = "Disable filter - " + searchbox.Text;
                }
                else
                {
                    // Empty filter... let's not do that
                    filterButton.Checked = false;
                }
            }
        }

        // Search for value in grid
        private void searchGrid(string Value)
        {
            if (grid.CurrentCell != null)
            {
                int start = grid.CurrentCell.RowIndex + 1;
                bool found = false;

                for (int i = start; i < grid.Rows.Count; i++)
                {
                    // If value has been found select cell
                    if (grid[2, i].Value.ToString().IndexOf(Value, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Select cell
                        grid.CurrentCell = grid[2, i];

                        // Scroll to cell -1
                        grid.FirstDisplayedScrollingRowIndex = i - 1;

                        // Stop searching
                        found = true;
                        break;
                    }
                }

                // No result - BEEP!
                if (!found)
                {
                    SystemSounds.Beep.Play();
                }
            }
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            searchGrid(searchbox.Text);
        }

        // STOP - MA - KING - THOSE - DING - NOIS - ES !!!11!!110!!!!
        private void searchbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
            }

            // Find next with F3
            if (e.KeyCode == Keys.F3)
            {
                searchGrid(searchbox.Text);
            }
        }

        private void about_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Gixxcel - Copyright 2013 That Shaman - thatshaman.blogspot.com", "About");
        }


        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLanguage("English");
        }

        private void frenchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLanguage("French");
        }

        private void germanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLanguage("German");
        }

        private void spanishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLanguage("Spanish");
        }

        private void koreanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLanguage("Korean");
        }

        private void setLanguage(string language)
        {
            this.Language = language;
            refreshGridAsync();
        }

        private void grid_KeyDown(object sender, KeyEventArgs e)
        {
            // Find next: F3
            if (e.KeyCode == Keys.F3)
            {
                searchGrid(searchbox.Text);
            }

            // Focus on search box: CTRL+F
            if (e.Control && e.KeyCode == Keys.F)
            {
                searchbox.SelectAll();
                searchbox.Focus();
            }
        }


    }
}
