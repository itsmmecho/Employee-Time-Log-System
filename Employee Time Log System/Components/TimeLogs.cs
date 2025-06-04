using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Employee_Time_Log_System
{
    public partial class TimeLogs : Form
    {
        private int currentEmployeeId;
        private bool isAdmin;
        private string connectionString = "server=localhost;database=employee_time_log_system;uid=root;pwd=;";

        public TimeLogs(int employeeId, bool admin)
        {
            InitializeComponent();
            currentEmployeeId = employeeId;
            isAdmin = admin;

            // Set default date range (current month)
            dateTimePickerFrom.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dateTimePickerTo.Value = DateTime.Now;
        }

        private void TimeLogs_Load(object sender, EventArgs e)
        {
            LoadTimeLogs();
            CheckCurrentStatus();
        }

        private void LoadTimeLogs()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query;

                    if (isAdmin)
                    {
                        query = @"SELECT 
                                    e.employee_id,
                                    CONCAT(e.first_name, ' ', e.last_name) AS employee_name,
                                    tl.clock_in, 
                                    tl.clock_out, 
                                    tl.total_hours, 
                                    tl.date,
                                    tl.notes
                                 FROM Time_Logs tl
                                 JOIN Employees e ON tl.employee_id = e.employee_id
                                 WHERE tl.date BETWEEN @fromDate AND @toDate
                                 ORDER BY tl.date DESC, tl.clock_in DESC";
                    }
                    else
                    {
                        query = @"SELECT 
                                    'You' AS employee_name,
                                    tl.clock_in, 
                                    tl.clock_out, 
                                    tl.total_hours, 
                                    tl.date,
                                    tl.notes
                                 FROM Time_Logs tl
                                 WHERE tl.employee_id = @employeeId
                                 AND tl.date BETWEEN @fromDate AND @toDate
                                 ORDER BY tl.date DESC, tl.clock_in DESC"
                        ;
                    }

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@fromDate", dateTimePickerFrom.Value.Date);
                    cmd.Parameters.AddWithValue("@toDate", dateTimePickerTo.Value.Date);

                    if (!isAdmin)
                    {
                        cmd.Parameters.AddWithValue("@employeeId", currentEmployeeId);
                    }

                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dataGridViewLogs.DataSource = dt;

                    // Format columns
                    if (dataGridViewLogs.Columns.Contains("clock_in"))
                    {
                        dataGridViewLogs.Columns["clock_in"].HeaderText = "Clock In";
                        dataGridViewLogs.Columns["clock_in"].DefaultCellStyle.Format = "g";
                    }

                    if (dataGridViewLogs.Columns.Contains("clock_out"))
                    {
                        dataGridViewLogs.Columns["clock_out"].HeaderText = "Clock Out";
                        dataGridViewLogs.Columns["clock_out"].DefaultCellStyle.Format = "g";
                    }

                    if (dataGridViewLogs.Columns.Contains("date"))
                    {
                        dataGridViewLogs.Columns["date"].HeaderText = "Date";
                        dataGridViewLogs.Columns["date"].DefaultCellStyle.Format = "d";
                    }

                    if (dataGridViewLogs.Columns.Contains("total_hours"))
                    {
                        dataGridViewLogs.Columns["total_hours"].HeaderText = "Hours Worked";
                        dataGridViewLogs.Columns["total_hours"].DefaultCellStyle.Format = "N2";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading time logs: " + ex.Message);
            }
        }

        private void CheckCurrentStatus()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT clock_in, clock_out 
                                    FROM Time_Logs 
                                    WHERE employee_id = @employeeId 
                                    AND date = CURDATE() 
                                    ORDER BY clock_in DESC 
                                    LIMIT 1"
                    ;

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@employeeId", currentEmployeeId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader["clock_in"] != DBNull.Value && reader["clock_out"] == DBNull.Value)
                            {
                                lblStatus.Text = "Current Status: CLOCKED IN";
                                btnClockIn.Enabled = false;
                                btnClockOut.Enabled = true;
                            }
                            else
                            {
                                lblStatus.Text = "Current Status: CLOCKED OUT";
                                btnClockIn.Enabled = true;
                                btnClockOut.Enabled = false;
                            }
                        }
                        else
                        {
                            lblStatus.Text = "Current Status: NOT CLOCKED IN TODAY";
                            btnClockIn.Enabled = true;
                            btnClockOut.Enabled = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking current status: " + ex.Message);
            }
        }

        private void btnClockIn_Click(object sender, EventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Check if already clocked in today without clocking out
                    string checkQuery = @"SELECT log_id FROM Time_Logs 
                                        WHERE employee_id = @employeeId 
                                        AND date = CURDATE() 
                                        AND clock_out IS NULL"
                    ;

                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@employeeId", currentEmployeeId);

                    object result = checkCmd.ExecuteScalar();
                    if (result != null)
                    {
                        MessageBox.Show("You're already clocked in today. Please clock out first.");
                        return;
                    }

                    // Insert new clock-in record
                    string insertQuery = @"INSERT INTO Time_Logs (employee_id, clock_in, date) 
                                          VALUES (@employeeId, NOW(), CURDATE())"
                    ;

                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@employeeId", currentEmployeeId);
                    insertCmd.ExecuteNonQuery();

                    MessageBox.Show("Clocked in successfully at " + DateTime.Now.ToString("t"));
                    CheckCurrentStatus();
                    LoadTimeLogs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error clocking in: " + ex.Message);
            }
        }

        private void btnClockOut_Click(object sender, EventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE Time_Logs 
                                    SET clock_out = NOW(),
                                        notes = @notes
                                    WHERE employee_id = @employeeId 
                                    AND date = CURDATE() 
                                    AND clock_out IS NULL"
                    ;

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@employeeId", currentEmployeeId);
                    cmd.Parameters.AddWithValue("@notes", txtNotes.Text);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Clocked out successfully at " + DateTime.Now.ToString("t"));
                        CheckCurrentStatus();
                        LoadTimeLogs();
                        txtNotes.Clear();
                    }
                    else
                    {
                        MessageBox.Show("No active clock-in found to clock out from.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error clocking out: " + ex.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblCurrentTime.Text = "Current Time: " + DateTime.Now.ToString("F");
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadTimeLogs();
            CheckCurrentStatus();
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            LoadTimeLogs();
        }

        private void TimeLogs_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if user is still clocked in
            if (btnClockOut.Enabled)
            {
                DialogResult result = MessageBox.Show("You're currently clocked in. Are you sure you want to exit?",
                                                    "Warning",
                                                    MessageBoxButtons.YesNo,
                                                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}