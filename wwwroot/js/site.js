// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const newCount = {
    get current() {
        return this._current;
    },
    set current(value) {
        this._current = value;
        if (value === globalFileVariable.length) {
            createNewQuestionObject()
            console.log("ready to party")
        }
    }
}
var globalFileVariable;
var i = 0;
var column = 0;
var Questions = []
var QuestionsMap = new Map()
var hashData = [];
class QuestionHashes {
    constructor(questionNumber, hashes) {
        this.questionNumber = questionNumber
        this.hashes = hashes;
    }
}
// Set up handlers on load.
window.addEventListener("load", function () {

    // Compute the hash when a selection is made with the file input
    var fileinput = document.getElementById('fileinput');
    fileinput.addEventListener('change', handleFileInputChange);

    // Compute the hash when a file is dropped into the drop area
    var dropArea = document.getElementById('drop-area');
    dropArea.addEventListener('dragover', handleDropAreaDragover);
    dropArea.addEventListener('drop', handleDropAreaDrop);

    document.addEventListener('change', changeQuestionText);
    // Open the file input prompt when the drop area is clicked
    //dropArea.addEventListener('click', handleDropAreaClick);								 
    var csvinput = document.getElementById("CSVSelect");
    csvinput.addEventListener('change', handleCSVInputChange)
});

// Event handlers

function handleCSVInputChange() {
    var fileinput = this;
    let file = fileinput.files[0];
    var fname = file.name;
    var re = /(\.csv)$/i;
    if (!re.exec(fname)) {
        fileinput.value = '';
        alert("File extension not supported.");
    }
    else {
    var csvinput = document.getElementById("CSVSelect");
    var CSVSubmit = document.getElementById("CSVUpload");
        CSVSubmit.disabled = csvinput.value == "";
    }
}

function handleFileInputChange() {
    // Hash a local file when selected via file input.
    //clearResult();
    var fileinput = this;
    let file = fileinput.files;
    globalFileVariable = file;
    hashFile(globalFileVariable[0])
        .then(showHash)
        .catch(showHashError);
}

function handleDropAreaDragover(evt) {
    // Set up the drop area.
    evt.preventDefault();
    evt.dataTransfer.dropEffect = 'copy';
}

function handleDropAreaDrop(evt) {
    // Hash the file that is dropped into the drop area.
    evt.preventDefault();
    var dropArea = document.getElementById('drop-area');
    var hashsubmit = document.getElementById('hashsubmit');
    hashsubmit.disabled = dropArea.value == "";
    hashData = [];

    //clearResult();
    let file = evt.dataTransfer.files;
    globalFileVariable = file;
    newCount.current = 0;
    for (i = 0; i < globalFileVariable.length; i++) {
        hashFile(globalFileVariable[i])
            .then(showHash)
        //.catch(showHashError);
    }
}

function RejectFile(message) {
    var csvinput = document.getElementById("CSVSelect");
    csvinput.value = "";
    var CSVSubmit = document.getElementById("CSVUpload");
    CSVSubmit.disabled = csvinput.value == "";
    window.alert(message);
}

function createNewQuestionObject() {
    var newQuestionHashes = new QuestionHashes();
    newQuestionHashes.questionNumber = "Question " + (column + 1).toString();
    newQuestionHashes.hashes = hashData;
    Questions.push(newQuestionHashes);
    QuestionsMap[newQuestionHashes.questionNumber.toString()] = newQuestionHashes.hashes
    mapToHTMLTable(mapQuestions())
    column += 1;
}

function handleDropAreaClick(evt) {
    // Show the file select dialog.
    evt.preventDefault();
    var doc = document.getElementById('fileinput').ownerDocument;
    var fileinputClick = doc.createEvent('MouseEvents');
    fileinputClick.initEvent('click', true, true);
    fileinput.dispatchEvent(fileinputClick, true);
}

// Utilities

function bufferToHex(buffer) {
    // Convert a buffer into a hexadecimal string.
    // From https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest
    var hexCodes = [];
    var view = new DataView(buffer);

    for (var i = 0; i < view.byteLength; i += 4) {
        // Using getUint32 reduces the number of iterations needed (we process
        // 4 bytes each time).
        var value = view.getUint32(i);
        // toString(16) will give the hex representation of the number without
        // padding
        var stringValue = value.toString(16);
        // We use concatenation and slice for padding.
        var padding = '00000000';
        var paddedValue = (padding + stringValue).slice(-padding.length);
        hexCodes.push(paddedValue);
    }

    return hexCodes.join("");
}

function bufferToBase64(buffer) {
    // Convert a buffer into a base64 string.
    var str = '';
    var bytes = new Uint8Array(buffer);
    var len = bytes.byteLength;

    for (var i = 0; i < len; i++) {
        str += String.fromCharCode(bytes[i]);
    }

    return window.btoa(str);
}

function hashFile(file) {
    // Hash a File object.
    // Returns a Promise of a successful hash.
    return new Promise(function (resolve, reject) {
        var fileReader = new FileReader();
        fileReader.addEventListener('load', function () {

            crypto.subtle.digest("SHA-256", this.result)
                .then(resolve)
                .catch(reject);
        });
        fileReader.readAsArrayBuffer(file);
    });
}

