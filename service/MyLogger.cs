//namespace ddns.net.service
//{
//    public class MyLogger
//    {
//        private string _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
//        //private string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

//        //private readonly object _lockObj = new object();
//        public async Task Debug(string message)
//        {
//            string logEntry = $"{DateTime.Now:yyyyMMddHHmmss} - {message}";
//            var filePath = Path.Combine(_logFilePath, $"log-debug-{DateTime.Now:yyyyMMdd}.txt");
//            if (!File.Exists(filePath))
//            {
//                File.Create(filePath);
//            }
//            try
//            {
//                await File.AppendAllTextAsync(filePath, logEntry + Environment.NewLine);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error writing to log file: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="message"></param>
//        /// <returns></returns>
//        public async Task Error(string message)
//        {
//            string logEntry = $"{DateTime.Now:yyyyMMddHHmmss} - {message}";
//            var filePath = Path.Combine(_logFilePath, $"log-debug-{DateTime.Now:yyyyMMdd}.txt");
//            if (!File.Exists(filePath))
//            {
//                File.Create(filePath);
//            }
//            try
//            {
//                await File.AppendAllTextAsync(filePath, logEntry + Environment.NewLine);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error writing to log file: {ex.Message}");
//            }
//        }
//    }
//}
