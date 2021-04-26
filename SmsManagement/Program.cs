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

            //Receive port name from user
            Console.WriteLine("Enter Port Name: ");
            portNo = Console.ReadLine();

            //Config GSM modem
            serialPort1.PortName = portNo;
            serialPort1.BaudRate = 9600;

            //Check if port was open, close that
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }

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
                    serialPort1.Open();
                }
                catch (Exception)
                {
                    LogErrors("Error On Openning Port" + Environment.NewLine);
                }


                //Change read language to normal text
                try
                {
                    serialPort1.WriteLine("AT+CSCS=\"IRA\"");
                    Thread.Sleep(1000);
                }
                catch (Exception)
                {
                    LogErrors("Error On Read Received Messages" + Environment.NewLine);
                }


                //Read received messages
                string output = "";
                try
                {
                    serialPort1.WriteLine("AT" + System.Environment.NewLine);
                    Thread.Sleep(1000);
                    serialPort1.WriteLine("AT+CMGF=1\r" + System.Environment.NewLine);
                    Thread.Sleep(1000);
                    serialPort1.WriteLine("AT+CMGL=\"ALL\"" + System.Environment.NewLine); //For get only unread messages use => serialPort1.WriteLine("AT+CMGL=\"REC UNREAD\"" + System.Environment.NewLine);
                    Thread.Sleep(4000);
                    output = serialPort1.ReadExisting();
                }
                catch (Exception)
                {
                    LogErrors("Error On Read Received Messages" + Environment.NewLine);
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
                }


                //Loop for analyze each line of received messages
                for (int i = 0; i < receivedMessages.Length - 1; i++)
                {
                    //Get each received messasge one by one, each received messasge start with "+CMGL:" 
                    if (receivedMessages[i].StartsWith("+CMGL:"))
                    {
                        //Eech received message include 2 lines
                        //First line include message information like phone number, date and etc. that separeted with "," => access with receivedMessages[i] => a sample of first line without parenthesis (+CMGL: 1,"REC UNREAD","+989345677654","","21/04/10,14:42:54+18")
                        //And second line include message body => access with receivedMessages[i+1]

                        //Convert first line to an string array by "," sign, MsgLine[2] means sender phone number
                        string[] MsgLine = null;
                        try
                        {
                            MsgLine = receivedMessages[i].Split(',');
                        }
                        catch (Exception)
                        {
                            LogErrors("Error On Split Message Line To Array" + Environment.NewLine);
                        }

                        //Analyzing received message body and get 10 first chars of it to a string named "receivedSms"
                        string receivedSms = null;
                        try
                        {
                            receivedSms = (receivedMessages[i + 1].Length > 10) ? receivedMessages[i + 1].Substring(0, 10) : receivedMessages[i + 1];
                        }
                        catch (Exception)
                        {
                            LogErrors("Error On Get 10 First Chars Of Message Line" + Environment.NewLine);
                        }

                        //Insert received message to "Tbl_SmsReceived" of database
                        try
                        {
                            Tbl_SmsReceived smsReceived = new Tbl_SmsReceived()
                            {
                                //Remove double quotation from sender phone number
                                Phone = MsgLine[2].Replace("\"", string.Empty),

                                //Use current date and time for message date and time field in database
                                Date = DateTime.Now,

                                //Store receiveed message body in database 
                                Message = receivedSms
                            };
                            db.Tbl_SmsReceiveds.InsertOnSubmit(smsReceived);
                            db.SubmitChanges();
                        }
                        catch (Exception)
                        {
                            LogErrors("Error On Insert received message to Tbl_SmsReceived" + Environment.NewLine);
                        }


                        //Check if received message body equals to 1 (persian or english) 
                        if (receivedSms.Equals("1") || receivedSms.Equals("06F1"))
                        {
                            //Generating a hash key from sender phone number and current date and time for user registeration ID
                            string HashId = null;
                            try
                            {
                                HashId = CreateMD5(MsgLine[2].Replace("\"", string.Empty) + DateTime.Now.Ticks);
                                while (db.Tbl_Links.Any(s => s.Id == HashId))
                                {
                                    HashId = CreateMD5(MsgLine[2].Replace("\"", string.Empty) + DateTime.Now.Ticks);
                                }
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Generating a Hash Key From Sender Phone" + Environment.NewLine);
                            }


                            //Create a row to "Tbl_Link" of database for registering user
                            try
                            {
                                Tbl_Link link = new Tbl_Link()
                                {
                                    //User generated hash for registeration ID
                                    Id = HashId,

                                    //use sender phone number as registeration phone number
                                    Phone = MsgLine[2].Replace("\"", string.Empty),

                                    //Make link deadline to 1 day
                                    ExpireDate = DateTime.Now.AddDays(1)
                                };
                                db.Tbl_Links.InsertOnSubmit(link);
                                db.SubmitChanges();
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Create a row to Tbl_Link of database for registering user" + Environment.NewLine);
                            }


                            //Display Registered user in console
                            try
                            {
                                Console.WriteLine(MsgLine[2] + "    " + MsgLine[4] + MsgLine[5]);
                                Console.WriteLine("New user submitted!");
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Display Registered User In Console" + Environment.NewLine);
                            }


                            //Send short link to current uesr
                            string[] shortedLink = null;
                            try
                            {
                                serialPort1.WriteLine("AT+CSCS=\"HEX\"");
                                Thread.Sleep(300);
                                serialPort1.WriteLine("AT+CSMP=17,167,0,8");
                                Thread.Sleep(300);
                                //Use sender phone number for send short link sms
                                serialPort1.WriteLine("AT+CMGS=\"" + MsgLine[2].Replace("\"", string.Empty) + "\"");
                                Thread.Sleep(300);
                                //Generate link of user registeration and store it in string named realLink
                                string realLink = "http://" + ReadAppIp() + "/" + HashId;
                                //Generate shortlink from realLink(generated link of user registeration) and split JSON data by ":"
                                shortedLink = Shortlink("http://" + ReadShortlinkIp() + "/api/Page/" + realLink).Split(':');
                                //Generate message include short link for user to send as sms
                                string Message = "لینک ثبت نام: " + "\r\n" + "http://" + (shortedLink[1] + ":" + shortedLink[2]).Replace("\"", string.Empty).Replace("}", string.Empty);
                                //Change read language to HEX text (it is necessary for sending sms)
                                serialPort1.Write(StringToHex(Message) + '\x001a');
                                Thread.Sleep(5000);
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Send Link To Uesr" + Environment.NewLine);
                            }


                            //add sent sms to database
                            try
                            {
                                Tbl_SmsSent tbl_SmsSent = new Tbl_SmsSent();
                                tbl_SmsSent.Date = DateTime.Now;
                                tbl_SmsSent.Phone = MsgLine[2].Replace("\"", string.Empty);
                                tbl_SmsSent.Message = (shortedLink[1] + ":" + shortedLink[2]).Replace("\"", string.Empty).Replace("}", string.Empty);
                                db.Tbl_SmsSents.InsertOnSubmit(tbl_SmsSent);
                                db.SubmitChanges();
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Add Sent Sms To Database" + Environment.NewLine);
                            }


                            //Change read language to normal text
                            try
                            {
                                serialPort1.WriteLine("AT+CSCS=\"IRA\"");
                                Thread.Sleep(1000);
                            }
                            catch (Exception)
                            {
                                LogErrors("Error On Change Read Language To Normal Text" + Environment.NewLine);
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
            }
            //In final of executaton try or catch the current port will be closed
            finally
            {
                //Close port after For Loop End
                try
                {
                    if (serialPort1.IsOpen)
                    {
                        serialPort1.Close();
                    }
                }
                catch (Exception)
                {
                    LogErrors("Error On Close Port After For Loop End" + Environment.NewLine);
                }
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
        private static string StringToHex(string hexstring)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char t in hexstring)
            {
                if (Convert.ToInt32(t).ToString("X").Length == 2)
                    sb.Append("00" + Convert.ToInt32(t).ToString("X"));
                if (Convert.ToInt32(t).ToString("X").Length == 3)
                    sb.Append("0" + Convert.ToInt32(t).ToString("X"));
            }
            return sb.ToString();
        }

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
    }
}
