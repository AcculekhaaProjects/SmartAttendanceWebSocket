using Attendance.BusinessLogic.Interfaces;
using Attendance.Library;
using Attendance.Models.ApiModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Threading.Tasks;

namespace Attendance.WS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MachineController : ControllerBase
    {
        private readonly IMachineProcessor _iMachineProcessor;
        public MachineController(IMachineProcessor machineProcessor)
        {
            _iMachineProcessor = machineProcessor;
        }
        [HttpPost("ExecuteCmd")]
        public async Task<IActionResult> ExecuteCmd(string TXN_NAME, string DATA)
        {
            int startTime;
            int endTime;
            int runTime;
            ActionResponseInfo stat = new ActionResponseInfo();
            MyWebSocketHandler myWebSocketHandler = new MyWebSocketHandler(_iMachineProcessor);
            try
            {
                Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(DATA) ?? new Dictionary<string, string>();

                switch (TXN_NAME)
                {
                    case "getuserlist":
                        myWebSocketHandler.getuserlist(dictionary["DeviceSlNo"].ToString());
                        Thread.Sleep(10000);
                        stat.Status = "OK";
                        stat.Msg = "UserCount:" + myWebSocketHandler.userlistindex.ToString();
                        break;
                    case "getuserinfo":
                        myWebSocketHandler.getuserinfo(dictionary["DeviceSlNo"].ToString(), Convert.ToInt32(dictionary["enrollid"]), Convert.ToInt32(dictionary["backupnum"]));
                        Thread.Sleep(10000);
                        stat.Status = myWebSocketHandler.getuserinfoflag ? "OK" : "Failed";
                        stat.Msg = myWebSocketHandler.getuserinfoflag ? "Get User Info OK" : "Get User Info Failed";
                        stat.RowData = JsonConvert.SerializeObject(myWebSocketHandler.tmpuserinfo).Split(',').ToList();
                        break;
                    case "getallusers":
                        myWebSocketHandler.getallusers(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "setpwd":
                        myWebSocketHandler.setuserinfo(dictionary["DeviceSlNo"].ToString(), 1, "AAAA", 10, 0, 123456, null);
                        break;
                    case "setcard":
                        myWebSocketHandler.setuserinfo(dictionary["DeviceSlNo"].ToString(), 1, "邹春庆", 11, 0, 2352253, null);
                        break;
                    case "setfp":
                        //thbio 1.0 
                        // myWebSocketHandler.setuserinfo(dictionary["DeviceSlNo"].ToString(), 1,"邹春庆",0, 0,0,"cb09194bbe53ba6845befe6ecd9d0272ab7b0c76bb77147a97ef9c7eb5e1a482abc7d90de8936707d2c5ce00c26c0f02139efc0720b87c0e2f48bc02cefeec0551199a06d3abe862d047a966546b7b3ae496f198f4943796db30b166f91e5ff1177be39676ecaf2793c8a696e94f601b95dc6b4b5230e3c0cd336de4a438ce82d5d6a61f197090ed0b7ffeee4b09022f(100)0100320001(3)83604024cb091a4ea6578a6a05be9e6e0d9d62722a7bcc763a77d47a94effc7e34e1c48228c7a90d0897671ed246eb02006d66040198ec1e41b8dc03ec58c4646ede3c2a9339dfa88da91ee8774389e6d6676ba08c8c70509f8cbf1a9438f3fd719f3200065bb97eeaed6e07e3d4641bc40b6a27735056b41324528bca33503745285aafd3ffa427d93bb6ff55ee9b647130022f4be30c6f(108)");
                        //thbio 3.0
                        myWebSocketHandler.setuserinfo(dictionary["DeviceSlNo"].ToString(), 1, "chingzou", 0, 0, 0, "c52c5b0081885a2107c5f903138a3276f83bf6c21689ca71ff3607417a876aa50fc7b343e88592b9073427040186a2cd077224c40987aae5007423838e8a9b2917ffd48207870b4de8b036857886f381300b96c80b86fba5e0ac47c9938af3a217ffca02db8543fdea3448872188a409e03027c5998a740e1705c983e3858c21dbb069482587ac2ddeaa28c7278ad429f8761a431486643ddc24491422873c45e4e839882e8ad499ff740843a8888cf11f47a7063688f501e0322845d0856d26e6a8db895d85e516cf1d2acd5386153ddf62384acc853d56ff7ecc5b39897d65d8742784368a6569f8761783ab8b1d7517ffca45d0856d99ff3f4e184a8795dde073f7034687fde9e8b5f702b7897e250fffb9820485cc7dcce87ad339866501d8a05a5443870d66e0ac3a4547892e3ef835fcc2bd87ee3d1fffb74236850515efcf36cdb384054ef9325c8939846d4a2fcd790a26843db957fff707c3874e3127ffa7c4(128)616695418a367a229651f85828c645836ce74336432579354944444f52f681f2233a71f3223943226fcf36845949624897532292423532a4f4113f49f7ff4f628947543361f0ffff384478324fa4f5662f232cf4f6629f244223f32228(19)1e5b721533d2cb81d54d52d5e9714332111125957d233370a172d074350486626e683525a871851c241192a4013311690392802b006212a582115114eb8123172548e4810d21214056a3d46522d07b302b2d017032822849026101b2313721124141f87615725290916631108730342f0021c1632f1a1441b8718e420150d6c65c66616266a0fc326461538083122100dee68b2d13758e409a2461508e92ce220036c2714c76746436700f213030a2700f104a03dc90100d120a0915220f23160c18061917050e03271b1a11141e07292b022600fe14");
                        break;
                    case "setphoto":
                        /////////////////////you can load jpg from file or database 
                        int enrolid = 1;
                        string path = @"C:\\EnrollPhoto\" + "LF" + enrolid.ToString().PadLeft(8, '0') + ".jpg";
                        bool bRet = System.IO.File.Exists(path);
                        if (bRet)
                        {
                            byte[] rawjpg = System.IO.File.ReadAllBytes(path);
                            string base64string = Convert.ToBase64String(rawjpg);
                            myWebSocketHandler.setuserinfo(dictionary["DeviceSlNo"].ToString(), 1, "chingzou", 50, 0, 0, base64string);
                        }
                        else
                        {
                            //Console.WriteLine("Picture does not exist");
                        }
                        break;
                    case "getname":
                        myWebSocketHandler.getusername(dictionary["DeviceSlNo"].ToString(), Convert.ToInt32(dictionary["enrollid"]));
                        break;
                    case "setname":
                        myWebSocketHandler.setusername(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "deleteuser":
                        myWebSocketHandler.deleteuser(dictionary["DeviceSlNo"].ToString(), Convert.ToInt32(dictionary["enrollid"]), Convert.ToInt32(dictionary["backupnum"])); //0~9 :fp  10 pwd ;11: card ;12: all fp ;13 :all(fp pwd card name)
                        break;
                    case "enableuser":
                        myWebSocketHandler.enableuser(dictionary["DeviceSlNo"].ToString(), 1, true); //1 for enalbe the user
                        break;
                    case "disableuser":
                        myWebSocketHandler.enableuser(dictionary["DeviceSlNo"].ToString(), 1, false); // 0 for disable the user
                        break;
                    case "cleanuser":
                        myWebSocketHandler.cleanuser(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "getnewlog":
                        myWebSocketHandler.getnewlog(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "getalllog":
                        myWebSocketHandler.getalllog(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "cleanlog":
                        myWebSocketHandler.cleanlog(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "initsys":
                        myWebSocketHandler.initsys(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "cleanadmin":
                        myWebSocketHandler.cleanadmin(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "setdevinfo":
                        myWebSocketHandler.setdevinfo(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "getdevinfo":
                        myWebSocketHandler.getdevinfo(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "opendoor":
                        myWebSocketHandler.opendoor(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "setdevlock":
                        myWebSocketHandler.setdevlock(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "getdevlock":
                        myWebSocketHandler.getdevlock(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "setuserlock":
                        myWebSocketHandler.setuserlock(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "getuserlock":
                        myWebSocketHandler.getuserlock(dictionary["DeviceSlNo"].ToString(), 2);
                        break;
                    case "deleteuserlock":
                        myWebSocketHandler.deleteuserlock(dictionary["DeviceSlNo"].ToString(), 1);
                        break;
                    case "cleanuserlock":
                        myWebSocketHandler.cleanuserlock(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "reboot":
                        myWebSocketHandler.reboot(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "settime":
                        myWebSocketHandler.settime(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "disabledevice":
                        myWebSocketHandler.disabledevice(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "enabledevice":
                        myWebSocketHandler.enabledevice(dictionary["DeviceSlNo"].ToString());
                        break;
                    case "adduser":
                        ////////0~9 fp  10 pwd 11 card 50:aiface
                        myWebSocketHandler.adduser(dictionary["DeviceSlNo"].ToString(), 888, 50, 0, "test");
                        break;

                    case "setprofile":
                        myWebSocketHandler.setuserprofile(dictionary["DeviceSlNo"].ToString(), 1, "1,3,5"); //can use \\n to wrap max is 4096,but display is 110
                        break;
                    case "getprofile":
                        myWebSocketHandler.getuserprofile(dictionary["DeviceSlNo"].ToString(), 1); //0 is notice >0 is the users
                        break;
                    ////////////////////////////////////////////for debug
                    //case "uploadalluser":
                    //    DataTable dbEnrollTble;
                    //    DataRow dbRow;
                    //    DataSet dsChange;
                    //    bool doubleid = false;

                    //    dbEnrollTble = dsEnrolls.Tables[0];

                    //    int startalltime = System.Environment.TickCount;
                    //    int errorcount = 0;
                    //    myWebSocketHandler.disablereturn = false;
                    //    myWebSocketHandler.disabledevice(dictionary["DeviceSlNo"].ToString());
                    //    while (!myWebSocketHandler.disablereturn) ;
                    //    myWebSocketHandler.getuserlistreturn = false;
                    //    myWebSocketHandler.userlistindex = 0;
                    //    myWebSocketHandler.getuserlist(dictionary["DeviceSlNo"].ToString());
                    //    while (!myWebSocketHandler.getuserlistreturn) ;
                    //    int a = 0;
                    //    while (a < myWebSocketHandler.userlistindex)
                    //    {
                    //        errorcount = 0;
                    //        getagain:
                    //        myWebSocketHandler.getuserinfoflag = false;
                    //        //Console.WriteLine("index:" + a + "==>getuser:" + myWebSocketHandler.str_userlist[a].enrollid + ";backupnum:" + myWebSocketHandler.str_userlist[a].backupnum);
                    //        startTime = System.Environment.TickCount;
                    //        myWebSocketHandler.getuserinfo(dictionary["DeviceSlNo"].ToString(), myWebSocketHandler.str_userlist[a].enrollid, myWebSocketHandler.str_userlist[a].backupnum);
                    //        while (!myWebSocketHandler.getuserinfoflag && System.Environment.TickCount - startTime < 10000) ;
                    //        if (System.Environment.TickCount - startTime >= 10000)
                    //        {
                    //            if (errorcount > 3)
                    //            {
                    //                //Console.WriteLine("error!!!!!!!!!!!!!!!!!!!");
                    //                goto getend;
                    //            }
                    //            else
                    //                goto getagain;
                    //        }

                    //        endTime = System.Environment.TickCount;
                    //        runTime = endTime - startTime;
                    //        //Console.WriteLine("time=" + runTime + "ms");
                    //        ////////////////////////////save to database
                    //        doubleid = false;
                    //        foreach (DataRow dbRow1 in dbEnrollTble.Rows)
                    //        {
                    //            if ((int)dbRow1["EnrollNumber"] == myWebSocketHandler.tmpuserinfo.enrollid)
                    //            {
                    //                if ((int)dbRow1["FingerNumber"] == myWebSocketHandler.tmpuserinfo.backupnum)
                    //                {
                    //                    doubleid = true;
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //        if (!doubleid)
                    //        {
                    //            dbRow = dbEnrollTble.NewRow();
                    //            dbRow["EnrollNumber"] = myWebSocketHandler.tmpuserinfo.enrollid;
                    //            dbRow["FingerNumber"] = myWebSocketHandler.tmpuserinfo.backupnum;
                    //            dbRow["Privilige"] = myWebSocketHandler.tmpuserinfo.admin;
                    //            dbRow["Username"] = myWebSocketHandler.tmpuserinfo.name;
                    //            if (myWebSocketHandler.tmpuserinfo.backupnum >= 20 && myWebSocketHandler.tmpuserinfo.backupnum < 28) //face
                    //            {
                    //                dbRow["Password1"] = 0;
                    //                dbRow["FPdata"] = myWebSocketHandler.tmpuserinfo.fpdata;
                    //            }
                    //            else if (myWebSocketHandler.tmpuserinfo.backupnum == 10 && myWebSocketHandler.tmpuserinfo.backupnum == 11) //card or pwd
                    //            {
                    //                dbRow["Password1"] = (double)myWebSocketHandler.tmpuserinfo.password;
                    //                dbRow["FPdata"] = "";
                    //            }
                    //            else if (myWebSocketHandler.tmpuserinfo.backupnum == 50) //50 is aiface photo base 64
                    //            {
                    //                dbRow["Password1"] = 0;
                    //                dbRow["FPdata"] = myWebSocketHandler.tmpuserinfo.fpdata;
                    //                /////////////////////////decode base 64
                    //                //Console.WriteLine("StartingEnrollPhoto");
                    //                byte[] rawjpg = Convert.FromBase64String(myWebSocketHandler.tmpuserinfo.fpdata);
                    //                System.IO.File.WriteAllBytes(@"C:\\EnrollPhoto\" + "LF" + myWebSocketHandler.tmpuserinfo.enrollid.ToString().PadLeft(8, '0') + ".jpg", rawjpg);
                    //            }
                    //            else  // 0~9 //fingerprint  // 
                    //            {
                    //                dbRow["Password1"] = 0;
                    //                dbRow["FPdata"] = myWebSocketHandler.tmpuserinfo.fpdata;

                    //            }
                    //            dbEnrollTble.Rows.Add(dbRow);
                    //        }
                    //        a++;
                    //        ////////////////////////////////
                    //    }
                    //    myWebSocketHandler.enablereturn = false;
                    //    myWebSocketHandler.enabledevice(dictionary["DeviceSlNo"].ToString());
                    //    while (!myWebSocketHandler.enablereturn) ;
                    //    getend:
                    //    dsChange = dsEnrolls.GetChanges();
                    //    EnrollData.DataModule.SaveEnrolls(dsEnrolls);
                    //    //Console.WriteLine("alltimes=" + (System.Environment.TickCount - startalltime) + "ms");
                    //    break;
                    //case "downloadalluser":
                    //    int vEnrollNumber;
                    //    int vFingerNumber;
                    //    int vPrivilege;
                    //    double glngEnrollPData;
                    //    string username;
                    //    string fpdata;

                    //    errorcount = 0;
                    //    startalltime = System.Environment.TickCount;
                    //    dbEnrollTble = dsEnrolls.Tables[0];
                    //    if (dbEnrollTble.Rows.Count == 0)
                    //    {
                    //        //Console.WriteLine("no data in database!");
                    //        break;
                    //    }
                    //    //Console.WriteLine("allcount=" + dbEnrollTble.Rows.Count);
                    //    myWebSocketHandler.disablereturn = false;
                    //    myWebSocketHandler.disabledevice(dictionary["DeviceSlNo"].ToString());
                    //    while (!myWebSocketHandler.disablereturn) ;
                    //    a = 1;
                    //    foreach (DataRow dbRow2 in dbEnrollTble.Rows)
                    //    {
                    //        errorcount = 0;
                    //        vEnrollNumber = (int)dbRow2["EnrollNumber"];
                    //        vFingerNumber = (int)dbRow2["FingerNumber"];
                    //        vPrivilege = (int)dbRow2["Privilige"];
                    //        username = (string)dbRow2["Username"];
                    //        if (vFingerNumber == 10 || vFingerNumber == 11) //is card or password
                    //        {
                    //            glngEnrollPData = (double)dbRow2["Password1"];
                    //            fpdata = "";
                    //        }
                    //        else //is fp or face
                    //        {
                    //            glngEnrollPData = 0;
                    //            fpdata = (string)dbRow2["FPdata"];
                    //        }
                    //        sendagain:
                    //        //Console.WriteLine("index:" + a + ":enrollid:" + vEnrollNumber + ",backnum=" + vFingerNumber + ",name=" + username);
                    //        myWebSocketHandler.setuserinfoflag = false;
                    //        startTime = System.Environment.TickCount;
                    //        myWebSocketHandler.setuserinfo(dictionary["DeviceSlNo"].ToString(), vEnrollNumber, username, vFingerNumber, vPrivilege, glngEnrollPData, fpdata);
                    //        while (!myWebSocketHandler.setuserinfoflag && System.Environment.TickCount - startTime < 10000) ;
                    //        if (System.Environment.TickCount - startTime >= 10000)
                    //        {
                    //            errorcount++;
                    //            if (errorcount > 3)
                    //            {
                    //                //Console.WriteLine("error!!!!!!!!!!!!!!!!!!!");
                    //                goto sendend;
                    //            }
                    //            else
                    //                goto sendagain;
                    //        }
                    //        endTime = System.Environment.TickCount;
                    //        runTime = endTime - startTime;
                    //        //Console.WriteLine("time=" + runTime + "ms");
                    //        a++;

                    //    }
                    //    myWebSocketHandler.enablereturn = false;
                    //    myWebSocketHandler.enabledevice(dictionary["DeviceSlNo"].ToString());
                    //    while (!myWebSocketHandler.enablereturn) ;
                    //    sendend:
                    //    //Console.WriteLine("alltimes=" + (System.Environment.TickCount - startalltime) + "ms");
                    //    break;
                    case "getholiday":
                        myWebSocketHandler.getholiday(dictionary["DeviceSlNo"].ToString(), 0);
                        break;
                    case "setholiday":
                        int[] accessidbuf = new int[100];
                        int c = 0;
                        int b = 0;

                        for (b = 0; b < 30; b++) //can accept 3000 id
                        {
                            for (c = 0; c < 100; c++)
                            {
                                accessidbuf[c] = b * 100 + c + 1;
                            }
                            myWebSocketHandler.setholidayflag = false;
                            if (b == 0)
                                myWebSocketHandler.setholiday(dictionary["DeviceSlNo"].ToString(), 0, true, 100, accessidbuf); //start frame
                            else
                                myWebSocketHandler.setholiday(dictionary["DeviceSlNo"].ToString(), 0, false, 100, accessidbuf); //others
                            while (!myWebSocketHandler.setholidayflag) ;  //waiting for return;
                        }
                        //Console.WriteLine("setholiday ok");
                        break;
                    case "deleteholiday":
                        myWebSocketHandler.deleteholiday(dictionary["DeviceSlNo"].ToString(), 0);
                        break;
                    case "cleanholiday":
                        myWebSocketHandler.cleanholiday(dictionary["DeviceSlNo"].ToString());
                        break;
                    //case "cleandb":
                    //    EnrollData.DataModule.DeleteDB();
                    //    //Console.WriteLine("delete db ok");
                    //    break;
                    default:
                        //Console.WriteLine("can not find this command!");
                        break;
                }
                return Ok(stat);
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}
