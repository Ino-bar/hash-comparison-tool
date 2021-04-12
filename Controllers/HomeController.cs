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
            DataTable dataTable = new DataTable();
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
                                dataTable.Columns.Add(Field);
                            }
                            firstLine = false;
                            continue;
                    }
                    dataTable.Rows.Add(Row);
                }
                var studentObjects = dataTableToStudentObjects(dataTable);
                if (String.IsNullOrEmpty(studentObjects))
                {
                    return Json(new { success = false, responseText = "The attached file does not contain sufficient data. Please upload a new file." });
                }
                else 
                { 
                    HttpContext.Session.SetString("studentData", dataTableToStudentObjects(dataTable));
                    return Json(new { success = true, responseText = "The data was processed successfully." });
                }
            }
        }

        public string dataTableToStudentObjects(DataTable table)
        {
            int i = 0;
            int j = 0;
            Students students = new Students();

            foreach(DataColumn column in table.Columns)
            {
                RenameQuestionAndCIDColumns(table, column, j);
                j++;
            }
            if (table.Rows[0].ItemArray.Length - 4 <= 0)
            {
                return "";
            }
            else
            { 
                foreach (DataRow row in table.Rows)
                {
                    Array questionColumns = table.Columns.Cast<DataColumn>()
                        .Select(x => x.ColumnName).Where(n => n.Contains("Question") && n.Contains("hash")).ToArray();
                    string CIDColumn = table.Columns.Cast<DataColumn>()
                        .Select(x => x.ColumnName).First(n => n.Contains("CID"));
                    
                    student_data instance = new student_data();
                    students.StudentList.Add(instance);
                    students.StudentList[i].Username = row["Username"].ToString();
                    students.StudentList[i].LastName = row["Last Name"].ToString();
                    students.StudentList[i].FirstName = row["First Name"].ToString();
                    students.StudentList[i].CID = row[row.Table.Columns[CIDColumn].Ordinal + 1].ToString();

                    SetStudentQuestionSubmissions(questionColumns, row, students, i);
                    i += 1;
                }
                string serialisedData = JsonConvert.SerializeObject(students.StudentList);
                return serialisedData;
            }
        }

        public void RenameQuestionAndCIDColumns(DataTable table, DataColumn column, int j)
        {
            List<string> tableAsList = table.AsEnumerable().Select(x => x[j].ToString()).ToList();
            bool columnIsHashQuestion = tableAsList.Any(s1 => s1.Contains("hash") && s1.Contains("Question"));
            bool columnIsCIDQuestion = tableAsList.Any(s1 => s1.Contains("CID"));
            if (columnIsHashQuestion)
            {
                column.ColumnName = tableAsList[0];
            }
            else if (columnIsCIDQuestion)
            {
                column.ColumnName = tableAsList[0];
            }
        }
        public void SetStudentQuestionSubmissions(Array questionColumns, DataRow row, Students students, int i)
        {
            foreach (string colname in questionColumns)
            {
                var qNumber = string.Join("", colname.ToCharArray().Where(Char.IsDigit));
                if (!string.IsNullOrEmpty(qNumber))
                {
                    var nextCol = row[row.Table.Columns[colname].Ordinal + 1];
                    QuestionSubmissions questionSubmission = new QuestionSubmissions();
                    questionSubmission.QuestionNumber = "Question " + qNumber;
                    questionSubmission.Hash = nextCol.ToString();
                    students.StudentList[i].studentSubmittedHashesPerQuestion.Add(questionSubmission);
                }
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
                Dictionary<string, List<string>> questionHashes = new Dictionary<string, List<string>>(); 
                questionHashes.Add(AllQuestionHashes.ElementAt(i).Key, AllQuestionHashes.ElementAt(i).Value);
                taskArray[i] = Task.Factory.StartNew(() => CompareHashes(StudentData, questionHashes));
            }
            Task.WaitAll(taskArray);
            ModelState.Clear();
            StudentViewModel.Students = StudentData;
        }

        public void CompareHashes(List<student_data> studentData, Dictionary<string, List<string>> questionHashes)
        {
            List<string> hashList = questionHashes.ElementAt(0).Value;
            List<QuestionSubmissions> questionSubmissionsList = new List<QuestionSubmissions>();
            List<List<QuestionSubmissions>> returnlist = new List<List<QuestionSubmissions>>();
            foreach (var hash in hashList)
            {
                var HashMatchQuery = studentData.SelectMany(s1 => s1.studentSubmittedHashesPerQuestion)
                    .Where(s2 => s2.Hash == hash);
                questionSubmissionsList = HashMatchQuery.ToList();
                foreach(var result in questionSubmissionsList)
                {
                    result.QuestionMatch.Add("Match on " + questionHashes.ElementAt(0).Key);
                }
                if (questionSubmissionsList.Count > 0) returnlist.Add(questionSubmissionsList);
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
