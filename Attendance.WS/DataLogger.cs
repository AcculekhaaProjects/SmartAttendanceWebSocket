namespace Attendance.WS
{
    public static class DataLogger
    {
        public static void SaveTextToLog(string Logtext)
        {
            try
            {
                string filepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"ServiceLogs\");
                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }
                filepath = filepath + DateTime.Today.ToString("dd-MM-yyyy") + ".txt";
                if (!File.Exists(filepath))
                {
                    File.Create(filepath).Dispose();
                }
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    string ServiceLog = DateTime.Now.ToString() + ": " + Logtext;
                    sw.WriteLine(ServiceLog);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }
    }
}
