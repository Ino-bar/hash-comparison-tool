using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace hash_comparison_tool.Models
{
    public class Students
    {
        public List<student_data> StudentList = new List<student_data>();
    }
    public class student_data
    {
        public string Username { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string CID { get; set; }
        public List<QuestionSubmissions> SubmissionIDs = new List<QuestionSubmissions>();
    }
    public class QuestionSubmissions
    {
        public string QuestionNumber { get; set; }
        public string Hash { get; set; }
        public List<string> QuestionMatch = new List<string>();
    }
}



