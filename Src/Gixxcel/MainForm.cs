/*  
    Copyright 2013 - 2021 That Shaman - thatshaman.com
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
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Media;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Gixxcel
{

    public partial class MainForm : Form
    {
        // Dataset and table for gridview.

        DataSet stringData = new();
        DataTable stringTable;

        // Default language
        string Language = "English";

        // Store data in Local appdata, we don't want this much stuff in roaming.
        readonly string datafolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gixxcel");

        public MainForm()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            // Set title
            Text = "Gixxcel [" + ProductVersion.ToString() + "]";

            SetFolders();

            Gixxcel.Update.FromPath(datafolder);

            // Load the grid
            RefreshGrid();
        }

        public void SetFolders()
        {
            if (!Directory.Exists(Path.Combine(datafolder, "data"))) Directory.CreateDirectory(Path.Combine(datafolder, "data"));

            string[] languages = new string[] { "English", "Korean", "French", "German", "Spanish", "Chinese" };

            foreach (string language in languages)
            {
                if (!Directory.Exists(Path.Combine(datafolder, "data", language))) Directory.CreateDirectory(Path.Combine(datafolder, "data", language));
            }
        }


        private void Import()
        {
            // Show open file dialog and parse files in a seperate thread.
            OpenFileDialog openFileDialog = new()
            {
                Filter = "String Files (*.raw;*.strs)|*.raw;*.strs|All files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                new Thread(delegate ()
                {
                    ImportFromList(openFileDialog.FileNames);
                }).Start();
            }
        }

        private void ImportFromList(string[] files)
        {
            BeginInvoke((Action)delegate ()
            {
                Text = "Gixxcel - Importing...";
                grid.DataSource = new DataSet();
                toolstrip.Enabled = false;
            });

            // Set the timestamp for new / altered items.
            DateTime timestamp = DateTime.Now;

            // Create data directory if necessary
            if (!Directory.Exists(datafolder + "data")) Directory.CreateDirectory(datafolder + "data");

            double step = (double)(100 / files.Length);

            // Loop through the files
            for (int i = 0; i < files.Length; i++)
            {
                // Read new file, and deserialize current file
                GW2StringFile newFile = new(files[i], timestamp);
                GW2StringFile serializedFile = new();

                // Make sure your new file actually has data
                if (newFile.Items.Count > 0)
                {
                    string serializedPath = Path.Combine(datafolder, "data", newFile.Language.ToString(), Path.GetFileNameWithoutExtension(newFile.Filename) + ".json");


                    // Deserialize old data if availible
                    if (File.Exists(serializedPath))
                    {
                        serializedFile = JsonConvert.DeserializeObject<GW2StringFile>(File.ReadAllText(serializedPath));
                    }

                    // Make sure the language and filename are reset (it tends to forget this one)
                    serializedFile.Language = newFile.Language;
                    serializedFile.Filename = newFile.Filename;

                    // Loop through new items
                    foreach (GW2Entry item in newFile.Items)
                    {
                        // Check if row is already present
                        GW2Entry oldEntry = serializedFile.Items.FirstOrDefault(a => a.row == item.row);

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
                            serializedFile.Items.Add(item);
                        }
                    }


                    File.WriteAllText(serializedPath, JsonConvert.SerializeObject(serializedFile, Formatting.Indented));

                }

            }

            BeginInvoke((Action)delegate ()
            {
                Text = "Gixxcel [" + ProductVersion.ToString() + "]";
                toolstrip.Enabled = true;
            });

            RefreshGridAsync();

        }

        private void RefreshGridAsync()
        {
            new Thread(delegate ()
            {
                RefreshGrid();
            }).Start();
        }

        // Updates the gridview from serialized files
        private void RefreshGrid()
        {
            // Clear previous data
            stringData = new DataSet();

            // Generate columns
            stringTable = stringData.Tables.Add();

            stringTable.Columns.Add("timestamp", typeof(DateTime));
            stringTable.Columns.Add("key");
            stringTable.Columns.Add("value");

            string inputFolder = Path.Combine(datafolder, "data", Language);


            // Make sure the data directory actually exists
            if (Directory.Exists(inputFolder))
            {
                // Get file list
                string[] files = Directory.GetFiles(inputFolder, "*.json");

                // Deserialize files
                foreach (string file in files)
                {
                    BeginInvoke((Action)delegate ()
                    {
                        Text = "Gixxcel - Loading: " + Path.GetFileNameWithoutExtension(file);
                    });

                    GW2StringFile gw2StringFile = JsonConvert.DeserializeObject<GW2StringFile>(File.ReadAllText(file));

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
                        row["value"] = entry.value.Replace("\n", Environment.NewLine) ?? "Emtpy String";

                        stringTable.Rows.Add(row);
                    }

                }
            }

            BeginInvoke((Action)delegate ()
            {
                grid.DataSource = stringTable.DefaultView;
                grid.Columns[0].Width = DeviceDpi + 20;
                grid.Columns[1].Width = DeviceDpi + 20;
                grid.Columns[2].Width = Width - 40 - (DeviceDpi * 3);
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
                grid.Columns[2].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

                grid.Sort(grid.Columns[0], System.ComponentModel.ListSortDirection.Descending);

                FilterGrid(string.Empty);
                filterButton.Checked = false;


                Text = "Gixxcel [" + ProductVersion.ToString() + "]";
            });




        }


        private void Refreshbutton_Click(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void Importbutton_Click(object sender, EventArgs e)
        {
            Import();
        }

        // Escapes character for search funtion
        public static string EscapeSearchString(string value)
        {
            // Create the stringbuilder with a little bit of overhead
            StringBuilder stringBuilder = new(value.Length + 8);

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '*' || character == '%' || character == '[' || character == ']')
                {
                    stringBuilder.Append('[').Append(character).Append(']');
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

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Apply search
        private void Searchbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Get rid of that annoying DING sound                
                e.Handled = false;
                e.SuppressKeyPress = true;

                // Apply search filter
                SearchGrid(searchbox.Text);
            }
        }

        // Filter grid on value
        private void FilterGrid(string Value)
        {
            if (Value.Length == 0)
            {
                stringTable.DefaultView.RowFilter = null;
            }
            else
            {
                stringTable.DefaultView.RowFilter = "value LIKE '%" + EscapeSearchString(Value) + "%'";
            }


            BeginInvoke((Action)delegate ()
            {
                //Text = grid.Rows.Count.ToString() + " strings found.";
            });
        }

        private void FilterButton_Click(object sender, EventArgs e)
        {
            if (!filterButton.Checked)
            {
                // Clear filter
                FilterGrid(string.Empty);
                filterButton.ToolTipText = "Enable filter";
            }
            else
            {
                // Enbable filter
                if (searchbox.Text.Length > 0)
                {
                    FilterGrid(searchbox.Text);
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
        private void SearchGrid(string Value)
        {
            if (grid.CurrentCell != null)
            {
                int start = grid.CurrentCell.RowIndex + 1;
                bool found = false;

                for (int i = start; i < grid.Rows.Count; i++)
                {
                    // If value has been found select cell
                    if (grid[2, i].Value.ToString().Contains(Value, StringComparison.OrdinalIgnoreCase))
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

        private void SearchButton_Click(object sender, EventArgs e)
        {
            SearchGrid(searchbox.Text);
        }

        // STOP - MA - KING - THOSE - DING - NOIS - ES !!!11!!110!!!!
        private void Searchbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
            }

            // Find next with F3
            if (e.KeyCode == Keys.F3)
            {
                SearchGrid(searchbox.Text);
            }
        }

        private void About_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Gixxcel - Copyright 2013 -2021 that_shaman - thatshaman.com", "About");
        }

        private void Datafolder_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(datafolder))
            {
                Process.Start("explorer.exe", datafolder);
            }
        }

        private void EnglishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLanguage("English");
        }

        private void FrenchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLanguage("French");
        }

        private void GermanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLanguage("German");
        }

        private void SpanishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLanguage("Spanish");
        }

        private void KoreanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLanguage("Korean");
        }

        private void ChineseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLanguage("Chinese");
        }

        private void SetLanguage(string language)
        {
            Language = language;
            RefreshGrid();
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            // Find next: F3
            if (e.KeyCode == Keys.F3)
            {
                SearchGrid(searchbox.Text);
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
