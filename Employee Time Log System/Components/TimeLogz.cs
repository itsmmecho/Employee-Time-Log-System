using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace Employee_Time_Log_System.Components
{
    public partial class TimeLogz : Form
    {
        private int currentEmployeeId = -1;

        public TimeLogz()
        {
            InitializeComponent();
        }

        // DB connection class
        public static class DB
        {
            public static MySqlConnection GetConnection()
            {
                string connStr = "server=localhost;user=root;database=TimeLogz;password=;";
                return new MySqlConnection(connStr);
            }
        }

        // Load time logs
        private void LoadLogs()
        {
            using (var conn = DB.GetConnection())
            {
                conn.Open();
                string sql = @"SELECT tl.log_id, e.first_name, e.last_name, tl.clock_in, tl.clock_out, tl.total_hours, tl.date, tl.notes
                               FROM Time_Logs tl
                               JOIN Employees e ON tl.employee_id = e.employee_id
                               ORDER BY tl.date DESC";
                var adapter = new MySqlDataAdapter(sql, conn);
                var table = new DataTable();
                adapter.Fill(table);
                dataGridViewLogs.DataSource = table;
            }
        }

        // Get employee ID by email
        private int? GetEmployeeIdByEmail(string email)
        {
            using (var conn = DB.GetConnection())
            {
                conn.Open();
                string sql = "SELECT employee_id FROM Employees WHERE email = @email";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@email", email);

                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
        }

        // Clock In by email
        private void btnClockIn_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Please enter your email address.");
                return;
            }

            int? employeeId = GetEmployeeIdByEmail(email);
            if (!employeeId.HasValue)
            {
                MessageBox.Show("No employee found with that email address.");
                return;
            }

            using (var conn = DB.GetConnection())
            {
                conn.Open();
                // Check if there's an open clock-in (no clock-out)
                string checkSql = "SELECT log_id FROM Time_Logs WHERE employee_id = @employee_id AND clock_out IS NULL";
                var checkCmd = new MySqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@employee_id", employeeId.Value);
                var existingLog = checkCmd.ExecuteScalar();

                if (existingLog != null)
                {
                    MessageBox.Show("You already have an open clock-in. Please clock out first.");
                    return;
                }

                string sql = "INSERT INTO Time_Logs (employee_id, clock_in, date) VALUES (@employee_id, @clock_in, @date)";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@employee_id", employeeId.Value);
                cmd.Parameters.AddWithValue("@clock_in", DateTime.Now);
                cmd.Parameters.AddWithValue("@date", DateTime.Now.Date);

                try
                {
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Clock-in successful!");
                    LoadLogs();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Clock-in failed: " + ex.Message);
                }
            }
        }

        // Clock Out by email
        private void btnClockOut_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Please enter your email address.");
                return;
            }

            int? employeeId = GetEmployeeIdByEmail(email);
            if (!employeeId.HasValue)
            {
                MessageBox.Show("No employee found with that email address.");
                return;
            }

            using (var conn = DB.GetConnection())
            {
                conn.Open();
                string sql = @"UPDATE Time_Logs 
                               SET clock_out = @clock_out 
                               WHERE employee_id = @employee_id AND clock_out IS NULL 
                               ORDER BY log_id DESC LIMIT 1";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@employee_id", employeeId.Value);
                cmd.Parameters.AddWithValue("@clock_out", DateTime.UtcNow);
                int rows = cmd.ExecuteNonQuery();

                if (rows > 0)
                {
                    // Calculate and update total hours
                    UpdateTotalHours(employeeId.Value, conn);
                    MessageBox.Show("Clock-out successful!");
                }
                else
                {
                    MessageBox.Show("No open clock-in found.");
                }
                LoadLogs();
            }
        }

        // Update total hours after clock-out
        private void UpdateTotalHours(int employeeId, MySqlConnection conn)
        {
            string sql = @"UPDATE Time_Logs 
                           SET total_hours = TIMESTAMPDIFF(MINUTE, clock_in, clock_out)/60.0
                           WHERE employee_id = @employee_id AND clock_out IS NOT NULL AND total_hours IS NULL
                           ORDER BY log_id DESC LIMIT 1";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@employee_id", employeeId);
            cmd.ExecuteNonQuery();
        }

        // Filter by date range
        private void btnFilter_Click(object sender, EventArgs e)
        {
            using (var conn = DB.GetConnection())
            {
                conn.Open();
                string sql = @"SELECT tl.*, e.first_name, e.last_name 
                               FROM Time_Logs tl 
                               JOIN Employees e ON tl.employee_id = e.employee_id
                               WHERE tl.date BETWEEN @from AND @to";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@from", dateTimePickerFrom.Value.Date);
                cmd.Parameters.AddWithValue("@to", dateTimePickerTo.Value.Date);
                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                dataGridViewLogs.DataSource = dt;
            }
        }

        // Refresh
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadLogs();
        }

        // Form load
        private void TimeLogz_Load(object sender, EventArgs e)
        {
            LoadLogs();
        }

        // Test connection
        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            using (var conn = DB.GetConnection())
            {
                try
                {
                    conn.Open();
                    MessageBox.Show("✅ Connected to MySQL!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Connection failed: " + ex.Message);
                }
            }
        }
    }
}