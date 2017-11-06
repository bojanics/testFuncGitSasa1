#r "Altsoft.XML2PDF.Formatting.Engine.dll"
#r "Newtonsoft.Json"

using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using Altsoft.Publisher;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, ExecutionContext context)
{
   log.Info("AltoSoft function isprocessing a request.");
   log.Info("PATH is "+System.Text.Encoding.UTF8.GetBytes(System.Environment.GetEnvironmentVariable("PATH")));

   APEngine engine = new APEngine(true);
   
   dynamic body = req.Content.ReadAsStringAsync().Result;
   dynamic json = JsonConvert.DeserializeObject(body);
   string xml = json.xml;
   xml = WebUtility.HtmlDecode(xml);

   // Removing possible BOM chars
   int index = xml.IndexOf('<');
   if (index > 0)
   {
       xml = xml.Substring(index, xml.Length - index);
   }
   string xsl = json.xsl;
   //xsl = WebUtility.HtmlDecode(xsl);

   MemoryStream sourceStream = new MemoryStream();
   StreamWriter writer = new StreamWriter(sourceStream);
   writer.Write(xml);
   writer.Flush();
   sourceStream.Position = 0;
   APSourceFormat sourceType = APSourceFormat.Unknown;

   if (!engine.IsLoaded)
   {
       engine.Load();
   }
   //APSource src = engine.CreateSource(source, sourceType);
   APSource src = engine.CreateSource(sourceStream, "", sourceType, false);
   APConfig config = engine.CreateConfig();
   APDestination dest = engine.CreateDestination(config);
   //string sourceXsl = "http://sasa-formio-pdf.azurewebsites.net/xsl/sleasing.xsl";
   //string sourceXsl = "http://sasaboy-asus:8080/xsl/sleasing.xsl";

   APLog aplog = new APLog();

   if (!string.IsNullOrEmpty(xsl))
       src.ApplyXSLT(xsl, aplog);
   dest.Append(src, aplog, 0.7);

   MemoryStream os = new MemoryStream();
   dest.Save(APDestinationFormat.PDF, os, aplog, 0.3);

   byte[] byteArray = os.ToArray();

   var result = req.CreateResponse();
   result.StatusCode = HttpStatusCode.OK;
   result.Content = new ByteArrayContent(byteArray);
   result.Content.Headers.Add("Content-Type", "application/pdf");

   return result;
}
