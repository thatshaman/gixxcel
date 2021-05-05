using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Gixxcel
{
    public static class Update
    {
        public static int FromPath(string path)
        {
            int retval = 0;
            bool migrationMessage = false;

            string[] languages = new string[] { "English", "Korean", "French", "German", "Spanish", "Chinese" };

            foreach (string language in languages)
            {

                string[] OldFiles = Directory.GetFiles(Path.Combine(path, "data", language), "*.gw2", SearchOption.TopDirectoryOnly);
                string BackupPath = Path.Combine(path, "backup", language);

                if (OldFiles.Length > 0)
                {
                    if(!migrationMessage)
                    {
                        MessageBox.Show("Old gixxcel files will be updated to the new format. This will take a while..", "Updated needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        migrationMessage = true;
                    }
                    if (!Directory.Exists(BackupPath)) Directory.CreateDirectory(BackupPath);
                }

                foreach (string sourceFile in OldFiles)
                {
                    using FileStream filestream = new(sourceFile, FileMode.Open);
                    BinaryFormatter serializer = new();

#pragma warning disable SYSLIB0011 // Type or member is obsolete
                    GW2StringFile binaryFile = (GW2StringFile)serializer.Deserialize(filestream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete

                    string outputPath = Path.Combine(path, "data", language, Path.GetFileNameWithoutExtension(sourceFile) + ".json");
                    File.WriteAllText(outputPath, JsonConvert.SerializeObject(binaryFile, Formatting.Indented));

                    filestream.Close();
                    File.Move(sourceFile, Path.Combine(BackupPath, Path.GetFileName(sourceFile)));

                    retval++;
                }
            }

            if (retval > 0)
            {
                MessageBox.Show(retval.ToString() + " succesfully files updated", "Update complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return retval;
        }
    }
}
