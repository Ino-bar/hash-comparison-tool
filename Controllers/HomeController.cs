using hash_comparison_tool.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using System.Text.Json;
using System.Data;
using Newtonsoft.Json;

namespace hash_comparison_tool.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        [HttpPost]
        public void ParseCSV(IFormFile file)
        {
            //StringBuilder MyTable = new StringBuilder(); //This is for creating HTML table from CSV data directly
            DataTable dt = new DataTable();
            using (TextFieldParser parser = new TextFieldParser(file.OpenReadStream()))
            {   
                bool firstLine = true;
                //parser.HasFieldsEnclosedInQuotes = true;
                
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                //MyTable.AppendLine("<table>"); //start of HTML table
                while(!parser.EndOfData)
                {
                    string[] Row = parser.ReadFields();
                    //MyTable.AppendLine("<tr>");
                    if (firstLine) 
                    { 
                            foreach (var Field in Row)
                            {
                                //MyTable.AppendLine("<td>" + Field + "</td>");
                                dt.Columns.Add(Field);
                            }
                            firstLine = false;
                            continue;
                    }
                    dt.Rows.Add(Row);
                    //MyTable.AppendLine("</tr>");
                }

                //MyTable.AppendLine("</table>");
                //var dataTable = ConvertDataTableToHTML(dt);
                //var jsonDataTable = ConvertDataTabletoJSON(dt); can delete method
                HttpContext.Session.SetString("studentData", dataTableToStudentObjects(dt));
                //return Content(MyTable.ToString(), "text/html"); //returns the HTML table
                //return Content(jsonDataTable, "text/json");
                //return Content(dataTable, "text/html");
                //return View(dataTable.ToList());
            }
        }
        public string dataTableToStudentObjects(DataTable table)
        {
            Students students = new Students();
            int i = 0;
            foreach (DataRow row in table.Rows)
            {
                student_data instance = new student_data();
                students.StudentList.Add(instance);
                object[] entries = row.ItemArray;
                students.StudentList[i].Username = entries.ElementAt(0).ToString();
                students.StudentList[i].LastName = entries.ElementAt(1).ToString();
                students.StudentList[i].FirstName = entries.ElementAt(2).ToString();
                students.StudentList[i].CID = entries.ElementAt(entries.Length - 4).ToString();

                for (int j = 5; j < entries.Length - 3; j += 6)
                {
                    QuestionSubmissions questionSubmission = new QuestionSubmissions();
                    questionSubmission.QuestionNumber = entries.ElementAt(j - 2).ToString();
                    questionSubmission.PaperID = entries.ElementAt(j).ToString();
                    students.StudentList[i].SubmissionIDs.Add(questionSubmission);
                }
                i += 1;
            }
            string serialisedData = JsonConvert.SerializeObject(students.StudentList);
            return serialisedData;
        }

        /*
         * This is for getting data saved as json out of session
         * var something = HttpContext.Session.GetString("studentData");
            var somethingelse = JsonConvert.DeserializeObject<List<student_data>>(something);
         * 
         */

        [HttpPost]
        public void GetHashesPerQuestion(string hashinfo)
        {
            var temp = hashinfo;
            compareHashes();
            //var generatedHashes = Request.Form["generatedHashes"];
            //HttpContext.Session.SetString("submittedfilehashes", generatedHashes);
            //Dictionary<string, List<string>> QuestionHashes = HashesToObjects(HttpContext.Session.GetString("submittedfilehashes"));
            //compareHashes(QuestionHashes);
        }

        private Dictionary<string,List<string>> HashesToObjects(Microsoft.Extensions.Primitives.StringValues jsonhashes)
        {
            Dictionary<string, List<string>> generatedHashesAsDictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonhashes);
            GeneratedHashes uploadedHashesPerQuestion = new GeneratedHashes();
            foreach (var entry in generatedHashesAsDictionary)
            {
                HashesPerQuestion item = new HashesPerQuestion();
                item.QuestionNumber = entry.Key;
                item.PaperIDs = entry.Value;
                uploadedHashesPerQuestion.GeneratedHashesList.Add(item);
            }
            return generatedHashesAsDictionary;
        }

        public void compareHashes()
        {
            var jsonStudentData = HttpContext.Session.GetString("studentData");
            var StudentData = JsonConvert.DeserializeObject<List<student_data>>(jsonStudentData);

            var generatedHashes = Request.Form["generatedHashes"];
            HttpContext.Session.SetString("submittedfilehashes", generatedHashes);
            Dictionary<string, List<string>> AllQuestionHashes = HashesToObjects(HttpContext.Session.GetString("submittedfilehashes"));

            Task[] taskArray = new Task[AllQuestionHashes.Count];
            for(int i = 0; i < taskArray.Length; i++)
            {
                Debug.Assert(i < taskArray.Length);
                Dictionary<string, List<string>> questionHashes = new Dictionary<string, List<string>>(); 
                questionHashes.Add(AllQuestionHashes.ElementAt(i).Key, AllQuestionHashes.ElementAt(i).Value);
                taskArray[i] = Task.Factory.StartNew(() => actuallyCompareHashes(StudentData, questionHashes));
            }
            Task.WaitAll(taskArray);
        }
        public List<student_data> actuallyCompareHashes(List<student_data> sd, Dictionary<string, List<string>> qh)
        {
            List<string> hashList = qh.ElementAt(0).Value;
            List<QuestionSubmissions> somethinglist = new List<QuestionSubmissions>();
            List<List<QuestionSubmissions>> returnlist = new List<List<QuestionSubmissions>>();
            foreach (var hash in hashList)
            {
                var something = sd.SelectMany(s1 => s1.SubmissionIDs)
                    .Where(s2 => s2.PaperID == hash);
                somethinglist = something.ToList();
                foreach(var result in somethinglist)
                {
                    result.QuestionMatch.Add("Match on " + qh.ElementAt(0).Key);
                }
                if (somethinglist.Count > 0) returnlist.Add(somethinglist);
            }
            return sd;
            //Debug.WriteLine("something");
        }
        /*
public string ConvertDataTabletoJSON(DataTable dt)
{
   List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
   Dictionary<string, object> row;
   foreach (DataRow dr in dt.Rows)
   {
       row = new Dictionary<string, object>();
       foreach (DataColumn col in dt.Columns)
       {
           row.Add(col.ColumnName, dr[col]);
       }
       rows.Add(row);
   }
   return System.Text.Json.JsonSerializer.Serialize(rows);
}
*/
        public static string ConvertDataTableToHTML(DataTable dt)
        {
            string html = "<table>";
            //add header row
            html += "<tr>";
            for (int i = 0; i < dt.Columns.Count; i++)
                html += "<td>" + dt.Columns[i].ColumnName + "</td>";
            html += "</tr>";
            //add rows
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                html += "<tr>";
                for (int j = 0; j < dt.Columns.Count; j++)
                    html += "<td>" + dt.Rows[i][j].ToString() + "</td>";
                html += "</tr>";
            }
            html += "</table>";
            return html;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
