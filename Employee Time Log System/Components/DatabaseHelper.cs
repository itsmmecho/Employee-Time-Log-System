using MySql.Data.MySqlClient;
using System;
using System.Configuration;

public class DatabaseHelper
{
    private static string connectionString = ConfigurationManager.ConnectionStrings["MySQLConnection"].ConnectionString;

    public static MySqlConnection GetConnection()
    {
        return new MySqlConnection(connectionString);
    }
}