// DOM mutation

function showHash(hash) {
    // Display the hash as a hex string.
    var filePath = document.getElementById('fileinput');
    var sha256 = bufferToHex(hash);
    hashData.push(sha256);
    newCount.current = newCount.current + 1;
    //var filename = globalFileVariable[i].name;
    //document.getElementById('result').innerHTML =
    //'<h2>SHA-256 Hash</h2><input type="text" id="hashValue" value="" style="width:470px;" readonly><button onclick="copyHash()">Copy hash</button>';
    //document.getElementById('hashValue').value = sha256;
}

function copyHash() {
    var copyText = document.getElementById("hashValue");

    copyText.select();
    copyText.setSelectionRange(0, 99999); /*For mobile devices*/

    /* Copy the text inside the text field */
    document.execCommand("copy");
}

function clearResult() {
    //document.getElementById('result').innerHTML = '';
    document.getElementById('hashTable').getElementsByTagName('tbody')[0].innerHTML = '';
    column = 0;
}

//function showHashError(err) {
// Display a generic hash error.
// document.getElementById('result').innerHTML ='<h2>' + gettext('error-header') + '</h2>' + '<p>' + gettext('error-hash');
//}

/* 13-01-21 no longer writing hashes to table
function writeHashToTable(hash)
{
var tbodyRef = document.getElementById('hashTable').getElementsByTagName('tbody')[0];
var newRow = tbodyRef.insertRow();
var newCell = newRow.insertCell();
var newText = document.createTextNode(hash);
newCell.appendChild(newText);
}
*/

// 13-01-21 current display of hashes is not in table form so this function does not apply
function download_table_as_csv(table_id, separator = ',') {
// Select rows from table_id
var rows = document.querySelectorAll('table#' + table_id + ' tr');
// Construct csv
var csv = [];
for (var i = 0; i < rows.length; i++) {
var row = [], cols = rows[i].querySelectorAll('td, th');
for (var j = 0; j < cols.length; j++) {
    // Clean innertext to remove multiple spaces and jumpline (break csv)
    var data = cols[j].innerText.replace(/(\r\n|\n|\r)/gm, '').replace(/(\s\s)/gm, ' ')
    // Escape double-quote with double-double-quote (see https://stackoverflow.com/questions/17808511/properly-escape-a-double-quote-in-csv)
    data = data.replace(/"/g, '""');
    // Push escaped string
    row.push('"' + data + '"');
}
csv.push(row.join(separator));
}
var csv_string = csv.join('\n');
// Download it
var filename = 'export_' + table_id + '_' + new Date().toLocaleDateString() + '.csv';
var link = document.createElement('a');
link.style.display = 'none';
link.setAttribute('target', '_blank');
link.setAttribute('href', 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv_string));
link.setAttribute('download', filename);
document.body.appendChild(link);
link.click();
document.body.removeChild(link);
}
//

function mapQuestions() {
    var questionsMap = Questions.map(question => ({ key: question.questionNumber, value: question.hashes }))
    var jsonArray = JSON.parse(JSON.stringify(Questions))
    var jsonArrayString = JSON.stringify(QuestionsMap)
    console.log(JSON.stringify(jsonArray))
    console.log(JSON.stringify(Questions))
    //var dafg = { ...Questions }
    //console.log(dafg)
    //var gsfh = Object.assign({}, Questions)
    //console.log(gsfh)
    //var fdsg = mapToObj(QuestionsMap)
    //var afg = JSON.stringify(fdsg)
    //sessionStorage.setItem("hashes", afg)
    //let sessionData = sessionStorage.getItem("hashes")
    //postJSON(jsonArrayString)
    //hashesToCookie(jsonArrayString)
    setMyValue(jsonArrayString)
    return questionsMap
};

function setMyValue(hashes) {
    document.getElementById("fooField").value = hashes;
}

function mapToObj(inputMap) {
    let obj = {};
    inputMap.forEach(function (value, key) {
        obj[key] = value
    });

    return obj;
}

function postJSON(hashes) {
    $.ajax({
        url: appURL.siteURL,
        type: "POST",
        data: JSON.stringify(Questions),
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            alert(response);
        },
        error: function (response) {
            alert(response.responseText);
        },
    });
}

function mapToHTMLTable(map) {
    var html = '<div class="row">';
    for (var i = 0; i < map.length; i++) {
        html += '<div class="column">';
        html += '<input type ="text" id="' + i + '" name="questionNumber" value="' + map[i].key + '">';
        for (var k = 0; k < map[i].value.length; k++) {
            html += '<p>' + map[i].value[k] + '</p>';
        }
        html += '</div>';
    }
    html += '</div>';
    document.getElementById('container').innerHTML = html;
};

function changeQuestionText(event) {
    var questionNumber = event.target.id;
    var newValue = event.target.value;
    Questions[questionNumber].questionNumber = newValue;
    mapQuestions();
}