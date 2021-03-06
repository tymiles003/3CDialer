﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace _cdialerclient
{
    /**
     *This class will handle all server socket related stuff.
     * It will contain functions like login,get list, update, refesh, etc.
     **/
    public class ServerHandler
    {
        protected ServerSocket server;
        protected SoftPhone softPhone;
        internal string userCampaign;
        internal List<Call> calls;
        internal Call CurrentCall;
        private string token;
        private string userid;
        internal string username;
        internal bool endReached;
        public string error = "";
        public popurl popURL;
        internal string campaign_script = "Sample script here...";
        internal uint SERVER_RETRIES = 3; //Default number is 3.
        public int wrapup;
        
        public ServerHandler() {
            server = new ServerSocket();        
        }
        public bool Login(string username, string password)
        {
            bool logged = false;
            LoginXML logins = new LoginXML();
            logins.Method = "Login";
            error = "Wrong username or password.";
            logins.Session = new session() { Userid = string.Empty, Token= string.Empty};
            logins.Args = new args() { Username = username, Password = password};
            if(server.GET("L:" + GetXMLString(logins)))
            {
                //get login response instance
                if (server.responseXml == "" || server.responseXml == null) 
                { 
                    return logged; 
                }
                else if (server.responseXml.IndexOf("<error msg=") > -1)
                {
                    Error err = GetObjectfromXML(server.responseXml, typeof(Error)) as Error;
                    error = err.Message;
                    Logger.Log("In login:" + error);
                    return logged;
                }
                LoginResponse response = (LoginResponse) GetObjectfromXML(server.responseXml,typeof(LoginResponse));
                if (response.args.Response == true)
                {
                    userid = response.args.Userid;
                    token = response.args.Token;
                    userCampaign = response.args.Campaign;
                    this.username = response.args.Name;
                    campaign_script = response.args.Script;
                    logged = true;
                    popURL = response.args.URL;
                    wrapup = response.args.WrapUp;
                    softPhone = new SoftPhone();
                }
            }
            else
            {
                if (server.netError)
                {
                    error = "Network error or check IP settings";
                    return logged;
                }
                else
                {
                    error = "Unknown error occured. Check event log.";
                    return logged;
                }
            }
            return logged;
        }
        public bool Logout()
        {
            LogoutReq lq = new LogoutReq() { Session = new session() { Userid = this.userid, Token = this.token } };
            if (server.GET("O:" + GetXMLString(lq)))
            {
                return true;
            }
            else
            {
                Logger.Log("Socket client error: failed to initiate logout.");
                error = "Failed logout";
                return false;
            }
        }

        //request call list
        public bool RequestCallList()
        {
            bool requested = false;
            ListRequest lr = new ListRequest();
            lr.Method = "GetCallList";
            lr.Session = new session() { Userid = this.userid, Token = this.token };
            lr.Args = new ReqArgs() { Ext = ClientSettingsHandler.GetSettings().Extension, Campaign = this.userCampaign};
            if(server.GET("C:" + GetXMLString(lr)))
            {
                List<Call> callsvar = null;
                try
                {
                    callsvar = ((CallListXML)GetObjectfromXML(server.responseXml, typeof(CallListXML))).Args.Calls.ToList<Call>();
                }
                catch (ArgumentNullException ane)
                {
                    Logger.Log(ane.Message + " arg null exception");
                    return false;
                }
                catch(Exception e)
                {
                    Logger.Log(e.Message + " Exception");
                }
                if (callsvar != null)
                {
                    if (calls != null)
                    {
                        calls.Clear();
                        Logger.Log("refetched list....");
                    }
                    calls = callsvar;
                    //set current call;
                    CurrentCall = calls[0];
                    //acknowledge receipt deprecated.
                    //server.GET("AL:" + GetXMLString(new AckRequest() { Session = new session() { Userid = this.userid, Token = this.token } }));
                    requested = true;
                    endReached = false;
                }
            }
            return requested;
        }

        //general funcs
        private string GetXMLString<T>(T arg)
        {
            StringWriter sw = new StringWriter();
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(arg.GetType());
            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
            ns.Add("", "");
            xs.Serialize(sw, arg, ns);
            System.Diagnostics.Trace.WriteLine(sw.ToString());
            return sw.ToString();
        }
        private Object GetObjectfromXML(string xmlString, Type t)
        {
            Object obj = null;
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            xmlString =  xmlString.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(lineSeparator, string.Empty).Replace(paragraphSeparator, string.Empty);
            try
            {
                StringReader sw = new StringReader(xmlString);
                System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(t);
                obj = xs.Deserialize(sw);
            }
            catch (Exception e)
            {
                Logger.Log("Deserialization failed. Server might have closed. " + e.Message);
            }
            return obj;
        }

        internal string SP_GetCallStatus()
        {
            if (softPhone.GET("GS"))
            {
                return softPhone.response;
            }
            else
            {
                softPhone.GET("E");
                return softPhone.response;
            }
        }

        internal bool SP_GetIfConnected()
        {
            if (softPhone.GET("GWC"))
            {
                return softPhone.response.ToLower() == "true" ? true : false;
            }
            return false;
        }

        internal void MarkDialed()
        {
            while (true)
            {
                string str = "UL:" + GetXMLString(new UpdateXML()
                {
                    Session = new session()
                    {
                        Userid = this.userid,
                        Token = this.token
                    },
                    CallId = CurrentCall.id.ToString(),
                    Status = this.SP_GetIfConnected()
                });
                if (server.GET(str)) break;
            }
            //if (server.responseXml != "OK")
            //{
            //    //retry
            //}
            if (!endReached)
            {
                calls.Remove(CurrentCall);
                CurrentCall = calls[0];
                if (calls.Count == 1) { endReached = true; calls.Clear(); }
            }
        }

        private DateTime SP_GetDailTime()
        {
            while (true)
            {
                if (softPhone.GET("GD")) break;
            }
            return DateTime.Parse(softPhone.response);
        }

        private DateTime SP_GetEndTime()
        {
            while (true)
            {
                if (softPhone.GET("GE")) break;
            }
            return DateTime.Parse(softPhone.response);
        }

        internal void SP_Call()
        {
            softPhone.GET("C" + CurrentCall.tel1 + ":" + CurrentCall.tel2);
        }

        internal void MarkDialed(string callNotes)
        {
            while (true)
            {
                string str = "UL:" + GetXMLString(new UpdateXML()
                {
                    Session = new session()
                    {
                        Userid = this.userid,
                        Token = this.token
                    },
                    CallId = CurrentCall.id.ToString(),
                    Status = this.SP_GetIfConnected(),
                    Notes = callNotes
                });
                if (server.GET(str)) break;
            }
            //if (server.responseXml != "OK")
            //{
            //    //retry
            //}
            if (!endReached)
            {
                calls.Remove(CurrentCall);
                if (calls.Count < 1)
                {
                    endReached = true;
                    CurrentCall = null;
                    return;
                }
                CurrentCall = calls[0];
            }
        }

        internal void SP_EndCall()
        {
            while (true)
            {
                if (softPhone.GET("D")) break;
            }
        }
    }

    [XmlRoot("error")]
    public class Error
    {
        [XmlAttribute("msg")]
        public string Message{get; set;}
    }
}
