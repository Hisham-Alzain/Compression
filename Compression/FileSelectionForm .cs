using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Compression
{
    public partial class FileSelectionForm : Form
    {
        public List<string> SelectedFiles { get; private set; } = new List<string>();

        public FileSelectionForm(List<string> availableFiles)
        {
            InitializeComponent();
            foreach (var file in availableFiles)
            {
                clbFiles.Items.Add(file, true);
            }
        }
        private void FileSelectionForm_Load(object sender, EventArgs e)
        {

        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedFiles = clbFiles.CheckedItems.Cast<string>().ToList();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbFiles.Items.Count; i++)
            {
                clbFiles.SetItemChecked(i, true);
            }
        }

        private void btnSelectNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbFiles.Items.Count; i++)
            {
                clbFiles.SetItemChecked(i, false);
            }
        }
    }
}
