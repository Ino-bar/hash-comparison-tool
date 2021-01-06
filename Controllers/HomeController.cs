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
        public void GetFileName(IFormFile file)
        {
            var filename = file;
            Console.WriteLine(filename);
        }
        [HttpPost]
        public ActionResult Index(IFormFile file)
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
                var dataTable = ConvertDataTableToHTML(dt);
                var jsonDataTable = ConvertDataTabletoJSON(dt);
                //return Content(MyTable.ToString(), "text/html"); //returns the HTML table
                return Content(jsonDataTable, "text/json");
                //return Content(dataTable, "text/html");
            }
        }
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
            return JsonSerializer.Serialize(rows);
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
