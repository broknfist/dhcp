/*
 * Created by SharpDevelop.
 * User: zsolt
 * Date: 2019. 01. 14.
 * Time: 17:29
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace dhcp
{
	class Program
	{
		public static List<string> excluded=new List<string>();
		public static Dictionary<string,string> reserved=new Dictionary<string,string>();
		public static Dictionary<string,string> serverActual=new Dictionary<string,string>();
		public static List<TestCommand> test=new List<TestCommand>();
		public struct TestCommand{
			public string testCommand;
			public string testParam;
		}
		public struct MAC_IP_Pairs{
			public string MAC;
			public string IP;
		}
		public static void Main(string[] args)
		{
			FileOpenAndRead();
			foreach (var i in test) {
				if (i.testCommand=="request") {
					Request(i.testParam);
				}
				if (i.testCommand=="release") {
					Release(i.testParam);
				}
			}
			writeActualDHCP();
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
		
		public static void writeActualDHCP(){
			FileStream fs=new FileStream(@"dhcp_kesz.csv",FileMode.Create);
			StreamWriter sw=new StreamWriter(fs,Encoding.Default);
			foreach (var i in serverActual) {
				sw.WriteLine("{0};{1}",i.Key,i.Value);
			}
			sw.Close();
		}
		
		public static void FileOpenAndRead(){
			//***********	excluded.csv
			FileStream fsEx=new FileStream(@"excluded.csv",FileMode.Open);
			StreamReader srEx=new StreamReader(fsEx,Encoding.Default);
			//string sor="";
			while (!srEx.EndOfStream) {
				/*
				sor=srEx.ReadLine();
				excluded.Add(sor);
				*/
				excluded.Add(srEx.ReadLine());
			}
			srEx.Close();
			Console.WriteLine("Excluded IP's (Nem kiosztható IP-k):");
			foreach (var i in excluded) {
				Console.WriteLine(i);
			}
			//**********	reserved.csv
			FileStream fsRe=new FileStream(@"reserved.csv",FileMode.Open);
			StreamReader srRe=new StreamReader(fsRe,Encoding.Default);
			MAC_IP_Pairs elem=new MAC_IP_Pairs();
			string[] rowArray;
			while (!srRe.EndOfStream) {
				rowArray=srRe.ReadLine().Split(';');
				/*
				elem.MAC=rowArray[0];
				elem.IP=rowArray[1];
				reserved.Add(elem.MAC,elem.IP);
				*/
				reserved.Add(rowArray[0],rowArray[1]);
			}
			srRe.Close();
			Console.WriteLine("Reserved IP's (Fenntartott IP-k):");
			foreach (var i in reserved) {
				Console.WriteLine("{0} {1}",i.Key,i.Value);
			}
			//***********	dhcp.csv	******	szerver aktuális állapota
			FileStream fsDHCP=new FileStream(@"dhcp.csv",FileMode.Open);
			StreamReader srDHCP=new StreamReader(fsDHCP,Encoding.Default);
			//rowArray,elem nem kell új
			while (!srDHCP.EndOfStream) {
				rowArray=srDHCP.ReadLine().Split(';');
				/*
				elem.MAC=rowArray[0];
				elem.IP=rowArray[1];
				serverActual.Add(elem.MAC,elem.IP);
				*/
				serverActual.Add(rowArray[0],rowArray[1]);
			}
			srDHCP.Close();
			Console.WriteLine("Actual IP's (szerver jelenlegi állapota, bérelt címek):");
			foreach (var i in serverActual) {
				Console.WriteLine("{0} {1}",i.Key,i.Value);
			}
			//***********	test.csv
			FileStream fsTest=new FileStream(@"test.csv",FileMode.Open);
			StreamReader srTest=new StreamReader(fsTest,Encoding.Default);
			//rowArray
			TestCommand oneCommand=new TestCommand();
			while (!srTest.EndOfStream) {
				rowArray=srTest.ReadLine().Split(' ');
				oneCommand.testCommand=rowArray[0];
				oneCommand.testParam=rowArray[1];
				test.Add(oneCommand);
			}
			srTest.Close();
			Console.WriteLine("Végrehajtandó parancsok:");
			foreach (var i in test) {
				Console.WriteLine("{0} {1}",i.testCommand,i.testParam);
			}
		}
		
		public static bool Excluded(string IP){
			bool excludedIP=false;
			foreach (var i in excluded) {
				if (i==IP) {
					excludedIP=true;
				}
			}
			return excludedIP;
		}
		
		public static void Request(string MAC){
			MAC_IP_Pairs newItem=new MAC_IP_Pairs();
			if (serverActual.ContainsKey(MAC)) {
				//MAC címnek már van érvényes foglalása?
				//Stop
			} else {
				//MAC cím szerepel a fenntartások között?
				if (reserved.ContainsKey(MAC)) {
					//IP cím=foglalások listában a MAC-hez tartozó cím
					newItem.IP=reserved[MAC];
					//newItem.MAC=MAC;
					if (serverActual.ContainsValue(newItem.IP)) {
						//IP cím ki van már osztva? (szerepel a bérelt címek között)
					} else {
						serverActual.Add(MAC,newItem.IP);
					}
				} else {
					int lastByte=100;
					bool success=false;
					newItem.IP="192.168.10."+Convert.ToString(lastByte);
					do{
						if (serverActual.ContainsValue(newItem.IP) && Excluded(newItem.IP) && reserved.ContainsValue(newItem.IP)) {
							lastByte++;
							newItem.IP="192.168.10."+Convert.ToString(lastByte);
							if (lastByte>199) {
								Console.WriteLine("sikertelen IP cím kiosztás");
							}
						} else {
							success=true;
							serverActual.Add(MAC,newItem.IP);
						}
					} while (lastByte>199 && !success);
					if (!success) {
						//saját kivétel
					}
				}
			}
		}
		
		public static void Release(string IP){
			string dictKeyToRemove="";
			foreach (var i in serverActual) {
				if (i.Value==IP) {
					dictKeyToRemove=i.Key;
				}
			}
			serverActual.Remove(dictKeyToRemove);
		}
	}
}