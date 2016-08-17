using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TheDataResourceImporter
{
    public partial class ErrorListForm : Form
    {
        public string SessionId { get; set; }

        public ErrorListForm()
        {
            InitializeComponent();
        }

        public ErrorListForm(string sessionId)
        {
            SessionId = sessionId;
            InitializeComponent();
        }
    }
}
