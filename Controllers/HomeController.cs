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
using System.Net;
using System.Net.Mime;

namespace hash_comparison_tool.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        StudentViewModel StudentViewModel = new StudentViewModel
        {
            Students = new List<student_data>()
        };
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        /*
 * This is for getting data saved as json out of session
 * var something = HttpContext.Session.GetString("studentData");
    var somethingelse = JsonConvert.DeserializeObject<List<student_data>>(something);
 * 
 */
        [HttpPost]
        public ActionResult ParseCSV(IFormFile file)
        {
            DataTable dt = new DataTable();
            using (TextFieldParser parser = new TextFieldParser(file.OpenReadStream()))
            {   
                bool firstLine = true;
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while(!parser.EndOfData)
                {
                    string[] Row = parser.ReadFields();
                    if (firstLine) 
                    { 
                            foreach (var Field in Row)
                            {
                                dt.Columns.Add(Field);
                            }
                            firstLine = false;
                            continue;
                    }
                    dt.Rows.Add(Row);
                }
                var studentObjects = dataTableToStudentObjects(dt);
                if (String.IsNullOrEmpty(studentObjects))
                {
                    return Json(new { success = false, responseText = "The attached file does not contain sufficient data. Please upload a new file." });
                }
                else 
                { 
                    HttpContext.Session.SetString("studentData", dataTableToStudentObjects(dt));
                    return Json(new { success = true, responseText = "The data was processed successfully." });
                }
            }
        }

        public string dataTableToStudentObjects(DataTable table)
        {
            Students students = new Students();
            int i = 0;
            if(table.Rows[0].ItemArray.Length - 4 <= 0)
            {
                return "";
            }
            else
            { 
                foreach (DataRow row in table.Rows)
                {
                    student_data instance = new student_data();
                    students.StudentList.Add(instance);
                    object[] entries = row.ItemArray;
                    //students.StudentList[i].Username = entries.ElementAt(0).ToString();
                    //students.StudentList[i].LastName = entries.ElementAt(1).ToString();
                    //students.StudentList[i].FirstName = entries.ElementAt(2).ToString();
                    //students.StudentList[i].CID = entries.ElementAt(entries.Length - 4).ToString();
                    students.StudentList[i].Username = row["Username"].ToString();
                    students.StudentList[i].LastName = row["Last Name"].ToString();
                    students.StudentList[i].FirstName = row["First Name"].ToString();
                   // students.StudentList[i].CID = entries.ElementAt(entries.Length - 4).ToString();

                    var AnswerColumns = table.Columns.Cast<DataColumn>()
                                         .Select(x => x.ColumnName).Where(n => n.Contains("Answer")).ToArray();
                    foreach(string colname in AnswerColumns)
                    {
                        var answer = row[colname].ToString();
                        if ((answer.Length == 64 && (answer.IndexOf(' ') <= 0)) || string.IsNullOrEmpty(answer))
                        {
                            int result = int.Parse(colname.Substring(colname.IndexOf(" ")));

                            QuestionSubmissions questionSubmission = new QuestionSubmissions();
                            questionSubmission.QuestionNumber = "Question " + result.ToString();
                            questionSubmission.Hash = answer;
                            students.StudentList[i].SubmissionIDs.Add(questionSubmission);
                            //Debug.WriteLine(row[colname].ToString());
                        }
                        else if((answer.Length == 7 || answer.Length == 8) && (answer.IndexOf(' ') <= 0))
                        {
                            students.StudentList[i].CID = answer;
                        }
                    }
                    /*
                    for (int j = 5; j < entries.Length - 4; j += 6)
                    {
                        QuestionSubmissions questionSubmission = new QuestionSubmissions();
                        questionSubmission.QuestionNumber = entries.ElementAt(j - 2).ToString();
                        questionSubmission.Hash = entries.ElementAt(j).ToString();
                        students.StudentList[i].SubmissionIDs.Add(questionSubmission);
                    }
                    */
                    i += 1;
                }
                string serialisedData = JsonConvert.SerializeObject(students.StudentList);
                return serialisedData;
            }
        }

        [HttpPost]
        public void GetHashesPerQuestion()
        {
            var generatedHashes = Request.Form["generatedHashes"];
            HttpContext.Session.SetString("submittedfilehashes", generatedHashes);
        }

        public ActionResult RunComparison()
        {
            var jsonStudentData = HttpContext.Session.GetString("studentData");
            var StudentData = JsonConvert.DeserializeObject<List<student_data>>(jsonStudentData);
            Dictionary<string, List<string>> AllQuestionHashes = HashesToObjects(HttpContext.Session.GetString("submittedfilehashes"));

            StartHashComparisonTasks(StudentData, AllQuestionHashes);
            return View("Index", StudentViewModel);
        }

        private Dictionary<string,List<string>> HashesToObjects(Microsoft.Extensions.Primitives.StringValues jsonhashes)
        {
            Dictionary<string, List<string>> generatedHashesAsDictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonhashes);
            GeneratedHashes uploadedHashesPerQuestion = new GeneratedHashes();
            foreach (var entry in generatedHashesAsDictionary)
            {
                HashesPerQuestion item = new HashesPerQuestion();
                item.QuestionNumber = entry.Key;
                item.Hashes = entry.Value;
                uploadedHashesPerQuestion.GeneratedHashesList.Add(item);
            }
            return generatedHashesAsDictionary;
        }
        [HttpPost]
        public void StartHashComparisonTasks(List<student_data> StudentData, Dictionary<string, List<string>> AllQuestionHashes)
        {
            Task[] taskArray = new Task[AllQuestionHashes.Count];
            for(int i = 0; i < taskArray.Length; i++)
            {
                Debug.Assert(i < taskArray.Length);
                Dictionary<string, List<string>> questionHashes = new Dictionary<string, List<string>>(); 
                questionHashes.Add(AllQuestionHashes.ElementAt(i).Key, AllQuestionHashes.ElementAt(i).Value);
                taskArray[i] = Task.Factory.StartNew(() => CompareHashes(StudentData, questionHashes));
            }
            Task.WaitAll(taskArray);
            ModelState.Clear();
            StudentViewModel.Students = StudentData;
        }

        public void CompareHashes(List<student_data> sd, Dictionary<string, List<string>> qh)
        {
            List<string> hashList = qh.ElementAt(0).Value;
            List<QuestionSubmissions> somethinglist = new List<QuestionSubmissions>();
            List<List<QuestionSubmissions>> returnlist = new List<List<QuestionSubmissions>>();
            foreach (var hash in hashList)
            {
                var something = sd.SelectMany(s1 => s1.SubmissionIDs)
                    .Where(s2 => s2.Hash == hash);
                somethinglist = something.ToList();
                foreach(var result in somethinglist)
                {
                    result.QuestionMatch.Add("Match on " + qh.ElementAt(0).Key);
                }
                if (somethinglist.Count > 0) returnlist.Add(somethinglist);
            }
        }

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
            return View(StudentViewModel);
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
