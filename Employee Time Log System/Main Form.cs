using Employee_Time_Log_System.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Employee_Time_Log_System
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            LoadFormInPanel(new Dashboard());

        }
        private void LoadFormInPanel(Form form)
        {
            // Clear previous controls
            MainPanel.Controls.Clear();

            form.TopLevel = false;  // Important: makes the form a child control
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;

            MainPanel.Controls.Add(form);
            form.Show();
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            LoadFormInPanel(new Dashboard());
        }

        private void btnTimeLogs_Click(object sender, EventArgs e)
        { 
            LoadFormInPanel(new TimeLogz());
        }

        private void btnEmployees_Click(object sender, EventArgs e)
        {
            LoadFormInPanel(new Employees());
        }


        private void btnSettings_Click(object sender, EventArgs e)
        {
            LoadFormInPanel(new Settings());
        }
    }
}
