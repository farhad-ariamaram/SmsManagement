using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace SmsManagement
{
    class Program
    {
        static string portNo;
        static SerialPort serialPort1;
        static DCDataContext db;
        public static void Main()
        {
            db = new DCDataContext();
            serialPort1 = new SerialPort();

            //List all connected ports
            foreach (string item in SerialPort.GetPortNames())
            {
                Console.WriteLine(item);
            }

            //Get port name from "port.txt" file
            var lines = File.ReadAllLines("port.txt");
            for (var i = 0; i < lines.Length; i += 1)
            {
                portNo = lines[i];
            }

            //Config GSM modem
            serialPort1.PortName = portNo;
            serialPort1.BaudRate = 9600;

            //Put program to listen mode
            while (true)
            {
                TimerCallback();
            }
        }

        private static void TimerCallback()
        {
            //General Try/Catch/Finally
            try
            {

                //Open Port
                try
                {
                    if (!serialPort1.IsOpen)
                    {
                        serialPort1.Open();
                    }
                }
                catch (Exception)
                {
                    LogErrors("Error On Openning Port" + Environment.NewLine);
                    ResetApp();
                }


                //Change read language to UCS2 text
                try
                {
                    serialPort1.WriteLine("AT+CSCS=\"UCS2\"");
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    LogErrors("Error On Change read language to UCS2" + Environment.NewLine);
                    ResetApp();
                }


                //Read received messages
                string output = "";
                try
                {
                    serialPort1.WriteLine("AT" + System.Environment.NewLine);
                    Thread.Sleep(1000);
                    serialPort1.WriteLine("AT+CMGF=1\r" + System.Environment.NewLine);
                    Thread.Sleep(1000);
                    serialPort1.WriteLine("AT+CMGL=\"REC UNREAD\"" + System.Environment.NewLine); //For get only unread messages use => serialPort1.WriteLine("AT+CMGL=\"REC UNREAD\"" + System.Environment.NewLine);
                    Thread.Sleep(4000);
                    output = serialPort1.ReadExisting();
                }
                catch (Exception)
                {
                    LogErrors("Error On Read Received Messages" + Environment.NewLine);
                    ResetApp();
                }


                //Convert received messages to an string array by each line, receivedMessages[i] means line i-th from all received messaages
                string[] receivedMessages = null;
                try
                {
                    receivedMessages = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                }
                catch (Exception)
                {
                    LogErrors("Error On Split Read Message To Array" + Environment.NewLine);
                    ResetApp();
                }

                //Loop for analyze each line of received messages
                for (int i = 0; i < receivedMessages.Length - 1; i++)
                {
                    if (receivedMessages[i].StartsWith("+CMGL"))
                    {
                        string[] message = receivedMessages[i].Split(',');

                        //Read phone number from raw UCS2 text and convert it to normal text
                        string ph = null;
                        string phone = null;
                        try
                        {
                            ph = message[2].Replace("\"", string.Empty);
                            StringBuilder sb2 = new StringBuilder();
                            for (int j = 0; j < ph.Length; j += 4)
                            {
                                sb2.AppendFormat("\\u{0:x4}", ph.Substring(j, 4));
                            }
                            phone = System.Text.RegularExpressions.Regex.Unescape(sb2.ToString()).Replace("\"", string.Empty);
                        }
                        catch (Exception)
                        {
                            LogErrors("Error On Read phone number from raw UCS2 text and convert it to normal text" + Environment.NewLine);
                            ResetApp();
                        }


                        //Read text message from raw UCS2 text and convert it to normal text
                        string msg = null;
                        string Msg = null;
                        try
                        {
                            msg = receivedMessages[i + 1];
                            StringBuilder sb = new StringBuilder();
                            for (int j = 0; j < msg.Length; j += 4)
                            {
                                sb.AppendFormat("\\u{0:x4}", msg.Substring(j, 4));
                            }
                            Msg = System.Text.RegularExpressions.Regex.Unescape(sb.ToString()).Replace("\"", string.Empty);
                        }
                        catch (Exception)
                        {
                            LogErrors("Error On Read text message from raw UCS2 text and convert it to normal text" + Environment.NewLine);
                            ResetApp();
                        }


                        //Insert received message to "Tbl_SmsReceived" of database
                        try
                        {
                            Tbl_SmsReceived smsReceived = new Tbl_SmsReceived()
                            {
                                Phone = phone,
                                Date = DateTime.Now,
                                Message = Msg
                            };
                            db.Tbl_SmsReceiveds.InsertOnSubmit(smsReceived);
                            db.SubmitChanges();
                        }
                        catch (Exception)
                        {
                            LogErrors("Error On Insert received message to Tbl_SmsReceived" + Environment.NewLine);
                            ResetApp();
                        }

                        //Check if received message body equals to 1 (persian or english) 
                        if (Msg.Equals("1") || Msg.Equals("۱"))
                        {
                            //Generating a hash key from sender phone number and current date and time for user registeration ID
                            string HashId = null;
                            try
                            {
                                HashId = CreateMD5(phone + DateTime.Now.Ticks);
                                while (db.Tbl_Links.Any(s => s.Id == HashId))
                                {
                                    HashId = CreateMD5(phone + DateTime.Now.Ticks);
                                }
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Generating a Hash Key From Sender Phone" + Environment.NewLine);
                                ResetApp();
                            }


                            //Create a row to "Tbl_Link" of database for registering user
                            try
                            {
                                Tbl_Link link = new Tbl_Link()
                                {
                                    Id = HashId,
                                    Phone = phone,
                                    ExpireDate = DateTime.Now.AddDays(1)
                                };
                                db.Tbl_Links.InsertOnSubmit(link);
                                db.SubmitChanges();
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Create a row to Tbl_Link of database for registering user" + Environment.NewLine);
                                ResetApp();
                            }


                            //Display Registered user in console
                            try
                            {
                                Console.WriteLine(phone + "    " + DateTime.Now);
                                Console.WriteLine("New user submitted!");
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Display Registered User In Console" + Environment.NewLine);
                                ResetApp();
                            }

                            //Generate link of user registeration
                            string realLink = null;
                            try
                            {
                                realLink = "http://" + ReadAppIp() + "/" + HashId;
                            }
                            catch (Exception)
                            {

                                LogErrors("Error On Generate link of user registeration" + Environment.NewLine);
                                ResetApp();
                            }

                            //Generate shortlink from realLink(generated link of user registeration) and split JSON data by ":"
                            string[] shortedLink = null;
                            string shortlinke = null;
                            try
                            {
                                shortedLink = Shortlink("http://" + ReadShortlinkIp() + "/api/Page/" + realLink).Split(':');
                                shortlinke = (shortedLink[1] + ":" + shortedLink[2]).Replace("\"", string.Empty).Replace("}", string.Empty);
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Generate shortlink" + Environment.NewLine);
                                ResetApp();
                            }

                            //Send short link to current uesr
                            string sentmsg = null;
                            try
                            {
                                serialPort1.WriteLine("AT+CMGF=1");
                                Thread.Sleep(100);
                                serialPort1.WriteLine("AT+CSCS=\"HEX\"");
                                Thread.Sleep(300);
                                serialPort1.WriteLine("AT+CSMP=17,167,0,8");
                                Thread.Sleep(300);
                                serialPort1.WriteLine("AT+CMGS=\"" + phone + "\"");
                                Thread.Sleep(300);
                                sentmsg = "لینک ثبت نام:\n" + "http://" + shortlinke + "\nدر صورت ایجاد هرگونه مشکل می‌توانید سوالات خود را بوسیله پیامک به همین شماره ارسال کنید ";
                                serialPort1.Write(StringToHex(sentmsg) + '\x001a');
                                Thread.Sleep(5000);
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Send Link To Uesr" + Environment.NewLine);
                                ResetApp();
                            }


                            //add sent sms to database
                            try
                            {
                                Tbl_SmsSent tbl_SmsSent = new Tbl_SmsSent();
                                tbl_SmsSent.Date = DateTime.Now;
                                tbl_SmsSent.Phone = phone;
                                tbl_SmsSent.Message = sentmsg;
                                db.Tbl_SmsSents.InsertOnSubmit(tbl_SmsSent);
                                db.SubmitChanges();
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Add Sent Sms To Database" + Environment.NewLine);
                                ResetApp();
                            }
                        }
                    }
                }

                //Delete all read messages (untouch unreads)
                try
                {
                    serialPort1.WriteLine("AT" + System.Environment.NewLine);
                    Thread.Sleep(1000);
                    serialPort1.WriteLine("AT+CMGF=1\r" + System.Environment.NewLine);
                    Thread.Sleep(1000);
                    serialPort1.WriteLine("AT+CMGD=1,3" + System.Environment.NewLine);
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    LogErrors("Error On Delete All Read Messages" + Environment.NewLine);
                }

            }
            //If in each section of try error occurs, this catch section will execute
            catch (Exception e)
            {
                //log occured error in a file named "log.txt"
                LogErrors("Unknow Error" + Environment.NewLine);
                LogErrors(e.ToString() + Environment.NewLine);

                //Display a message in console to inform that an error has occured
                Console.WriteLine("Unknow Error");

                //Restart app
                ResetApp();
            }
        }

        //Generating hash from string => used in generating ID for user registeration
        private static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        //Log errors in file named "log.txt"
        private static void LogErrors(string error)
        {
            using (StreamWriter writetext = new StreamWriter("log.txt", true))
            {
                writetext.WriteLine(error);
            }
        }

        //Convert string to HEX for sending message
        #region StringToHex
        private static string StringToHex(string hexstring)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char t in hexstring)
            {
                if (Convert.ToInt32(t).ToString("X").Length == 1)
                    sb.Append("000" + Convert.ToInt32(t).ToString("X"));
                if (Convert.ToInt32(t).ToString("X").Length == 2)
                    sb.Append("00" + Convert.ToInt32(t).ToString("X"));
                if (Convert.ToInt32(t).ToString("X").Length == 3)
                    sb.Append("0" + Convert.ToInt32(t).ToString("X"));
            }
            return sb.ToString();
        }
        #endregion

        //Convert link to Shortlink
        private static string Shortlink(string url)
        {
            var request = WebRequest.Create(url);
            string text;
            request.ContentType = "application/json; charset=utf-8";
            var response = (HttpWebResponse)request.GetResponse();

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
            }
            return text;
        }

        //Read Shortlink app IP
        private static string ReadShortlinkIp()
        {
            string s = null;
            using (StreamReader sr = File.OpenText("shortlink.txt"))
            {
                s = sr.ReadLine();
            }
            return s;
        }

        //Read Employement Web App IP
        private static string ReadAppIp()
        {
            string s = null;
            using (StreamReader sr = File.OpenText("AppIp.txt"))
            {
                s = sr.ReadLine();
            }
            return s;
        }

        //Reset App
        public static void ResetApp()
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }

            Console.WriteLine("App has been restarted!");

            System.Diagnostics.Process.Start("SmsManagement.exe");

            Environment.Exit(0);
        }

    }
}